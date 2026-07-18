using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Identity;

public sealed class Permission : SoftDeletableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private Permission()
    {
    }

    public static Permission FromDefinition(PermissionDefinition definition, bool isSystem, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return Create(
            PermissionCode.Create(definition.Code),
            definition.Name,
            definition.Module,
            definition.Description,
            isSystem,
            nowUtc);
    }

    public static Permission Create(
        PermissionCode code,
        string name,
        string module,
        string? description,
        bool isSystem,
        DateTime nowUtc,
        Guid? createdBy = null)
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = code.Value,
            Name = AppGuard.NotEmpty(name, nameof(name)).Trim(),
            Module = AppGuard.NotEmpty(module, nameof(module)).Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsSystem = isSystem
        };

        permission.SetCreatedAudit(nowUtc, createdBy);
        return permission;
    }

    public void Rename(string name, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        if (IsSystem)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.SystemPermissionRenameForbidden));
        }

        Name = AppGuard.NotEmpty(name, nameof(name)).Trim();
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void UpdateDescription(string? description, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
