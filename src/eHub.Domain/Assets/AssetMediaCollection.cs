using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

/// <summary>
/// Internal media collection for the Asset aggregate.
/// Items are partitioned by <see cref="AssetMediaKind"/> so filtered views are O(1).
/// </summary>
public sealed class AssetMediaCollection
{
    private readonly List<AssetMediaItem> _items = [];
    private readonly List<AssetMediaItem> _images = [];
    private readonly List<AssetMediaItem> _videos = [];
    private readonly List<AssetMediaItem> _documents = [];

    public IReadOnlyCollection<AssetMediaItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<AssetMediaItem> Images => _images.AsReadOnly();
    public IReadOnlyCollection<AssetMediaItem> Videos => _videos.AsReadOnly();
    public IReadOnlyCollection<AssetMediaItem> Documents => _documents.AsReadOnly();

    public bool HasImage => _images.Count > 0;

    internal AssetMediaItem Add(
        Guid assetId,
        AssetMediaKind kind,
        string url,
        DateTime nowUtc,
        string? fileName = null,
        string? contentType = null,
        long? sizeBytes = null,
        bool isPrimary = false)
    {
        var bucket = BucketFor(kind);

        if (isPrimary && kind == AssetMediaKind.Image)
        {
            foreach (var image in _images)
            {
                image.SetPrimary(false);
            }
        }

        var item = AssetMediaItem.Create(
            assetId,
            kind,
            url,
            nowUtc,
            fileName,
            contentType,
            sizeBytes,
            bucket.Count,
            isPrimary && kind == AssetMediaKind.Image);

        _items.Add(item);
        bucket.Add(item);
        return item;
    }

    internal void Remove(Guid mediaId)
    {
        var item = _items.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetMediaNotFound));

        _items.Remove(item);
        BucketFor(item.Kind).Remove(item);
    }

    private List<AssetMediaItem> BucketFor(AssetMediaKind kind)
        => kind switch
        {
            AssetMediaKind.Image => _images,
            AssetMediaKind.Video => _videos,
            AssetMediaKind.Document => _documents,
            _ => throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BadRequest))
        };
}
