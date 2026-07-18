namespace eHub.Application.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public int CommandTimeoutSeconds { get; set; } = 30;
}
