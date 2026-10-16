using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Persistence.Repositories;

using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Services;

public interface IDynamicsService
{
    Task<bool> SetIndexPriceAsync(string index, decimal amount, int year, int month);
    Task<VersionedEstimate> DischargeAsync(string cargoId, decimal mmBtu);
}

public class DynamicsService(
    IUnitOfWork unitOfWork,
    IEstimateCalculatorService estimateCalculatorService, 
    IMarketDataService marketDataService,
    IActiveCargoRepository activeCargoRepository,
    ICargoRepository cargoRepository, 
    IEstimateRepository estimateRepository) : IDynamicsService
{
    public async Task<VersionedEstimate> DischargeAsync(string cargoId, decimal mmBtu)
    {
        if (mmBtu < 0) throw new ArgumentException($"Invalid mmBtu {mmBtu}");
        await using var tx = await unitOfWork.BeginTransactionAsync();
        await activeCargoRepository.LockForDischargeAsync(cargoId);
        var (activeCargoDao, cargoDao, destinationDao, shippingDao) = await activeCargoRepository.GetActiveCargoAsync(cargoId)??
                          throw new UnknownActiveCargoException($"Invalid cargoId {cargoId}, there is no active cargo");
        var cargo = CargoDao.ToModel(cargoDao);
        var destination = DestinationDao.ToModel(destinationDao);
        var shipping = ShippingDao.ToModel(shippingDao);
        
        activeCargoDao.SetDestinationMmBtu(mmBtu);
        var trackers = await estimateCalculatorService.GetEstimatesAsync(new CargoInput(cargo, shipping, [destination]), 
            new Track() {
                DeliveredEnergy = mmBtu
        });
        var estimateDao = await CreateEstimate(trackers.Select(de => de.Item2).FirstOrDefault()!, activeCargoDao);
        await tx.CommitAsync();
        return EstimateDao.ToModel(estimateDao);
    }

    public async Task<bool> SetIndexPriceAsync(string index, decimal amount, int year, int month)
    {
        return !Enum.TryParse<Index>(index, true, out var tradeIndex) ? 
            throw new ArgumentException($"Invalid index {index}") : 
            await SetIndexPriceAsync(tradeIndex, amount, year, month);
    }

    private async Task<bool> SetIndexPriceAsync(Index index, decimal amount, int year, int month)
    {

        if (month is < 1 or > 12)
        {
            throw new ArgumentException($"Invalid month {month}");
        }
        if (year is < 1990)
        {
            throw new ArgumentException($"Invalid year {year}");
        }

        await using var tx = await unitOfWork.BeginTransactionAsync();
        var result = await marketDataService.SetIndexPriceAsync(amount, year, month, index);
        if (result.Changed) {
            await PriceChangedAsync(result.Price, month);
        }
        await tx.CommitAsync();
        return result.Created;
    }
    
    private async Task PriceChangedAsync(PriceDao priceDao, int month)
    {
        var activeCargosByPrice = await activeCargoRepository.GetActiveCargosByPriceIdAsync(priceDao, month);
        foreach (var activeCargo in activeCargosByPrice)
        {
            var cargo = CargoDao.ToModel(activeCargo.Cargo);
            var shipping = ShippingDao.ToModel(activeCargo.Shipping);
            var destination = DestinationDao.ToModel(activeCargo.Destination);
            var cargoInput = new CargoInput(cargo, shipping, [destination]);
            var trackers = await estimateCalculatorService.GetEstimatesAsync(cargoInput, new Track()
            {
                DeliveredEnergy = activeCargo.DestinationMmBtu
            });
            await CreateEstimate(trackers.Select(de => de.Item2).FirstOrDefault()!, activeCargo);
        }
    }

    private async Task<EstimateDao> CreateEstimate(Track track, ActiveCargoDao activeCargoDao)
    {
        var estimate = Track.ToEstimate(track);
        var lastEstimateVersion = activeCargoDao.LastEstimateVersion + 1;
        var estimateDao = EstimateDao.FromModel(estimate, activeCargoDao.Id, activeCargoDao.LastEstimateVersion + 1);
        activeCargoDao.SetLastEstimateVersion(lastEstimateVersion);
        return await estimateRepository.CreateEstimateAsync(estimateDao);
    }
}