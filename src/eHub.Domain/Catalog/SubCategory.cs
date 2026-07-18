using eHub.Domain.Common;

namespace eHub.Domain.Catalog;

public sealed class SubCategory : CatalogEntity
{
    public Guid CategoryId { get; private set; }

    private SubCategory()
    {
    }

    public static SubCategory Create(
        Guid categoryId,
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new SubCategory
        {
            CategoryId = AppGuard.NotEmpty(categoryId, nameof(categoryId))
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void MoveToCategory(Guid categoryId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        CategoryId = AppGuard.NotEmpty(categoryId, nameof(categoryId));
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
