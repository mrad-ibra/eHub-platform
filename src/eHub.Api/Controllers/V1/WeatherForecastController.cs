using Asp.Versioning;
using eHub.Application.Weather.Queries.GetWeatherForecastList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class WeatherForecastController(ISender sender) : ControllerBase
{
    [HttpGet]
    [MapToApiVersion(1.0)]
    public async Task<ActionResult<IReadOnlyList<WeatherForecastListItemDto>>> Get(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWeatherForecastListQuery(), cancellationToken);
        return Ok(result);
    }
}
