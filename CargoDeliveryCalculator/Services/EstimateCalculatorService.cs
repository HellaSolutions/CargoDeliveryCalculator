using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Services;
using Microsoft.AspNetCore.Connections;
using NodaTime;

namespace CargoDeliveryCalculator.Services;

public interface IEstimateCalculatorService
{
    Task<List<(Destination, Track)>> GetEstimatesAsync(CargoInput cargoInput, Track track);
    Task<List<(Destination, Track)>> GetEstimatesAsync(CargoInput cargoInput);
}

public class EstimateCalculatorService(IMarketDataService marketDataService) : IEstimateCalculatorService
{
    public async Task<List<(Destination, Track)>> GetEstimatesAsync(CargoInput cargoInput, Track track)
    {
        var destinations = cargoInput.Destinations;
        if (destinations.Count == 0)
        {
            throw new ArgumentException("Empty destinations are not allowed");
        }
        
        var cargo = cargoInput.Cargo;
        var shipping = cargoInput.Shipping;
        var month = cargo.LoadDate.Month;
        var year = cargo.LoadDate.Year;
        var index = cargo.PurchaseFormula.Index;
        var price = await marketDataService.GetIndexPriceAsync(index, year);

        var monthlyPrice = price.GetPrice(month)
                           ?? throw IndexUnavailableException.ForMonth(index, year, month);
        
        track.PurchasePrice ??= cargo.PurchaseFormula.Slope * monthlyPrice + cargo.PurchaseFormula.Constant;
        track.PurchaseCost ??= cargo.LoadedEnergy * track.PurchasePrice;
        track.IndexLoadPrice ??= price.Id;
        
        var results = new List<(Destination destination, Track tracker)>(destinations.Count);
        foreach (var destination in destinations)
        {
            results.Add((destination, await CargoOutputTrackAsync(cargo, shipping, destination, track)));
        }

        return results;
    }

    public async Task<List<(Destination, Track)>> GetEstimatesAsync(CargoInput cargoInput)
    {
        return await GetEstimatesAsync(cargoInput, new Track());
    }
    
    private async Task<Track> CargoOutputTrackAsync(Cargo cargo, Shipping shipping, Destination destination, Track track)
    {
        // Each destination needs its own copy: fields are only filled when null (??=), so sharing one
        // Track would leak the first destination's values into the others. Copying the whole Track
        // keeps any caller-supplied overrides (e.g. the measured DeliveredEnergy on discharge).
        var destinationTracker = track with { };

        var (year, month, _) = GetDeliveryDate(cargo, destination);
        destinationTracker.DischargeMonth ??= month;
        destinationTracker.DeliveredEnergy ??= GetDeliveredEnergy(cargo, shipping, destination);
        var index = destination.SaleFormula.Index;
        var indexPrice = await marketDataService.GetIndexPriceAsync(index, year);
        destinationTracker.IndexDischargePrice ??= indexPrice.Id;
        var monthPrice = indexPrice.GetPrice(month) ?? 
                         throw IndexUnavailableException.ForMonth(index, year, month);
        destinationTracker.SalePrice ??= (destination.SaleFormula.Slope * monthPrice + destination.SaleFormula.Constant);
        destinationTracker.Revenue ??= destinationTracker.DeliveredEnergy * destinationTracker.SalePrice;
        
       destinationTracker.RegasCost ??= destinationTracker.DeliveredEnergy * destination.RegasFee;

        destinationTracker.ShippingCost ??= GetShippingCost(shipping, destination);
        var basicRevenue = destinationTracker.Revenue - (destinationTracker.ShippingCost + destinationTracker.RegasCost + destination.CanalFee);
        destinationTracker.Margin ??= basicRevenue - destinationTracker.PurchaseCost;
        destinationTracker.Netback ??= basicRevenue / cargo.LoadedEnergy;
        destinationTracker.CanalFee ??= destination.CanalFee;
        
        return destinationTracker;
    }

    private static decimal GetDeliveredEnergy(Cargo cargoDto, Shipping shipping, Destination destination)
    {
        var totalBoilOff = shipping.BoilOffRate * destination.LadenVoyageDurationInDays;
        if (totalBoilOff >= 1.00m)
        {
            throw new ArgumentException("Total boil off should be less than 100%");
        }

        return cargoDto.LoadedEnergy * (1 - totalBoilOff);
    }
    
    public static LocalDate GetDeliveryDate(Cargo cargoDto, Destination destination)
    {
        try
        {
            return cargoDto.LoadDate.PlusDays(destination.LadenVoyageDurationInDays);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentException($"Too large Laden duration : {destination.LadenVoyageDurationInDays}");
        }
    }
    
    private static decimal GetShippingCost(Shipping shipping, Destination destination)
    {
        //laden duration == ballast duration
        return 2 * shipping.DailyHireRate * destination.LadenVoyageDurationInDays;
    }
}