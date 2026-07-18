namespace eHub.Domain.Bookings;

public sealed class BookingTerms
{
    public int? MinRentalDays { get; private set; }
    public int? MaxRentalDays { get; private set; }
    public int? MinDriverAge { get; private set; }
    public bool RequiresLicense { get; private set; }
    public string? Notes { get; private set; }
    public int BufferDays { get; private set; }

    private BookingTerms()
    {
    }

    public static BookingTerms Create(
        int bufferDays,
        int? minRentalDays = null,
        int? maxRentalDays = null,
        int? minDriverAge = null,
        bool requiresLicense = false,
        string? notes = null)
    {
        return new BookingTerms
        {
            BufferDays = bufferDays,
            MinRentalDays = minRentalDays,
            MaxRentalDays = maxRentalDays,
            MinDriverAge = minDriverAge,
            RequiresLicense = requiresLicense,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
