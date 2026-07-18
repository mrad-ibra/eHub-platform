using Asp.Versioning;
using eHub.Application.Catalog.Queries.ListCatalogItems;
using eHub.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/catalog")]
public sealed class CatalogController(ISender sender) : ControllerBase
{
    [HttpGet("categories")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Categories(CancellationToken ct)
        => List(CatalogKind.Category, null, ct);

    [HttpGet("subcategories")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> SubCategories(
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
        => List(CatalogKind.SubCategory, categoryId, ct);

    [HttpGet("brands")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Brands(CancellationToken ct)
        => List(CatalogKind.Brand, null, ct);

    [HttpGet("models")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Models(
        [FromQuery] Guid? brandId,
        CancellationToken ct)
        => List(CatalogKind.Model, brandId, ct);

    [HttpGet("countries")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Countries(CancellationToken ct)
        => List(CatalogKind.Country, null, ct);

    [HttpGet("cities")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Cities(
        [FromQuery] Guid? countryId,
        CancellationToken ct)
        => List(CatalogKind.City, countryId, ct);

    [HttpGet("districts")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Districts(
        [FromQuery] Guid? cityId,
        CancellationToken ct)
        => List(CatalogKind.District, cityId, ct);

    [HttpGet("currencies")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Currencies(CancellationToken ct)
        => List(CatalogKind.Currency, null, ct);

    [HttpGet("languages")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Languages(CancellationToken ct)
        => List(CatalogKind.Language, null, ct);

    [HttpGet("transmissions")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Transmissions(CancellationToken ct)
        => List(CatalogKind.Transmission, null, ct);

    [HttpGet("fuel-types")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> FuelTypes(CancellationToken ct)
        => List(CatalogKind.FuelType, null, ct);

    [HttpGet("vehicle-types")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> VehicleTypes(CancellationToken ct)
        => List(CatalogKind.VehicleType, null, ct);

    [HttpGet("equipment-types")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> EquipmentTypes(CancellationToken ct)
        => List(CatalogKind.EquipmentType, null, ct);

    [HttpGet("features")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Features(CancellationToken ct)
        => List(CatalogKind.FeatureDefinition, null, ct);

    [HttpGet("colors")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> Colors(CancellationToken ct)
        => List(CatalogKind.Color, null, ct);

    [HttpGet("document-types")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> DocumentTypes(CancellationToken ct)
        => List(CatalogKind.DocumentType, null, ct);

    [HttpGet("media-types")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> MediaTypes(CancellationToken ct)
        => List(CatalogKind.MediaType, null, ct);

    [HttpGet("rental-period-types")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> RentalPeriodTypes(CancellationToken ct)
        => List(CatalogKind.RentalPeriodType, null, ct);

    [HttpGet("payment-methods")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> PaymentMethods(CancellationToken ct)
        => List(CatalogKind.PaymentMethod, null, ct);

    [HttpGet("booking-statuses")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> BookingStatuses(CancellationToken ct)
        => List(CatalogKind.BookingStatus, null, ct);

    [HttpGet("asset-statuses")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> AssetStatuses(CancellationToken ct)
        => List(CatalogKind.AssetStatus, null, ct);

    [HttpGet("review-statuses")]
    public Task<ActionResult<IReadOnlyList<CatalogItemDto>>> ReviewStatuses(CancellationToken ct)
        => List(CatalogKind.ReviewStatus, null, ct);

    private async Task<ActionResult<IReadOnlyList<CatalogItemDto>>> List(
        CatalogKind kind,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListCatalogItemsQuery(kind, parentId),
            cancellationToken);
        return Ok(result);
    }
}
