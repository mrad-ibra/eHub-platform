namespace eHub.Domain.Common;

/// <summary>
/// Base type for persisted aggregates that track create/update timestamps and actors.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    /// <summary>Sets create audit fields. Prefer calling from factory methods.</summary>
    public void SetCreatedAudit(DateTime nowUtc, Guid? createdBy = null)
    {
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        CreatedBy = createdBy;
        UpdatedBy = createdBy;
    }

    /// <summary>Sets update audit fields. Prefer calling from mutating methods.</summary>
    public void SetUpdatedAudit(DateTime nowUtc, Guid? updatedBy = null)
    {
        UpdatedAtUtc = nowUtc;

        if (updatedBy.HasValue)
        {
            UpdatedBy = updatedBy;
        }
    }
}
