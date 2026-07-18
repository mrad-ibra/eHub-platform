using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.EmailInvalid));
    }
}
