using System.Net;
using System.Net.Http.Json;
using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Services;
using CargoDeliveryCalculator.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Tests;

[TestFixture]
public class ActiveCargoItTests : IntegrationTestsBase
{
    private const string ActiveUrl = "/api/recommendation/active";

    private HttpClient _client = null!;

    [OneTimeSetUp]
    public new async Task OneTimeSetUp()
    {
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var priceEventService = scope.ServiceProvider.GetRequiredService<IDynamicsService>();
        await priceEventService.SetIndexPriceAsync("Hh", 3.00m, 2026, 11);
        await priceEventService.SetIndexPriceAsync("Jkm", 12.00m, 2026, 12);
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        _client.Dispose();
    }

    // Tests share the fixture's database, so each one uses its own cargo id
    private static CargoDto NewCargo() =>
        new(Guid.NewGuid().ToString(), "2026-11-10", 160000m, 22.0m, new PurchaseFormula(1.15m, Index.Hh, 2.50m));

    private static readonly Shipping Shipping = new(60000m, 0.001m);

    // Loads 2026-11-10, delivers 2026-12-05
    private static readonly Destination Japan = new("Japan", new PurchaseFormula(1.0m, Index.Jkm, 0), 25, 0, 600000m);

    private AppDbContext NewDbContext() =>
        _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Test]
    public async Task SetActive_ReturnsSameEstimateAsRecommendation()
    {
        var cargo = NewCargo();

        var recommendation = await _client.PostAsJsonAsync("/api/recommendation",
            new CargoInputsDto(cargo, Shipping, [Japan]));
        var expected = (await recommendation.Content.ReadFromJsonAsync<CargoOutputDto>())!.Estimates.Single().Estimate;

        var response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, Japan));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var versioned = await response.Content.ReadFromJsonAsync<VersionDto>(Json);
        Assert.That(versioned!.Estimate, Is.EqualTo(expected));
    }

    [Test]
    public async Task SetActive_PersistsAllLinkedRows_WithFirstEstimateVersion()
    {
        var cargo = NewCargo();

        var response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, Japan));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var versioned = await response.Content.ReadFromJsonAsync<VersionDto>(Json);

        await using var db = NewDbContext();
        var cargoRow = await db.Cargos.AsNoTracking().SingleAsync(c => c.CargoId == cargo.CargoId);
        var activeCargo = await db.ActiveCargos.AsNoTracking().SingleAsync(a => a.CargoId == cargoRow.Id);
        var shippingRow = await db.Shippings.AsNoTracking().SingleAsync(s => s.Id == activeCargo.ShippingId);
        var destinationRow = await db.Destinations.AsNoTracking().SingleAsync(d => d.Id == activeCargo.DestinationId);
        var loadPrices = await db.Prices.AsNoTracking().SingleAsync(p => p.Index == Index.Hh && p.Year == 2026);
        var dischargePrices = await db.Prices.AsNoTracking().SingleAsync(p => p.Index == Index.Jkm && p.Year == 2026);
        var estimates = await db.Estimates.AsNoTracking().Where(e => e.ActiveCargoId == activeCargo.Id).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(cargoRow.Index, Is.EqualTo(Index.Hh));
            Assert.That(cargoRow.LoadDate.ToString("yyyy-MM-dd", null), Is.EqualTo(cargo.LoadDate));
            Assert.That(shippingRow.DailyHireRate, Is.EqualTo(Shipping.DailyHireRate));
            Assert.That(shippingRow.BoilOffRate, Is.EqualTo(Shipping.BoilOffRate));
            Assert.That(destinationRow.Name, Is.EqualTo(Japan.Name));
            Assert.That(destinationRow.Index, Is.EqualTo(Index.Jkm));

            Assert.That(activeCargo.LoadMonth, Is.EqualTo(11));
            Assert.That(activeCargo.LoadPricesId, Is.EqualTo(loadPrices.Id));
            Assert.That(activeCargo.DischargeMonth, Is.EqualTo(12));
            Assert.That(activeCargo.DischargePricesId, Is.EqualTo(dischargePrices.Id));
            Assert.That(activeCargo.LastEstimateVersion, Is.EqualTo(1));

            Assert.That(estimates, Has.Count.EqualTo(1));
            Assert.That(estimates[0].Version, Is.EqualTo(1));
            Assert.That(versioned, Is.EqualTo(Expected(estimates[0])));
        });
    }

    [Test]
    public async Task SetActive_MissingPrice_ReturnsNotFoundAndPersistsNothing()
    {
        var cargo = NewCargo();
        // No Nbp price is ever set
        var destination = Japan with { SaleFormula = new PurchaseFormula(1.0m, Index.Nbp, 0) };

        var response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, destination));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("no Nbp value for 2026"));

        await using var db = NewDbContext();
        Assert.That(await db.Cargos.AnyAsync(c => c.CargoId == cargo.CargoId), Is.False);
    }
    
    [Test]
    public async Task Discharge_UsesMeasuredEnergy_AndAppendsEstimateVersion()
    {
        var cargo = NewCargo();
        var response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, Japan));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var initial = (await response.Content.ReadFromJsonAsync<VersionDto>(Json))!.Estimate;

        const decimal measuredMmBtu = 3_000_000m;
        response = await _client.PostAsJsonAsync($"/api/dynamics/discharge/cargos/{cargo.CargoId}", measuredMmBtu);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var versioned = (await response.Content.ReadFromJsonAsync<VersionDto>(Json))!;
        var estimate = versioned.Estimate;

        await using var db = NewDbContext();
        var cargoRow = await db.Cargos.AsNoTracking().SingleAsync(c => c.CargoId == cargo.CargoId);
        var activeCargo = await db.ActiveCargos.AsNoTracking().SingleAsync(a => a.CargoId == cargoRow.Id);
        var latest = await db.Estimates.AsNoTracking()
            .SingleAsync(e => e.ActiveCargoId == activeCargo.Id && e.Version == 2);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.DeliveredEnergy, Is.EqualTo(measuredMmBtu));
            Assert.That(estimate.DeliveredEnergy, Is.Not.EqualTo(initial.DeliveredEnergy));
            // Sale price is unchanged, so revenue scales with the measured energy
            Assert.That(estimate.Revenue, Is.EqualTo(measuredMmBtu * initial.SalePrice));
            Assert.That(estimate.PurchaseCost, Is.EqualTo(initial.PurchaseCost));

            Assert.That(activeCargo.DestinationMmBtu, Is.EqualTo(measuredMmBtu));
            Assert.That(activeCargo.LastEstimateVersion, Is.EqualTo(2));
            Assert.That(latest.EstimateJson, Is.EqualTo(estimate));
            Assert.That(versioned.Version, Is.EqualTo(2));
            Assert.That(versioned.CreationDateTime, Is.EqualTo(latest.CreationInstant.InUtc().LocalDateTime));
        });
    }

    [Test]
    public async Task SetSameCargoActiveTwice_ReturnsBadRequest()
    {
        var cargo = NewCargo();

        var response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, Japan));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        //duplicate
        response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, Japan));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo($"Cannot activate the same cargo twice. CargoId: {cargo.CargoId}"));
    }

    // Activates a cargo and reports its discharge, so it has estimate versions 1 and 2
    private async Task<(CargoDto Cargo, EstimateDao V1, EstimateDao V2)> CargoWithTwoVersions()
    {
        var cargo = NewCargo();
        var response = await _client.PostAsJsonAsync(ActiveUrl, new CargoInputDto(cargo, Shipping, Japan));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        response = await _client.PostAsJsonAsync($"/api/dynamics/discharge/cargos/{cargo.CargoId}", 3_000_000m);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await using var db = NewDbContext();
        var cargoRow = await db.Cargos.AsNoTracking().SingleAsync(c => c.CargoId == cargo.CargoId);
        var activeCargo = await db.ActiveCargos.AsNoTracking().SingleAsync(a => a.CargoId == cargoRow.Id);
        var estimates = await db.Estimates.AsNoTracking()
            .Where(e => e.ActiveCargoId == activeCargo.Id).OrderBy(e => e.Version).ToListAsync();
        return (cargo, estimates[0], estimates[1]);
    }

    // The API reads a zone-less date time as UTC
    private static string Format(Instant instant) =>
        LocalDateTimePattern.ExtendedIso.Format(instant.InUtc().LocalDateTime);

    private static string VersionsUrl(string cargoId) => $"/api/estimate/cargos/{cargoId}/versions";

    // What the history endpoints should return for a stored estimate row
    private static VersionDto Expected(EstimateDao dao) =>
        new(dao.EstimateJson, dao.Version, dao.CreationInstant.InUtc().LocalDateTime);

    [Test]
    public async Task EstimateAtDateTime_ReturnsVersionCurrentAtThatTime()
    {
        var (cargo, v1, v2) = await CargoWithTwoVersions();
        var url = VersionsUrl(cargo.CargoId);

        var before = await _client.GetAsync($"{url}?dateTime={Format(v1.CreationInstant - Duration.FromSeconds(1))}");
        var atV1 = await _client.GetFromJsonAsync<VersionDto>($"{url}?dateTime={Format(v1.CreationInstant)}", Json);
        var atV2 = await _client.GetFromJsonAsync<VersionDto>($"{url}?dateTime={Format(v2.CreationInstant)}", Json);
        var afterV2 = await _client.GetFromJsonAsync<VersionDto>(
            $"{url}?dateTime={Format(v2.CreationInstant + Duration.FromHours(1))}", Json);

        Assert.Multiple(() =>
        {
            Assert.That(before.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            // v2 is written in a later transaction, so now() puts it strictly after v1
            Assert.That(v2.CreationInstant, Is.GreaterThan(v1.CreationInstant));
            Assert.That(atV1, Is.EqualTo(Expected(v1)));
            Assert.That(atV2, Is.EqualTo(Expected(v2)));
            Assert.That(afterV2, Is.EqualTo(Expected(v2)));
        });
    }

    [Test]
    public async Task EstimatesInRange_ReturnsVersionsCreatedInHalfOpenRange()
    {
        var (cargo, v1, v2) = await CargoWithTwoVersions();
        var url = $"{VersionsUrl(cargo.CargoId)}/range";

        var all = await _client.GetFromJsonAsync<List<VersionDto>>(
            $"{url}?fromDateTime={Format(v1.CreationInstant)}&toDateTime={Format(v2.CreationInstant + Duration.FromSeconds(1))}", Json);
        // toDateTime is exclusive
        var onlyV1 = await _client.GetFromJsonAsync<List<VersionDto>>(
            $"{url}?fromDateTime={Format(v1.CreationInstant)}&toDateTime={Format(v2.CreationInstant)}", Json);
        var none = await _client.GetFromJsonAsync<List<VersionDto>>(
            $"{url}?fromDateTime={Format(v2.CreationInstant + Duration.FromSeconds(1))}&toDateTime={Format(v2.CreationInstant + Duration.FromHours(1))}", Json);
        var reversed = await _client.GetAsync(
            $"{url}?fromDateTime={Format(v2.CreationInstant)}&toDateTime={Format(v1.CreationInstant)}");

        Assert.Multiple(() =>
        {
            Assert.That(all, Is.EqualTo(new[] { Expected(v1), Expected(v2) }));
            Assert.That(onlyV1, Is.EqualTo(new[] { Expected(v1) }));
            Assert.That(none, Is.Empty);
            Assert.That(reversed.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });
    }

    [Test]
    public async Task EstimateHistory_UnknownCargo_ReturnsNotFound()
    {
        var url = VersionsUrl(Guid.NewGuid().ToString());

        var latest = await _client.GetAsync($"{url}/latest");
        var version = await _client.GetAsync($"{url}/1");
        var atDateTime = await _client.GetAsync($"{url}?dateTime=2026-11-20T10:00:00");
        var range = await _client.GetAsync($"{url}/range?fromDateTime=2026-01-01T00:00:00&toDateTime=2027-01-01T00:00:00");

        Assert.Multiple(() =>
        {
            Assert.That(latest.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(version.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(atDateTime.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(range.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task EstimateDiff_ReportsChangedFieldsOnly()
    {
        var (cargo, v1, v2) = await CargoWithTwoVersions();

        var diff = await _client.GetFromJsonAsync<EstimateDiffDto>($"{VersionsUrl(cargo.CargoId)}/1/2", Json);

        var (old, @new) = (v1.EstimateJson, v2.EstimateJson);
        Assert.Multiple(() =>
        {
            Assert.That(diff!.OldVersion, Is.EqualTo(Expected(v1)));
            Assert.That(diff.NewVersion, Is.EqualTo(Expected(v2)));
            Assert.That(diff.ElapsedTime, Is.EqualTo(DateHandler.FormatElapsed(v2.CreationInstant - v1.CreationInstant)));

            // The discharge replaces the delivered energy, so everything computed from it moves
            Assert.That(diff.DeliveredEnergy, Is.EqualTo(new ChangeValue(old.DeliveredEnergy, @new.DeliveredEnergy)));
            Assert.That(diff.DeliveredEnergy!.Difference, Is.EqualTo(@new.DeliveredEnergy - old.DeliveredEnergy));
            Assert.That(diff.Revenue, Is.EqualTo(new ChangeValue(old.Revenue, @new.Revenue)));
            Assert.That(diff.Margin, Is.EqualTo(new ChangeValue(old.Margin, @new.Margin)));
            Assert.That(diff.Netback, Is.EqualTo(new ChangeValue(old.Netback, @new.Netback)));

            // Prices and fixed costs don't depend on it; regas stays 0 because the regas fee is 0
            Assert.That(diff.SalePrice, Is.Null);
            Assert.That(diff.PurchasePrice, Is.Null);
            Assert.That(diff.PurchaseCost, Is.Null);
            Assert.That(diff.ShippingCost, Is.Null);
            Assert.That(diff.RegasCost, Is.Null);
            Assert.That(diff.CanalFee, Is.Null);
        });
    }

    [Test]
    public async Task EstimateDiff_InvalidVersions_ReturnErrors()
    {
        var (cargo, _, _) = await CargoWithTwoVersions();
        var url = VersionsUrl(cargo.CargoId);

        var reversed = await _client.GetAsync($"{url}/2/1");
        var same = await _client.GetAsync($"{url}/1/1");
        var zero = await _client.GetAsync($"{url}/0/1");
        var missingVersion = await _client.GetAsync($"{url}/1/3");
        var unknownCargo = await _client.GetAsync($"{VersionsUrl(Guid.NewGuid().ToString())}/1/2");

        Assert.Multiple(() =>
        {
            Assert.That(reversed.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(same.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(zero.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(missingVersion.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(unknownCargo.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }
}
