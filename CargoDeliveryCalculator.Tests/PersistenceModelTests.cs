using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoDeliveryCalculator.Tests;

// Builds the EF model and generates the PostgreSQL DDL without connecting to a database
[TestFixture]
public class PersistenceModelTests
{
    private string _createScript = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused", o => o.UseNodaTime())
            .Options;
        using var db = new AppDbContext(options);
        _createScript = db.Database.GenerateCreateScript();
        Console.WriteLine(_createScript);
    }

    [Test]
    public void Model_GeneratesCreateScript()
    {
        Assert.That(_createScript, Does.Contain("CREATE TABLE"));
    }

    // Check constraints must mirror the validation in the Model records
    [TestCase("\"LoadMonth\" >= 1 AND \"LoadMonth\" <= 12")]
    [TestCase("\"DischargeMonth\" >= 1 AND \"DischargeMonth\" <= 12")]
    [TestCase("\"DestinationMmBtu\" >= 0")]
    [TestCase("\"LoadedVolume\" > 0")]
    [TestCase("\"ConversionFactor\" > 0")]
    [TestCase("\"DailyHireRate\" > 0")]
    [TestCase("\"BoilOffRate\" >= 0 AND \"BoilOffRate\" <= 1")]
    [TestCase("\"LadenVoyageDurationInDays\" > 0")]
    [TestCase("\"RegasFee\" >= 0")]
    [TestCase("\"CanalFee\" >= 0")]
    [TestCase("\"Year\" >= 1990")]
    [TestCase("\"Index\" IN ('Hh', 'Ttf', 'Nbp', 'Jkm', 'Brent')")]
    public void Model_HasCheckConstraint(string condition)
    {
        Assert.That(_createScript, Does.Contain($"CHECK ({condition}"));
    }

    // Mirrors Cargo.MaxCargoIdLength
    [Test]
    public void Model_CargoIdIsLimitedToMaxLength()
    {
        Assert.That(_createScript, Does.Contain($"\"CargoId\" character varying({Cargo.MaxCargoIdLength})"));
    }

    [Test]
    public void Model_CreationInstantDefaultsToNow()
    {
        var columns = _createScript.Split('\n').Where(l => l.Contains("\"CreationInstant\"")).ToList();

        Assert.That(columns, Is.Not.Empty);
        Assert.That(columns, Has.All.Contains("DEFAULT (now())"));
    }
}
