using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPanelTemplate.Api.Migrations;

public partial class zelaton3 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Price",
            table: "Products",
            type: "money",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "money");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Price",
            table: "Products",
            type: "money",
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "money",
            oldNullable: true);
    }
}
