namespace eHub.Domain.Catalog;

public sealed class Transmission : CatalogEntity
{
    private Transmission()
    {
    }

    public static Transmission Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new Transmission();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
