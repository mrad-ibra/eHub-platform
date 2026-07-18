using eHub.Application.Common.Time;
using eHub.Domain.Common;

namespace eHub.Application.Abstractions.Audit;

public interface IAuditFieldStamper
{
    void StampCreated(AuditableEntity entity);

    void StampUpdated(AuditableEntity entity);

    void StampDeleted(SoftDeletableEntity entity);

    void StampRestored(SoftDeletableEntity entity);
}

public sealed class AuditFieldStamper(IClock clock, IAuditContext auditContext) : IAuditFieldStamper
{
    public void StampCreated(AuditableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.SetCreatedAudit(clock.UtcNow, auditContext.UserId);
    }

    public void StampUpdated(AuditableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.SetUpdatedAudit(clock.UtcNow, auditContext.UserId);
    }

    public void StampDeleted(SoftDeletableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.SoftDelete(clock.UtcNow, auditContext.UserId);
    }

    public void StampRestored(SoftDeletableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.Restore(clock.UtcNow, auditContext.UserId);
    }
}
