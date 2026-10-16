using System.Net;
using System.Net.Http.Json;
using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Tests;

// Changing a price an active cargo depends on must store a new estimate version for it.
// Each test works in its own year so it has its own price rows and can't affect the others.
[TestFixture]
public class PriceEventItTests : IntegrationTestsBase
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        _client.Dispose();
    }

    private static readonly Shipping Shipping = new(60000m, 0.001m);

    // From a 10 November load: Japan delivers 5 December (Jkm), NWEurope 24 November (Ttf)
    private static readonly Destination Japan = new("Japan", new PurchaseFormula(1.0m, Index.Jkm, 0), 25, 0, 600000m);
    private static readonly Destination NwEurope = new("NWEurope", new PurchaseFormula(1.0m, Index.Ttf, -0.40m), 14, 0.45m, 0);

    private static CargoDto NewCargo(int year) =>
        new(Guid.NewGuid().ToString(), $"{year}-11-10", 160000m, 22.0m, new PurchaseFormula(1.15m, Index.Hh, 2.50m));

    private AppDbContext NewDbContext() =>
        _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private async Task<HttpResponseMessage> PutPrice(Index index, int year, int month, decimal amount) =>
        await _client.PutAsJsonAsync($"/api/dynamics/indexes/{index}/{year}/{month}", amount);

    private async Task SeedPrices(int year)
    {
        Assert.That((await PutPrice(Index.Hh, year, 11, 3.00m)).IsSuccessStatusCode, Is.True);
        Assert.That((await PutPrice(Index.Ttf, year, 11, 11.00m)).IsSuccessStatusCode, Is.True);
        Assert.That((await PutPrice(Index.Jkm, year, 12, 12.00m)).IsSuccessStatusCode, Is.True);
    }

    // Activates the cargo and returns the ActiveCargos row id
    private async Task<int> Activate(CargoDto cargo, Destination destination)
    {
        var response = await _client.PostAsJsonAsync("/api/recommendation/active",
            new CargoInputDto(cargo, Shipping, destination));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await using var db = NewDbContext();
        return await db.ActiveCargos.AsNoTracking()
            .Where(a => a.Cargo.CargoId == cargo.CargoId)
            .Select(a => a.Id)
            .SingleAsync();
    }

    private async Task<List<EstimateDao>> Estimates(int activeCargoId)
    {
        await using var db = NewDbContext();
        return await db.Estimates.AsNoTracking()
            .Where(e => e.ActiveCargoId == activeCargoId)
            .OrderBy(e => e.Version)
            .ToListAsync();
    }

    private async Task<int> LastEstimateVersion(int activeCargoId)
    {
        await using var db = NewDbContext();
        return await db.ActiveCargos.AsNoTracking()
            .Where(a => a.Id == activeCargoId)
            .Select(a => a.LastEstimateVersion)
            .SingleAsync();
    }

    // What the recommendation endpoint computes right now for this cargo and destination
    private async Task<Estimate> CurrentRecommendation(CargoDto cargo, Destination destination)
    {
        var response = await _client.PostAsJsonAsync("/api/recommendation",
            new CargoInputsDto(cargo, Shipping, [destination]));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return (await response.Content.ReadFromJsonAsync<CargoOutputDto>())!.Estimates.Single().Estimate;
    }

    [Test]
    public async Task DischargePriceChange_AddsVersion2_WithRecalculatedEstimate()
    {
        const int year = 2030;
        await SeedPrices(year);
        var cargo = NewCargo(year);
        var activeCargoId = await Activate(cargo, Japan);

        var response = await PutPrice(Index.Jkm, year, 12, 13.00m);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var estimates = await Estimates(activeCargoId);
        var expected = await CurrentRecommendation(cargo, Japan);
        Assert.Multiple(async () =>
        {
            Assert.That(estimates.Select(e => e.Version), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(estimates[0].EstimateJson.SalePrice, Is.EqualTo(12.00m));
            Assert.That(estimates[1].EstimateJson.SalePrice, Is.EqualTo(13.00m));
            Assert.That(estimates[1].EstimateJson, Is.EqualTo(expected));
            Assert.That(await LastEstimateVersion(activeCargoId), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task LoadPriceChange_AddsVersion2_WithRecalculatedPurchaseCost()
    {
        const int year = 2031;
        await SeedPrices(year);
        var cargo = NewCargo(year);
        var activeCargoId = await Activate(cargo, Japan);

        var response = await PutPrice(Index.Hh, year, 11, 4.00m);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var estimates = await Estimates(activeCargoId);
        var expected = await CurrentRecommendation(cargo, Japan);
        Assert.Multiple(async () =>
        {
            Assert.That(estimates.Select(e => e.Version), Is.EqualTo(new[] { 1, 2 }));
            // 1.15 × 4.00 + 2.50
            Assert.That(estimates[1].EstimateJson.PurchasePrice, Is.EqualTo(7.10m));
            Assert.That(estimates[1].EstimateJson, Is.EqualTo(expected));
            Assert.That(await LastEstimateVersion(activeCargoId), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ConsecutivePriceChanges_KeepIncrementingTheVersion()
    {
        const int year = 2032;
        await SeedPrices(year);
        var activeCargoId = await Activate(NewCargo(year), Japan);

        Assert.That((await PutPrice(Index.Jkm, year, 12, 13.00m)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await PutPrice(Index.Jkm, year, 12, 14.00m)).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var estimates = await Estimates(activeCargoId);
        Assert.Multiple(async () =>
        {
            Assert.That(estimates.Select(e => e.Version), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(estimates.Select(e => e.EstimateJson.SalePrice), Is.EqualTo(new[] { 12.00m, 13.00m, 14.00m }));
            Assert.That(await LastEstimateVersion(activeCargoId), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task PriceChange_UpdatesEveryActiveCargoUsingIt_AndNoOther()
    {
        const int year = 2033;
        await SeedPrices(year);
        var japanA = await Activate(NewCargo(year), Japan);
        var japanB = await Activate(NewCargo(year), Japan);
        var nwEurope = await Activate(NewCargo(year), NwEurope);

        Assert.That((await PutPrice(Index.Jkm, year, 12, 13.00m)).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.Multiple(async () =>
        {
            Assert.That((await Estimates(japanA)).Select(e => e.Version), Is.EqualTo(new[] { 1, 2 }));
            Assert.That((await Estimates(japanB)).Select(e => e.Version), Is.EqualTo(new[] { 1, 2 }));
            // Sells on Ttf, so a Jkm change doesn't concern it
            Assert.That((await Estimates(nwEurope)).Select(e => e.Version), Is.EqualTo(new[] { 1 }));
            Assert.That(await LastEstimateVersion(japanA), Is.EqualTo(2));
            Assert.That(await LastEstimateVersion(japanB), Is.EqualTo(2));
            Assert.That(await LastEstimateVersion(nwEurope), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task PriceChangeForAnotherMonthOfTheSameYear_DoesNotAddAVersion()
    {
        const int year = 2035;
        await SeedPrices(year);
        var activeCargoId = await Activate(NewCargo(year), Japan);

        // Same Jkm row as the cargo's December discharge price, but March
        Assert.That((await PutPrice(Index.Jkm, year, 3, 9.00m)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        // Same Hh row as the cargo's November load price, but June
        Assert.That((await PutPrice(Index.Hh, year, 6, 2.00m)).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.Multiple(async () =>
        {
            Assert.That((await Estimates(activeCargoId)).Select(e => e.Version), Is.EqualTo(new[] { 1 }));
            Assert.That(await LastEstimateVersion(activeCargoId), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task PriceChangeAfterDischarge_KeepsMeasuredDeliveredEnergy()
    {
        const int year = 2036;
        await SeedPrices(year);
        var cargo = NewCargo(year);
        var activeCargoId = await Activate(cargo, Japan);

        const decimal measuredMmBtu = 3_000_000m;
        var response = await _client.PostAsJsonAsync($"/api/dynamics/discharge/cargos/{cargo.CargoId}", measuredMmBtu);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.That((await PutPrice(Index.Jkm, year, 12, 13.00m)).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var estimates = await Estimates(activeCargoId);
        Assert.Multiple(async () =>
        {
            Assert.That(estimates.Select(e => e.Version), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(estimates[2].EstimateJson.SalePrice, Is.EqualTo(13.00m));
            // Without the measured energy the recalculation would fall back to the boil-off estimate
            Assert.That(estimates[2].EstimateJson.DeliveredEnergy, Is.EqualTo(measuredMmBtu));
            Assert.That(estimates[2].EstimateJson.Revenue, Is.EqualTo(measuredMmBtu * 13.00m));
            Assert.That(await LastEstimateVersion(activeCargoId), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task SettingTheSamePrice_DoesNotAddAVersion()
    {
        const int year = 2034;
        await SeedPrices(year);
        var activeCargoId = await Activate(NewCargo(year), Japan);

        await PutPrice(Index.Jkm, year, 12, 12.00m);

        Assert.Multiple(async () =>
        {
            Assert.That((await Estimates(activeCargoId)).Select(e => e.Version), Is.EqualTo(new[] { 1 }));
            Assert.That(await LastEstimateVersion(activeCargoId), Is.EqualTo(1));
        });
    }
}
