using CargoDeliveryCalculator.Model;

namespace CargoDeliveryCalculator.Services;

public record CargoInput(Cargo Cargo, Shipping Shipping, List<Destination> Destinations);