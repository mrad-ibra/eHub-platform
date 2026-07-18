namespace eHub.Domain.Bookings;

public sealed class DropoffInformation
{
    public bool UseAssetLocation { get; private set; }
    public string? AddressLine { get; private set; }
    public string? Notes { get; private set; }

    private DropoffInformation()
    {
    }

    public static DropoffInformation UseAsset() => new() { UseAssetLocation = true };

    public static DropoffInformation Custom(string? addressLine, string? notes = null)
        => new()
        {
            UseAssetLocation = false,
            AddressLine = string.IsNullOrWhiteSpace(addressLine) ? null : addressLine.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
}
