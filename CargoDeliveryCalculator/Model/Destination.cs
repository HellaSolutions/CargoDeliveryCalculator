namespace CargoDeliveryCalculator.Model;

public record Destination
{
    public string Name { get; init; }
    public PurchaseFormula SaleFormula { get; init; }
    public int LadenVoyageDurationInDays { get; init; }
    public decimal RegasFee { get; init; }
    public decimal CanalFee { get; init; }

    public Destination(
        string Name, 
        PurchaseFormula SaleFormula, 
        int LadenVoyageDurationInDays, 
        decimal RegasFee,
        decimal CanalFee)
    {
        if (LadenVoyageDurationInDays <= 0)
        {
            throw new ArgumentException("Laden duration should be positive");
        }
        if (RegasFee < 0)
        {
            throw new ArgumentException("Regas fee should be not negative");
        }
        if (CanalFee < 0)
        {
            throw new ArgumentException("Canal fee should be not negative");
        }

        this.Name = Name;
        this.SaleFormula = SaleFormula;
        this.LadenVoyageDurationInDays = LadenVoyageDurationInDays;
        this.RegasFee = RegasFee;
        this.CanalFee = CanalFee;
    }
}
