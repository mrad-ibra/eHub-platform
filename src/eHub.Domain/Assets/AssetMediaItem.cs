using eHub.Domain.Common;

namespace eHub.Domain.Assets;

public sealed class AssetMediaItem
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public AssetMediaKind Kind { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }
    public long? SizeBytes { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AssetMediaItem()
    {
    }

    internal static AssetMediaItem Create(
        Guid assetId,
        AssetMediaKind kind,
        string url,
        DateTime nowUtc,
        string? fileName = null,
        string? contentType = null,
        long? sizeBytes = null,
        int sortOrder = 0,
        bool isPrimary = false)
    {
        return new AssetMediaItem
        {
            Id = Guid.NewGuid(),
            AssetId = AppGuard.NotEmpty(assetId, nameof(assetId)),
            Kind = kind,
            Url = AppGuard.NotEmpty(url, nameof(url)).Trim(),
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim(),
            SizeBytes = sizeBytes,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            CreatedAtUtc = nowUtc
        };
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    internal void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;
}
