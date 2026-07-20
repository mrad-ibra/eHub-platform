namespace eHub.Application.Configuration;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>When false, <c>TEST</c> provider requests are rejected (production default).</summary>
    public bool AllowTestProvider { get; set; }
}
