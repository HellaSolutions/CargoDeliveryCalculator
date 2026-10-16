using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Services;
using CargoDeliveryCalculator.Util;
using Microsoft.AspNetCore.Mvc;

namespace CargoDeliveryCalculator.Controllers;

[Route("api/[controller]/cargos/{cargoId}")]
[ApiController]
public class EstimateController(IEstimateService estimateService) : ControllerBase
{
    [HttpGet("versions/latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VersionDto>> GetLastEstimate(string cargoId)
    {
        var estimate = await estimateService.GetLastEstimateAsync(cargoId);
        return Ok(Mappers.VersionedEstimateToVersionDto(estimate));
    }
    
    [HttpGet("versions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VersionDto>> GetEstimate(string cargoId, [FromQuery] string dateTime)
    {
        var dt = DateHandler.GetLocalDateTime(dateTime);
        var estimate = await estimateService.GetEstimateAsync(cargoId, dt);
        return Ok(Mappers.VersionedEstimateToVersionDto(estimate));
    }
    
    // A separate route: query parameters don't take part in route matching, so sharing "versions"
    // with the dateTime lookup above makes every request to it ambiguous
    [HttpGet("versions/range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VersionDto>>> GetEstimates(string cargoId, [FromQuery] string fromDateTime, [FromQuery] string toDateTime)
    {
        var fromDt = DateHandler.GetLocalDateTime(fromDateTime);
        var toDt = DateHandler.GetLocalDateTime(toDateTime);
        var estimate = await estimateService.GetEstimateAsync(cargoId, fromDt, toDt);
        return Ok(estimate.Select(Mappers.VersionedEstimateToVersionDto));
    }
    
    [HttpGet("versions/{version:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VersionDto>> GetEstimate(string cargoId, int version)
    {
        if (version <= 0)
        {
            throw new ArgumentException($"{nameof(version)} must be greater than zero");
        }

        var estimate = await estimateService.GetEstimateAsync(cargoId, version);
        return Ok(Mappers.VersionedEstimateToVersionDto(estimate));
    }
    
    [HttpGet("versions/{versionA:int}/{versionB:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstimateDiffDto>> GetDelta(string cargoId, int versionA, int versionB)
    {
        
        if (versionB <= 0 || versionA <= 0)
        {
            throw new ArgumentException($"{nameof(versionA)} and {nameof(versionB)} must be both greater than zero");
        }
        if (versionA >= versionB)
        {
            throw new ArgumentException($"{nameof(versionA)} must precede {nameof(versionB)}");
        }
        
        var estimateDiff = await estimateService.GetDeltaAsync(cargoId, versionA, versionB);
        return Ok(Mappers.EstimateDiffToDto(estimateDiff));
    }
    
}