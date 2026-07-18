using Asp.Versioning;
using eHub.Application.Identity.Commands.ConfirmEmail;
using eHub.Application.Identity.Commands.ForgotPassword;
using eHub.Application.Identity.Commands.Login;
using eHub.Application.Identity.Commands.Logout;
using eHub.Application.Identity.Commands.ResendEmailVerification;
using eHub.Application.Identity.Commands.ResetPassword;
using eHub.Application.Identity.Commands.RevokeOtherUserSessions;
using eHub.Application.Identity.Commands.RevokeUserSession;
using eHub.Application.Identity.Queries.GetCurrentUser;
using eHub.Application.Identity.Queries.GetLoginHistory;
using eHub.Application.Identity.Queries.GetUserSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(AuthSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionResult>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginCommand
            {
                Email = request.Email,
                Password = request.Password
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sessions")]
    [Authorize]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(IReadOnlyList<UserSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserSessionDto>>> Sessions(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserSessionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("login-history")]
    [Authorize]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(IReadOnlyList<LoginHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<LoginHistoryDto>>> LoginHistory(
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLoginHistoryQuery(take), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeUserSessionCommand(sessionId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("sessions")]
    [Authorize]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeOtherSessions(CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeOtherUserSessionsCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email")]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(AuthSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthSessionResult>> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ConfirmEmailCommand(request.UserId, request.Token),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("resend-verification")]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ResendEmailVerificationCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [MapToApiVersion(1.0)]
    [ProducesResponseType(typeof(AuthSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthSessionResult>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record VerifyEmailRequest(Guid UserId, string Token);

public sealed record ResendVerificationRequest(string Email);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);
