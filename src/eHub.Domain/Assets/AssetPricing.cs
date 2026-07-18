using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

public sealed class AssetPricing
{
    public Guid CurrencyId { get; private set; }
    public Guid RentalPeriodTypeId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? WeekendAmount { get; private set; }
    public decimal? WeeklyAmount { get; private set; }
    public decimal? MonthlyAmount { get; private set; }

    private AssetPricing()
    {
    }

    public static AssetPricing Create(
        Guid currencyId,
        Guid rentalPeriodTypeId,
        decimal amount,
        decimal? weekendAmount = null,
        decimal? weeklyAmount = null,
        decimal? monthlyAmount = null)
    {
        if (amount < 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetPriceInvalid));
        }

        return new AssetPricing
        {
            CurrencyId = AppGuard.NotEmpty(currencyId, nameof(currencyId)),
            RentalPeriodTypeId = AppGuard.NotEmpty(rentalPeriodTypeId, nameof(rentalPeriodTypeId)),
            Amount = amount,
            WeekendAmount = weekendAmount,
            WeeklyAmount = weeklyAmount,
            MonthlyAmount = monthlyAmount
        };
    }
}
