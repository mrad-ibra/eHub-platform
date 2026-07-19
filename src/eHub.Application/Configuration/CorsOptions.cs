namespace eHub.Application.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Browser origins allowed to call the API. Empty = CORS middleware not registered.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public AuthRateLimitOptions Auth { get; set; } = new();
}

public sealed class AuthRateLimitOptions
{
    public const string PolicyName = "auth";

    public int PermitLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 60;
}
