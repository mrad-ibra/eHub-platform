namespace eHub.Application.Abstractions.Audit;

/// <summary>
/// Ambient actor for auditing persistence (who mutated the data).
/// Distinct from <see cref="Common.Context.ICurrentUser"/> authorization concerns.
/// </summary>
public interface IAuditContext
{
    Guid? UserId { get; }
}
