using System.Collections.Concurrent;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Time;
using eHub.Domain.Bookings;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryBookingNumberGenerator(IClock clock) : IBookingNumberGenerator
{
    private long _sequence;

    public Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var year = clock.UtcNow.Year;
        var n = Interlocked.Increment(ref _sequence);
        var value = $"BK-{year}-{n:D9}";
        return Task.FromResult(value);
    }
}

public sealed class InMemoryBookingIdempotencyStore : IBookingIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Guid> _keys = new(StringComparer.Ordinal);

    private static string Key(Guid userId, string idempotencyKey)
        => $"{userId:N}:{idempotencyKey}";

    public Task<Guid?> FindBookingIdAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (_keys.TryGetValue(Key(userId, idempotencyKey), out var id))
        {
            return Task.FromResult<Guid?>(id);
        }

        return Task.FromResult<Guid?>(null);
    }

    public Task SaveAsync(
        Guid userId,
        string idempotencyKey,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        _keys[Key(userId, idempotencyKey)] = bookingId;
        return Task.CompletedTask;
    }
}
