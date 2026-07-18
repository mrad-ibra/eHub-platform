using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eHub.Application.Bookings.Commands.CreateBooking;

namespace eHub.Application.Bookings.Services;

public static class BookingRequestHasher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Compute(CreateBookingCommand command)
    {
        var payload = new
        {
            command.AssetId,
            command.StartDate,
            command.EndDate,
            command.DriverRequested,
            command.DeliveryRequested,
            command.PickupUseAssetLocation,
            PickupAddressLine = Normalize(command.PickupAddressLine),
            command.DropoffUseAssetLocation,
            DropoffAddressLine = Normalize(command.DropoffAddressLine),
            Notes = Normalize(command.Notes)
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
