using NodaTime;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Persistence.Model;

public class PriceDao
{
    public  int Id { get; init; }
    public Index Index { get; init; }
    public  int Year { get; init; }
    public  decimal? Jan { get; private set; }
    public  decimal? Feb { get; private set; }
    public  decimal? Mar { get; private set; }
    public  decimal? Apr { get; private set; }
    public  decimal? May { get; private set; }
    public  decimal? Jun { get; private set; }
    public  decimal? Jul { get; private set; }
    public  decimal? Aug { get; private set; }
    public  decimal? Sep { get; private set; }
    public  decimal? Oct { get; private set; }
    public  decimal? Nov { get; private set; }
    public  decimal? Dec { get; private set; }
    public Instant CreationInstant { get; init; }

    private PriceDao() { }
    public PriceDao(Index index, int year)
    {
        this.Index = index;
        this.Year = year;
    }

    public void SetPrice(int month, decimal? amount)
    {
        switch (month)
        {
            case 1: Jan = amount; break;
            case 2: Feb = amount; break;
            case 3: Mar = amount; break;
            case 4: Apr = amount; break;
            case 5: May = amount; break;
            case 6: Jun = amount; break;
            case 7: Jul = amount; break;
            case 8: Aug = amount; break;
            case 9: Sep = amount; break;
            case 10: Oct = amount; break;
            case 11: Nov = amount; break;
            case 12: Dec = amount; break;
            default: throw new ArgumentException($"Invalid month {month}");
        }
    }
    
    public decimal? GetPrice(int month)
    {
        return month switch
        {
            1 => Jan,
            2 => Feb,
            3 => Mar,
            4 => Apr,
            5 => May,
            6 => Jun,
            7 => Jul,
            8 => Aug,
            9 => Sep,
            10 => Oct,
            11 => Nov,
            12 => Dec,
            _ => throw new ArgumentException($"Invalid month {month}")
        };
    }
}