using System.Net;
using System.Net.Http.Json;
using CargoDeliveryCalculator.Services;

namespace CargoDeliveryCalculator.Tests;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Index = CargoDeliveryCalculator.Model.Index;

public class IndexesItTests : IntegrationTestsBase
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public new async Task OneTimeSetUp()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var priceEventService = scope.ServiceProvider.GetRequiredService<IDynamicsService>();
        
        await priceEventService.SetIndexPriceAsync("Hh", 3.00m, 2026, 11);
        await priceEventService.SetIndexPriceAsync("Ttf", 11.00m, 2026, 11);
        await priceEventService.SetIndexPriceAsync("Jkm", 12.00m, 2026, 12);
        await priceEventService.SetIndexPriceAsync("Hh", 3.00m, 2023, 11);
        await priceEventService.SetIndexPriceAsync("Hh", 3.00m, 2023, 11);
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        _client.Dispose();
    }
    
    [Test]
    public async Task Get_Price()
    {
        var index = nameof(Index.Hh);
        int year = 2023; 
        int month = 11;
        var response = await _client.GetAsync($"/api/indexes/{index}/{year}/{month}");
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("3.00"));
        });
    }
    
    [Test]
    public async Task Put_Price_Upsert()
    {
        
        var index = nameof(Index.Hh);
        decimal amount = 5.0m;
        int year = 2020;
        int month = 10;
        
        var response = await _client.PutAsJsonAsync($"/api/dynamics/indexes/{index}/{year}/{month}", amount);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        response = await _client.GetAsync($"/api/indexes/{index}/{year}/{month}");
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("5.0"));
        });
        
        amount = 10.0m;
        response = await _client.PutAsJsonAsync($"/api/dynamics/indexes/{index}/{year}/{month}", amount);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        response = await _client.GetAsync($"/api/indexes/{index}/{year}/{month}");
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("10.0"));
        });
    }
    
    [Test]
    public async Task Put_Wrong_Index_Price()
    {
        var index = "XX";
        decimal amount = 10.0m;
        int year = 2026; 
        int month = 11;
        var response = await _client.PutAsJsonAsync($"/api/dynamics/indexes/{index}/{year}/{month}", amount);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("Invalid index XX"));
        
    }
    
    [Test]
    public async Task Put_Wrong_Month_Price()
    {
        var index = nameof(Index.Hh);
        decimal amount = 10.0m;
        int year = 2026; 
        int month = 15;
        var response = await _client.PutAsJsonAsync($"/api/dynamics/indexes/{index}/{year}/{month}", amount);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("Invalid month 15"));
        
    }
    
    [Test]
    public async Task Put_Wrong_Year_Price()
    {
        var index = nameof(Index.Hh);
        decimal amount = 10.0m;
        int year = 1832; 
        int month = 1;
        var response = await _client.PutAsJsonAsync($"/api/dynamics/indexes/{index}/{year}/{month}", amount);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("Invalid year 1832"));
        
    }
    
    [Test]
    public async Task Get_Wrong_Index_Price()
    {
        var index = "XX";
        int year = 2026; 
        int month = 11;
        var response = await _client.GetAsync($"/api/indexes/{index}/{year}/{month}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("Invalid index XX"));
        
    }
    
    [Test]
    public async Task Get_Wrong_Month_Price()
    {
        var index = nameof(Index.Hh);
        int year = 2026; 
        int month = 15;
        var response = await _client.GetAsync($"/api/indexes/{index}/{year}/{month}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("Invalid month 15"));
        
    }
    
    [Test]
    public async Task Get_Wrong_Year_Price()
    {
        var index = nameof(Index.Hh);
        int year = 1832; 
        int month = 1;
        var response = await _client.GetAsync($"/api/indexes/{index}/{year}/{month}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("no Hh value for 1832"));

    }

    [Test]
    public async Task Get_Missing_Month_Price()
    {
        // Hh has a 2023 row, but only November is set
        var response = await _client.GetAsync($"/api/indexes/{nameof(Index.Hh)}/2023/12");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problemDetails!.Title, Is.EqualTo("no Hh value for 12-2023"));
    }
}