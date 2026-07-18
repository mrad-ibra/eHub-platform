using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Domain.Assets;

/// <summary>
/// Universal rentable item. Category-specific meaning comes from Catalog references —
/// never create Car/Boat/Excavator root entities.
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
    public string StatusCode { get; private set; } = AssetStatusCodes.Draft;
    public string? RejectionReason { get; private set; }
    public int VersionNumber { get; private set; } = 1;
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }

    public AssetPricing? Pricing { get; private set; }
    public AssetLocation? Location { get; private set; }
    public AssetRentalRules? RentalRules { get; private set; }
    public AssetSecurityDeposit SecurityDeposit { get; private set; } = AssetSecurityDeposit.None();
    public AssetSupportOptions SupportOptions { get; private set; } = AssetSupportOptions.Create();

    private readonly List<AssetMediaItem> _media = [];
    private readonly List<AssetAvailabilityBlock> _availability = [];
    private readonly List<AssetTag> _tags = [];
    private readonly List<AssetFeature> _features = [];
    private readonly List<AssetVersionEntry> _versions = [];

    public IReadOnlyCollection<AssetMediaItem> Media => _media.AsReadOnly();
    public IReadOnlyCollection<AssetMediaItem> Images => _media.Where(m => m.Kind == AssetMediaKind.Image).ToList();
    public IReadOnlyCollection<AssetMediaItem> Videos => _media.Where(m => m.Kind == AssetMediaKind.Video).ToList();
    public IReadOnlyCollection<AssetMediaItem> Documents => _media.Where(m => m.Kind == AssetMediaKind.Document).ToList();
    public IReadOnlyCollection<AssetAvailabilityBlock> AvailabilityBlocks => _availability.AsReadOnly();
    public IReadOnlyCollection<AssetTag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<AssetFeature> Features => _features.AsReadOnly();
    public IReadOnlyCollection<AssetVersionEntry> VersionHistory => _versions.AsReadOnly();

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
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            StatusCode = AssetStatusCodes.Draft,
            VersionNumber = 1
        };

        asset.SetCreatedAudit(nowUtc, createdBy ?? ownerId);
        asset.AddVersion(nowUtc, createdBy ?? ownerId, "Asset created");
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
        Pricing = pricing ?? throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, nameof(pricing)));
        Touch(nowUtc, updatedBy, "Pricing updated");
    }

    public void SetLocation(AssetLocation location, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        Location = location ?? throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, nameof(location)));
        Touch(nowUtc, updatedBy, "Location updated");
    }

    public void SetRentalRules(AssetRentalRules rules, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        RentalRules = rules ?? throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, nameof(rules)));
        Touch(nowUtc, updatedBy, "Rental rules updated");
    }

    public void SetSecurityDeposit(AssetSecurityDeposit deposit, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        SecurityDeposit = deposit ?? AssetSecurityDeposit.None();
        Touch(nowUtc, updatedBy, "Security deposit updated");
    }

    public void SetSupportOptions(AssetSupportOptions options, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        SupportOptions = options ?? AssetSupportOptions.Create();
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

        if (isPrimary && kind == AssetMediaKind.Image)
        {
            foreach (var image in _media.Where(m => m.Kind == AssetMediaKind.Image))
            {
                image.SetPrimary(false);
            }
        }

        var item = AssetMediaItem.Create(
            Id,
            kind,
            url,
            nowUtc,
            fileName,
            contentType,
            sizeBytes,
            _media.Count(m => m.Kind == kind),
            isPrimary && kind == AssetMediaKind.Image);

        _media.Add(item);
        Touch(nowUtc, updatedBy, $"{kind} added");
        return item;
    }

    public void RemoveMedia(Guid mediaId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        var item = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetMediaNotFound));
        _media.Remove(item);
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
        EnsureNotArchived();

        if (_availability.Any(block => block.Overlaps(startDate, endDate)))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetAvailabilityOverlap));
        }

        var block = AssetAvailabilityBlock.Create(Id, startDate, endDate, nowUtc, note);
        _availability.Add(block);
        SetUpdatedAudit(nowUtc, updatedBy);
        return block;
    }

    public void RemoveAvailabilityBlock(Guid blockId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        var block = _availability.FirstOrDefault(b => b.Id == blockId)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetAvailabilityNotFound));
        _availability.Remove(block);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void AddTag(string tag, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        var normalized = AppGuard.NotEmpty(tag, nameof(tag)).Trim().ToLowerInvariant();
        if (_tags.Any(t => t.Tag == normalized))
        {
            return;
        }

        _tags.Add(AssetTag.Create(Id, normalized));
        Touch(nowUtc, updatedBy, "Tag added");
    }

    public void RemoveTag(string tag, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        var normalized = tag.Trim().ToLowerInvariant();
        var existing = _tags.FirstOrDefault(t => t.Tag == normalized);
        if (existing is null)
        {
            return;
        }

        _tags.Remove(existing);
        Touch(nowUtc, updatedBy, "Tag removed");
    }

    public void AddFeature(Guid featureDefinitionId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        if (_features.Any(f => f.FeatureDefinitionId == featureDefinitionId))
        {
            return;
        }

        _features.Add(AssetFeature.Create(Id, featureDefinitionId));
        Touch(nowUtc, updatedBy, "Feature added");
    }

    public void RemoveFeature(Guid featureDefinitionId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        var existing = _features.FirstOrDefault(f => f.FeatureDefinitionId == featureDefinitionId);
        if (existing is null)
        {
            return;
        }

        _features.Remove(existing);
        Touch(nowUtc, updatedBy, "Feature removed");
    }

    public void SubmitForApproval(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        EnsureReadyForPublish();
        ChangeStatus(AssetStatusCodes.PendingApproval, nowUtc, updatedBy, "Submitted for approval");
    }

    public void Approve(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        if (StatusCode is not (AssetStatusCodes.PendingApproval or AssetStatusCodes.Rejected))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetInvalidStatusTransition));
        }

        RejectionReason = null;
        ChangeStatus(AssetStatusCodes.Published, nowUtc, updatedBy, "Approved and published");
        PublishedAtUtc = nowUtc;
        ArchivedAtUtc = null;
    }

    public void Reject(string reason, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        if (StatusCode != AssetStatusCodes.PendingApproval)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetInvalidStatusTransition));
        }

        RejectionReason = AppGuard.NotEmpty(reason, nameof(reason)).Trim();
        ChangeStatus(AssetStatusCodes.Rejected, nowUtc, updatedBy, "Rejected");
    }

    public void Publish(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureEditable();
        EnsureReadyForPublish();
        ChangeStatus(AssetStatusCodes.Published, nowUtc, updatedBy, "Published");
        PublishedAtUtc = nowUtc;
        ArchivedAtUtc = null;
        RejectionReason = null;
    }

    public void Archive(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        if (StatusCode == AssetStatusCodes.Archived)
        {
            return;
        }

        ChangeStatus(AssetStatusCodes.Archived, nowUtc, updatedBy, "Archived");
        ArchivedAtUtc = nowUtc;
    }

    private void EnsureReadyForPublish()
    {
        if (Pricing is null)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetPricingRequired));
        }

        if (Location is null)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetLocationRequired));
        }

        if (!_media.Any(m => m.Kind == AssetMediaKind.Image))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetImageRequired));
        }
    }

    private void EnsureEditable()
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        if (StatusCode == AssetStatusCodes.PendingApproval)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetPendingApprovalLocked));
        }
    }

    private void EnsureNotArchived()
    {
        if (StatusCode == AssetStatusCodes.Archived)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetArchived));
        }
    }

    private void ChangeStatus(string statusCode, DateTime nowUtc, Guid? updatedBy, string summary)
    {
        StatusCode = statusCode;
        Touch(nowUtc, updatedBy, summary);
    }

    private void Touch(DateTime nowUtc, Guid? updatedBy, string summary)
    {
        VersionNumber++;
        SetUpdatedAudit(nowUtc, updatedBy);
        AddVersion(nowUtc, updatedBy, summary);
    }

    private void AddVersion(DateTime nowUtc, Guid? changedBy, string summary)
    {
        _versions.Add(AssetVersionEntry.Create(
            Id,
            VersionNumber,
            StatusCode,
            nowUtc,
            changedBy,
            summary));
    }

    private static Guid? NullIfEmpty(Guid? value)
        => value is null || value == Guid.Empty ? null : value;
}
