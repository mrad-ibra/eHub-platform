using System.Collections.Concurrent;
using eHub.Application.Bookings.Abstractions;
using eHub.Domain.Bookings;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly ConcurrentDictionary<string, Guid> _byNumber =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, object> _assetLocks = new();

    public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var gate = _assetLocks.GetOrAdd(booking.AssetId, _ => new object());
        lock (gate)
        {
            foreach (var existing in _bookings.Values.Where(b =>
                         b.AssetId == booking.AssetId && b.Status.IsBlocking))
            {
                if (BookingAvailability.ConflictsWithBlockingBooking(
                        booking.Period,
                        booking.BufferDays,
                        existing.Period,
                        existing.BufferDays))
                {
                    throw new Domain.Exceptions.ConflictException(
                        Localization.ErrorResources.Get(Localization.ErrorCodes.BookingConflict));
                }
            }

            if (!_bookings.TryAdd(booking.Id, booking))
            {
                throw new InvalidOperationException($"Booking '{booking.Id}' already exists.");
            }

            if (!_byNumber.TryAdd(booking.BookingNumber, booking.Id))
            {
                _bookings.TryRemove(booking.Id, out _);
                throw new InvalidOperationException($"Booking number '{booking.BookingNumber}' already exists.");
            }
        }

        return Task.CompletedTask;
    }

    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        _bookings.TryGetValue(bookingId, out var booking);
        return Task.FromResult(booking);
    }

    public Task<Booking?> GetByNumberAsync(string bookingNumber, CancellationToken cancellationToken = default)
    {
        if (!_byNumber.TryGetValue(bookingNumber, out var id))
        {
            return Task.FromResult<Booking?>(null);
        }

        return GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Booking>> ListBlockingByAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Booking> items = _bookings.Values
            .Where(b => b.AssetId == assetId && b.Status.IsBlocking)
            .OrderBy(b => b.Period.StartDate)
            .ToArray();

        return Task.FromResult(items);
    }
}
