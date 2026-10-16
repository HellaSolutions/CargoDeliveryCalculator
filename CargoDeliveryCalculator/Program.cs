using System.Text.Json.Serialization;
using CargoDeliveryCalculator.Controllers;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Repositories;
using CargoDeliveryCalculator.Services;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DevConnection"),
        npgsqlOptions => npgsqlOptions.UseNodaTime()));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // NodaTime values as ISO strings, e.g. "2026-11-20T10:00:00", instead of their every property
        options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    });
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options => options.AddOperationTransformer<IndexParameterTransformer>());

builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();
builder.Services.AddScoped<IActiveCargoRepository, ActiveCargoRepository>();
builder.Services.AddScoped<ICargoRepository, CargoRepository>();
builder.Services.AddScoped<IDynamicsService, DynamicsService>();
builder.Services.AddScoped<IShippingRepository, ShippingRepository>();
builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
builder.Services.AddScoped<IEstimateRepository, EstimateRepository>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<ICargoOutputService, CargoOutputService>();
builder.Services.AddScoped<IEstimateCalculatorService, EstimateCalculatorService>();
builder.Services.AddScoped<IEstimateService, EstimateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.UseExceptionHandler();

app.Run();
