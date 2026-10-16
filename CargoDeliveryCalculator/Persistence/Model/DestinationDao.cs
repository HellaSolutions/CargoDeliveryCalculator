using CargoDeliveryCalculator.Model;
using NodaTime;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Persistence.Model;

public record DestinationDao
{
    public int Id { get; init; }
    public required string Name { get; init; }
 
    public int LadenVoyageDurationInDays { get; init; }
    public decimal RegasFee { get; init; }
    public decimal CanalFee { get; init; }
    public decimal Slope { get; init; }
    
    public Index Index { get; init; }
    
    public decimal Constant { get; init; }
    
    public Instant CreationInstant { get; init; }

    // Id and CreationInstant are left unset: the database generates them
    public static DestinationDao FromModel(Destination destination) => new()
    {
        Name = destination.Name,
        LadenVoyageDurationInDays = destination.LadenVoyageDurationInDays,
        RegasFee = destination.RegasFee,
        CanalFee = destination.CanalFee,
        Slope = destination.SaleFormula.Slope,
        Index = destination.SaleFormula.Index,
        Constant = destination.SaleFormula.Constant
    };

    public static Destination ToModel(DestinationDao dao) => new(
        dao.Name,
        new PurchaseFormula(dao.Slope, dao.Index, dao.Constant),
        dao.LadenVoyageDurationInDays,
        dao.RegasFee,
        dao.CanalFee);
}
