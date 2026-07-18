using eHub.Domain.Identity;

namespace eHub.Application.Identity.Authorization;

public interface IAuthorizationCatalog
{
    IReadOnlyCollection<PermissionDefinition> Permissions { get; }
    IReadOnlyCollection<RoleDefinition> Roles { get; }

    PermissionDefinition? FindPermission(string code);
    RoleDefinition? FindRole(string name);
}
