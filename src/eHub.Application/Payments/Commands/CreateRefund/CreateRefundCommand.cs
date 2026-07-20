using eHub.Application.Bookings.Abstractions;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Payments.Commands.CreateRefund;

public sealed record CreateRefundCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason,
    string IdempotencyKey) : ICommand<CreateRefundResult>;

public sealed record CreateRefundResult(
    Guid RefundId,
    Guid PaymentId,
    string Status,
    decimal Amount,
    Guid CurrencyId,
    string Reason,
    string IdempotencyKey,
    string? ProviderRefundId,
    decimal PaymentRefundedAmount,
    string PaymentStatus);

public sealed class CreateRefundCommandValidator : AbstractValidator<CreateRefundCommand>
{
    public CreateRefundCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(PaymentDefaults.MaxRefundReasonLength);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(PaymentDefaults.MaxIdempotencyKeyLength);
    }
}

public sealed class CreateRefundCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository payments,
    ICurrencyRepository currencies,
    IMinorUnitConverter minorUnits,
    IPaymentProviderResolver providerResolver,
    IOutboxWriter outbox,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateRefundCommand, CreateRefundResult>
{
    public async Task<CreateRefundResult> Handle(
        CreateRefundCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.HasPermission(AuthPolicies.PaymentsRefund))
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
        }

        if (!currentUser.IsInRole("Admin"))
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
        }

        var now = clock.UtcNow;
        var userId = currentUser.RequireUserId();
        var key = request.IdempotencyKey.Trim();
        var reason = request.Reason.Trim();

        CreateRefundResult? result = null;
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // FOR UPDATE serializes parallel partial refunds on the same payment row.
                var payment = await payments.GetByIdForUpdateAsync(request.PaymentId, ct)
                    ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

                var existing = payment.FindRefundByIdempotencyKey(key);
                if (existing is not null)
                {
                    if (!existing.MatchesIdempotentPayload(request.Amount, payment.Amount.CurrencyId, reason))
                    {
                        throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentRefundIdempotencyPayloadMismatch));
                    }

                    result = Map(payment, existing);
                    return;
                }

                if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
                {
                    throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentRefundNotAllowed));
                }

                var currency = await currencies.GetByIdAsync(payment.Amount.CurrencyId, ct)
                    ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.CatalogItemNotFound));
                if (!minorUnits.IsSupported(currency.Code))
                {
                    throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentCurrencyUnsupported));
                }

                var amount = Money.Create(request.Amount, payment.Amount.CurrencyId);
                var refund = payment.BeginRefund(amount, reason, key, now, userId);

                foreach (var domainEvent in payment.DomainEvents)
                {
                    await outbox.EnqueueAsync(domainEvent, now, ct);
                }

                payment.ClearDomainEvents();

                var adapter = providerResolver.GetRequired(payment.Provider.Value);
                var providerResult = await adapter.RefundAsync(
                    new ProviderRefundRequest(
                        payment.ProviderPaymentId,
                        amount.Amount,
                        currency.Code,
                        key,
                        reason),
                    ct);

                if (providerResult.IsSuccess)
                {
                    payment.CompleteRefund(refund.Id, providerResult.ProviderRefundId, now);
                }
                else
                {
                    payment.FailRefund(refund.Id, providerResult.Failure.ToDomainCode(), now);
                }

                foreach (var domainEvent in payment.DomainEvents)
                {
                    await outbox.EnqueueAsync(domainEvent, now, ct);
                }

                payment.ClearDomainEvents();
                await unitOfWork.SaveChangesAsync(ct);

                var settled = payment.Refunds.First(r => r.Id == refund.Id);
                result = Map(payment, settled);
            }, cancellationToken);
        }
        catch (ConflictException ex) when (
            ex.Message == ErrorResources.Get(ErrorCodes.PaymentRefundAmountInvalid))
        {
            var reloaded = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
                ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));
            var raced = reloaded.FindRefundByIdempotencyKey(key);
            if (raced is not null && raced.MatchesIdempotentPayload(request.Amount, reloaded.Amount.CurrencyId, reason))
            {
                return Map(reloaded, raced);
            }

            throw;
        }

        return result ?? throw new InvalidOperationException("CreateRefund completed without a result.");
    }

    private static CreateRefundResult Map(Payment payment, Refund refund)
        => new(
            refund.Id,
            payment.Id,
            refund.Status,
            refund.Amount.Amount,
            refund.Amount.CurrencyId,
            refund.Reason,
            refund.IdempotencyKey,
            refund.ProviderRefundId,
            payment.RefundedAmount.Amount,
            payment.Status.Value);
}
