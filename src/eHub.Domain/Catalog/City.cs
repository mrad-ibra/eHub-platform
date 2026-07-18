using eHub.Domain.Common;

namespace eHub.Domain.Catalog;

public sealed class City : CatalogEntity
{
    public Guid CountryId { get; private set; }

    private City()
    {
    }

    public static City Create(
        Guid countryId,
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new City
        {
            CountryId = AppGuard.NotEmpty(countryId, nameof(countryId))
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void MoveToCountry(Guid countryId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        CountryId = AppGuard.NotEmpty(countryId, nameof(countryId));
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
