using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Domain.Catalog;

/// <summary>
/// Shared base for system-wide lookup / dictionary entities.
/// </summary>
public abstract class CatalogEntity : SoftDeletableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSystem { get; private set; }

    protected void Initialize(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        Id = Guid.NewGuid();
        ApplyCode(code);
        ApplyName(name);
        SortOrder = sortOrder;
        IsActive = true;
        IsSystem = isSystem;
        SetCreatedAudit(nowUtc, createdBy);
    }

    public void Rename(string name, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        ApplyName(name);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void ChangeSortOrder(int sortOrder, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        SortOrder = sortOrder;
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Activate(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        IsActive = true;
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Deactivate(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        IsActive = false;
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    protected void ApplyCode(string code)
    {
        var trimmed = AppGuard.NotEmpty(code, nameof(code)).Trim().ToUpperInvariant();
        if (trimmed.Length > 64)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.CatalogCodeTooLong));
        }

        Code = trimmed;
    }

    protected void ApplyName(string name)
    {
        Name = AppGuard.NotEmpty(name, nameof(name)).Trim();
    }
}
