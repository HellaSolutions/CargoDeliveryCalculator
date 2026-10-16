using CargoDeliveryCalculator.Model;

namespace CargoDeliveryCalculator.Services;

public record Track
{
    public decimal? DeliveredEnergy  { get; set; }
    public decimal? SalePrice  { get; set; }
    public decimal? Revenue  { get; set; }
    public decimal? PurchaseCost  { get; set; }
    public decimal? PurchasePrice  { get; set; }
    public decimal? ShippingCost  { get; set; }
    public decimal? RegasCost  { get; set; }
    public decimal? CanalFee  { get; set; }
    public decimal? Margin  { get; set; }
    public decimal? Netback  { get; set; }
    public int? IndexLoadPrice  { get; set; }
    public int? IndexDischargePrice  { get; set; }
    public int? DischargeMonth { get; set; }

    // Drops the price row ids and discharge month, which are only needed to persist an active cargo
    public static Estimate ToEstimate(Track track) => new(
        track.DeliveredEnergy.GetValueOrDefault(),
        track.SalePrice.GetValueOrDefault(),
        track.Revenue.GetValueOrDefault(),
        track.PurchaseCost.GetValueOrDefault(),
        track.PurchasePrice.GetValueOrDefault(),
        track.ShippingCost.GetValueOrDefault(),
        track.RegasCost.GetValueOrDefault(),
        track.CanalFee.GetValueOrDefault(),
        track.Margin.GetValueOrDefault(),
        track.Netback.GetValueOrDefault());
}