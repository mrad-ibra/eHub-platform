using eHub.Domain.Catalog;

namespace eHub.UnitTests.Domain.Catalog;

public sealed class CatalogEntityTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Category_Create_NormalizesCode()
    {
        var category = Category.Create(" vehicle ", "Vehicles", Now, 1);

        category.Code.Should().Be("VEHICLE");
        category.Name.Should().Be("Vehicles");
        category.IsActive.Should().BeTrue();
        category.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void SubCategory_RequiresParent()
    {
        var category = Category.Create("VEHICLE", "Vehicles", Now);
        var sub = SubCategory.Create(category.Id, "CAR", "Car", Now);

        sub.CategoryId.Should().Be(category.Id);
        sub.Code.Should().Be("CAR");
    }

    [Fact]
    public void Currency_StoresSymbolAndDecimals()
    {
        var currency = Currency.Create("AZN", "Manat", "₼", Now, 2);

        currency.Symbol.Should().Be("₼");
        currency.DecimalPlaces.Should().Be(2);
    }

    [Fact]
    public void Color_NormalizesHex()
    {
        var color = Color.Create("RED", "Red", Now, "ff0000");

        color.HexCode.Should().Be("#FF0000");
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var item = FuelType.Create("PETROL", "Petrol", Now);
        item.Deactivate(Now.AddMinutes(1));

        item.IsActive.Should().BeFalse();
    }
}
