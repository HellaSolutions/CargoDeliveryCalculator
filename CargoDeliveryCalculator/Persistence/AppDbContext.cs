using CargoDeliveryCalculator.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ActiveCargoDao> ActiveCargos => Set<ActiveCargoDao>();
    public DbSet<CargoDao> Cargos => Set<CargoDao>();
    public DbSet<DestinationDao> Destinations => Set<DestinationDao>();
    public DbSet<EstimateDao> Estimates => Set<EstimateDao>();
    public DbSet<PriceDao> Prices => Set<PriceDao>();
    public DbSet<ShippingDao> Shippings => Set<ShippingDao>();
    
    private static readonly string IndexCheck =
        $"\"Index\" IN ({string.Join(", ", Enum.GetNames<Index>().Select(n => $"'{n}'"))})";

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Index>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ActiveCargoDao>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CargoId).IsRequired();
            entity.Property(e => e.DestinationId).IsRequired();
            entity.Property(e => e.ShippingId).IsRequired();
            entity.Property(e => e.LoadMonth).IsRequired();
            entity.Property(e => e.LoadPricesId).IsRequired();
            entity.Property(e => e.DischargeMonth).IsRequired();
            entity.Property(e => e.DischargePricesId).IsRequired();
            entity.Property(e => e.LastEstimateVersion).IsRequired()
                .HasDefaultValueSql("0");
            entity.Property(e => e.CreationInstant).IsRequired()
                .HasDefaultValueSql("now()");

            entity.HasIndex(e => e.CargoId)
                .IsUnique();
            
            entity.HasOne(e => e.Cargo)
                .WithMany()
                .HasForeignKey(e => e.CargoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Destination)
                .WithMany()
                .HasForeignKey(e => e.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Shipping)
                .WithMany()
                .HasForeignKey(e => e.ShippingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Two relationships to Prices: each navigation is tied to its own FK so EF can tell them apart
            entity.HasOne(e => e.LoadPrices)
                .WithMany()
                .HasForeignKey(e => e.LoadPricesId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DischargePrices)
                .WithMany()
                .HasForeignKey(e => e.DischargePricesId)
                .OnDelete(DeleteBehavior.Restrict);
           
            entity.ToTable(t => t.HasCheckConstraint("CK_ActiveCargo_DestinationMmBtu_Range", "\"DestinationMmBtu\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ActiveCargo_LoadMonth_Range", "\"LoadMonth\" >= 1 AND \"LoadMonth\" <= 12"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ActiveCargo_DischargeMonth_Range", "\"DischargeMonth\" >= 1 AND \"DischargeMonth\" <= 12"));
        });

        modelBuilder.Entity<CargoDao>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CargoId)
                .IsUnique();
            entity.Property(e => e.CargoId)
                .HasMaxLength(CargoDeliveryCalculator.Model.Cargo.MaxCargoIdLength);
            entity.Property(e => e.LoadDate)
                .IsRequired();
            entity.Property(e => e.Slope)
                .IsRequired();
            entity.Property(e => e.Constant)
                .IsRequired();
            entity.Property(e => e.ConversionFactor)
                .IsRequired();
            entity.Property(e => e.LoadedVolume)
                .IsRequired();
            entity.Property(e => e.Index)
                .IsRequired();
            entity.Property(e => e.CreationInstant)
                .IsRequired()
                .HasDefaultValueSql("now()");

            entity.ToTable(t => t.HasCheckConstraint("CK_Cargo_Index_Valid", IndexCheck));
            entity.ToTable(t => t.HasCheckConstraint("CK_Cargo_Slope_NonNegative", "\"Slope\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Cargo_ConversionFactor_Positive", "\"ConversionFactor\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Cargo_LoadedVolume_Positive", "\"LoadedVolume\" > 0"));
        });
        
        modelBuilder.Entity<DestinationDao>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100); // Sets the column size to max 100 characters
            
            entity.Property(e => e.LadenVoyageDurationInDays)
                .IsRequired();

            entity.Property(e => e.RegasFee)
                .IsRequired();

            entity.Property(e => e.CanalFee)
                .IsRequired();

            entity.Property(e => e.Slope).IsRequired();
            entity.Property(e => e.Index).IsRequired();
            entity.Property(e => e.Constant).IsRequired();
            entity.Property(e => e.CreationInstant).IsRequired()
                .HasDefaultValueSql("now()");

            entity.ToTable(t => t.HasCheckConstraint("CK_Destination_Index_Valid", IndexCheck));
            entity.ToTable(t => t.HasCheckConstraint("CK_Destination_Slope_NonNegative", "\"Slope\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Destination_Duration_Positive", "\"LadenVoyageDurationInDays\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Destination_RegasFee_NonNegative", "\"RegasFee\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Destination_CanalFee_NonNegative", "\"CanalFee\" >= 0"));
        });
        
        modelBuilder.Entity<EstimateDao>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ActiveCargoId).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.CreationInstant).IsRequired()
                .HasDefaultValueSql("now()");
            
            entity.HasOne<ActiveCargoDao>()
                .WithMany()
                .HasForeignKey(e => e.ActiveCargoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.ActiveCargoId, e.Version })
                .IsUnique();
            
            entity.OwnsOne(e => e.EstimateJson, estimate =>
            {
                estimate.ToJson("EstimateJson");
            });
            
            entity.ToTable(t => t.HasCheckConstraint("CK_Version_Positive", "\"Version\" >= 1"));
        });

        modelBuilder.Entity<PriceDao>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Index).IsRequired();
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.CreationInstant).IsRequired()
                .HasDefaultValueSql("now()");

            entity.HasIndex(e => new { e.Index, e.Year })
                .IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_PriceDao_Index_Valid", IndexCheck));
            entity.ToTable(t => t.HasCheckConstraint("CK_PriceDao_Year_LowerBound", "\"Year\" >= 1990"));
        });
        
        modelBuilder.Entity<ShippingDao>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.DailyHireRate)
                .IsRequired();

            entity.Property(s => s.BoilOffRate)
                .IsRequired();

            entity.Property(s => s.CreationInstant)
                .IsRequired()
                .HasDefaultValueSql("now()");
            
            entity.ToTable(t => t.HasCheckConstraint("CK_Shipping_DailyHireRate_Positive", "\"DailyHireRate\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Shipping_BoilOffRate_Range", "\"BoilOffRate\" >= 0 AND \"BoilOffRate\" <= 1"));
        });
    }
}
