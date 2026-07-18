using eHub.Domain.Resources;
using FluentValidation;

namespace eHub.Application.Identity.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationCommandValidator : AbstractValidator<ResendEmailVerificationCommand>
{
    public ResendEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.EmailInvalid));
    }
}
