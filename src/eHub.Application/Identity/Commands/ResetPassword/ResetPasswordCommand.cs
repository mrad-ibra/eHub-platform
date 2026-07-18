using eHub.Application.Common.Messaging;
using eHub.Application.Identity.Commands.Login;

namespace eHub.Application.Identity.Commands.ResetPassword;

public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword)
    : ICommand<AuthSessionResult>;
