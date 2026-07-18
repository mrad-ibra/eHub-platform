using eHub.Domain.Common;

namespace eHub.Domain.Assets;

/// <summary>
/// Universal rentable item aggregate root. Category-specific meaning comes from Catalog references —
/// never create Car/Boat/Excavator root entities.
/// <para>
/// Complexity lives in internal components (<see cref="AssetLifecycle"/>, <see cref="AssetCommercialTerms"/>,
/// <see cref="AssetMediaCollection"/>, <see cref="AssetAvailability"/>, <see cref="AssetAttributeCollection"/>).
/// External callers mutate only through this root.
/// </para>
/// <para>
/// <b>Catalog / Identity references are identity-only:</b> keep <see cref="CategoryId"/>, <see cref="BrandId"/>,
/// <see cref="ModelId"/>, <see cref="OwnerId"/> (and location country/city ids) as <c>Guid</c> foreign keys.
/// Do not add navigation properties to other aggregates or Catalog entities on this root — that couples
/// aggregate boundaries. Resolve names via Application queries / read models when needed.
/// </para>
/// </summary>
public sealed class Asset : SoftDeletableEntity
{
    public Guid OwnerId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? SubCategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public Guid? ModelId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public AssetLifecycle Lifecycle { get; private set; } = new();
    public AssetCommercialTerms Commercial { get; private set; } = new();
    public AssetMediaCollection MediaCollection { get; private set; } = new();
    public AssetAvailability Availability { get; private set; } = new();
    public AssetAttributeCollection Attributes { get; private set; } = new();

    // Compatibility surface for queries / mapping (delegates to components).
    public AssetStatusCode Status => Lifecycle.Status;
    public string StatusCode => Lifecycle.Status.Value;
    public string? RejectionReason => Lifecycle.RejectionReason;
    public int VersionNumber => Lifecycle.VersionNumber;
    public DateTime? PublishedAtUtc => Lifecycle.PublishedAtUtc;
    public DateTime? ArchivedAtUtc => Lifecycle.ArchivedAtUtc;

    public AssetPricing? Pricing => Commercial.Pricing;
    public AssetLocation? Location => Commercial.Location;
    public AssetRentalRules? RentalRules => Commercial.RentalRules;
    public AssetSecurityDeposit SecurityDeposit => Commercial.SecurityDeposit;
    public AssetSupportOptions SupportOptions => Commercial.Support;

    public IReadOnlyCollection<AssetMediaItem> Media => MediaCollection.Items;
    public IReadOnlyCollection<AssetMediaItem> Images => MediaCollection.Images;
    public IReadOnlyCollection<AssetMediaItem> Videos => MediaCollection.Videos;
    public IReadOnlyCollection<AssetMediaItem> Documents => MediaCollection.Documents;
    public IReadOnlyCollection<AssetAvailabilityBlock> AvailabilityBlocks => Availability.Blocks;
    public IReadOnlyCollection<AssetTag> Tags => Attributes.Tags;
    public IReadOnlyCollection<AssetFeature> Features => Attributes.Features;
    public IReadOnlyCollection<AssetVersionEntry> VersionHistory => Lifecycle.VersionHistory;

    private Asset()
    {
    }

