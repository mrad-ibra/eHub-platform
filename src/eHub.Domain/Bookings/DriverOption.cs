using eHub.Domain.Common;

namespace eHub.Domain.Bookings;

public sealed class DriverOption
{
    public bool Requested { get; private set; }
    public Money? Fee { get; private set; }

    private DriverOption()
    {
    }

    public static DriverOption None() => new() { Requested = false };

    public static DriverOption Request(Money fee)
        => new() { Requested = true, Fee = fee };
}
