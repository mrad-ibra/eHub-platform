namespace eHub.Application.Payments.Abstractions;

/// <summary>
/// Converts major-unit amounts to provider minor units (e.g. 10.50 AZN → 1050).
/// Supported currencies are explicitly limited for Phase A.
/// </summary>
public interface IMinorUnitConverter
{
    long ToMinorUnits(decimal amount, string currencyCode);

    decimal FromMinorUnits(long minorUnits, string currencyCode);

    bool IsSupported(string currencyCode);
}
