namespace eHub.Domain.Catalog;

public sealed class BookingStatus : CatalogEntity
{
    private BookingStatus()
    {
    }

    public static BookingStatus Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new BookingStatus();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
