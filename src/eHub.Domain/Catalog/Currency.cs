using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Domain.Catalog;

public sealed class Currency : CatalogEntity
{
    public string Symbol { get; private set; } = string.Empty;
    public int DecimalPlaces { get; private set; } = 2;

    private Currency()
    {
    }

    public static Currency Create(
        string code,
        string name,
        string symbol,
        DateTime nowUtc,
        int decimalPlaces = 2,
        int sortOrder = 0,
        bool isSystem = true,
        Guid? createdBy = null)
    {
        if (decimalPlaces is < 0 or > 6)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.CatalogDecimalPlacesOutOfRange));
        }

        var entity = new Currency
        {
            Symbol = AppGuard.NotEmpty(symbol, nameof(symbol)).Trim(),
            DecimalPlaces = decimalPlaces
        };
        entity.Initialize(code, name, nowUtc, sortOrder, isSystem, createdBy);
        return entity;
    }

    public void UpdateMoneyFormat(string symbol, int decimalPlaces, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        if (decimalPlaces is < 0 or > 6)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.CatalogDecimalPlacesOutOfRange));
        }

        Symbol = AppGuard.NotEmpty(symbol, nameof(symbol)).Trim();
        DecimalPlaces = decimalPlaces;
        SetUpdatedAudit(nowUtc, updatedBy);
    }
}
