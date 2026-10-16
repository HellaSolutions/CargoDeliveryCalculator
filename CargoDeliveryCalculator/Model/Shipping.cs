namespace CargoDeliveryCalculator.Model;

public record Shipping
{
    public decimal DailyHireRate { get; init; }
    public decimal BoilOffRate { get; init; }

    public Shipping(
        decimal DailyHireRate, 
        decimal BoilOffRate)
    {
        if (DailyHireRate <= 0)
        {
            throw new ArgumentException("Daily rate should be positive");
        }
        if (BoilOffRate is < 0.0m or > 1.0m)
        {
            throw new ArgumentException("The Boil off rate must be between 0.0 and 1.0.");
        }

        this.DailyHireRate = DailyHireRate;
        this.BoilOffRate = BoilOffRate;
    }
}
