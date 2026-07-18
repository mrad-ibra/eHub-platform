using eHub.Domain.Common;

namespace eHub.Domain.Catalog;

public sealed class Model : CatalogEntity
{
    public Guid BrandId { get; private set; }

    private Model()
    {
    }

    public static Model Create(
        Guid brandId,
        string code,
        string name,
        DateTime nowUtc,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        var entity = new Model
        {
            BrandId = AppGuard.NotEmpty(brandId, nameof(brandId))
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void MoveToBrand(Guid brandId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        BrandId = AppGuard.NotEmpty(brandId, nameof(brandId));
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
