using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Time;

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
