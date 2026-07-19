using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Bookings;

/// <summary>Inclusive calendar-day rental period.</summary>
public sealed class BookingPeriod : IEquatable<BookingPeriod>
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    private BookingPeriod()
    {
        StartDate = default;
        EndDate = default;
    }

    private BookingPeriod(DateOnly startDate, DateOnly endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public int Days => EndDate.DayNumber - StartDate.DayNumber + 1;

    public static BookingPeriod Create(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingPeriodInvalid));
        }

        return new BookingPeriod(startDate, endDate);
    }

    /// <summary>Inclusive overlap of two periods.</summary>
    public bool Overlaps(BookingPeriod other)
        => StartDate <= other.EndDate && EndDate >= other.StartDate;

    public bool Overlaps(DateOnly start, DateOnly end)
        => StartDate <= end && EndDate >= start;

    /// <summary>Occupied calendar end including preparation buffer days after EndDate.</summary>
    public DateOnly OccupiedEnd(int bufferDays)
    {
        if (bufferDays < 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingBufferInvalid));
        }

        return EndDate.AddDays(bufferDays);
    }

    public bool OverlapsOccupied(BookingPeriod other, int otherBufferDays)
        => Overlaps(other.StartDate, other.OccupiedEnd(otherBufferDays));

    public bool Equals(BookingPeriod? other)
        => other is not null && StartDate == other.StartDate && EndDate == other.EndDate;

    public override bool Equals(object? obj) => Equals(obj as BookingPeriod);

    public override int GetHashCode() => HashCode.Combine(StartDate, EndDate);
}
