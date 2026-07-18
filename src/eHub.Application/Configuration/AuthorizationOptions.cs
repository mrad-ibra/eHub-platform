using eHub.Domain.Identity;

namespace eHub.Application.Configuration;

public sealed class AuthorizationOptions
{
    public const string SectionName = "Authorization";

    public List<PermissionDefinition> Permissions { get; set; } = [];
    public List<RoleDefinition> Roles { get; set; } = [];
}
