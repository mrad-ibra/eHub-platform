using Asp.Versioning;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Commands.CancelPayment;
using eHub.Application.Payments.Commands.CreatePayment;
using eHub.Application.Payments.Queries.GetPayment;
using eHub.Domain.Exceptions;
using eHub.Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/payments")]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    [HttpPost]
    [Authorize(Policy = AuthPolicies.BookingsCreate)]
    [ProducesResponseType(typeof(CreatePaymentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatePaymentResult>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentIdempotencyKeyRequired));
        }

        var result = await sender.Send(
            new CreatePaymentCommand(
                request.BookingId,
                keyValues.ToString()!,
                request.Provider ?? "TEST"),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id, version = "1.0" },
            result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.BookingsRead)]
    [ProducesResponseType(typeof(PaymentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.BookingsCreate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelPaymentCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreatePaymentRequest(Guid BookingId, string? Provider = "TEST");
