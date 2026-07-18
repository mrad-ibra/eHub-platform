using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
