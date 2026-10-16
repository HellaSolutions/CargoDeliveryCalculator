using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace CargoDeliveryCalculator.Model;


public record Cargo {
    public const int MaxCargoIdLength = 64;

    public string CargoId { get; init; }
    public LocalDate LoadDate { get; init; }
    public decimal LoadedVolume { get; init; }
    public decimal ConversionFactor { get; init; }
    public PurchaseFormula PurchaseFormula { get; init; }
    public decimal LoadedEnergy { get; init; }
    
    public Cargo(
        string CargoId, 
        LocalDate LoadDate,
        decimal LoadedVolume, 
        decimal ConversionFactor, 
        PurchaseFormula PurchaseFormula)
    {
        if (CargoId.Length > MaxCargoIdLength)
        {
            throw new ArgumentException($"Cargo id should be at most {MaxCargoIdLength} characters");
        }
        if (LoadedVolume <= 0)
        {
            throw new ArgumentException("Loaded volume should be positive");
        }
        if (ConversionFactor <= 0)
        {
            throw new ArgumentException("Conversion factor should be positive");
        }

        this.CargoId = CargoId;
        this.LoadDate = LoadDate;
        this.LoadedVolume = LoadedVolume;
        this.ConversionFactor = ConversionFactor;
        this.PurchaseFormula = PurchaseFormula;
        this.LoadedEnergy = LoadedVolume * ConversionFactor;
    }
}
