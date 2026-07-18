using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

public sealed class AssetAvailabilityBlock
{
    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AssetAvailabilityBlock()
    {
    }

    internal static AssetAvailabilityBlock Create(
        Guid assetId,
        DateOnly startDate,
        DateOnly endDate,
        DateTime nowUtc,
        string? note = null)
    {
        if (endDate < startDate)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetAvailabilityRangeInvalid));
        }

        return new AssetAvailabilityBlock
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            StartDate = startDate,
            EndDate = endDate,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAtUtc = nowUtc
        };
    }

    public bool Overlaps(DateOnly start, DateOnly end)
        => StartDate <= end && EndDate >= start;
}
