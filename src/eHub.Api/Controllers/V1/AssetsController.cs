using Asp.Versioning;
using eHub.Application.Assets.Commands.AddAssetMedia;
using eHub.Application.Assets.Commands.AssetLifecycle;
using eHub.Application.Assets.Commands.CreateAsset;
using eHub.Application.Assets.Commands.SetAssetLocation;
using eHub.Application.Assets.Commands.SetAssetPricing;
using eHub.Application.Assets.Commands.UpdateAssetDetails;
using eHub.Application.Assets.Queries.GetAsset;
using eHub.Application.Assets.Queries.ListMyAssets;
using eHub.Domain.Assets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/assets")]
public sealed class AssetsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreateAssetCommand(
                request.CategoryId,
                request.Title,
                request.SubCategoryId,
                request.BrandId,
                request.ModelId,
                request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { assetId = id, version = "1.0" }, id);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<AssetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> Mine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListMyAssetsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{assetId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetDto>> GetById(Guid assetId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAssetQuery(assetId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid assetId,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateAssetDetailsCommand(
                assetId,
                request.Title,
                request.Description,
                request.SubCategoryId,
                request.BrandId,
                request.ModelId),
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{assetId:guid}/pricing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPricing(
        Guid assetId,
        [FromBody] SetPricingRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SetAssetPricingCommand(
                assetId,
                request.CurrencyId,
                request.RentalPeriodTypeId,
                request.Amount,
                request.WeekendAmount,
                request.WeeklyAmount,
                request.MonthlyAmount),
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{assetId:guid}/location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetLocation(
        Guid assetId,
        [FromBody] SetLocationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SetAssetLocationCommand(
                assetId,
                request.CountryId,
                request.CityId,
                request.DistrictId,
                request.AddressLine,
                request.Latitude,
                request.Longitude),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{assetId:guid}/media")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> AddMedia(
        Guid assetId,
        [FromBody] AddMediaRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new AddAssetMediaCommand(
                assetId,
                request.Kind,
                request.Url,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.IsPrimary),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPost("{assetId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(Guid assetId, CancellationToken cancellationToken)
    {
        await sender.Send(new PublishAssetCommand(assetId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assetId:guid}/submit-approval")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitApproval(Guid assetId, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitAssetForApprovalCommand(assetId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assetId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid assetId, CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveAssetCommand(assetId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assetId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(
        Guid assetId,
        [FromBody] RejectAssetRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RejectAssetCommand(assetId, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{assetId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Archive(Guid assetId, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveAssetCommand(assetId), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateAssetRequest(
    Guid CategoryId,
    string Title,
    Guid? SubCategoryId,
    Guid? BrandId,
    Guid? ModelId,
    string? Description);

public sealed record UpdateAssetRequest(
    string Title,
    string? Description,
    Guid? SubCategoryId,
    Guid? BrandId,
    Guid? ModelId);

public sealed record SetPricingRequest(
    Guid CurrencyId,
    Guid RentalPeriodTypeId,
    decimal Amount,
    decimal? WeekendAmount,
    decimal? WeeklyAmount,
    decimal? MonthlyAmount);

public sealed record SetLocationRequest(
    Guid CountryId,
    Guid CityId,
    Guid? DistrictId,
    string? AddressLine,
    double? Latitude,
    double? Longitude);

public sealed record AddMediaRequest(
    AssetMediaKind Kind,
    string Url,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    bool IsPrimary);

public sealed record RejectAssetRequest(string Reason);
