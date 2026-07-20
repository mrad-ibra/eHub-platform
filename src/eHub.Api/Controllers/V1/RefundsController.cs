using Asp.Versioning;
using eHub.Application.Identity.Authorization;
using eHub.Application.Payments.Commands.CreateRefund;
using eHub.Application.Payments.Queries.GetRefund;
using eHub.Application.Payments.Queries.ListPaymentRefunds;
using eHub.Domain.Exceptions;
using eHub.Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}")]
public sealed class RefundsController(ISender sender) : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    [HttpPost("payments/{paymentId:guid}/refunds")]
    [Authorize(Policy = AuthPolicies.PaymentsRefund)]
    [ProducesResponseType(typeof(CreateRefundResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateRefundResult>> Create(
        Guid paymentId,
        [FromBody] CreateRefundRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentRefundIdempotencyKeyRequired));
        }

        var result = await sender.Send(
            new CreateRefundCommand(
                paymentId,
                request.Amount,
                request.Reason,
                keyValues.ToString()!),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { refundId = result.RefundId, version = "1.0" },
            result);
    }

    [HttpGet("payments/{paymentId:guid}/refunds")]
    [Authorize(Policy = AuthPolicies.PaymentsRefundRead)]
    [ProducesResponseType(typeof(IReadOnlyList<RefundDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RefundDetailDto>>> ListByPayment(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListPaymentRefundsQuery(paymentId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("refunds/{refundId:guid}")]
    [Authorize(Policy = AuthPolicies.PaymentsRefundRead)]
    [ProducesResponseType(typeof(RefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundDetailDto>> GetById(
        Guid refundId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRefundQuery(refundId), cancellationToken);
        return Ok(result);
    }
}

public sealed record CreateRefundRequest(decimal Amount, string Reason);
