using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Payments.Commands.CancelPayment;

public sealed record CancelPaymentCommand(Guid PaymentId) : ICommand;

public sealed class CancelPaymentCommandValidator : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator()
        => RuleFor(x => x.PaymentId).NotEmpty();
}

public sealed class CancelPaymentCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository payments,
    IBookingRepository bookings,
    IOutboxWriter outbox,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<CancelPaymentCommand>
{
    public async Task Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var userId = currentUser.RequireUserId();
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        var booking = await bookings.GetByIdAsync(payment.BookingId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        var isRenter = booking.RenterId == userId;
        var isAdmin = currentUser.IsInRole("Admin")
            && currentUser.HasPermission(AuthPolicies.PaymentsCancel);

        // BR-PAY-016: Host cannot cancel Payment. Renter self-service OR Admin (fraud/dispute).
        if (!isRenter && !isAdmin)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.PaymentAccessDenied));
        }

        payment.MarkCancelled(now, userId);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await outbox.EnqueueAsync(domainEvent, now, cancellationToken);
        }

        payment.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
