using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Persistence.Repositories;
using CargoDeliveryCalculator.Util;
using NodaTime;

namespace CargoDeliveryCalculator.Services;

public interface IEstimateService
{
    Task<VersionedEstimate> GetLastEstimateAsync(string cargoId);
    Task<VersionedEstimate> GetEstimateAsync(string cargoId, LocalDateTime dateTime);
    Task<VersionedEstimate> GetEstimateAsync(string cargoId, int version);
    Task<EstimateDiff> GetDeltaAsync(string cargoId, int versionA, int versionB);
    Task<List<VersionedEstimate>> GetEstimateAsync(string cargoId, LocalDateTime fromDt, LocalDateTime toDt);
}

public class EstimateService(
    IActiveCargoRepository activeCargoRepository) : IEstimateService
{
    public async Task<VersionedEstimate> GetLastEstimateAsync(string cargoId)
    {
        var estimateDao = await activeCargoRepository.GetLastEstimate(cargoId)??
                          throw new  UnknownActiveCargoException($"Invalid cargoId {cargoId}, there is no active cargo");
        return EstimateDao.ToModel(estimateDao);
    }

    public async Task<VersionedEstimate> GetEstimateAsync(string cargoId, LocalDateTime dateTime)
    {
        var instant = DateHandler.ToInstant(dateTime);
        var estimateDao = await activeCargoRepository.GetEstimate(cargoId, instant)??
                          throw new  UnknownActiveCargoException($"No estimate for cargo {cargoId} at {instant}");
        return EstimateDao.ToModel(estimateDao);
    }

    public async Task<EstimateDiff> GetDeltaAsync(string cargoId, int versionA, int versionB)
    {
        var estimateDaoA = await GetEstimateDaoAsync(cargoId, versionA);
        var estimateDaoB = await GetEstimateDaoAsync(cargoId, versionB);

        var elapsed = estimateDaoB.CreationInstant - estimateDaoA.CreationInstant;

        return new EstimateDiff(
            OldVersion: EstimateDao.ToModel(estimateDaoA),
            NewVersion: EstimateDao.ToModel(estimateDaoB),
            ElapsedTime: DateHandler.FormatElapsed(elapsed),
            DeliveredEnergy: Diff(e => e.DeliveredEnergy),
            SalePrice: Diff(e => e.SalePrice),
            Revenue: Diff(e => e.Revenue),
            PurchaseCost: Diff(e => e.PurchaseCost),
            PurchasePrice: Diff(e => e.PurchasePrice),
            ShippingCost: Diff(e => e.ShippingCost),
            RegasCost: Diff(e => e.RegasCost),
            CanalFee: Diff(e => e.CanalFee),
            Margin: Diff(e => e.Margin),
            Netback: Diff(e => e.Netback));

        ChangeValue? Diff(Func<Estimate, decimal> field) =>
            field(estimateDaoA.EstimateJson) == field(estimateDaoB.EstimateJson) ? null : new ChangeValue(field(estimateDaoA.EstimateJson), field(estimateDaoB.EstimateJson));
    }
    
    public async Task<List<VersionedEstimate>> GetEstimateAsync(string cargoId, LocalDateTime fromDt, LocalDateTime toDt)
    {
        if (fromDt >= toDt)
        {
            throw new ArgumentException($"fromDateTime {fromDt} must be before toDateTime {toDt}");
        }
        var fromInstant = DateHandler.ToInstant(fromDt);
        var toInstant = DateHandler.ToInstant(toDt);
        var estimateDaos = await activeCargoRepository.GetEstimate(cargoId, fromInstant, toInstant);
        // An empty range is a valid answer for a known cargo, but an unknown cargo is a 404
        if (estimateDaos.Count == 0 && await activeCargoRepository.GetLastEstimate(cargoId) is null)
        {
            throw new UnknownActiveCargoException($"Invalid cargoId {cargoId}, there is no active cargo");
        }
        return [.. estimateDaos.Select(EstimateDao.ToModel)];
    }

    public async Task<VersionedEstimate> GetEstimateAsync(string cargoId, int version)
    {
        var estimateDao = await GetEstimateDaoAsync(cargoId, version);
        return EstimateDao.ToModel(estimateDao);
    }

    private async Task<EstimateDao> GetEstimateDaoAsync(string cargoId, int version)
    {
        return await activeCargoRepository.GetEstimate(cargoId, version)??
            throw new  UnknownActiveCargoException($"No estimate version {version} for cargo {cargoId}");
    }
}