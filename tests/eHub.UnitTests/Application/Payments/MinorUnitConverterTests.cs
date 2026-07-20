using eHub.Application.Payments;

namespace eHub.UnitTests.Application.Payments;

public sealed class MinorUnitConverterTests
{
    private readonly MinorUnitConverter _converter = new();

    [Theory]
    [InlineData("AZN", 10.50, 1050)]
    [InlineData("USD", 1.00, 100)]
    [InlineData("EUR", 0.01, 1)]
    public void ToMinorUnits_SupportedCurrencies(string code, decimal amount, long expected)
        => _converter.ToMinorUnits(amount, code).Should().Be(expected);

    [Fact]
    public void FromMinorUnits_RoundTrips()
        => _converter.FromMinorUnits(1050, "AZN").Should().Be(10.50m);

    [Fact]
    public void UnsupportedCurrency_Throws()
    {
        var act = () => _converter.ToMinorUnits(10m, "JPY");
        act.Should().Throw<eHub.Domain.Exceptions.ValidationFailedException>();
    }
}
