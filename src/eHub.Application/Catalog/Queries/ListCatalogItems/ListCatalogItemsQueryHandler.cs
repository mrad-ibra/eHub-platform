using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Messaging;
using eHub.Domain.Catalog;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Application.Catalog.Queries.ListCatalogItems;

public sealed class ListCatalogItemsQueryHandler(
    ICategoryRepository categories,
    ISubCategoryRepository subCategories,
    IBrandRepository brands,
    IModelRepository models,
    ICountryRepository countries,
    ICityRepository cities,
    IDistrictRepository districts,
    ICurrencyRepository currencies,
    ILanguageRepository languages,
    ITransmissionRepository transmissions,
    IFuelTypeRepository fuelTypes,
    IVehicleTypeRepository vehicleTypes,
    IEquipmentTypeRepository equipmentTypes,
    IFeatureDefinitionRepository features,
    IColorRepository colors,
    IDocumentTypeRepository documentTypes,
    IMediaTypeRepository mediaTypes,
    IRentalPeriodTypeRepository rentalPeriodTypes,
    IPaymentMethodRepository paymentMethods,
    IBookingStatusRepository bookingStatuses,
    IAssetStatusRepository assetStatuses,
    IReviewStatusRepository reviewStatuses)
    : IQueryHandler<ListCatalogItemsQuery, IReadOnlyList<CatalogItemDto>>
{
    public async Task<IReadOnlyList<CatalogItemDto>> Handle(
        ListCatalogItemsQuery request,
        CancellationToken cancellationToken)
    {
        return request.Kind switch
        {
            CatalogKind.Category => Map(
                await categories.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.SubCategory => MapChildren(
                request.ParentId is null
                    ? await subCategories.ListAsync(request.ActiveOnly, cancellationToken)
                    : await subCategories.ListByCategoryIdAsync(
                        request.ParentId.Value,
                        request.ActiveOnly,
                        cancellationToken),
                x => x.CategoryId),
            CatalogKind.Brand => Map(
                await brands.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.Model => MapChildren(
                request.ParentId is null
                    ? await models.ListAsync(request.ActiveOnly, cancellationToken)
                    : await models.ListByBrandIdAsync(
                        request.ParentId.Value,
                        request.ActiveOnly,
                        cancellationToken),
                x => x.BrandId),
            CatalogKind.Country => Map(
                await countries.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.City => MapChildren(
                request.ParentId is null
                    ? await cities.ListAsync(request.ActiveOnly, cancellationToken)
                    : await cities.ListByCountryIdAsync(
                        request.ParentId.Value,
                        request.ActiveOnly,
                        cancellationToken),
                x => x.CountryId),
            CatalogKind.District => MapChildren(
                request.ParentId is null
                    ? await districts.ListAsync(request.ActiveOnly, cancellationToken)
                    : await districts.ListByCityIdAsync(
                        request.ParentId.Value,
                        request.ActiveOnly,
                        cancellationToken),
                x => x.CityId),
            CatalogKind.Currency => MapCurrencies(
                await currencies.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.Language => MapLanguages(
                await languages.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.Transmission => Map(
                await transmissions.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.FuelType => Map(
                await fuelTypes.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.VehicleType => Map(
                await vehicleTypes.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.EquipmentType => Map(
                await equipmentTypes.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.FeatureDefinition => MapFeatures(
                await features.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.Color => MapColors(
                await colors.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.DocumentType => Map(
                await documentTypes.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.MediaType => Map(
                await mediaTypes.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.RentalPeriodType => Map(
                await rentalPeriodTypes.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.PaymentMethod => Map(
                await paymentMethods.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.BookingStatus => Map(
                await bookingStatuses.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.AssetStatus => Map(
                await assetStatuses.ListAsync(request.ActiveOnly, cancellationToken)),
            CatalogKind.ReviewStatus => Map(
                await reviewStatuses.ListAsync(request.ActiveOnly, cancellationToken)),
            _ => throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BadRequest))
        };
    }

    private static IReadOnlyList<CatalogItemDto> Map(IEnumerable<CatalogEntity> items)
        => items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToArray();

    private static IReadOnlyList<CatalogItemDto> MapChildren<T>(
        IEnumerable<T> items,
        Func<T, Guid> parentSelector)
        where T : CatalogEntity
        => items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, parentSelector(x)))
            .ToArray();

    private static IReadOnlyList<CatalogItemDto> MapCurrencies(IEnumerable<Currency> items)
        => items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, symbol: x.Symbol, decimalPlaces: x.DecimalPlaces))
            .ToArray();

    private static IReadOnlyList<CatalogItemDto> MapLanguages(IEnumerable<Language> items)
        => items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, cultureName: x.CultureName))
            .ToArray();

    private static IReadOnlyList<CatalogItemDto> MapFeatures(IEnumerable<FeatureDefinition> items)
        => items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, groupCode: x.GroupCode))
            .ToArray();

    private static IReadOnlyList<CatalogItemDto> MapColors(IEnumerable<Color> items)
        => items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x, hexCode: x.HexCode))
            .ToArray();

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
