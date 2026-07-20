using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Payments.Queries.GetPayment;

public sealed record GetPaymentQuery(Guid PaymentId) : IQuery<PaymentDetailDto>;

public sealed record PaymentDetailDto(
    Guid Id,
    Guid BookingId,
    string Status,
    string Provider,
    string? ProviderPaymentId,
    decimal Amount,
    Guid CurrencyId,
    decimal RefundedAmount,
    string IdempotencyKey,
    string? FailureReason,
    DateTime? PaidAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc);

public sealed class GetPaymentQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository payments,
    IBookingRepository bookings) : IQueryHandler<GetPaymentQuery, PaymentDetailDto>
{
    public async Task<PaymentDetailDto> Handle(GetPaymentQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        var booking = await bookings.GetByIdAsync(payment.BookingId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        if (booking.RenterId != userId && booking.HostId != userId)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
        }

        return Map(payment);
    }

    internal static PaymentDetailDto Map(Domain.Payments.Payment payment)
        => new(
            payment.Id,
            payment.BookingId,
            payment.Status.Value,
            payment.Provider.Value,
            payment.ProviderPaymentId,
            payment.Amount.Amount,
            payment.Amount.CurrencyId,
            payment.RefundedAmount.Amount,
            payment.IdempotencyKey,
            payment.FailureReason,
            payment.PaidAtUtc,
            payment.ExpiresAtUtc,
            payment.CreatedAtUtc);
}
