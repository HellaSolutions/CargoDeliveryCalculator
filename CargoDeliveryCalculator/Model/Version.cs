using NodaTime;

namespace CargoDeliveryCalculator.Model;

public record VersionedEstimate(Estimate Estimate, int Version, LocalDateTime CreationDateTime);