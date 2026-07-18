namespace eHub.Application.Bookings.Abstractions;

public interface IBookingNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
