using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using CargoDeliveryCalculator.Controllers.Dto;
using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Tests;

[TestFixture]
public class RecommendationItTests : IntegrationTestsBase
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public new async Task OneTimeSetUp()
    {
        _client = _factory.CreateClient();

        var scope = _factory.Services.CreateScope();
        var priceEventService = scope.ServiceProvider.GetRequiredService<IDynamicsService>();
        await priceEventService.SetIndexPriceAsync("Hh", 3.00m, 2026, 11);
        await priceEventService.SetIndexPriceAsync("Ttf", 11.00m, 2026, 11);
        await priceEventService.SetIndexPriceAsync("Jkm", 12.00m, 2026, 12);
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        _client.Dispose();
    }

    [Test]
    public async Task Expected_Recommendation_ReturnsOkAndBestDestination()
    {
        var purchaseFormula = new PurchaseFormula(1.15m, Index.Hh, 2.50m);
        var cargo = new CargoDto(Guid.NewGuid().ToString(), "2026-11-10", 160000m, 22.0m,
            purchaseFormula);
        var shipping = new Shipping(60000m, 0.001m);

        var saleFormulaNwEu = new PurchaseFormula(1.0m, Index.Ttf, -0.40m);
        var nwEurope = new Destination("NWEurope", saleFormulaNwEu, 14, 0.45m, 0);

        var saleFormulaJapan = new PurchaseFormula(1.0m, Index.Jkm, 0);
        var japan = new Destination("Japan", saleFormulaJapan, 25, 0, 600000m);

        var arg = new CargoInputsDto(cargo, shipping, [nwEurope, japan]);

        var response = await _client.PostAsJsonAsync("/api/recommendation", arg);

        Console.WriteLine(response.StatusCode);
        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var result = await response.Content.ReadFromJsonAsync<CargoOutputDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(japan, Is.EqualTo(result.BestDestination));
        Assert.That(result.Estimates.Select(e => e.Estimate.Margin), Is.Ordered.Descending);
    }

    [Test]
    public async Task Post_Recommendation_InvalidDateFormat_ReturnsBadRequest()
    {
        var purchaseFormula = new PurchaseFormula(1.15m, Index.Hh, 2.50m);
        var cargo = new CargoDto(Guid.NewGuid().ToString(), "INVALID", 160000m, 22.0m,
            purchaseFormula);
        var shipping = new Shipping(60000m, 0.001m);

        var saleFormulaNwEu = new PurchaseFormula(1.0m, Index.Ttf, -0.40m);
        var nwEurope = new Destination("NWEurope", saleFormulaNwEu, 14, 0.45m, 0);

        var saleFormulaJapan = new PurchaseFormula(1.0m, Index.Jkm, 0);
        var japan = new Destination("Japan", saleFormulaJapan, 25, 0, 600000m);

        var arg = new CargoInputsDto(cargo, shipping, [nwEurope, japan]);

        var response = await _client.PostAsJsonAsync("/api/recommendation", arg);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("Invalid format for date INVALID"));
    }

    // Raw JSON because the model constructors reject invalid values client-side too
    private const string ValidRequestJson = """
        {
          "cargo": {
            "cargoId": "cargo",
            "loadDate": "2026-11-10",
            "loadedVolume": 160000,
            "conversionFactor": 22.0,
            "purchaseFormula": { "slope": 1.15, "index": "Hh", "constant": 2.50 }
          },
          "shipping": { "dailyHireRate": 60000, "boilOffRate": 0.001 },
          "destinations": [
            {
              "name": "NWEurope",
              "saleFormula": { "slope": 1.0, "index": "Ttf", "constant": -0.40 },
              "ladenVoyageDurationInDays": 14,
              "regasFee": 0.45,
              "canalFee": 0
            }
          ]
        }
        """;

    [TestCase("cargo.loadedVolume", 0, "Loaded volume should be positive")]
    [TestCase("cargo.loadedVolume", -1, "Loaded volume should be positive")]
    [TestCase("cargo.conversionFactor", 0, "Conversion factor should be positive")]
    [TestCase("cargo.conversionFactor", -1, "Conversion factor should be positive")]
    [TestCase("cargo.purchaseFormula.slope", -1, "Slope should be non negative")]
    [TestCase("shipping.dailyHireRate", 0, "Daily rate should be positive")]
    [TestCase("shipping.dailyHireRate", -1, "Daily rate should be positive")]
    [TestCase("shipping.boilOffRate", -0.1, "The Boil off rate must be between 0.0 and 1.0.")]
    [TestCase("shipping.boilOffRate", 1.1, "The Boil off rate must be between 0.0 and 1.0.")]
    [TestCase("destinations.0.ladenVoyageDurationInDays", 0, "Laden duration should be positive")]
    [TestCase("destinations.0.ladenVoyageDurationInDays", -1, "Laden duration should be positive")]
    [TestCase("destinations.0.regasFee", -1, "Regas fee should be not negative")]
    [TestCase("destinations.0.canalFee", -1, "Canal fee should be not negative")]
    [TestCase("destinations.0.saleFormula.slope", -1, "Slope should be non negative")]
    public async Task Post_Recommendation_InvalidModelValue_ReturnsBadRequest(string path, decimal value, string expectedTitle)
    {
        var request = JsonNode.Parse(ValidRequestJson)!;
        var segments = path.Split('.');
        var parent = segments[..^1].Aggregate(request, (node, segment) =>
            int.TryParse(segment, out var i) ? node[i]! : node[segment]!);
        parent[segments[^1]] = value;

        var response = await _client.PostAsync("/api/recommendation",
            new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo(expectedTitle));
    }

    // Nov-2026 Hh purchase; the destination's index decides which sale price is needed
    private static CargoInputsDto Request(string loadDate, decimal boilOffRate, params Destination[] destinations)
    {
        var cargo = new CargoDto(Guid.NewGuid().ToString(), loadDate, 160000m, 22.0m,
            new PurchaseFormula(1.15m, Index.Hh, 2.50m));
        return new CargoInputsDto(cargo, new Shipping(60000m, boilOffRate), destinations.ToList());
    }

    private static Destination NwEurope(int ladenDays = 14, Index index = Index.Ttf) =>
        new("NWEurope", new PurchaseFormula(1.0m, index, -0.40m), ladenDays, 0.45m, 0);

    private async Task AssertProblem(CargoInputsDto request, HttpStatusCode expectedStatus, string expectedTitle)
    {
        var response = await _client.PostAsJsonAsync("/api/recommendation", request);

        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo(expectedTitle));
    }

    private static CargoInputsDto RequestWithCargoId(string cargoId)
    {
        var request = Request("2026-11-10", 0.001m, NwEurope());
        return request with { Cargo = request.Cargo with { CargoId = cargoId } };
    }

    [Test]
    public async Task Post_Recommendation_CargoIdAtMaxLength_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/recommendation",
            RequestWithCargoId(new string('x', Cargo.MaxCargoIdLength)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public Task Post_Recommendation_CargoIdTooLong_ReturnsBadRequest() =>
        AssertProblem(RequestWithCargoId(new string('x', Cargo.MaxCargoIdLength + 1)), HttpStatusCode.BadRequest,
            $"Cargo id should be at most {Cargo.MaxCargoIdLength} characters");

    [Test]
    public Task Post_Recommendation_EmptyDestinations_ReturnsBadRequest() =>
        AssertProblem(Request("2026-11-10", 0.001m), HttpStatusCode.BadRequest,
            "Empty destinations are not allowed");

    [Test]
    public Task Post_Recommendation_TotalBoilOffOf100Percent_ReturnsBadRequest() =>
        // 0.05 * 20 days = exactly 100%
        AssertProblem(Request("2026-11-10", 0.05m, NwEurope(ladenDays: 20)), HttpStatusCode.BadRequest,
            "Total boil off should be less than 100%");

    [Test]
    public Task Post_Recommendation_TotalBoilOffAbove100Percent_ReturnsBadRequest() =>
        AssertProblem(Request("2026-11-10", 0.1m, NwEurope(ladenDays: 14)), HttpStatusCode.BadRequest,
            "Total boil off should be less than 100%");

    [Test]
    public Task Post_Recommendation_MissingPurchaseMonthPrice_ReturnsNotFound() =>
        // Hh has 2026 data, but only for November
        AssertProblem(Request("2026-10-10", 0.001m, NwEurope()), HttpStatusCode.NotFound,
            "no Hh value for 10-2026");

    [Test]
    public Task Post_Recommendation_MissingSaleYearPrice_ReturnsNotFound() =>
        // No Nbp price is ever set
        AssertProblem(Request("2026-11-10", 0.001m, NwEurope(index: Index.Nbp)), HttpStatusCode.NotFound,
            "no Nbp value for 2026");

    [Test]
    public Task Post_Recommendation_MissingSaleMonthPrice_ReturnsNotFound() =>
        // Delivery on 2026-12-01 needs a December Ttf price; only November is set
        AssertProblem(Request("2026-11-10", 0.001m, NwEurope(ladenDays: 21)), HttpStatusCode.NotFound,
            "no Ttf value for 12-2026");
}