using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

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
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserName = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                Password = table.Column<string>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", nullable: false),
                Gender = table.Column<int>(type: "INTEGER", nullable: true),
                BirthDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                ProfileImageName = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "Users",
            columns: ["Id", "BirthDate", "Email", "FullName", "Gender", "Password", "ProfileImageName", "UserName"],
            values: [1, null, "test@bitplatform.dev", "Boilerplate test account", null, "123456", null, "test@bitplatform.dev"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Users");
    }
}
