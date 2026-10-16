using System.Text.Json;
using System.Text.Json.Serialization;
using CargoDeliveryCalculator.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Testcontainers.PostgreSql;

namespace CargoDeliveryCalculator.Tests;

public class IntegrationTestsBase
{
    
    private PostgreSqlContainer _postgresContainer;
    protected WebApplicationFactory<Program> _factory;

    // Matches the API's JSON setup, so responses containing NodaTime values can be read back
    protected static readonly JsonSerializerOptions Json =
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);


    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
         _postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("cargo_db")
        .WithUsername("myuser")
        .WithPassword("mypassword")
        .Build();
            
        await _postgresContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor =
                        services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });
                });
            });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(); 
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}