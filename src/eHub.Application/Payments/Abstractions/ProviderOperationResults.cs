namespace eHub.Application.Payments.Abstractions;

public sealed class ProviderCreatePaymentResult
{
    public bool IsSuccess { get; }
    public string? ProviderPaymentId { get; }
    public string? RedirectUrl { get; }
    public ProviderFailure? Failure { get; }

    private ProviderCreatePaymentResult(
        bool isSuccess,
        string? providerPaymentId,
        string? redirectUrl,
        ProviderFailure? failure)
    {
        IsSuccess = isSuccess;
        ProviderPaymentId = providerPaymentId;
        RedirectUrl = redirectUrl;
        Failure = failure;
    }

    public static ProviderCreatePaymentResult Success(string providerPaymentId, string? redirectUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerPaymentId);
        return new(true, providerPaymentId, redirectUrl, null);
    }

    public static ProviderCreatePaymentResult Failed(ProviderFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new(false, null, null, failure);
    }
}

public sealed class ProviderRefundResult
{
    public bool IsSuccess { get; }
    public string? ProviderRefundId { get; }
    public ProviderFailure? Failure { get; }

    private ProviderRefundResult(bool isSuccess, string? providerRefundId, ProviderFailure? failure)
    {
        IsSuccess = isSuccess;
        ProviderRefundId = providerRefundId;
        Failure = failure;
    }

    public static ProviderRefundResult Success(string providerRefundId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRefundId);
        return new(true, providerRefundId, null);
    }

    public static ProviderRefundResult Failed(ProviderFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new(false, null, failure);
    }
}

public sealed class ProviderCancelResult
{
    public bool IsSuccess { get; }
    public ProviderFailure? Failure { get; }

    private ProviderCancelResult(bool isSuccess, ProviderFailure? failure)
    {
        IsSuccess = isSuccess;
        Failure = failure;
    }

    public static ProviderCancelResult Success() => new(true, null);

    public static ProviderCancelResult Failed(ProviderFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new(false, failure);
    }
}
