using eHub.Application.Bookings.Abstractions;
using eHub.Domain.Bookings;
using Microsoft.Extensions.Logging;

namespace eHub.Infrastructure.Jobs;

public sealed class LoggingBookingExpiryNotifier(ILogger<LoggingBookingExpiryNotifier> logger)
    : IBookingExpiryNotifier
{
    public Task NotifyExpiredAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notify stub: booking {BookingNumber} ({BookingId}) expired",
            booking.BookingNumber,
            booking.Id);
        return Task.CompletedTask;
    }
}
