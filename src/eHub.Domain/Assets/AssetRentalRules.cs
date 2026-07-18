namespace eHub.Domain.Assets;

public sealed class AssetRentalRules
{
    public int? MinRentalDays { get; private set; }
    public int? MaxRentalDays { get; private set; }
    public int? MinDriverAge { get; private set; }
    public bool RequiresLicense { get; private set; }
    public string? Notes { get; private set; }

    private AssetRentalRules()
    {
    }

    public static AssetRentalRules Create(
        int? minRentalDays = null,
        int? maxRentalDays = null,
        int? minDriverAge = null,
        bool requiresLicense = false,
        string? notes = null)
    {
        return new AssetRentalRules
        {
            MinRentalDays = minRentalDays,
            MaxRentalDays = maxRentalDays,
            MinDriverAge = minDriverAge,
            RequiresLicense = requiresLicense,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
