using NodaTime;

namespace CargoDeliveryCalculator.Persistence.Model;

public class ActiveCargoDao
{
    public int Id { get; init; }
    public required int CargoId { get; init; }
    public int DestinationId { get; init; }
    public int ShippingId { get; init; }
    public int LoadMonth { get; init; }
    public int LoadPricesId { get; init; }
    public int DischargeMonth { get; init; }
    public int DischargePricesId { get; init; }

    public int LastEstimateVersion { get; private set; }

    public decimal? DestinationMmBtu { get; private set; }
    public Instant CreationInstant { get; init; }

    // Navigations: only populated when loaded with Include(...) or used in a query; inserts only need the Id fields
    public CargoDao Cargo { get; init; } = null!;
    public DestinationDao Destination { get; init; } = null!;
    public ShippingDao Shipping { get; init; } = null!;
    public PriceDao LoadPrices { get; init; } = null!;
    public PriceDao DischargePrices { get; init; } = null!;

    public void SetDestinationMmBtu(decimal destinationMmBtu)
    {
        this.DestinationMmBtu = destinationMmBtu;
    }

    public void SetLastEstimateVersion(int lastEstimateVersion)
    {
        this.LastEstimateVersion = lastEstimateVersion;
    }
}