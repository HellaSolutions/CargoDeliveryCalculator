namespace CargoDeliveryCalculator.Model;

public record Estimate(
    decimal DeliveredEnergy,
    decimal SalePrice,
    decimal Revenue,
    decimal PurchaseCost,
    decimal PurchasePrice,
    decimal ShippingCost,
    decimal RegasCost,
    decimal CanalFee,
    decimal Margin,
    decimal Netback);