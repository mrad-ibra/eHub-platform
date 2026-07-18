namespace eHub.Domain.Catalog;

/// <summary>Feature dictionary entry (e.g. GPS, AirConditioning).</summary>
public sealed class FeatureDefinition : CatalogEntity
{
    public string? GroupCode { get; private set; }

    private FeatureDefinition()
    {
    }

    public static FeatureDefinition Create(
        string code,
        string name,
        DateTime nowUtc,
        string? groupCode = null,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new FeatureDefinition
        {
            GroupCode = string.IsNullOrWhiteSpace(groupCode) ? null : groupCode.Trim().ToUpperInvariant()
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void ChangeGroup(string? groupCode, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        GroupCode = string.IsNullOrWhiteSpace(groupCode) ? null : groupCode.Trim().ToUpperInvariant();
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
