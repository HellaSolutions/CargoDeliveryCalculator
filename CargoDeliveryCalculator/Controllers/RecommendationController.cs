using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CargoDeliveryCalculator.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationController(ICargoOutputService cargoOutputService) : ControllerBase
{
    [HttpPost("active/")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VersionDto>> SetActive([FromBody] CargoInputDto cargoInputDto)
    {
        var cargoInput = new CargoInput(Mappers.DtoToCargo(cargoInputDto.Cargo), cargoInputDto.Shipping,
            [cargoInputDto.Destination]);
        var estimate = await cargoOutputService.SetActiveAsync(cargoInput);
        return Ok(Mappers.VersionedEstimateToVersionDto(estimate));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CargoOutputDto>> CargoOutput([FromBody] CargoInputsDto cargoInputsDto)
    {
        var cargoOutput = await cargoOutputService.CargoOutputAsync(new CargoInput(Mappers.DtoToCargo(cargoInputsDto.Cargo), 
            cargoInputsDto.Shipping, 
            cargoInputsDto.Destinations));
        return Ok(new CargoOutputDto(
            Mappers.CargoToDto(cargoOutput.Cargo), 
            cargoOutput.Shipping, 
            cargoOutput.BestDestination,
            cargoOutput.Estimates.Select(de => new EstimateDto(de.Item1, de.Item2)).ToList()));
    }
}
