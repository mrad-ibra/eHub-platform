using eHub.Application.Bookings.Abstractions;
using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eHub.Persistence.Repositories;

/// <summary>
/// Idempotency rows are tracked on <see cref="EHubDbContext"/> and committed only via <see cref="EfUnitOfWork"/>.
/// </summary>
public sealed class EfBookingIdempotencyStore(EHubDbContext db) : IBookingIdempotencyStore
{
    public async Task<IdempotencyBeginResult> BeginAsync(
        Guid userId,
        string idempotencyKey,
        string requestHash,
        DateTime nowUtc,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.BookingIdempotencyEntries
            .FirstOrDefaultAsync(
                e => e.RenterId == userId && e.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is null)
        {
            var created = new BookingIdempotencyEntry
            {
                RenterId = userId,
                IdempotencyKey = idempotencyKey,
                RequestHash = requestHash,
                Status = BookingIdempotencyStatus.Started,
                BookingId = null,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.Add(ttl)
            };

            try
            {
                await db.BookingIdempotencyEntries.AddAsync(created, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return new IdempotencyBeginResult.Began(
                    new BookingIdempotencyRecord(
                        created.RenterId,
                        created.IdempotencyKey,
                        created.RequestHash,
                        created.Status,
                        created.BookingId,
                        created.CreatedAtUtc,
                        created.ExpiresAtUtc));
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                db.Entry(created).State = EntityState.Detached;
                existing = await db.BookingIdempotencyEntries
                    .FirstAsync(e => e.RenterId == userId && e.IdempotencyKey == idempotencyKey, cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                PostgresExceptionMapper.ThrowIfMapped(ex);
                throw;
            }
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return new IdempotencyBeginResult.PayloadMismatch();
        }

        if (existing.Status == BookingIdempotencyStatus.Completed && existing.BookingId is { } bookingId)
        {
            return new IdempotencyBeginResult.CompletedReplay(bookingId);
        }

        if (existing.Status == BookingIdempotencyStatus.Started && existing.ExpiresAtUtc <= nowUtc)
        {
            existing.RequestHash = requestHash;
            existing.Status = BookingIdempotencyStatus.Started;
            existing.BookingId = null;
            existing.CreatedAtUtc = nowUtc;
            existing.ExpiresAtUtc = nowUtc.Add(ttl);
            await db.SaveChangesAsync(cancellationToken);
            return new IdempotencyBeginResult.Began(
                new BookingIdempotencyRecord(
                    existing.RenterId,
                    existing.IdempotencyKey,
                    existing.RequestHash,
                    existing.Status,
                    existing.BookingId,
                    existing.CreatedAtUtc,
                    existing.ExpiresAtUtc));
        }

        return new IdempotencyBeginResult.InProgress();
    }

    public async Task CompleteAsync(
        Guid userId,
        string idempotencyKey,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.BookingIdempotencyEntries
            .FirstOrDefaultAsync(
                e => e.RenterId == userId && e.IdempotencyKey == idempotencyKey,
                cancellationToken)
            ?? throw new InvalidOperationException("Idempotency record was not started.");

        existing.Status = BookingIdempotencyStatus.Completed;
        existing.BookingId = bookingId;
        // Intentionally no SaveChanges — committed with Booking insert via IUnitOfWork.
        await Task.CompletedTask;
    }

    public async Task AbandonAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.BookingIdempotencyEntries
            .FirstOrDefaultAsync(
                e => e.RenterId == userId && e.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is null || existing.Status != BookingIdempotencyStatus.Started)
        {
            return;
        }

        db.BookingIdempotencyEntries.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
