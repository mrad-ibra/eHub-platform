namespace eHub.Domain.Catalog;

public sealed class ReviewStatus : CatalogEntity
{
    private ReviewStatus()
    {
    }

    public static ReviewStatus Create(
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new ReviewStatus();
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }
}
