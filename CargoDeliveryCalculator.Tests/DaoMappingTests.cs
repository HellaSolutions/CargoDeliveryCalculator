using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Util;
using Destination = CargoDeliveryCalculator.Model.Destination;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Tests;

[TestFixture]
public class DaoMappingTests
{
    [Test]
    public void Cargo_RoundTrips()
    {
        var cargo = new Cargo("C-001", DateHandler.GetLocalDate("2026-11-10"), 160000m, 22.0m,
            new PurchaseFormula(1.15m, Index.Hh, 2.50m));

        Assert.That(CargoDao.ToModel(CargoDao.FromModel(cargo)), Is.EqualTo(cargo));
    }

    [Test]
    public void Destination_RoundTrips()
    {
        var destination = new Destination("Japan", new PurchaseFormula(1.0m, Index.Jkm, 0), 25, 0, 600000m);

        Assert.That(DestinationDao.ToModel(DestinationDao.FromModel(destination)), Is.EqualTo(destination));
    }

    [Test]
    public void Shipping_RoundTrips()
    {
        var shipping = new Shipping(60000m, 0.001m);

        Assert.That(ShippingDao.ToModel(ShippingDao.FromModel(shipping)), Is.EqualTo(shipping));
    }

    [Test]
    public void Estimate_RoundTrips()
    {
        var estimate = new Estimate(3432000, 12.0m, 41184000, 20944000, 5.95m, 3000000, 0, 600000, 16640000, 10.6773m);

        var dao = EstimateDao.FromModel(estimate, activeCargoId: 7, version: 2);

        Assert.Multiple(() =>
        {
            Assert.That(dao.ActiveCargoId, Is.EqualTo(7));
            Assert.That(dao.Version, Is.EqualTo(2));
            Assert.That(EstimateDao.ToModel(dao).Estimate, Is.EqualTo(estimate));
            Assert.That(EstimateDao.ToModel(dao).Version, Is.EqualTo(2));
        });
    }

    [Test]
    public void ToModel_InvalidRow_ThrowsModelValidation()
    {
        var dao = new ShippingDao { DailyHireRate = 0, BoilOffRate = 0.001m };

        Assert.That(() => ShippingDao.ToModel(dao),
            Throws.ArgumentException.With.Message.EqualTo("Daily rate should be positive"));
    }
}
