using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

/// <summary>
/// Commercial / listing terms for an Asset (pricing, location, rules, deposit, support).
/// </summary>
public sealed class AssetCommercialTerms
{
    public AssetPricing? Pricing { get; private set; }
    public AssetLocation? Location { get; private set; }
    public AssetRentalRules? RentalRules { get; private set; }
    public AssetSecurityDeposit SecurityDeposit { get; private set; } = AssetSecurityDeposit.None();
    public AssetSupportOptions Support { get; private set; } = AssetSupportOptions.Create();

    public bool HasPricing => Pricing is not null;
    public bool HasLocation => Location is not null;

    internal void SetPricing(AssetPricing pricing)
        => Pricing = pricing
            ?? throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, nameof(pricing)));

    internal void SetLocation(AssetLocation location)
        => Location = location
            ?? throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, nameof(location)));

    internal void SetRentalRules(AssetRentalRules rules)
        => RentalRules = rules
            ?? throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, nameof(rules)));

    internal void SetSecurityDeposit(AssetSecurityDeposit? deposit)
        => SecurityDeposit = deposit ?? AssetSecurityDeposit.None();

    internal void SetSupport(AssetSupportOptions? options)
        => Support = options ?? AssetSupportOptions.Create();

    internal void EnsureReadyForPublish(bool hasImage)
    {
        if (!HasPricing)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetPricingRequired));
        }

        if (!HasLocation)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetLocationRequired));
        }

        if (!hasImage)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetImageRequired));
        }
    }
}
