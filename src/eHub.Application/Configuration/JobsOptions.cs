namespace eHub.Application.Configuration;

public sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    public ExpirePendingBookingsOptions ExpirePendingBookings { get; set; } = new();
}

public sealed class ExpirePendingBookingsOptions
{
    /// <summary>When false, the hosted expire worker does not run.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Delay between successful batches.</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>Max bookings expired per tick.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Backoff after a failed batch (seconds).</summary>
    public int RetryDelaySeconds { get; set; } = 15;
}
