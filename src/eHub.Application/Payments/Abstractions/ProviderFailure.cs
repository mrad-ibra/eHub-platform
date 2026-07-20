using eHub.Application.Payments;

namespace eHub.Application.Payments.Abstractions;

/// <summary>
/// Normalized provider failure. Raw exceptions and secrets must not propagate past Infrastructure.
/// </summary>
public sealed record ProviderFailure(
    PaymentFailureReason Reason,
    string? ProviderCode,
    string? SafeMessage,
    bool IsRetryable);

public static class ProviderFailureExtensions
{
    public static string ToDomainCode(this ProviderFailure? failure)
        => failure?.Reason.ToString() ?? PaymentFailureReason.Unknown.ToString();
}
