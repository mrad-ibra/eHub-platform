using eHub.Application.Common.Messaging;
using eHub.Application.Identity.Commands.Login;

namespace eHub.Application.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token) : ICommand<AuthSessionResult>;
