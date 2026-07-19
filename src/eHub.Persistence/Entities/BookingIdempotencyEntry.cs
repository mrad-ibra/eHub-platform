using eHub.Application.Bookings.Abstractions;

namespace eHub.Persistence.Entities;

/// <summary>
/// Persistence entity for create-booking idempotency.
/// Completed in the same SaveChanges transaction as the Booking insert.
/// </summary>
public sealed class BookingIdempotencyEntry
{
    public Guid RenterId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public BookingIdempotencyStatus Status { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
