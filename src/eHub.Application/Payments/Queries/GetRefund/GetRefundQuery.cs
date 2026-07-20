using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Queries.GetPayment;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Localization;

namespace eHub.Application.Payments.Queries.GetRefund;

public sealed record GetRefundQuery(Guid RefundId) : IQuery<RefundDetailDto>;

public sealed record RefundDetailDto(
    Guid Id,
    Guid PaymentId,
    Guid BookingId,
    string Status,
    decimal Amount,
    Guid CurrencyId,
    string Reason,
    string IdempotencyKey,
    string? ProviderRefundId,
    DateTime RequestedAtUtc,
    DateTime? SettledAtUtc,
    string PaymentStatus,
    decimal PaymentRefundedAmount);

public sealed class GetRefundQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository payments,
    IBookingRepository bookings) : IQueryHandler<GetRefundQuery, RefundDetailDto>
{
    public async Task<RefundDetailDto> Handle(GetRefundQuery request, CancellationToken cancellationToken)
    {
        var refund = await payments.GetRefundByIdAsync(request.RefundId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        var payment = await payments.GetByIdAsync(refund.PaymentId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        await EnsureCanReadAsync(payment, cancellationToken);

        return Map(payment, refund);
    }

    internal static RefundDetailDto Map(Payment payment, Refund refund)
        => new(
            refund.Id,
            payment.Id,
            payment.BookingId,
            refund.Status,
            refund.Amount.Amount,
            refund.Amount.CurrencyId,
            refund.Reason,
            refund.IdempotencyKey,
            refund.ProviderRefundId,
            refund.RequestedAtUtc,
            refund.SettledAtUtc,
            payment.Status.Value,
            payment.RefundedAmount.Amount);

    private async Task EnsureCanReadAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (currentUser.HasPermission(AuthPolicies.PaymentsRefundRead)
            || currentUser.HasPermission(AuthPolicies.PaymentsRead))
        {
            var booking = await bookings.GetByIdAsync(payment.BookingId, cancellationToken)
                ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

            var userId = currentUser.RequireUserId();
            if (booking.RenterId == userId || booking.HostId == userId || currentUser.IsInRole("Admin"))
            {
                return;
            }
        }

        throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
    }
}
