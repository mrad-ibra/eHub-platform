using eHub.Domain.Common;

namespace eHub.Domain.Assets;

/// <summary>
/// Tags and feature links owned by the Asset aggregate.
/// </summary>
public sealed class AssetAttributeCollection
{
    private readonly List<AssetTag> _tags = [];
    private readonly List<AssetFeature> _features = [];

    public IReadOnlyCollection<AssetTag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<AssetFeature> Features => _features.AsReadOnly();

    internal bool AddTag(Guid assetId, string tag)
    {
        var normalized = AppGuard.NotEmpty(tag, nameof(tag)).Trim().ToLowerInvariant();
        if (_tags.Any(t => t.Tag == normalized))
        {
            return false;
        }

        _tags.Add(AssetTag.Create(assetId, normalized));
        return true;
    }

    internal bool RemoveTag(string tag)
    {
        var normalized = tag.Trim().ToLowerInvariant();
        var existing = _tags.FirstOrDefault(t => t.Tag == normalized);
        if (existing is null)
        {
            return false;
        }

        _tags.Remove(existing);
        return true;
    }

    internal bool AddFeature(Guid assetId, Guid featureDefinitionId)
    {
        if (_features.Any(f => f.FeatureDefinitionId == featureDefinitionId))
        {
            return false;
        }

        _features.Add(AssetFeature.Create(assetId, featureDefinitionId));
        return true;
    }

    internal bool RemoveFeature(Guid featureDefinitionId)
    {
        var existing = _features.FirstOrDefault(f => f.FeatureDefinitionId == featureDefinitionId);
        if (existing is null)
        {
            return false;
        }

        _features.Remove(existing);
        return true;
    }
}
