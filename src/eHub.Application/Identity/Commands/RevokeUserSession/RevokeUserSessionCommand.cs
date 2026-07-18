using eHub.Application.Common.Messaging;

namespace eHub.Application.Identity.Commands.RevokeUserSession;

public sealed record RevokeUserSessionCommand(Guid SessionId) : ICommand;
