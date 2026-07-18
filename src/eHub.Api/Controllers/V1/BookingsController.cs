using Asp.Versioning;
using eHub.Application.Bookings.Commands.CreateBooking;
using eHub.Application.Bookings.Queries.GetBooking;
using eHub.Application.Identity.Authorization;
using eHub.Localization;
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
    [Authorize(Policy = AuthPolicies.BookingsCreate)]
    [ProducesResponseType(typeof(CreateBookingResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateBookingResult>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            throw new Domain.Exceptions.ValidationFailedException(
                ErrorResources.Get(ErrorCodes.BookingIdempotencyKeyRequired));
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
    [Authorize(Policy = AuthPolicies.BookingsRead)]
    [ProducesResponseType(typeof(BookingDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingQuery(id), cancellationToken);
        return Ok(result);
    }
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
