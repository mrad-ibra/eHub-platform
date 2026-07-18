using eHub.Application.Common.Messaging;
using eHub.Domain.Catalog;

namespace eHub.Application.Catalog.Queries.ListCatalogItems;

public sealed record ListCatalogItemsQuery(
    CatalogKind Kind,
    Guid? ParentId = null,
    bool ActiveOnly = true) : IQuery<IReadOnlyList<CatalogItemDto>>;

public sealed record CatalogItemDto(
    Guid Id,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive,
    Guid? ParentId = null,
    string? Symbol = null,
    int? DecimalPlaces = null,
    string? CultureName = null,
    string? GroupCode = null,
    string? HexCode = null);
