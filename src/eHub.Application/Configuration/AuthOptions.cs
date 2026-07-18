namespace eHub.Application.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public JwtOptions Jwt { get; set; } = new();
    public RefreshTokenOptions RefreshToken { get; set; } = new();
    public AuthSeedOptions Seed { get; set; } = new();
    public AccountKindRoleMapOptions AccountKindRoles { get; set; } = new();
    public LoginHistoryOptions LoginHistory { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ehub";
    public string Audience { get; set; } = "ehub";
    public int AccessTokenMinutes { get; set; } = 60;
    public int ClockSkewMinutes { get; set; } = 1;
}

public sealed class RefreshTokenOptions
{
    public int LifetimeDays { get; set; } = 14;
    public int TokenSizeBytes { get; set; } = 64;
}

public sealed class AuthSeedOptions
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "eHub Admin";
}

public sealed class AccountKindRoleMapOptions
{
    public string Personal { get; set; } = "Customer";
    public string Business { get; set; } = "Host";
    public string Admin { get; set; } = "Admin";
}

public sealed class LoginHistoryOptions
{
    public int DefaultTake { get; set; } = 50;
    public int MaxTake { get; set; } = 100;
}
