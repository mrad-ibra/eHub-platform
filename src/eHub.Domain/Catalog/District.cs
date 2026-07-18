using eHub.Domain.Common;

namespace eHub.Domain.Catalog;

public sealed class District : CatalogEntity
{
    public Guid CityId { get; private set; }

    private District()
    {
    }

    public static District Create(
        Guid cityId,
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new District
        {
            CityId = AppGuard.NotEmpty(cityId, nameof(cityId))
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void MoveToCity(Guid cityId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        CityId = AppGuard.NotEmpty(cityId, nameof(cityId));
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
