using eHub.Domain.Common;

namespace eHub.Domain.Assets;

/// <summary>Links an asset to a Catalog FeatureDefinition.</summary>
public sealed class AssetFeature
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid FeatureDefinitionId { get; private set; }

    private AssetFeature()
    {
    }

    internal static AssetFeature Create(Guid assetId, Guid featureDefinitionId)
    {
        return new AssetFeature
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            FeatureDefinitionId = AppGuard.NotEmpty(featureDefinitionId, nameof(featureDefinitionId))
        };
    }
}
