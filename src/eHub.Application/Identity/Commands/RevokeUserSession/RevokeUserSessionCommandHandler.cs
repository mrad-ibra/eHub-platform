using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Identity.Commands.RevokeUserSession;

public sealed class RevokeUserSessionCommandValidator : AbstractValidator<RevokeUserSessionCommand>
{
    public RevokeUserSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.FieldRequired, nameof(RevokeUserSessionCommand.SessionId)));
    }
}

public sealed class RevokeUserSessionCommandHandler(
    ICurrentUser currentUser,
    IClientContext clientContext,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<RevokeUserSessionCommand>
{
    public async Task Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var session = await refreshTokens.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null || session.UserId != userId)
        {
            throw new NotFoundException(ErrorResources.Get(ErrorCodes.SessionNotFound));
        }

        if (session.IsRevoked)
        {
            return;
        }

        session.Revoke(clock.UtcNow, clientContext.IpAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
