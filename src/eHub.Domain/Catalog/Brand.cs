namespace eHub.Domain.Catalog;

public sealed class Brand : CatalogEntity
{
    private Brand()
    {
    }

    public static Brand Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new Brand();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