    public static Asset Create(
        Guid ownerId,
        Guid categoryId,
        string title,
        DateTime nowUtc,
        Guid? subCategoryId = null,
        Guid? brandId = null,
        Guid? modelId = null,
        string? description = null,
        Guid? createdBy = null)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OwnerId = AppGuard.NotEmpty(ownerId, nameof(ownerId)),
            CategoryId = AppGuard.NotEmpty(categoryId, nameof(categoryId)),
            SubCategoryId = NullIfEmpty(subCategoryId),
            BrandId = NullIfEmpty(brandId),
            ModelId = NullIfEmpty(modelId),
            Title = AppGuard.NotEmpty(title, nameof(title)).Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        var actor = createdBy ?? ownerId;
        asset.SetCreatedAudit(nowUtc, actor);
        asset.Lifecycle.RecordCreated(asset.Id, nowUtc, actor);
        return asset;
    }

    public void UpdateDetails(
        string title,
        string? description,
        Guid? subCategoryId,
        Guid? brandId,
        Guid? modelId,
        DateTime nowUtc,
        Guid? updatedBy = null)
    {
        EnsureEditable();
        Title = AppGuard.NotEmpty(title, nameof(title)).Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SubCategoryId = NullIfEmpty(subCategoryId);
        BrandId = NullIfEmpty(brandId);
        ModelId = NullIfEmpty(modelId);
        Touch(nowUtc, updatedBy, "Details updated");
    }

    public void SetPricing(AssetPricing pricing, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        Commercial.SetPricing(pricing);
        Touch(nowUtc, updatedBy, "Pricing updated");
    }

    public void SetLocation(AssetLocation location, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        Commercial.SetLocation(location);
        Touch(nowUtc, updatedBy, "Location updated");
    }

    public void SetRentalRules(AssetRentalRules rules, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        Commercial.SetRentalRules(rules);
        Touch(nowUtc, updatedBy, "Rental rules updated");
    }

    public void SetSecurityDeposit(AssetSecurityDeposit deposit, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        Commercial.SetSecurityDeposit(deposit);
        Touch(nowUtc, updatedBy, "Security deposit updated");
    }

    public void SetSupportOptions(AssetSupportOptions options, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        Commercial.SetSupport(options);
        Touch(nowUtc, updatedBy, "Support options updated");
    }

    public AssetMediaItem AddMedia(
        AssetMediaKind kind,
        string url,
        DateTime nowUtc,
        string? fileName = null,
        string? contentType = null,
        long? sizeBytes = null,
        bool isPrimary = false,
        Guid? updatedBy = null)
    {
        EnsureEditable();
        var item = MediaCollection.Add(Id, kind, url, nowUtc, fileName, contentType, sizeBytes, isPrimary);
        Touch(nowUtc, updatedBy, $"{kind} added");
        return item;
    }

    public void RemoveMedia(Guid mediaId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        MediaCollection.Remove(mediaId);
        Touch(nowUtc, updatedBy, "Media removed");
    }

    public AssetAvailabilityBlock BlockAvailability(
        DateOnly startDate,
        DateOnly endDate,
        DateTime nowUtc,
        string? note = null,
        Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Lifecycle.EnsureNotArchived();
        var block = Availability.Block(Id, startDate, endDate, nowUtc, note);
        SetUpdatedAudit(nowUtc, updatedBy);
        return block;
    }

    public void RemoveAvailabilityBlock(Guid blockId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Availability.Remove(blockId);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void AddTag(string tag, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        if (Attributes.AddTag(Id, tag))
        {
            Touch(nowUtc, updatedBy, "Tag added");
        }
    }

    public void RemoveTag(string tag, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        if (Attributes.RemoveTag(tag))
        {
            Touch(nowUtc, updatedBy, "Tag removed");
        }
    }

    public void AddFeature(Guid featureDefinitionId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        if (Attributes.AddFeature(Id, featureDefinitionId))
        {
            Touch(nowUtc, updatedBy, "Feature added");
        }
    }

    public void RemoveFeature(Guid featureDefinitionId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        if (Attributes.RemoveFeature(featureDefinitionId))
        {
            Touch(nowUtc, updatedBy, "Feature removed");
        }
    }

    public void SubmitForApproval(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Lifecycle.SubmitForApproval(Id, nowUtc, updatedBy, EnsureReadyForPublish);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Approve(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Lifecycle.Approve(Id, nowUtc, updatedBy);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Reject(string reason, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Lifecycle.Reject(Id, reason, nowUtc, updatedBy);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Publish(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Lifecycle.Publish(Id, nowUtc, updatedBy, EnsureReadyForPublish);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Archive(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Lifecycle.Archive(Id, nowUtc, updatedBy);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    private void EnsureEditable()
    {
        EnsureNotDeleted();
        Lifecycle.EnsureEditable();
    }

    private void EnsureReadyForPublish()
        => Commercial.EnsureReadyForPublish(MediaCollection.HasImage);

    private void Touch(DateTime nowUtc, Guid? updatedBy, string summary)
    {
        Lifecycle.Touch(Id, nowUtc, updatedBy, summary);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    private static Guid? NullIfEmpty(Guid? value)
        => value is null || value == Guid.Empty ? null : value;
}
