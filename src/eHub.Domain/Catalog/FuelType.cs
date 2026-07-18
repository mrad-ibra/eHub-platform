namespace eHub.Domain.Catalog;

public sealed class FuelType : CatalogEntity
{
    private FuelType()
    {
    }

    public static FuelType Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new FuelType();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
