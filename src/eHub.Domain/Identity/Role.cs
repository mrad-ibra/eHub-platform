using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Identity;

public sealed class Role : SoftDeletableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role()
    {
    }

    public static Role FromDefinition(RoleDefinition definition, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Create(definition.Name, definition.Description, definition.IsSystem, nowUtc);
    }

    public static Role Create(string name, string? description, bool isSystem, DateTime nowUtc, Guid? createdBy = null)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            IsSystem = isSystem,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        role.ApplyName(name);
        role.SetCreatedAudit(nowUtc, createdBy);
        return role;
    }

    public void Rename(string name, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        if (IsSystem)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.SystemRoleRenameForbidden));
        }

        ApplyName(name);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void UpdateDescription(string? description, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void GrantPermission(Permission permission, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(permission);

        if (HasPermission(permission.Id))
        {
            return;
        }

        _permissions.Add(RolePermission.Create(Id, permission.Id, nowUtc));
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void RevokePermission(Guid permissionId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        var existing = _permissions.FirstOrDefault(permission => permission.PermissionId == permissionId);
        if (existing is null)
        {
            return;
        }

        _permissions.Remove(existing);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public bool HasPermission(Guid permissionId)
        => _permissions.Any(permission => permission.PermissionId == permissionId);

    private void ApplyName(string name)
    {
        var trimmed = AppGuard.NotEmpty(name, nameof(name)).Trim();
        Name = trimmed;
        NormalizedName = trimmed.ToUpperInvariant();
    }
}
