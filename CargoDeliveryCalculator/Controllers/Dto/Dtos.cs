using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Services;
using CargoDeliveryCalculator.Util;
using NodaTime;

namespace CargoDeliveryCalculator.Controllers.Dto;

public record CargoInputDto(CargoDto Cargo, Shipping Shipping, Destination Destination);
public record CargoInputsDto(CargoDto Cargo, Shipping Shipping, List<Destination> Destinations);
public record CargoOutputDto(
    CargoDto Cargo, 
    Shipping Shipping,
    Destination BestDestination,
    List<EstimateDto> Estimates);

public record VersionDto(Estimate Estimate, int Version, LocalDateTime CreationDateTime);

public record EstimateDiffDto(
    VersionDto OldVersion,
    VersionDto NewVersion,
    string ElapsedTime,
    ChangeValue? DeliveredEnergy,
    ChangeValue? SalePrice,
    ChangeValue? Revenue,
    ChangeValue? PurchaseCost,
    ChangeValue? PurchasePrice,
    ChangeValue? ShippingCost,
    ChangeValue? RegasCost,
    ChangeValue? CanalFee,
    ChangeValue? Margin,
    ChangeValue? Netback);

public record EstimateDto(Destination Destination, Estimate Estimate);

public record CargoDto(
    string CargoId,
    string LoadDate,
    decimal LoadedVolume,
    decimal ConversionFactor,
    PurchaseFormula PurchaseFormula);

public static class Mappers
{
    public static CargoDto CargoToDto(Cargo model)
    {
        return new CargoDto(model.CargoId, model.LoadDate.ToString(),  model.LoadedVolume, model.ConversionFactor, model.PurchaseFormula);
    }

    public static Cargo DtoToCargo(CargoDto dto)
    {
        return new Cargo(dto.CargoId, DateHandler.GetLocalDate(dto.LoadDate),  dto.LoadedVolume, dto.ConversionFactor, dto.PurchaseFormula);
    }
    
    public static VersionDto VersionedEstimateToVersionDto(VersionedEstimate versionedEstimate)
    {
        return new VersionDto(versionedEstimate.Estimate, versionedEstimate.Version,
            versionedEstimate.CreationDateTime);
    }

    // Only the two versions change shape; the rest is copied as is
    public static EstimateDiffDto EstimateDiffToDto(EstimateDiff diff)
    {
        return new EstimateDiffDto(
            VersionedEstimateToVersionDto(diff.OldVersion),
            VersionedEstimateToVersionDto(diff.NewVersion),
            diff.ElapsedTime,
            diff.DeliveredEnergy,
            diff.SalePrice,
            diff.Revenue,
            diff.PurchaseCost,
            diff.PurchasePrice,
            diff.ShippingCost,
            diff.RegasCost,
            diff.CanalFee,
            diff.Margin,
            diff.Netback);
    }
}
