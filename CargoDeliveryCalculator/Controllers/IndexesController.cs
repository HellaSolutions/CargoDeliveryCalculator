using CargoDeliveryCalculator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CargoDeliveryCalculator.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IndexesController(IMarketDataService marketDataService, IDynamicsService dynamicsService) : ControllerBase
{
    /*[HttpPut("{index}/{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SetPrice(string index, int year, int month, [FromBody] decimal amount)
    {
        var created = await dynamicsService.SetIndexPriceAsync(index, amount, year, month);
        var selfLink = Url.Action(nameof(GetPrice), new { index, year, month }) ?? string.Empty;
        return created ? Created(selfLink, "") : Ok();
    }*/
    
    [HttpGet("{index}/{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<decimal>> GetPrice(string index, int year, int month)
    {
        var price = await marketDataService.GetIndexPriceAsync(index, year);
        return Ok(price.GetPrice(month) ?? throw IndexUnavailableException.ForMonth(price.Index, year, month));
    }
}