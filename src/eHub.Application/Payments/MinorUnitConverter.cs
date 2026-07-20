using eHub.Application.Payments.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Payments;

/// <summary>
/// ISO-4217 minor-unit conversion for currencies supported by payment providers (Phase A: AZN, USD, EUR).
/// </summary>
public sealed class MinorUnitConverter : IMinorUnitConverter
{
    private static readonly Dictionary<string, int> DecimalPlaces = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AZN"] = 2,
        ["USD"] = 2,
        ["EUR"] = 2
    };

    public bool IsSupported(string currencyCode)
        => !string.IsNullOrWhiteSpace(currencyCode)
           && DecimalPlaces.ContainsKey(currencyCode.Trim());

    public long ToMinorUnits(decimal amount, string currencyCode)
    {
        var places = RequirePlaces(currencyCode);
        var factor = (decimal)Math.Pow(10, places);
        return (long)decimal.Round(amount * factor, 0, MidpointRounding.AwayFromZero);
    }

    public decimal FromMinorUnits(long minorUnits, string currencyCode)
    {
        var places = RequirePlaces(currencyCode);
        var factor = (decimal)Math.Pow(10, places);
        return minorUnits / factor;
    }

    private static int RequirePlaces(string currencyCode)
    {
        var code = currencyCode?.Trim() ?? string.Empty;
        if (!DecimalPlaces.TryGetValue(code, out var places))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentCurrencyUnsupported));
        }

        return places;
    }
}
