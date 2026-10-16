using System.Collections.ObjectModel;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Persistence.Repositories;
using Destination = CargoDeliveryCalculator.Model.Destination;

namespace CargoDeliveryCalculator.Services;

public interface ICargoOutputService
{
    Task<CargoOutput> CargoOutputAsync(CargoInput cargoInput);
    Task<VersionedEstimate> SetActiveAsync(CargoInput cargoInput);
}

public class CargoOutputService(
    IUnitOfWork unitOfWork,
    IMarketDataService marketDataService, 
    IEstimateCalculatorService estimateCalculatorService,
    IActiveCargoRepository activeCargoRepository,
    ICargoRepository cargoRepository,
    IShippingRepository shippingRepository,
    IDestinationRepository destinationRepository,
    IEstimateRepository estimateRepository
    ) : ICargoOutputService
{
    
    public async Task<CargoOutput> CargoOutputAsync(CargoInput cargoInput)
    {
        var results = await estimateCalculatorService.GetEstimatesAsync(cargoInput);
        var estimates = results
            .OrderByDescending(de => de.Item2.Margin)
            .Select(de => (de.Item1, Track.ToEstimate(de.Item2)))
            .ToList();
        return new CargoOutput(cargoInput.Cargo, cargoInput.Shipping, estimates[0].Item1, new ReadOnlyCollection<(Destination, Estimate)>(estimates));
    }

    public async Task<VersionedEstimate> SetActiveAsync(CargoInput cargoInput)
    {
        await using var tx = await unitOfWork.BeginTransactionAsync();  
        var destinations = cargoInput.Destinations;
        if (destinations.Count > 1)
        {
            throw new ArgumentException("Only one destination is allowed for an active cargo");
        }

        var destination = cargoInput.Destinations[0];
        var shipping = cargoInput.Shipping;
        var cargo = cargoInput.Cargo;
        
        await marketDataService.LockPricesForShareAsync(                                                                                                                                                                                                                                                             
            cargo.PurchaseFormula.Index, cargo.LoadDate.Year,                                                                                                                                                                                                                                                        
            destination.SaleFormula.Index, EstimateCalculatorService.GetDeliveryDate(cargo, destination).Year);                    

        var trackers = await estimateCalculatorService.GetEstimatesAsync(cargoInput);
        var track = trackers.Select(de => de.Item2).FirstOrDefault()!;
        var cargoData = await cargoRepository.CreateCargoAsync(CargoDao.FromModel(cargo));
        var shippingData = await shippingRepository.CreateShippingAsync(ShippingDao.FromModel(shipping));
        var destinationData = await destinationRepository.CreateDestinationAsync(DestinationDao.FromModel(destination));

        ActiveCargoDao activeCargo = new()
        {
            CargoId = cargoData.Id,
            DestinationId = destinationData.Id,
            ShippingId = shippingData.Id,
            LoadMonth = cargo.LoadDate.Month,
            LoadPricesId = track.IndexLoadPrice??throw new InvalidDataException("Expected Index load price"),
            DischargeMonth = track.DischargeMonth??throw new InvalidDataException("Expected Discharge Month"),
            DischargePricesId = track.IndexDischargePrice??throw new InvalidDataException("Expected Index discharge price"),
        };
        activeCargo.SetLastEstimateVersion(1);

        var activeCargoData = await activeCargoRepository.CreateActiveCargoAsync(activeCargo);
        var estimate = Track.ToEstimate(track);
        var estimateDao = EstimateDao.FromModel(estimate, activeCargoData.Id, activeCargo.LastEstimateVersion);
        // The insert reads back the database-filled CreationInstant
        var estimateData = await estimateRepository.CreateEstimateAsync(estimateDao);
        await tx.CommitAsync();
        return EstimateDao.ToModel(estimateData);
    }
}