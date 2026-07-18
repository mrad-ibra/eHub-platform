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
        var store = new InMemoryCatalogStore();
        var vehicles = Category.Create("VEHICLE", "Vehicles", Now);
        var equipment = Category.Create("EQUIPMENT", "Equipment", Now);
        await store.AddAsync(vehicles);
        await store.AddAsync(equipment);
        await store.AddAsync(SubCategory.Create(vehicles.Id, "CAR", "Car", Now));
        await store.AddAsync(SubCategory.Create(equipment.Id, "EXCAVATOR", "Excavator", Now));

        var handler = new ListCatalogItemsQueryHandler(store);
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
        var store = new InMemoryCatalogStore();
        await store.AddAsync(Currency.Create("AZN", "Manat", "₼", Now));

        var handler = new ListCatalogItemsQueryHandler(store);
        var result = await handler.Handle(
            new ListCatalogItemsQuery(CatalogKind.Currency),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Symbol.Should().Be("₼");
        result[0].DecimalPlaces.Should().Be(2);
    }
}
