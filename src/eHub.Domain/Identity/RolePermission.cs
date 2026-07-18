using eHub.Domain.Common;

namespace eHub.Domain.Identity;

/// <summary>
/// Join entity linking a <see cref="Role"/> to a <see cref="Permission"/>.
/// </summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime GrantedAtUtc { get; private set; }

    private RolePermission()
    {
    }

    internal static RolePermission Create(Guid roleId, Guid permissionId, DateTime grantedAtUtc)
        => new()
        {
            RoleId = AppGuard.NotEmpty(roleId, nameof(roleId)),
            PermissionId = AppGuard.NotEmpty(permissionId, nameof(permissionId)),
            GrantedAtUtc = grantedAtUtc
        };
}
