using eHub.Application.Identity.Commands.Login;
using eHub.Domain.Resources;
using FluentValidation;

namespace eHub.Application.Identity.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.FieldRequired, nameof(ResetPasswordCommand.UserId)));

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.FieldRequired, nameof(ResetPasswordCommand.Token)));

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(LoginCommandValidator.MinimumPasswordLength)
            .MaximumLength(128)
            .WithMessage(_ => ErrorResources.Get(
                ErrorCodes.PasswordTooShort,
                LoginCommandValidator.MinimumPasswordLength));
    }
}
