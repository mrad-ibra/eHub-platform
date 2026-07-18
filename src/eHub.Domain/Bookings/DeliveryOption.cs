using eHub.Domain.Common;

namespace eHub.Domain.Bookings;

public sealed class DeliveryOption
{
    public bool Requested { get; private set; }
    public Money? Fee { get; private set; }
    public string? AddressLine { get; private set; }

    private DeliveryOption()
    {
    }

    public static DeliveryOption None() => new() { Requested = false };

    public static DeliveryOption Request(Money fee, string? addressLine = null)
        => new()
        {
            Requested = true,
            Fee = fee,
            AddressLine = string.IsNullOrWhiteSpace(addressLine) ? null : addressLine.Trim()
        };
}
