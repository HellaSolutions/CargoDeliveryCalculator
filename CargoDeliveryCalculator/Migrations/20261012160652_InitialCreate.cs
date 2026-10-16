using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CargoDeliveryCalculator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cargos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargoId = table.Column<string>(type: "text", nullable: false),
                    LoadDate = table.Column<LocalDate>(type: "date", nullable: false),
                    LoadedVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    Slope = table.Column<decimal>(type: "numeric", nullable: false),
                    Index = table.Column<string>(type: "text", nullable: false),
                    Constant = table.Column<decimal>(type: "numeric", nullable: false),
                    CreationInstant = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                    table.CheckConstraint("CK_Cargo_ConversionFactor_Positive", "\"ConversionFactor\" > 0");
                    table.CheckConstraint("CK_Cargo_Index_Valid", "\"Index\" IN ('Hh', 'Ttf', 'Nbp', 'Jkm', 'Brent')");
                    table.CheckConstraint("CK_Cargo_LoadedVolume_Positive", "\"LoadedVolume\" > 0");
                    table.CheckConstraint("CK_Cargo_Slope_NonNegative", "\"Slope\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LadenVoyageDurationInDays = table.Column<int>(type: "integer", nullable: false),
                    RegasFee = table.Column<decimal>(type: "numeric", nullable: false),
                    CanalFee = table.Column<decimal>(type: "numeric", nullable: false),
                    Slope = table.Column<decimal>(type: "numeric", nullable: false),
                    Index = table.Column<string>(type: "text", nullable: false),
                    Constant = table.Column<decimal>(type: "numeric", nullable: false),
                    CreationInstant = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                    table.CheckConstraint("CK_Destination_CanalFee_NonNegative", "\"CanalFee\" >= 0");
                    table.CheckConstraint("CK_Destination_Duration_Positive", "\"LadenVoyageDurationInDays\" > 0");
                    table.CheckConstraint("CK_Destination_Index_Valid", "\"Index\" IN ('Hh', 'Ttf', 'Nbp', 'Jkm', 'Brent')");
                    table.CheckConstraint("CK_Destination_RegasFee_NonNegative", "\"RegasFee\" >= 0");
                    table.CheckConstraint("CK_Destination_Slope_NonNegative", "\"Slope\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Jan = table.Column<decimal>(type: "numeric", nullable: true),
                    Feb = table.Column<decimal>(type: "numeric", nullable: true),
                    Mar = table.Column<decimal>(type: "numeric", nullable: true),
                    Apr = table.Column<decimal>(type: "numeric", nullable: true),
                    May = table.Column<decimal>(type: "numeric", nullable: true),
                    Jun = table.Column<decimal>(type: "numeric", nullable: true),
                    Jul = table.Column<decimal>(type: "numeric", nullable: true),
                    Aug = table.Column<decimal>(type: "numeric", nullable: true),
                    Sep = table.Column<decimal>(type: "numeric", nullable: true),
                    Oct = table.Column<decimal>(type: "numeric", nullable: true),
                    Nov = table.Column<decimal>(type: "numeric", nullable: true),
                    Dec = table.Column<decimal>(type: "numeric", nullable: true),
                    CreationInstant = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.Id);
                    table.CheckConstraint("CK_PriceDao_Index_Valid", "\"Index\" IN ('Hh', 'Ttf', 'Nbp', 'Jkm', 'Brent')");
                    table.CheckConstraint("CK_PriceDao_Year_LowerBound", "\"Year\" >= 1990");
                });

            migrationBuilder.CreateTable(
                name: "Shippings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DailyHireRate = table.Column<decimal>(type: "numeric", nullable: false),
                    BoilOffRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CreationInstant = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shippings", x => x.Id);
                    table.CheckConstraint("CK_Shipping_BoilOffRate_Range", "\"BoilOffRate\" >= 0 AND \"BoilOffRate\" <= 1");
                    table.CheckConstraint("CK_Shipping_DailyHireRate_Positive", "\"DailyHireRate\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "ActiveCargos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargoId = table.Column<int>(type: "integer", nullable: false),
                    DestinationId = table.Column<int>(type: "integer", nullable: false),
                    ShippingId = table.Column<int>(type: "integer", nullable: false),
                    LoadMonth = table.Column<int>(type: "integer", nullable: false),
                    LoadPricesId = table.Column<int>(type: "integer", nullable: false),
                    DischargeMonth = table.Column<int>(type: "integer", nullable: false),
                    DischargePricesId = table.Column<int>(type: "integer", nullable: false),
                    LastEstimateVersion = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "0"),
                    DestinationMmBtu = table.Column<decimal>(type: "numeric", nullable: true),
                    CreationInstant = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveCargos", x => x.Id);
                    table.CheckConstraint("CK_ActiveCargo_DestinationMmBtu_Range", "\"DestinationMmBtu\" >= 0");
                    table.CheckConstraint("CK_ActiveCargo_DischargeMonth_Range", "\"DischargeMonth\" >= 1 AND \"DischargeMonth\" <= 12");
                    table.CheckConstraint("CK_ActiveCargo_LoadMonth_Range", "\"LoadMonth\" >= 1 AND \"LoadMonth\" <= 12");
                    table.ForeignKey(
                        name: "FK_ActiveCargos_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActiveCargos_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActiveCargos_Prices_DischargePricesId",
                        column: x => x.DischargePricesId,
                        principalTable: "Prices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActiveCargos_Prices_LoadPricesId",
                        column: x => x.LoadPricesId,
                        principalTable: "Prices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActiveCargos_Shippings_ShippingId",
                        column: x => x.ShippingId,
                        principalTable: "Shippings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Estimates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActiveCargoId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreationInstant = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    EstimateJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimates", x => x.Id);
                    table.CheckConstraint("CK_Version_Positive", "\"Version\" >= 1");
                    table.ForeignKey(
                        name: "FK_Estimates_ActiveCargos_ActiveCargoId",
                        column: x => x.ActiveCargoId,
                        principalTable: "ActiveCargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCargos_CargoId",
                table: "ActiveCargos",
                column: "CargoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCargos_DestinationId",
                table: "ActiveCargos",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCargos_DischargePricesId",
                table: "ActiveCargos",
                column: "DischargePricesId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCargos_LoadPricesId",
                table: "ActiveCargos",
                column: "LoadPricesId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCargos_ShippingId",
                table: "ActiveCargos",
                column: "ShippingId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_CargoId",
                table: "Cargos",
                column: "CargoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_ActiveCargoId_Version",
                table: "Estimates",
                columns: new[] { "ActiveCargoId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prices_Index_Year",
                table: "Prices",
                columns: new[] { "Index", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Estimates");

            migrationBuilder.DropTable(
                name: "ActiveCargos");

            migrationBuilder.DropTable(
                name: "Cargos");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "Prices");

            migrationBuilder.DropTable(
                name: "Shippings");
        }
    }
}
