using eHub.Application.Abstractions.Audit;

namespace eHub.Application.Common.Context;

/// <summary>Uses the authenticated caller as the audit actor when present.</summary>
public sealed class CurrentUserAuditContext(ICurrentUser currentUser) : IAuditContext
{
    public Guid? UserId => currentUser.UserId;
}
