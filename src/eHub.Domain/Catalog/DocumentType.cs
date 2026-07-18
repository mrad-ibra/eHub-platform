namespace eHub.Domain.Catalog;

public sealed class DocumentType : CatalogEntity
{
    private DocumentType()
    {
    }

    public static DocumentType Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new DocumentType();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
