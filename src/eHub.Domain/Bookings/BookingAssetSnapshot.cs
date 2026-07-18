namespace eHub.Domain.Bookings;

public sealed class BookingAssetSnapshot
{
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public IReadOnlyList<string> PrimaryImageUrls { get; private set; } = [];
    public Guid HostId { get; private set; }
    public string? HostDisplayName { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }

    private BookingAssetSnapshot()
    {
    }

    public static BookingAssetSnapshot Create(
        string name,
        Guid hostId,
        DateTime capturedAtUtc,
        string? brand = null,
        string? model = null,
        IEnumerable<string>? primaryImageUrls = null,
        string? hostDisplayName = null)
    {
        return new BookingAssetSnapshot
        {
            Name = name.Trim(),
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
            Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
            PrimaryImageUrls = (primaryImageUrls ?? []).Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToArray(),
            HostId = hostId,
            HostDisplayName = string.IsNullOrWhiteSpace(hostDisplayName) ? null : hostDisplayName.Trim(),
            CapturedAtUtc = capturedAtUtc
        };
    }
}
