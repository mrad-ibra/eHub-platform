namespace eHub.Domain.Bookings;

public sealed class PickupInformation
{
    public bool UseAssetLocation { get; private set; }
    public string? AddressLine { get; private set; }
    public string? Notes { get; private set; }

    private PickupInformation()
    {
    }

    public static PickupInformation UseAsset() => new() { UseAssetLocation = true };

    public static PickupInformation Custom(string? addressLine, string? notes = null)
        => new()
        {
            UseAssetLocation = false,
            AddressLine = string.IsNullOrWhiteSpace(addressLine) ? null : addressLine.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
}
