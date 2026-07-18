namespace eHub.Domain.Catalog;

public sealed class VehicleType : CatalogEntity
{
    private VehicleType()
    {
    }

    public static VehicleType Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new VehicleType();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
