namespace eHub.Application.Bookings.Abstractions;

public interface IBookingIdempotencyStore
{
    Task<Guid?> FindBookingIdAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);

    Task SaveAsync(Guid userId, string idempotencyKey, Guid bookingId, CancellationToken cancellationToken = default);
}
