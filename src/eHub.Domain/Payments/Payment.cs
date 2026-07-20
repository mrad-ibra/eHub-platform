using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments.Events;
using eHub.Localization;

namespace eHub.Domain.Payments;

/// <summary>
/// Payment aggregate root. Separate from Booking (id-only). Amount is a Booking TotalPrice snapshot (L1).
/// Domain never calls Booking — consumers react to Outbox events (L9).
/// </summary>
public sealed class Payment : AggregateRoot
{
    private readonly List<PaymentTimelineEntry> _timeline = [];
    private readonly List<PaymentStatusHistoryEntry> _statusHistory = [];
    private readonly List<PaymentAttempt> _attempts = [];
    private readonly List<Refund> _refunds = [];

    public Guid BookingId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Money RefundedAmount { get; private set; } = null!;
    public PaymentProviderCode Provider { get; private set; } = PaymentProviderCode.Test;
    public string? ProviderPaymentId { get; private set; }
    public PaymentStatusCode Status { get; private set; } = PaymentStatusCode.Created;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    /// <summary>Optimistic concurrency / mutation counter.</summary>
    public int AggregateVersion { get; private set; }

    public Money RemainingRefundable => Amount.Subtract(RefundedAmount);

    public IReadOnlyCollection<PaymentTimelineEntry> Timeline => _timeline;
    public IReadOnlyCollection<PaymentStatusHistoryEntry> StatusHistory => _statusHistory;
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts;
    public IReadOnlyCollection<Refund> Refunds => _refunds;

    private Payment()
    {
    }

