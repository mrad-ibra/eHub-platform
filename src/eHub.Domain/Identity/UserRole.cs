using eHub.Domain.Common;

namespace eHub.Domain.Identity;

/// <summary>
/// Join entity linking a <see cref="User"/> to a <see cref="Role"/>.
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private UserRole()
    {
    }

    internal static UserRole Create(Guid userId, Guid roleId, DateTime assignedAtUtc)
        => new()
        {
            UserId = AppGuard.NotEmpty(userId, nameof(userId)),
            RoleId = AppGuard.NotEmpty(roleId, nameof(roleId)),
            AssignedAtUtc = assignedAtUtc
        };
}
