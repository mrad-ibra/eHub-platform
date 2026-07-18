namespace eHub.Application.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string InstanceName { get; set; } = "ehub:";

    public int DefaultDatabase { get; set; }
}
