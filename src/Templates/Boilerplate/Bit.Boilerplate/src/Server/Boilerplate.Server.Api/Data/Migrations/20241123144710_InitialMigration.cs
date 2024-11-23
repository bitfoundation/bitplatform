#nullable disable

namespace Boilerplate.Server.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "PrivilegedAccessTokenRequestedOn",
            table: "Users",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"),
            column: "PrivilegedAccessTokenRequestedOn",
            value: null);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PrivilegedAccessTokenRequestedOn",
            table: "Users");
    }
}
