using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Common;

/// <summary>Monetary amount with currency identity (Catalog Currency Id).</summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Guid CurrencyId { get; }

    private Money()
    {
    }

    private Money(decimal amount, Guid currencyId)
    {
        Amount = amount;
        CurrencyId = currencyId;
    }

    public static Money Create(decimal amount, Guid currencyId)
    {
        if (amount < 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.MoneyAmountInvalid));
        }

        // Marketplace amounts: at most 4 decimal places (currency catalog may tighten later).
        if (decimal.Round(amount, 4) != amount)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.MoneyAmountInvalid));
        }

        return new Money(amount, AppGuard.NotEmpty(currencyId, nameof(currencyId)));
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Create(Amount + other.Amount, CurrencyId);
    }

    public Money Multiply(int factor)
    {
        if (factor < 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.MoneyAmountInvalid));
        }

        return Create(Amount * factor, CurrencyId);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return Create(Amount - other.Amount, CurrencyId);
    }

    public static Money Zero(Guid currencyId) => Create(0m, currencyId);

    public bool Equals(Money? other)
        => other is not null
           && Amount == other.Amount
           && CurrencyId == other.CurrencyId;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, CurrencyId);

    private void EnsureSameCurrency(Money other)
    {
        if (CurrencyId != other.CurrencyId)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.MoneyCurrencyMismatch));
        }
    }
}
