namespace eHub.Domain.Assets;

/// <summary>
/// Stable status codes for <see cref="Asset"/>. Values align with Catalog AssetStatus codes.
/// </summary>
public static class AssetStatusCodes
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Published = "PUBLISHED";
    public const string Suspended = "SUSPENDED";
    public const string Archived = "ARCHIVED";
    public const string Rejected = "REJECTED";
}
