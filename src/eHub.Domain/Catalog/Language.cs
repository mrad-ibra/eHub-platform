using eHub.Domain.Common;

namespace eHub.Domain.Catalog;

public sealed class Language : CatalogEntity
{
    public string CultureName { get; private set; } = string.Empty;

    private Language()
    {
    }

    public static Language Create(
        string code,
        string name,
        string cultureName,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new Language
        {
            CultureName = AppGuard.NotEmpty(cultureName, nameof(cultureName)).Trim()
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void ChangeCulture(string cultureName, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        CultureName = AppGuard.NotEmpty(cultureName, nameof(cultureName)).Trim();
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
