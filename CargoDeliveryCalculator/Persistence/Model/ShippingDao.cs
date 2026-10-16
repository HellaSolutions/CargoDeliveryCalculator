using CargoDeliveryCalculator.Model;
using NodaTime;

namespace CargoDeliveryCalculator.Persistence.Model;

public record ShippingDao
{
    public int Id  { get; init; }
    public decimal DailyHireRate { get; init; }
    public decimal BoilOffRate { get; init; }
    public Instant CreationInstant { get; init; }

    // Id and CreationInstant are left unset: the database generates them
    public static ShippingDao FromModel(Shipping shipping) => new()
    {
        DailyHireRate = shipping.DailyHireRate,
        BoilOffRate = shipping.BoilOffRate
    };

    public static Shipping ToModel(ShippingDao dao) => new(dao.DailyHireRate, dao.BoilOffRate);
}
