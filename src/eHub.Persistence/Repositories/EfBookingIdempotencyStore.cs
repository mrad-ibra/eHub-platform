using eHub.Application.Bookings.Abstractions;
using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eHub.Persistence.Repositories;

/// <summary>
/// Idempotency lease is claimed outside the booking unit-of-work (Begin flushes immediately).
/// On crash, the Started row remains until <c>ExpiresAtUtc</c> (processing TTL, default 5 minutes).
/// Completed status is written in the same SaveChanges as the Booking insert.
/// Expired lease takeover uses a conditional UPDATE so only one concurrent reclaimer wins.
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
                return Began(created);
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

        return await ClassifyOrReclaimAsync(existing, userId, idempotencyKey, requestHash, nowUtc, ttl, cancellationToken);
    }

    private async Task<IdempotencyBeginResult> ClassifyOrReclaimAsync(
        BookingIdempotencyEntry existing,
        Guid userId,
        string idempotencyKey,
        string requestHash,
        DateTime nowUtc,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
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
            var newExpires = nowUtc.Add(ttl);
            var rows = await db.BookingIdempotencyEntries
                .Where(e =>
                    e.RenterId == userId
                    && e.IdempotencyKey == idempotencyKey
                    && e.Status == BookingIdempotencyStatus.Started
                    && e.ExpiresAtUtc <= nowUtc
                    && e.RequestHash == requestHash)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(e => e.RequestHash, requestHash)
                        .SetProperty(e => e.Status, BookingIdempotencyStatus.Started)
                        .SetProperty(e => e.BookingId, (Guid?)null)
                        .SetProperty(e => e.CreatedAtUtc, nowUtc)
                        .SetProperty(e => e.ExpiresAtUtc, newExpires),
                    cancellationToken);

            if (rows == 1)
            {
                db.Entry(existing).State = EntityState.Detached;
                var claimed = await db.BookingIdempotencyEntries
                    .FirstAsync(e => e.RenterId == userId && e.IdempotencyKey == idempotencyKey, cancellationToken);
                return Began(claimed);
            }

            // Lost race — re-read winner and classify (InProgress / Completed / mismatch).
            existing = await db.BookingIdempotencyEntries
                .AsNoTracking()
                .FirstAsync(e => e.RenterId == userId && e.IdempotencyKey == idempotencyKey, cancellationToken);

            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return new IdempotencyBeginResult.PayloadMismatch();
            }

            if (existing.Status == BookingIdempotencyStatus.Completed && existing.BookingId is { } completedId)
            {
                return new IdempotencyBeginResult.CompletedReplay(completedId);
            }

            return new IdempotencyBeginResult.InProgress();
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

    private static IdempotencyBeginResult.Began Began(BookingIdempotencyEntry entry)
        => new(new BookingIdempotencyRecord(
            entry.RenterId,
            entry.IdempotencyKey,
            entry.RequestHash,
            entry.Status,
            entry.BookingId,
            entry.CreatedAtUtc,
            entry.ExpiresAtUtc));

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
