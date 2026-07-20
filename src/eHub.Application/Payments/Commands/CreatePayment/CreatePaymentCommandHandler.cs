using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Bookings;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Localization;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace eHub.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(PaymentDefaults.MaxIdempotencyKeyLength);
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage(ErrorResources.Get(ErrorCodes.PaymentProviderRequired))
            .MaximumLength(32)
            .Must(p => PaymentProviderCode.TryParse(p, out _))
            .WithMessage(ErrorResources.Get(ErrorCodes.PaymentProviderCodeInvalid));
    }
}

public sealed class CreatePaymentCommandHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings,
    IPaymentRepository payments,
    IPaymentProviderResolver providerResolver,
    IOutboxWriter outbox,
    IClock clock,
    IUnitOfWork unitOfWork,
    IOptions<PaymentsOptions> paymentOptions) : ICommandHandler<CreatePaymentCommand, CreatePaymentResult>
{
    public async Task<CreatePaymentResult> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var key = request.IdempotencyKey.Trim();
        var userId = currentUser.RequireUserId();
        var providerCode = PaymentProviderCode.Parse(request.Provider);
        EnsureProviderAllowed(providerCode);

        var existingByKey = await payments.GetByIdempotencyKeyAsync(key, cancellationToken);
        if (existingByKey is not null)
        {
            await EnsureIdempotentReplayAsync(existingByKey, request, userId, cancellationToken);
            return Map(existingByKey);
        }

        var booking = await bookings.GetByIdAsync(request.BookingId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        if (booking.RenterId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
        }

        if (booking.Status != BookingStatusCode.PendingPayment)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentBookingNotPayable));
        }

        if (booking.ExpiresAtUtc is not null && now >= booking.ExpiresAtUtc)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.BookingHoldExpired));
        }

        var active = await payments.GetActiveByBookingIdAsync(booking.Id, cancellationToken);
        if (active is not null)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentActiveAlreadyExists));
        }

        var adapter = providerResolver.GetRequired(providerCode.Value);
        var payment = Payment.Create(
            booking.Id,
            booking.TotalPrice,
            providerCode,
            key,
            now);

        await payments.AddAsync(payment, cancellationToken);

        var created = await adapter.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(
                payment.Id,
                payment.BookingId,
                payment.Amount.Amount,
                payment.Amount.CurrencyId,
                payment.IdempotencyKey),
            cancellationToken);

        if (!created.IsSuccess)
        {
            payment.MarkFailed(created.Failure.ToDomainCode(), now);

            foreach (var domainEvent in payment.DomainEvents)
            {
                await outbox.EnqueueAsync(domainEvent, now, cancellationToken);
            }

            payment.ClearDomainEvents();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var detail = created.Failure?.SafeMessage ?? created.Failure.ToDomainCode();
            throw new ConflictException(detail);
        }

        payment.MarkPending(created.ProviderPaymentId!, now);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await outbox.EnqueueAsync(domainEvent, now, cancellationToken);
        }

        payment.ClearDomainEvents();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            var resolved = await TryResolveCreateRaceAsync(key, request, userId, cancellationToken);
            if (resolved is not null)
            {
                return resolved;
            }

            throw;
        }

        return Map(payment, created.RedirectUrl);
    }

    private void EnsureProviderAllowed(PaymentProviderCode providerCode)
    {
        if (providerCode == PaymentProviderCode.Test && !paymentOptions.Value.AllowTestProvider)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentTestProviderNotAllowed));
        }
    }

    private async Task EnsureIdempotentReplayAsync(
        Payment existing,
        CreatePaymentCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!MatchesIdempotentPayload(existing, request))
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentIdempotencyPayloadMismatch));
        }

        var booking = await bookings.GetByIdAsync(existing.BookingId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        if (booking.RenterId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
        }
    }

    private async Task<CreatePaymentResult?> TryResolveCreateRaceAsync(
        string idempotencyKey,
        CreatePaymentCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var byKey = await payments.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (byKey is not null && MatchesIdempotentPayload(byKey, request))
        {
            await EnsureIdempotentReplayAsync(byKey, request, userId, cancellationToken);
            return Map(byKey);
        }

        var active = await payments.GetActiveByBookingIdAsync(request.BookingId, cancellationToken);
        if (active is not null)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentActiveAlreadyExists));
        }

        return null;
    }

    private static bool MatchesIdempotentPayload(Payment existing, CreatePaymentCommand request)
    {
        if (existing.BookingId != request.BookingId)
        {
            return false;
        }

        var requestedProvider = PaymentProviderCode.Parse(request.Provider);
        return existing.Provider == requestedProvider;
    }

    private static CreatePaymentResult Map(Payment payment, string? redirectUrl = null)
        => new(
            payment.Id,
            payment.BookingId,
            payment.Status.Value,
            payment.Provider.Value,
            payment.Amount.Amount,
            payment.Amount.CurrencyId,
            payment.IdempotencyKey,
            payment.ExpiresAtUtc,
            payment.ProviderPaymentId,
            redirectUrl);
}
