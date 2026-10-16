using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Services;

public class IndexUnavailableException(string message) : Exception(message)
{
    // No price row at all for that index and year
    public static IndexUnavailableException ForYear(Index index, int year) =>
        new($"no {index} value for {year}");

    // The year exists but that month was never set
    public static IndexUnavailableException ForMonth(Index index, int year, int month) =>
        new($"no {index} value for {month}-{year}");
}
