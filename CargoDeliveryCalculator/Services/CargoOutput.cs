using CargoDeliveryCalculator.Model;
using Destination = CargoDeliveryCalculator.Model.Destination;

namespace CargoDeliveryCalculator.Services
{
    public record CargoOutput(
        Cargo Cargo, 
        Shipping Shipping,
        Destination BestDestination,
        IReadOnlyList<(Destination, Estimate)> Estimates);
}

