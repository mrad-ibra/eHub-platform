using eHub.Localization;
using FluentValidation;

namespace eHub.Application.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.FieldRequired, nameof(ConfirmEmailCommand.UserId)));

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage(_ => ErrorResources.Get(ErrorCodes.FieldRequired, nameof(ConfirmEmailCommand.Token)));
    }
}
