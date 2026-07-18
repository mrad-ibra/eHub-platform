using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

/// <summary>
/// Status transitions and version history for the Asset aggregate.
/// </summary>
public sealed class AssetLifecycle
{
    private readonly List<AssetVersionEntry> _versions = [];

    public AssetStatusCode Status { get; private set; } = AssetStatusCode.Draft;
    public string? RejectionReason { get; private set; }
    public int VersionNumber { get; private set; } = 1;
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }

    public IReadOnlyCollection<AssetVersionEntry> VersionHistory => _versions.AsReadOnly();

    public bool IsArchived => Status == AssetStatusCode.Archived;
    public bool IsPendingApproval => Status == AssetStatusCode.PendingApproval;

    internal void RecordCreated(Guid assetId, DateTime nowUtc, Guid? changedBy)
        => AddVersion(assetId, nowUtc, changedBy, "Asset created");

    internal void EnsureEditable()
    {
        EnsureNotArchived();
        if (IsPendingApproval)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetPendingApprovalLocked));
        }
    }

    internal void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetArchived));
        }
    }

    internal void SubmitForApproval(Guid assetId, DateTime nowUtc, Guid? updatedBy, Action ensureReady)
    {
        EnsureEditable();
        ensureReady();
        ChangeStatus(assetId, AssetStatusCode.PendingApproval, nowUtc, updatedBy, "Submitted for approval");
    }

    internal void Approve(Guid assetId, DateTime nowUtc, Guid? updatedBy)
    {
        if (!Status.IsOneOf(AssetStatusCode.PendingApproval, AssetStatusCode.Rejected))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetInvalidStatusTransition));
        }

        RejectionReason = null;
        PublishedAtUtc = nowUtc;
        ArchivedAtUtc = null;
        ChangeStatus(assetId, AssetStatusCode.Published, nowUtc, updatedBy, "Approved and published");
    }

    internal void Reject(Guid assetId, string reason, DateTime nowUtc, Guid? updatedBy)
    {
        if (Status != AssetStatusCode.PendingApproval)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetInvalidStatusTransition));
        }

        RejectionReason = AppGuard.NotEmpty(reason, nameof(reason)).Trim();
        ChangeStatus(assetId, AssetStatusCode.Rejected, nowUtc, updatedBy, "Rejected");
    }

    internal void Publish(Guid assetId, DateTime nowUtc, Guid? updatedBy, Action ensureReady)
    {
        EnsureEditable();
        ensureReady();
        PublishedAtUtc = nowUtc;
        ArchivedAtUtc = null;
        RejectionReason = null;
        ChangeStatus(assetId, AssetStatusCode.Published, nowUtc, updatedBy, "Published");
    }

    internal void Archive(Guid assetId, DateTime nowUtc, Guid? updatedBy)
    {
        if (IsArchived)
        {
            return;
        }

        ArchivedAtUtc = nowUtc;
        ChangeStatus(assetId, AssetStatusCode.Archived, nowUtc, updatedBy, "Archived");
    }

    internal void Touch(Guid assetId, DateTime nowUtc, Guid? updatedBy, string summary)
    {
        VersionNumber++;
        AddVersion(assetId, nowUtc, updatedBy, summary);
    }

    private void ChangeStatus(
        Guid assetId,
        AssetStatusCode status,
        DateTime nowUtc,
        Guid? updatedBy,
        string summary)
    {
        Status = status;
        Touch(assetId, nowUtc, updatedBy, summary);
    }

    private void AddVersion(Guid assetId, DateTime nowUtc, Guid? changedBy, string summary)
    {
        _versions.Add(AssetVersionEntry.Create(
            assetId,
            VersionNumber,
            Status,
            nowUtc,
            changedBy,
            summary));
    }
}
