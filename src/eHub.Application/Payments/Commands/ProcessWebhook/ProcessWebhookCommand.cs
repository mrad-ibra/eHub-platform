using System.Security.Cryptography;
using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using FluentValidation;

namespace eHub.Application.Payments.Commands.ProcessWebhook;

public sealed record ProcessWebhookCommand(
    string Provider,
    IReadOnlyDictionary<string, string> Headers,
    byte[] RawBody) : ICommand<ProcessWebhookResult>;

public sealed record ProcessWebhookResult(bool Accepted, string Code, string? Detail = null);

public sealed class ProcessWebhookCommandValidator : AbstractValidator<ProcessWebhookCommand>
{
    public ProcessWebhookCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(32);
        RuleFor(x => x.RawBody).NotNull();
    }
}

public sealed class ProcessWebhookCommandHandler(
    IPaymentProviderResolver providers,
    IPaymentWebhookInboxStore inbox,
    IPaymentRepository payments,
    IOutboxWriter outbox,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<ProcessWebhookCommand, ProcessWebhookResult>
{
    public async Task<ProcessWebhookResult> Handle(
        ProcessWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        IPaymentProvider provider;
        try
        {
            provider = providers.GetRequired(request.Provider);
        }
        catch (NotFoundException)
        {
            return new ProcessWebhookResult(false, "unknown_provider", "Unknown provider.");
        }

        if (!provider.VerifyWebhook(request.Headers, request.RawBody, now))
        {
            return new ProcessWebhookResult(false, "invalid_signature", "Signature or timestamp invalid.");
        }

        ProviderWebhookEvent? parsed;
        try
        {
            parsed = provider.ParseWebhook(request.RawBody);
        }
        catch
        {
            // Provider must not take down the endpoint; treat as safe ack (L6 follow-up: log).
            parsed = null;
        }

        if (parsed is null)
        {
            return new ProcessWebhookResult(true, "unparseable", "Acknowledged without effect.");
        }

        var payloadHash = Convert.ToHexString(SHA256.HashData(request.RawBody)).ToLowerInvariant();
        var began = await inbox.TryBeginAsync(
            provider.ProviderKey,
            parsed.EventId,
            payloadHash,
            now,
            cancellationToken);
        if (!began)
        {
            return new ProcessWebhookResult(true, "duplicate", "Already processed.");
        }

        if (parsed.Outcome == ProviderWebhookOutcome.Unknown)
        {
            await inbox.CompleteAsync(
                provider.ProviderKey,
                parsed.EventId,
                null,
                PaymentWebhookInboxStatuses.Ignored,
                now,
                "unknown_event_type",
                cancellationToken);
            return new ProcessWebhookResult(true, "ignored", "Unknown event type acknowledged.");
        }

        Payment? payment = null;
        if (parsed.PaymentId is { } paymentId)
        {
            payment = await payments.GetByIdAsync(paymentId, cancellationToken);
        }

        if (payment is null && !string.IsNullOrWhiteSpace(parsed.ProviderPaymentId))
        {
            payment = await payments.GetByProviderPaymentIdAsync(
                provider.ProviderKey,
                parsed.ProviderPaymentId!,
                cancellationToken);
        }

        if (payment is null)
        {
            await inbox.CompleteAsync(
                provider.ProviderKey,
                parsed.EventId,
                null,
                PaymentWebhookInboxStatuses.Failed,
                now,
                "payment_not_found",
                cancellationToken);
            return new ProcessWebhookResult(true, "payment_not_found", "Recorded; no payment matched.");
        }

        try
        {
            ApplyOutcome(payment, parsed, now);
            foreach (var domainEvent in payment.DomainEvents)
            {
                await outbox.EnqueueAsync(domainEvent, now, cancellationToken);
            }

            payment.ClearDomainEvents();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await inbox.CompleteAsync(
                provider.ProviderKey,
                parsed.EventId,
                payment.Id,
                PaymentWebhookInboxStatuses.Processed,
                now,
                null,
                cancellationToken);
            return new ProcessWebhookResult(true, "processed");
        }
        catch (ConflictException ex)
        {
            payment.RecordIgnoredAttempt(
                PaymentAttemptKind.Webhook,
                now,
                parsed.ProviderPaymentId,
                ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await inbox.CompleteAsync(
                provider.ProviderKey,
                parsed.EventId,
                payment.Id,
                PaymentWebhookInboxStatuses.Ignored,
                now,
                "illegal_transition",
                cancellationToken);
            return new ProcessWebhookResult(true, "ignored_transition", ex.Message);
        }
    }

    private static void ApplyOutcome(Payment payment, ProviderWebhookEvent parsed, DateTime now)
    {
        switch (parsed.Outcome)
        {
            case ProviderWebhookOutcome.Authorized:
                payment.MarkAuthorized(now);
                break;
            case ProviderWebhookOutcome.Succeeded:
                if (parsed.Amount is { } amount
                    && (amount != payment.Amount.Amount
                        || (parsed.CurrencyId is { } c && c != payment.Amount.CurrencyId)))
                {
                    payment.MarkFailed("amount_mismatch", now);
                    break;
                }

                payment.MarkSucceeded(parsed.OccurredAtUtc, parsed.ProviderPaymentId);
                break;
            case ProviderWebhookOutcome.Failed:
                payment.MarkFailed(parsed.FailureReason ?? "provider_failed", now);
                break;
            case ProviderWebhookOutcome.Cancelled:
                payment.MarkCancelled(now);
                break;
            case ProviderWebhookOutcome.Refunded:
                var refundAmount = parsed.RefundAmount ?? payment.RemainingRefundable.Amount;
                payment.AddRefund(
                    Money.Create(refundAmount, payment.Amount.CurrencyId),
                    "webhook_refund",
                    now);
                break;
            default:
                break;
        }
    }
}
