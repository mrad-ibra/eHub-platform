using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Commands.ResendEmailVerification;

public sealed record ResendEmailVerificationCommand(string Email) : ICommand;
