using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Payments.Commands.ExpirePayment;

public sealed record ExpirePaymentCommand(Guid PaymentId) : ICommand;

public sealed class ExpirePaymentCommandValidator : AbstractValidator<ExpirePaymentCommand>
{
    public ExpirePaymentCommandValidator()
        => RuleFor(x => x.PaymentId).NotEmpty();
}

/// <summary>System/worker entry — expires payment when window elapsed (no actor check).</summary>
public sealed class ExpirePaymentCommandHandler(
    IPaymentRepository payments,
    IOutboxWriter outbox,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<ExpirePaymentCommand>
{
    public async Task Handle(ExpirePaymentCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        payment.MarkExpired(now);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await outbox.EnqueueAsync(domainEvent, now, cancellationToken);
        }

        payment.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
