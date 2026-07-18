using Asp.Versioning;
using eHub.Application.Weather.Queries.GetWeatherForecast;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V2;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class WeatherForecastController(ISender sender) : ControllerBase
{
    [HttpGet]
    [MapToApiVersion(2.0)]
    public async Task<ActionResult<GetWeatherForecastResult>> Get(
        [FromQuery] GetWeatherForecastQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
