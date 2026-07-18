using eHub.Application.Common.Messaging;

namespace eHub.Application.Bookings.Commands.CreateBooking;

public sealed record CreateBookingCommand(
    Guid AssetId,
    DateOnly StartDate,
    DateOnly EndDate,
    string IdempotencyKey,
    bool DriverRequested = false,
    bool DeliveryRequested = false,
    bool PickupUseAssetLocation = true,
    string? PickupAddressLine = null,
    bool DropoffUseAssetLocation = true,
    string? DropoffAddressLine = null,
    string? Notes = null) : ICommand<CreateBookingResult>;

public sealed record CreateBookingResult(
    Guid Id,
    string BookingNumber,
    string Status,
    Guid AssetId,
    DateOnly StartDate,
    DateOnly EndDate,
    int BufferDays,
    decimal TotalAmount,
    Guid CurrencyId,
    DateTime? ExpiresAtUtc,
    int AggregateVersion,
    string SnapshotName);
