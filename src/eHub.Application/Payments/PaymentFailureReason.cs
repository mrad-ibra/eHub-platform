namespace eHub.Application.Payments;

/// <summary>
/// Provider-independent payment failure taxonomy. Application and Domain use these codes only —
/// never raw Stripe/Payriff error strings (ACL).
/// </summary>
public enum PaymentFailureReason
{
    Unknown = 0,
    CardDeclined,
    InsufficientFunds,
    ExpiredCard,
    InvalidCard,
    AuthenticationRequired,
    ProviderUnavailable,
    Timeout,
    RateLimited,
    InvalidRequest
}
