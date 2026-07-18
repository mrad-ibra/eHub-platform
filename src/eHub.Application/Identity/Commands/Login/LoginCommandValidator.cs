using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Identity.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public const int MinimumPasswordLength = 6;

    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.EmailInvalid));

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(MinimumPasswordLength)
            .MaximumLength(128)
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.PasswordTooShort, MinimumPasswordLength));
    }
}
