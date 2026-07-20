using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Abstractions;
using eHub.Application.Payments.Queries.GetRefund;
using eHub.Domain.Exceptions;
using eHub.Domain.Payments;
using eHub.Localization;

namespace eHub.Application.Payments.Queries.ListPaymentRefunds;

public sealed record ListPaymentRefundsQuery(Guid PaymentId) : IQuery<IReadOnlyList<RefundDetailDto>>;

public sealed class ListPaymentRefundsQueryHandler(
    ICurrentUser currentUser,
    IPaymentRepository payments,
    IBookingRepository bookings) : IQueryHandler<ListPaymentRefundsQuery, IReadOnlyList<RefundDetailDto>>
{
    public async Task<IReadOnlyList<RefundDetailDto>> Handle(
        ListPaymentRefundsQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        await EnsureCanReadAsync(payment, cancellationToken);

        return payment.Refunds
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => GetRefundQueryHandler.Map(payment, r))
            .ToArray();
    }

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
