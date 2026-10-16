using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CargoDeliveryCalculator.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DynamicsController(IDynamicsService dynamicsService) : ControllerBase
{
    [HttpPut("indexes/{index}/{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SetPrice(string index, int year, int month, [FromBody] decimal amount)
    {
        var created = await dynamicsService.SetIndexPriceAsync(index, amount, year, month);
        var selfLink = Url.Action(
            nameof(IndexesController.GetPrice), 
            nameof(IndexesController).Replace("Controller", ""), 
            new { index, year, month }) ?? string.Empty;
        return created ? Created(selfLink, "") : Ok();
    }
    
    [HttpPost("discharge/cargos/{CargoId}/")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VersionDto>> DischargeAsync(string cargoId, [FromBody] decimal mmBtu)
    {
        var estimate = await dynamicsService.DischargeAsync(cargoId, mmBtu);
        return Ok(Mappers.VersionedEstimateToVersionDto(estimate));
    }
}