using System.Collections.Concurrent;
using eHub.Application.Bookings.Abstractions;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryBookingIdempotencyStore : IBookingIdempotencyStore
{
    private readonly ConcurrentDictionary<string, BookingIdempotencyRecord> _records =
        new(StringComparer.Ordinal);

    private static string Compose(Guid userId, string key) => $"{userId:N}:{key}";

    public Task<IdempotencyBeginResult> BeginAsync(
        Guid userId,
        string idempotencyKey,
        string requestHash,
        DateTime nowUtc,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var storageKey = Compose(userId, idempotencyKey);
        var created = new BookingIdempotencyRecord(
            userId,
            idempotencyKey,
            requestHash,
            BookingIdempotencyStatus.Started,
            null,
            nowUtc,
            nowUtc.Add(ttl));

        if (_records.TryAdd(storageKey, created))
        {
            return Task.FromResult<IdempotencyBeginResult>(new IdempotencyBeginResult.Began(created));
        }

        if (!_records.TryGetValue(storageKey, out var existing))
        {
            // Extremely unlikely race: removed between TryAdd fail and get — retry once.
            if (_records.TryAdd(storageKey, created))
            {
                return Task.FromResult<IdempotencyBeginResult>(new IdempotencyBeginResult.Began(created));
            }

            existing = _records[storageKey];
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Task.FromResult<IdempotencyBeginResult>(new IdempotencyBeginResult.PayloadMismatch());
        }

        if (existing.Status == BookingIdempotencyStatus.Completed && existing.BookingId is { } bookingId)
        {
            return Task.FromResult<IdempotencyBeginResult>(new IdempotencyBeginResult.CompletedReplay(bookingId));
        }

        // Stale Started past expiry → reclaim
        if (existing.ExpiresAtUtc <= nowUtc
            && existing.Status == BookingIdempotencyStatus.Started)
        {
            if (_records.TryUpdate(storageKey, created, existing))
            {
                return Task.FromResult<IdempotencyBeginResult>(new IdempotencyBeginResult.Began(created));
            }
        }

        return Task.FromResult<IdempotencyBeginResult>(new IdempotencyBeginResult.InProgress());
    }

    public Task CompleteAsync(
        Guid userId,
        string idempotencyKey,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var storageKey = Compose(userId, idempotencyKey);
        if (!_records.TryGetValue(storageKey, out var existing))
        {
            throw new InvalidOperationException("Idempotency record was not started.");
        }

        var completed = existing with
        {
            Status = BookingIdempotencyStatus.Completed,
            BookingId = bookingId
        };

        if (!_records.TryUpdate(storageKey, completed, existing))
        {
            throw new InvalidOperationException("Idempotency record was modified concurrently.");
        }

        return Task.CompletedTask;
    }

    public Task AbandonAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var storageKey = Compose(userId, idempotencyKey);
        if (_records.TryGetValue(storageKey, out var existing)
            && existing.Status == BookingIdempotencyStatus.Started)
        {
            _records.TryRemove(storageKey, out _);
        }

        return Task.CompletedTask;
    }
}
