namespace eHub.Domain.Payments;

/// <summary>Provider interaction audit row (normalized — raw payload optional for ops only).</summary>
public sealed class PaymentAttempt
{
    public Guid Id { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Result { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public string? Detail { get; private set; }
    public DateTime AtUtc { get; private set; }

    private PaymentAttempt()
    {
    }

    internal static PaymentAttempt Create(
        PaymentAttemptKind kind,
        PaymentAttemptResult result,
        DateTime atUtc,
        string? providerReference = null,
        string? detail = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Kind = kind.Value,
            Result = result.Value,
            ProviderReference = Truncate(providerReference, PaymentDefaults.MaxProviderPaymentIdLength),
            Detail = Truncate(detail, PaymentDefaults.MaxFailureReasonLength),
            AtUtc = atUtc
        };

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
