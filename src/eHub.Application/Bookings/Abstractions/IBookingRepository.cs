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

    /// <summary>
    /// Soft/Hard holds whose <c>ExpiresAtUtc</c> has passed (tracked entities for update).
    /// </summary>
    Task<IReadOnlyList<Booking>> ListExpiredHoldsAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default);
}
