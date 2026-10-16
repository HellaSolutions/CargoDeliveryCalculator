using CargoDeliveryCalculator.Model;
using NodaTime;

namespace CargoDeliveryCalculator.Persistence.Model;

public class EstimateDao
{
    public int Id { get; init; }
    public int ActiveCargoId { get; init; }
    public int Version { get; init; }
    public Instant CreationInstant { get; init; }
    public required Estimate EstimateJson { get; init; }

    // The estimate alone doesn't say which active cargo or version it belongs to, so the caller supplies them
    public static EstimateDao FromModel(Estimate estimate, int activeCargoId, int version) => new()
    {
        ActiveCargoId = activeCargoId,
        Version = version,
        EstimateJson = estimate
    };

    public static VersionedEstimate ToModel(EstimateDao dao) => new(dao.EstimateJson, dao.Version, dao.CreationInstant.InUtc().LocalDateTime);
}
