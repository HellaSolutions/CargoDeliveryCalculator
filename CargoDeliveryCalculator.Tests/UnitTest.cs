using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Persistence.Repositories;
using CargoDeliveryCalculator.Services;
using CargoDeliveryCalculator.Util;
using Moq;
using Destination = CargoDeliveryCalculator.Model.Destination;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Tests;

public class Tests
{
    private MarketDataService _marketDataService;
    private Mock<IMarketDataRepository> _priceRepoMock;
    private EstimateCalculatorService _estimateCalculatorService;

    [SetUp]
    public void Setup()
    {
        _priceRepoMock = new Mock<IMarketDataRepository>();
        _marketDataService = new MarketDataService(_priceRepoMock.Object);
        _estimateCalculatorService = new EstimateCalculatorService(_marketDataService);
    }

    private static PriceDao Price(Index index, int year, int month, decimal amount)
    {
        var price = new PriceDao(index, year);
        price.SetPrice(month, amount);
        return price;
    }

    [Test]
    public async Task BasicOutput()
    {
        _priceRepoMock.Setup(m => m.GetIndexPriceAsync(Index.Hh, 2026))
            .ReturnsAsync(Price(Index.Hh, 2026, 11, 3.00m));
        _priceRepoMock.Setup(m => m.GetIndexPriceAsync(Index.Ttf, 2026))
            .ReturnsAsync(Price(Index.Ttf, 2026, 11, 11.00m));
        _priceRepoMock.Setup(m => m.GetIndexPriceAsync(Index.Jkm, 2026))
            .ReturnsAsync(Price(Index.Jkm, 2026, 12, 12.00m));

        // Strict mocks: computing a recommendation must not persist anything
        var uut = new CargoOutputService(
            new Mock<IUnitOfWork>(MockBehavior.Strict).Object,
            _marketDataService,
            _estimateCalculatorService,
            new Mock<IActiveCargoRepository>(MockBehavior.Strict).Object,
            new Mock<ICargoRepository>(MockBehavior.Strict).Object,
            new Mock<IShippingRepository>(MockBehavior.Strict).Object,
            new Mock<IDestinationRepository>(MockBehavior.Strict).Object,
            new Mock<IEstimateRepository>(MockBehavior.Strict).Object);

        var purchaseFormula = new PurchaseFormula(1.15m, Index.Hh, 2.50m);
        var cargo = new Cargo("cargoId", DateHandler.GetLocalDate("2026-11-10"), 160000m, 22.0m,
            purchaseFormula);
        var shipping = new Shipping(60000m, 0.001m);

        var saleFormulaNwEu = new PurchaseFormula(1.0m, Index.Ttf, -0.40m);
        var nwEurope = new Destination("NWEurope", saleFormulaNwEu, 14, 0.45m, 0);
        
        var saleFormulaJapan = new PurchaseFormula(1.0m, Index.Jkm, 0);
        var japan = new Destination("Japan", saleFormulaJapan, 25, 0, 600000m);
        
        var cargoOutput = await uut.CargoOutputAsync(new CargoInput(cargo, shipping, [nwEurope, japan]));
        Assert.That(cargoOutput.BestDestination, Is.EqualTo(japan));
        Assert.That(cargoOutput.Estimates.Select(de => de.Item1).ToList(), 
            Is.EqualTo(new []{japan, nwEurope}));
        
        Assert.Multiple( () =>
        {
            Assert.That(cargoOutput.Estimates[1].Item2.Margin, Is.EqualTo(12603808).Within(0.0001m));
            Assert.That(cargoOutput.Estimates[1].Item2.Netback, Is.EqualTo(9.5306).Within(0.0001m));
            Assert.That(cargoOutput.Estimates[0].Item2.Margin, Is.EqualTo(16640000).Within(0.0001m));
            Assert.That(cargoOutput.Estimates[0].Item2.Netback, Is.EqualTo(10.6772).Within(0.0001m));
            
        });
    }
}
