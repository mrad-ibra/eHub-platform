using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Domain.Assets;

public sealed class AssetSecurityDeposit
{
    public bool Required { get; private set; }
    public decimal? Amount { get; private set; }
    public Guid? CurrencyId { get; private set; }

    private AssetSecurityDeposit()
    {
    }

    public static AssetSecurityDeposit None()
        => new() { Required = false };

    public static AssetSecurityDeposit Create(decimal amount, Guid currencyId)
    {
        if (amount < 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetPriceInvalid));
        }

        return new AssetSecurityDeposit
        {
            Required = true,
            Amount = amount,
            CurrencyId = currencyId
        };
    }
}
