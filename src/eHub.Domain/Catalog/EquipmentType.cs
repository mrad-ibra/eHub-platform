namespace eHub.Domain.Catalog;

public sealed class EquipmentType : CatalogEntity
{
    private EquipmentType()
    {
    }

    public static EquipmentType Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new EquipmentType();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
