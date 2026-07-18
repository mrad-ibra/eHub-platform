namespace eHub.Domain.Assets;

public sealed class AssetVersionEntry
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public int VersionNumber { get; private set; }
    public string StatusCode { get; private set; } = string.Empty;
    public string? ChangeSummary { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private AssetVersionEntry()
    {
    }

    internal static AssetVersionEntry Create(
        Guid assetId,
        int versionNumber,
        string statusCode,
        DateTime changedAtUtc,
        Guid? changedBy = null,
        string? changeSummary = null)
    {
        return new AssetVersionEntry
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            VersionNumber = versionNumber,
            StatusCode = statusCode,
            ChangeSummary = string.IsNullOrWhiteSpace(changeSummary) ? null : changeSummary.Trim(),
            ChangedBy = changedBy,
            ChangedAtUtc = changedAtUtc
        };
    }
}
