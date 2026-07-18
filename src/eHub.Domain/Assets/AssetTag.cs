using eHub.Domain.Common;

namespace eHub.Domain.Assets;

public sealed class AssetTag
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public string Tag { get; private set; } = string.Empty;

    private AssetTag()
    {
    }

    internal static AssetTag Create(Guid assetId, string tag)
    {
        return new AssetTag
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Tag = AppGuard.NotEmpty(tag, nameof(tag)).Trim().ToLowerInvariant()
        };
    }
}
