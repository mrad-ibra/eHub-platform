using eHub.Domain.Exceptions;
using eHub.Domain.Bookings;
using eHub.Localization;

namespace eHub.Domain.Assets;

public sealed class AssetRentalRules
{
    public int? MinRentalDays { get; private set; }
    public int? MaxRentalDays { get; private set; }
    public int? MinDriverAge { get; private set; }
    public bool RequiresLicense { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Preparation buffer after rental end (calendar days). Null = use platform default at booking time.
    /// </summary>
    public int? PreparationBufferDays { get; private set; }

    private AssetRentalRules()
    {
    }

    public static AssetRentalRules Create(
        int? minRentalDays = null,
        int? maxRentalDays = null,
        int? minDriverAge = null,
        bool requiresLicense = false,
        string? notes = null,
        int? preparationBufferDays = null)
    {
        if (preparationBufferDays is { } buffer)
        {
            if (buffer < 0 || buffer > BookingDefaults.MaxPreparationBufferDays)
            {
                throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingBufferInvalid));
            }
        }

        return new AssetRentalRules
        {
            MinRentalDays = minRentalDays,
            MaxRentalDays = maxRentalDays,
            MinDriverAge = minDriverAge,
            RequiresLicense = requiresLicense,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PreparationBufferDays = preparationBufferDays
        };
    }
}