    /// <summary>
    /// Creates Payment in <see cref="PaymentStatusCode.Created"/> with amount copied from Booking snapshot.
    /// </summary>
    public static Payment Create(
        Guid bookingId,
        Money amountFromBooking,
        PaymentProviderCode provider,
        string idempotencyKey,
        DateTime nowUtc)
    {
        AppGuard.NotEmpty(bookingId, nameof(bookingId));
        ArgumentNullException.ThrowIfNull(amountFromBooking);
        ArgumentNullException.ThrowIfNull(provider);

        if (amountFromBooking.Amount <= 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentAmountInvalid));
        }

        var key = AppGuard.NotEmpty(idempotencyKey, nameof(idempotencyKey)).Trim();
        if (key.Length > PaymentDefaults.MaxIdempotencyKeyLength)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentIdempotencyKeyInvalid));
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Amount = amountFromBooking,
            RefundedAmount = Money.Zero(amountFromBooking.CurrencyId),
            Provider = provider,
            IdempotencyKey = key,
            ExpiresAtUtc = nowUtc.Add(PaymentDefaults.PaymentWindow)
        };

        payment.SetCreatedAudit(nowUtc, null);
        payment.ApplyTransition(
            PaymentStatusCode.Created,
            nowUtc,
            null,
            "Created",
            "Payment created from booking snapshot.",
            reason: null);

        payment.Raise(new PaymentCreated(
            payment.Id,
            payment.BookingId,
            payment.Amount.Amount,
            payment.Amount.CurrencyId,
            payment.Provider.Value,
            payment.Status.Value,
            nowUtc));

        return payment;
    }

    /// <summary>Provider session opened — moves Created → Pending.</summary>
    public void MarkPending(string? providerPaymentId, DateTime nowUtc)
    {
        if (Status == PaymentStatusCode.Pending)
        {
            MaybeSetProviderPaymentId(providerPaymentId);
            return;
        }

        EnsureTransitionFrom(PaymentStatusCode.Created);

        MaybeSetProviderPaymentId(providerPaymentId);
        RecordAttempt(
            PaymentAttemptKind.Create,
            PaymentAttemptResult.Pending,
            nowUtc,
            providerPaymentId,
            detail: null);

        ApplyTransition(
            PaymentStatusCode.Pending,
            nowUtc,
            null,
            "Pending",
            "Provider payment session opened.",
            reason: null);

        Raise(new PaymentPending(Id, BookingId, ProviderPaymentId, nowUtc));
    }

    /// <summary>Auth-only hold (future capture path).</summary>
    public void MarkAuthorized(DateTime nowUtc)
    {
        if (Status == PaymentStatusCode.Authorized)
        {
            return;
        }

        EnsureTransitionFrom(PaymentStatusCode.Pending);

        RecordAttempt(PaymentAttemptKind.Capture, PaymentAttemptResult.Pending, nowUtc);
        ApplyTransition(
            PaymentStatusCode.Authorized,
            nowUtc,
            null,
            "Authorized",
            "Payment authorized (not captured).",
            reason: null);

        Raise(new PaymentAuthorized(Id, BookingId, nowUtc));
    }

    /// <summary>
    /// Capture confirmed. Only from Pending/Authorized. Terminal Payment statuses reject success (L4).
    /// </summary>
    public void MarkSucceeded(DateTime paidAtUtc, string? providerPaymentId = null)
    {
        if (Status == PaymentStatusCode.Succeeded)
        {
            MaybeSetProviderPaymentId(providerPaymentId);
            return;
        }

        if (!Status.CanBecomeSucceeded)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentInvalidStatusTransition));
        }

        MaybeSetProviderPaymentId(providerPaymentId);
        PaidAtUtc = paidAtUtc;
        FailureReason = null;
        ExpiresAtUtc = null;

        RecordAttempt(
            PaymentAttemptKind.Webhook,
            PaymentAttemptResult.Succeeded,
            paidAtUtc,
            providerPaymentId);

        ApplyTransition(
            PaymentStatusCode.Succeeded,
            paidAtUtc,
            null,
            "Paid",
            "Payment succeeded — capture confirmed.",
            reason: null);

        Raise(new PaymentSucceeded(
            Id,
            BookingId,
            Amount.Amount,
            Amount.CurrencyId,
            paidAtUtc,
            paidAtUtc));
    }

    public void MarkFailed(string failureReason, DateTime nowUtc)
    {
        if (Status == PaymentStatusCode.Failed)
        {
            return;
        }

        EnsureTransitionFrom(PaymentStatusCode.Pending, PaymentStatusCode.Authorized);

        FailureReason = NormalizeFailureReason(failureReason);
        ExpiresAtUtc = null;

        RecordAttempt(
            PaymentAttemptKind.Webhook,
            PaymentAttemptResult.Failed,
            nowUtc,
            detail: FailureReason);

        ApplyTransition(
            PaymentStatusCode.Failed,
            nowUtc,
            null,
            "Failed",
            $"Payment failed: {FailureReason}",
            FailureReason);

        Raise(new PaymentFailed(Id, BookingId, FailureReason!, nowUtc));
    }

    public void MarkCancelled(DateTime nowUtc, Guid? actorId = null)
    {
        if (Status == PaymentStatusCode.Cancelled)
        {
            return;
        }

        EnsureTransitionFrom(
            PaymentStatusCode.Created,
            PaymentStatusCode.Pending,
            PaymentStatusCode.Authorized);

        ExpiresAtUtc = null;
        RecordAttempt(PaymentAttemptKind.Cancel, PaymentAttemptResult.Succeeded, nowUtc);

        ApplyTransition(
            PaymentStatusCode.Cancelled,
            nowUtc,
            actorId,
            "Cancelled",
            "Payment cancelled before capture.",
            reason: null);

        Raise(new PaymentCancelled(Id, BookingId, nowUtc));
    }

    public void MarkExpired(DateTime nowUtc)
    {
        if (Status == PaymentStatusCode.Expired)
        {
            return;
        }

        EnsureTransitionFrom(
            PaymentStatusCode.Created,
            PaymentStatusCode.Pending,
            PaymentStatusCode.Authorized);

        if (ExpiresAtUtc is null || nowUtc < ExpiresAtUtc)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentNotExpiredYet));
        }

        ExpiresAtUtc = null;
        RecordAttempt(PaymentAttemptKind.Reconcile, PaymentAttemptResult.Failed, nowUtc, detail: "Window elapsed");

        ApplyTransition(
            PaymentStatusCode.Expired,
            nowUtc,
            null,
            "Expired",
            "Payment window elapsed without success.",
            reason: null);

        Raise(new PaymentExpired(Id, BookingId, nowUtc));
    }

    /// <summary>
    /// Webhook or internal path: request + settle in one step (provider already confirmed).
    /// </summary>
    public Refund AddRefund(
        Money amount,
        string reason,
        DateTime nowUtc,
        Guid? actorId = null,
        string? providerRefundId = null,
        string? idempotencyKey = null)
    {
        var key = idempotencyKey ?? $"wh_{Guid.NewGuid():N}";
        var refund = BeginRefund(amount, reason, key, nowUtc, actorId);
        CompleteRefund(refund.Id, providerRefundId, nowUtc);
        return refund;
    }

    /// <summary>Starts a refund row in Requested state (API / provider path).</summary>
    public Refund BeginRefund(
        Money amount,
        string reason,
        string idempotencyKey,
        DateTime nowUtc,
        Guid? actorId = null)
    {
        EnsureRefundAmount(amount);

        if (Refunds.Any(r => r.IdempotencyKey == idempotencyKey.Trim()))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentRefundIdempotencyPayloadMismatch));
        }

        var refund = Refund.Request(Id, amount, reason, idempotencyKey, nowUtc, actorId);
        _refunds.Add(refund);

        Raise(new RefundRequested(
            Id,
            BookingId,
            refund.Id,
            amount.Amount,
            amount.CurrencyId,
            refund.IdempotencyKey,
            nowUtc));

        return refund;
    }

    /// <summary>Settles a previously requested refund after provider success.</summary>
    public void CompleteRefund(Guid refundId, string? providerRefundId, DateTime nowUtc)
    {
        var refund = FindOpenRefund(refundId);
        refund.MarkSucceeded(nowUtc, providerRefundId);

        RefundedAmount = RefundedAmount.Add(refund.Amount);

        RecordAttempt(
            PaymentAttemptKind.Refund,
            PaymentAttemptResult.Succeeded,
            nowUtc,
            providerRefundId,
            detail: refund.Reason);

        var fully = RefundedAmount.Amount >= Amount.Amount;
        var target = fully ? PaymentStatusCode.Refunded : PaymentStatusCode.PartiallyRefunded;

        if (Status != target)
        {
            ApplyTransition(
                target,
                nowUtc,
                refund.RequestedByActorId,
                fully ? "Refunded" : "PartiallyRefunded",
                fully
                    ? "Payment fully refunded."
                    : $"Partial refund settled ({refund.Amount.Amount}).",
                refund.Reason);
        }
        else
        {
            _timeline.Add(PaymentTimelineEntry.Create(
                "PartiallyRefunded",
                $"Further partial refund settled ({refund.Amount.Amount}).",
                nowUtc,
                refund.RequestedByActorId));
            SetUpdatedAudit(nowUtc, refund.RequestedByActorId);
            MarkChanged();
        }

        Raise(new RefundSucceeded(
            Id,
            BookingId,
            refund.Id,
            refund.Amount.Amount,
            refund.Amount.CurrencyId,
            nowUtc));

        if (fully)
        {
            Raise(new PaymentFullyRefunded(
                Id,
                BookingId,
                Amount.Amount,
                Amount.CurrencyId,
                nowUtc));
        }
        else
        {
            Raise(new PaymentPartiallyRefunded(
                Id,
                BookingId,
                RefundedAmount.Amount,
                RemainingRefundable.Amount,
                Amount.CurrencyId,
                nowUtc));
        }

        Raise(new PaymentRefunded(
            Id,
            BookingId,
            refund.Id,
            refund.Amount.Amount,
            refund.Amount.CurrencyId,
            fully,
            nowUtc));
    }

    /// <summary>Marks a requested refund failed; totals unchanged.</summary>
    public void FailRefund(Guid refundId, string? failureReason, DateTime nowUtc)
    {
        var refund = FindOpenRefund(refundId);
        refund.MarkFailed(nowUtc);

        RecordAttempt(
            PaymentAttemptKind.Refund,
            PaymentAttemptResult.Failed,
            nowUtc,
            refund.ProviderRefundId,
            detail: failureReason ?? "provider_refund_failed");

        Raise(new RefundFailed(
            Id,
            BookingId,
            refund.Id,
            refund.Amount.Amount,
            refund.Amount.CurrencyId,
            failureReason,
            nowUtc));
    }

    public Refund? FindRefundByIdempotencyKey(string idempotencyKey)
    {
        var key = idempotencyKey.Trim();
        return Refunds.FirstOrDefault(r => string.Equals(r.IdempotencyKey, key, StringComparison.Ordinal));
    }

    private Refund FindOpenRefund(Guid refundId)
    {
        var refund = Refunds.FirstOrDefault(r => r.Id == refundId)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        if (refund.Status != RefundStatusCode.Requested.Value)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentInvalidStatusTransition));
        }

        return refund;
    }

    private void EnsureRefundAmount(Money amount)
    {
        if (!Status.AllowsRefund)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentRefundNotAllowed));
        }

        ArgumentNullException.ThrowIfNull(amount);
        if (amount.CurrencyId != Amount.CurrencyId)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.MoneyCurrencyMismatch));
        }

        if (amount.Amount <= 0 || amount.Amount > RemainingRefundable.Amount)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentRefundAmountInvalid));
        }
    }

    public void RecordIgnoredAttempt(
        PaymentAttemptKind kind,
        DateTime nowUtc,
        string? providerReference = null,
        string? detail = null)
        => RecordAttempt(kind, PaymentAttemptResult.Ignored, nowUtc, providerReference, detail);

    private void RecordAttempt(
        PaymentAttemptKind kind,
        PaymentAttemptResult result,
        DateTime atUtc,
        string? providerReference = null,
        string? detail = null)
        => _attempts.Add(PaymentAttempt.Create(kind, result, atUtc, providerReference, detail));

    private void MaybeSetProviderPaymentId(string? providerPaymentId)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            return;
        }

        var trimmed = providerPaymentId.Trim();
        if (trimmed.Length > PaymentDefaults.MaxProviderPaymentIdLength)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentProviderPaymentIdInvalid));
        }

        ProviderPaymentId = trimmed;
    }

    private static string NormalizeFailureReason(string failureReason)
    {
        var trimmed = AppGuard.NotEmpty(failureReason, nameof(failureReason)).Trim();
        if (trimmed.Length > PaymentDefaults.MaxFailureReasonLength)
        {
            trimmed = trimmed[..PaymentDefaults.MaxFailureReasonLength];
        }

        return trimmed;
    }

    private void ApplyTransition(
        PaymentStatusCode to,
        DateTime nowUtc,
        Guid? actorId,
        string timelineCode,
        string timelineMessage,
        string? reason)
    {
        PaymentStatusCode? from = _statusHistory.Count == 0 ? null : Status;
        _statusHistory.Add(PaymentStatusHistoryEntry.Create(from, to, nowUtc, actorId, reason));
        _timeline.Add(PaymentTimelineEntry.Create(timelineCode, timelineMessage, nowUtc, actorId));
        Status = to;
        SetUpdatedAudit(nowUtc, actorId);
        AggregateVersion++;
    }

    /// <summary>Bump concurrency version without a status transition (e.g. further partial refund).</summary>
    private void MarkChanged() => AggregateVersion++;

    private void EnsureTransitionFrom(params PaymentStatusCode[] allowed)
    {
        if (!Status.IsOneOf(allowed))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentInvalidStatusTransition));
        }
    }
}
