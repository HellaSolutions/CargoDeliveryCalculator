namespace CargoDeliveryCalculator.Persistence.Model;

using CargoDeliveryCalculator.Model;
using NodaTime;
using Index = CargoDeliveryCalculator.Model.Index;

public class CargoDao {
    
    public int Id { get; init; }
    
    public required string CargoId { get; init; }
    
    public LocalDate LoadDate { get; init; }
    
    public decimal LoadedVolume { get; init; }
    
    public decimal ConversionFactor { get; init; }

    public decimal Slope { get; init; }
    
    public Index Index { get; init; }
    
    public decimal Constant { get; init; }
    
    public Instant CreationInstant { get; init; }

    // Id and CreationInstant are left unset: the database generates them
    public static CargoDao FromModel(Cargo cargo) => new()
    {
        CargoId = cargo.CargoId,
        LoadDate = cargo.LoadDate,
        LoadedVolume = cargo.LoadedVolume,
        ConversionFactor = cargo.ConversionFactor,
        Slope = cargo.PurchaseFormula.Slope,
        Index = cargo.PurchaseFormula.Index,
        Constant = cargo.PurchaseFormula.Constant
    };

    // LoadedEnergy is not stored; the Cargo constructor recomputes it
    public static Cargo ToModel(CargoDao dao) => new(
        dao.CargoId,
        dao.LoadDate,
        dao.LoadedVolume,
        dao.ConversionFactor,
        new PurchaseFormula(dao.Slope, dao.Index, dao.Constant));
}
