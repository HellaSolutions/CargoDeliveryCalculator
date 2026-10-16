namespace CargoDeliveryCalculator.Model;

public record ChangeValue(decimal OldValue, decimal NewValue)
{
    public decimal Difference => NewValue - OldValue;
}

public record EstimateDiff(
    VersionedEstimate OldVersion,
    VersionedEstimate NewVersion,
    string ElapsedTime,
    ChangeValue? DeliveredEnergy,
    ChangeValue? SalePrice,
    ChangeValue? Revenue,
    ChangeValue? PurchaseCost,
    ChangeValue? PurchasePrice,
    ChangeValue? ShippingCost,
    ChangeValue? RegasCost,
    ChangeValue? CanalFee,
    ChangeValue? Margin,
    ChangeValue? Netback);