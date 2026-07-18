using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Domain.Common;

/// <summary>
/// Auditable entity that supports logical deletion instead of physical removal.
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public void SoftDelete(DateTime nowUtc, Guid? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = nowUtc;
        DeletedBy = deletedBy;
        SetUpdatedAudit(nowUtc, deletedBy);
    }

    public void Restore(DateTime nowUtc, Guid? restoredBy = null)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
        SetUpdatedAudit(nowUtc, restoredBy);
    }

    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.EntityDeleted));
        }
    }
}
