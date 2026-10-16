namespace CargoDeliveryCalculator.Services;

public class UnknownActiveCargoException(string message) : Exception(message)
{
}