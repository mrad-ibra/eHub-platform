using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Jobs;

/// <summary>
/// Expires Soft/Hard holds whose TTL has elapsed so PostgreSQL EXCLUDE no longer blocks the slot.
/// Multi-instance safe: AggregateVersion concurrency + domain guards skip already-expired rows.
/// </summary>
public sealed class ExpirePendingBookingsProcessor(
    IBookingRepository bookings,
    IOutboxWriter outbox,
    IBookingExpiryNotifier notifier,
    IExpireBookingsMetrics metrics,
    IUnitOfWork unitOfWork,
    IClock clock,
    IOptions<JobsOptions> options,
    ILogger<ExpirePendingBookingsProcessor> logger)
{
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var started = clock.UtcNow;
        var batchSize = Math.Max(1, options.Value.ExpirePendingBookings.BatchSize);
        var now = clock.UtcNow;
        var batch = await bookings.ListExpiredHoldsAsync(now, batchSize, cancellationToken);

        var expired = 0;
        var skipped = 0;

        foreach (var booking in batch)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown: stop taking new rows; persist what this tick already prepared.
                break;
            }

            try
            {
                // EF-loaded aggregates have an empty buffer; clear anyway for in-memory reuse.
                booking.ClearDomainEvents();
                booking.Expire(now);
                foreach (var domainEvent in booking.DomainEvents)
                {
                    await outbox.EnqueueAsync(domainEvent, now, cancellationToken);
                }

                booking.ClearDomainEvents();
                await notifier.NotifyExpiredAsync(booking, cancellationToken);
                expired++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConflictException)
            {
                skipped++;
            }
            catch (ValidationFailedException)
            {
                skipped++;
            }
        }

        if (expired > 0)
        {
            // Prefer committing the prepared slice even when shutting down.
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        var duration = clock.UtcNow - started;
        metrics.RecordBatch(expired, skipped, duration);

        if (expired > 0)
        {
            logger.LogInformation(
                "Expired {ExpiredCount} booking hold(s); skipped {SkippedCount} in {DurationMs}ms",
                expired,
                skipped,
                (int)duration.TotalMilliseconds);
        }

        return expired;
    }
}
