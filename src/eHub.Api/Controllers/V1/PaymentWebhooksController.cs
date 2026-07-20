using System.Text;
using Asp.Versioning;
using eHub.Application.Payments.Commands.ProcessWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/payments/webhooks")]
public sealed class PaymentWebhooksController(ISender sender) : ControllerBase
{
    [HttpPost("{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Receive(string provider, CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var raw = ms.ToArray();

        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var result = await sender.Send(new ProcessWebhookCommand(provider, headers, raw), cancellationToken);

        if (result.Code is "invalid_signature")
        {
            return Unauthorized(new { received = false, code = result.Code });
        }

        if (result.Code is "unknown_provider")
        {
            return NotFound(new { received = false, code = result.Code });
        }

        return Ok(new { received = true, code = result.Code });
    }
}
