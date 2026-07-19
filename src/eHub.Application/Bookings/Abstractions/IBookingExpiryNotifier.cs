using eHub.Domain.Bookings;

namespace eHub.Application.Bookings.Abstractions;

/// <summary>Notification stub — replace with real email/push publisher later.</summary>
public interface IBookingExpiryNotifier
{
    Task NotifyExpiredAsync(Booking booking, CancellationToken cancellationToken = default);
}
