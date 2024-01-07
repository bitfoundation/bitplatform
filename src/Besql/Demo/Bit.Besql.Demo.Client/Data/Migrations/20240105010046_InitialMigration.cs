using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bit.Besql.Demo.Client.Data.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WeatherForecasts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<long>(type: "INTEGER", nullable: false),
                TemperatureC = table.Column<int>(type: "INTEGER", nullable: false),
                Summary = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WeatherForecasts", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "WeatherForecasts",
            columns: new[] { "Id", "Date", "Summary", "TemperatureC" },
            values: new object[,]
            {
                { 1, 1307437068492800000L, "Hot", 30 },
                { 2, 1307438837964800000L, "Normal", 20 },
                { 3, 1307440607436800000L, "Cold", 10 }
            });

        migrationBuilder.CreateIndex(
            name: "IX_WeatherForecasts_TemperatureC",
            table: "WeatherForecasts",
            column: "TemperatureC");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WeatherForecasts");
    }
}
