using eHub.Domain.Bookings;

namespace eHub.Application.Bookings.Abstractions;

public interface IBookingRepository
{
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<Booking?> GetByNumberAsync(string bookingNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListBlockingByAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListBlockingByAssetAsync(
        Guid assetId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
