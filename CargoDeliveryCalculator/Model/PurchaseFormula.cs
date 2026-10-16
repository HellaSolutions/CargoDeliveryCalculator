namespace CargoDeliveryCalculator.Model;

public record PurchaseFormula
{
    public decimal Slope { get; init; }
    public Index Index { get; init; }
    public decimal Constant { get; init; }

    public PurchaseFormula(
        decimal Slope, 
        Index Index, 
        decimal Constant)
    {
        if (Slope < 0)
        {
            throw new ArgumentException("Slope should be non negative");
        }

        this.Slope = Slope;
        this.Index = Index;
        this.Constant = Constant;
    }
}
