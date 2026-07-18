using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

/// <summary>
/// Internal availability calendar for the Asset aggregate.
/// </summary>
public sealed class AssetAvailability
{
    private readonly List<AssetAvailabilityBlock> _blocks = [];

    public IReadOnlyCollection<AssetAvailabilityBlock> Blocks => _blocks.AsReadOnly();

    internal AssetAvailabilityBlock Block(
        Guid assetId,
        DateOnly startDate,
        DateOnly endDate,
        DateTime nowUtc,
        string? note = null)
    {
        if (_blocks.Any(block => block.Overlaps(startDate, endDate)))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.AssetAvailabilityOverlap));
        }

        var block = AssetAvailabilityBlock.Create(assetId, startDate, endDate, nowUtc, note);
        _blocks.Add(block);
        return block;
    }

    internal void Remove(Guid blockId)
    {
        var block = _blocks.FirstOrDefault(b => b.Id == blockId)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.AssetAvailabilityNotFound));
        _blocks.Remove(block);
    }
}
