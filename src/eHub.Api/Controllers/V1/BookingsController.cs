using Asp.Versioning;
using eHub.Application.Bookings.Commands.CreateBooking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/bookings")]
public sealed class BookingsController(ISender sender) : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    [HttpPost]
    [ProducesResponseType(typeof(CreateBookingResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateBookingResult>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            return Problem(
                detail: "Idempotency-Key header is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "validation_failed");
        }

        var result = await sender.Send(
            new CreateBookingCommand(
                request.AssetId,
                request.StartDate,
                request.EndDate,
                keyValues.ToString()!,
                request.DriverRequested,
                request.DeliveryRequested,
                request.Pickup?.UseAssetLocation ?? true,
                request.Pickup?.AddressLine,
                request.Dropoff?.UseAssetLocation ?? true,
                request.Dropoff?.AddressLine,
                request.Notes),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id, version = "1.0" },
            result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetById(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);
}

public sealed record CreateBookingRequest(
    Guid AssetId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool DriverRequested = false,
    bool DeliveryRequested = false,
    LocationPreferenceDto? Pickup = null,
    LocationPreferenceDto? Dropoff = null,
    string? Notes = null);

public sealed record LocationPreferenceDto(
    bool UseAssetLocation = true,
    string? AddressLine = null);
