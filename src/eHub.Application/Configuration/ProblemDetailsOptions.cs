namespace eHub.Application.Configuration;

public sealed class ProblemDetailsOptions
{
    public const string SectionName = "ProblemDetails";

    /// <summary>
    /// Base URI used for the RFC 7807 <c>type</c> field, e.g. https://ehub.local/errors/{code}.
    /// </summary>
    public string ErrorBaseUrl { get; set; } = "https://ehub.local/errors";
}
