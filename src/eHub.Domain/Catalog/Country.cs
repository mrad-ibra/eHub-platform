namespace eHub.Domain.Catalog;

public sealed class Country : CatalogEntity
{
    private Country()
    {
    }

    public static Country Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new Country();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
