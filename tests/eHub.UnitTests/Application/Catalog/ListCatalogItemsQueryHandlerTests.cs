using eHub.Application.Catalog.Abstractions;
using eHub.Application.Catalog.Queries.ListCatalogItems;
using eHub.Domain.Catalog;
using eHub.Infrastructure.Persistence;

namespace eHub.UnitTests.Application.Catalog;

public sealed class ListCatalogItemsQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_FiltersSubCategoriesByParent()
    {
        var catalog = new InMemoryCatalogPersistence();
        var vehicles = Category.Create("VEHICLE", "Vehicles", Now);
        var equipment = Category.Create("EQUIPMENT", "Equipment", Now);
        await ((ICategoryRepository)catalog).AddAsync(vehicles);
        await ((ICategoryRepository)catalog).AddAsync(equipment);
        await ((ISubCategoryRepository)catalog).AddAsync(SubCategory.Create(vehicles.Id, "CAR", "Car", Now));
        await ((ISubCategoryRepository)catalog).AddAsync(SubCategory.Create(equipment.Id, "EXCAVATOR", "Excavator", Now));

        var handler = CreateHandler(catalog);
        var result = await handler.Handle(
            new ListCatalogItemsQuery(CatalogKind.SubCategory, vehicles.Id),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Code.Should().Be("CAR");
        result[0].ParentId.Should().Be(vehicles.Id);
    }

    [Fact]
    public async Task Handle_ReturnsCurrencyExtras()
    {
        var catalog = new InMemoryCatalogPersistence();
        await ((ICurrencyRepository)catalog).AddAsync(Currency.Create("AZN", "Manat", "₼", Now));

        var handler = CreateHandler(catalog);
        var result = await handler.Handle(
            new ListCatalogItemsQuery(CatalogKind.Currency),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Symbol.Should().Be("₼");
        result[0].DecimalPlaces.Should().Be(2);
    }

    private static ListCatalogItemsQueryHandler CreateHandler(InMemoryCatalogPersistence catalog)
        => new(
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog,
            catalog);
}
