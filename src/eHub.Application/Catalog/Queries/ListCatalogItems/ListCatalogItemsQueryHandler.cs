using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Messaging;
using eHub.Domain.Catalog;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Application.Catalog.Queries.ListCatalogItems;

public sealed class ListCatalogItemsQueryHandler(ICatalogStore catalog)
    : IQueryHandler<ListCatalogItemsQuery, IReadOnlyList<CatalogItemDto>>
{
    public async Task<IReadOnlyList<CatalogItemDto>> Handle(
        ListCatalogItemsQuery request,
        CancellationToken cancellationToken)
    {
        return request.Kind switch
        {
            CatalogKind.Category => await MapFlat<Category>(request.ActiveOnly, cancellationToken),
            CatalogKind.SubCategory => await MapChildren<SubCategory>(
                request.ActiveOnly,
                request.ParentId,
                x => x.CategoryId,
                cancellationToken),
            CatalogKind.Brand => await MapFlat<Brand>(request.ActiveOnly, cancellationToken),
            CatalogKind.Model => await MapChildren<Model>(
                request.ActiveOnly,
                request.ParentId,
                x => x.BrandId,
                cancellationToken),
            CatalogKind.Country => await MapFlat<Country>(request.ActiveOnly, cancellationToken),
            CatalogKind.City => await MapChildren<City>(
                request.ActiveOnly,
                request.ParentId,
                x => x.CountryId,
                cancellationToken),
            CatalogKind.District => await MapChildren<District>(
                request.ActiveOnly,
                request.ParentId,
                x => x.CityId,
                cancellationToken),
            CatalogKind.Currency => await MapCurrencies(request.ActiveOnly, cancellationToken),
            CatalogKind.Language => await MapLanguages(request.ActiveOnly, cancellationToken),
            CatalogKind.Transmission => await MapFlat<Transmission>(request.ActiveOnly, cancellationToken),
            CatalogKind.FuelType => await MapFlat<FuelType>(request.ActiveOnly, cancellationToken),
            CatalogKind.VehicleType => await MapFlat<VehicleType>(request.ActiveOnly, cancellationToken),
            CatalogKind.EquipmentType => await MapFlat<EquipmentType>(request.ActiveOnly, cancellationToken),
            CatalogKind.FeatureDefinition => await MapFeatures(request.ActiveOnly, cancellationToken),
            CatalogKind.Color => await MapColors(request.ActiveOnly, cancellationToken),
            CatalogKind.DocumentType => await MapFlat<DocumentType>(request.ActiveOnly, cancellationToken),
            CatalogKind.MediaType => await MapFlat<MediaType>(request.ActiveOnly, cancellationToken),
            CatalogKind.RentalPeriodType => await MapFlat<RentalPeriodType>(request.ActiveOnly, cancellationToken),
            CatalogKind.PaymentMethod => await MapFlat<PaymentMethod>(request.ActiveOnly, cancellationToken),
            CatalogKind.BookingStatus => await MapFlat<BookingStatus>(request.ActiveOnly, cancellationToken),
            CatalogKind.AssetStatus => await MapFlat<AssetStatus>(request.ActiveOnly, cancellationToken),
            CatalogKind.ReviewStatus => await MapFlat<ReviewStatus>(request.ActiveOnly, cancellationToken),
            _ => throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BadRequest))
        };
    }

    private async Task<IReadOnlyList<CatalogItemDto>> MapFlat<T>(
        bool activeOnly,
        CancellationToken cancellationToken)
        where T : CatalogEntity
    {
        var items = await catalog.ListAsync<T>(activeOnly, cancellationToken);
        return items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogItemDto>> MapChildren<T>(
        bool activeOnly,
        Guid? parentId,
        Func<T, Guid> parentSelector,
        CancellationToken cancellationToken)
        where T : CatalogEntity
    {
        var items = await catalog.ListAsync<T>(activeOnly, cancellationToken);
        return items
            .Where(x => parentId is null || parentSelector(x) == parentId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, parentSelector(x)))
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogItemDto>> MapCurrencies(bool activeOnly, CancellationToken ct)
    {
        var items = await catalog.ListAsync<Currency>(activeOnly, ct);
        return items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, symbol: x.Symbol, decimalPlaces: x.DecimalPlaces))
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogItemDto>> MapLanguages(bool activeOnly, CancellationToken ct)
    {
        var items = await catalog.ListAsync<Language>(activeOnly, ct);
        return items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, cultureName: x.CultureName))
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogItemDto>> MapFeatures(bool activeOnly, CancellationToken ct)
    {
        var items = await catalog.ListAsync<FeatureDefinition>(activeOnly, ct);
        return items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, groupCode: x.GroupCode))
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogItemDto>> MapColors(bool activeOnly, CancellationToken ct)
    {
        var items = await catalog.ListAsync<Color>(activeOnly, ct);
        return items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, hexCode: x.HexCode))
            .ToArray();
    }

    private static CatalogItemDto ToDto(
        CatalogEntity entity,
        Guid? parentId = null,
        string? symbol = null,
        int? decimalPlaces = null,
        string? cultureName = null,
        string? groupCode = null,
        string? hexCode = null)
        => new(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.SortOrder,
            entity.IsActive,
            parentId,
            symbol,
            decimalPlaces,
            cultureName,
            groupCode,
            hexCode);
}
