using eHub.Domain.Bookings;

namespace eHub.Application.Bookings.Abstractions;

public interface IBookingRepository
{
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<Booking?> GetByNumberAsync(string bookingNumber, CancellationToken cancellationToken = default);

    /// <summary>Blocking Soft/Hard/Confirmed/InProgress bookings for an asset.</summary>
    Task<IReadOnlyList<Booking>> ListBlockingByAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);
}
