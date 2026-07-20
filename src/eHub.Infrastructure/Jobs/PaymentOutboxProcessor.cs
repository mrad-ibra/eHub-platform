using System.Text.Json;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments.Events;
using eHub.Persistence;
using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eHub.Infrastructure.Jobs;

/// <summary>
/// Dispatches PaymentSucceeded outbox rows to Booking.Confirm (L9). Never called from Payment handlers.
/// </summary>
public sealed class PaymentOutboxConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentOutboxConsumerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<PaymentOutboxProcessor>();
                await processor.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment outbox consumer tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public sealed class PaymentOutboxProcessor(
    EHubDbContext db,
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<PaymentOutboxProcessor> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.Type == nameof(PaymentSucceeded))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var message in batch)
        {
            try
            {
                var evt = JsonSerializer.Deserialize<PaymentSucceeded>(message.PayloadJson, JsonOptions);
                if (evt is null)
                {
                    message.ProcessedAtUtc = now;
                    message.AttemptCount++;
                    continue;
                }

                var booking = await bookings.GetByIdAsync(evt.BookingId, cancellationToken);
                if (booking is null)
                {
                    message.ProcessedAtUtc = now;
                    message.AttemptCount++;
                    continue;
                }

                try
                {
                    booking.ClearDomainEvents();
                    booking.Confirm(evt.PaymentId, now);
                    foreach (var domainEvent in booking.DomainEvents)
                    {
                        db.OutboxMessages.Add(new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            Type = domainEvent.GetType().Name,
                            PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
                            OccurredAtUtc = now,
                            CreatedAtUtc = now
                        });
                    }

                    booking.ClearDomainEvents();
                }
                catch (ConflictException ex)
                {
                    // Late success / hold expired: do not confirm; mark outbox processed (L4).
                    logger.LogWarning(
                        ex,
                        "PaymentSucceeded did not confirm booking {BookingId} (late or illegal)",
                        evt.BookingId);
                }

                message.ProcessedAtUtc = now;
                message.AttemptCount++;
                processed++;
            }
            catch (Exception ex)
            {
                message.AttemptCount++;
                logger.LogError(ex, "Failed processing outbox {OutboxId}", message.Id);
            }
        }

        if (batch.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }
}
