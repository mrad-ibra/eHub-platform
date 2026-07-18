namespace eHub.Application.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; } = true;

    /// <summary>When true, login requires a confirmed email address.</summary>
    public bool RequireConfirmation { get; set; } = true;

    public string FromAddress { get; set; } = "noreply@ehub.local";
    public string FromName { get; set; } = "eHub";
    public int VerificationTokenExpiryHours { get; set; } = 24;
    public int PasswordResetTokenExpiryHours { get; set; } = 1;

    /// <summary>Frontend path appended to <see cref="SiteOptions.PublicAppUrl"/>.</summary>
    public string VerificationPath { get; set; } = "/verify-email";

    /// <summary>Frontend path appended to <see cref="SiteOptions.PublicAppUrl"/>.</summary>
    public string PasswordResetPath { get; set; } = "/reset-password";
}

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string PublicAppUrl { get; set; } = "http://localhost:3000";
}
