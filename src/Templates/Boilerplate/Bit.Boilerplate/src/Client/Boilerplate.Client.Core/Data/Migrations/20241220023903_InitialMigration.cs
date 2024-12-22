using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable DateTimeOffsetInsteadOfDateTimeAnalyzer

namespace Boilerplate.Client.Core.Data.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserName = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                Password = table.Column<string>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", nullable: false),
                Gender = table.Column<int>(type: "INTEGER", nullable: true),
                BirthDate = table.Column<long>(type: "INTEGER", nullable: true),
                ProfileImageName = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "BirthDate", "ConcurrencyStamp", "Email", "FullName", "Gender", "Password", "PhoneNumber", "ProfileImageName", "UserName" },
            values: new object[] { new Guid("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), 1306790461440000000L, null, "test@bitplatform.dev", "Boilerplate test account", 0, "123456", "+31684207362", null, "test" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Users");
    }
}
