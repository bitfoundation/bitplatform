using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bit.Bsqlite.Demo.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260816000000_WorkshopCatalog")]
public sealed class WorkshopCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Color",
            table: "Categories",
            type: "TEXT",
            maxLength: 16,
            nullable: false,
            defaultValue: "#0f766e");

        migrationBuilder.AddColumn<string>(
            name: "PublicId",
            table: "Products",
            type: "TEXT",
            nullable: false,
            defaultValue: "00000000-0000-0000-0000-000000000000");
        migrationBuilder.AddColumn<string>(
            name: "Sku",
            table: "Products",
            type: "TEXT",
            maxLength: 32,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<double>(
            name: "WeightKg",
            table: "Products",
            type: "REAL",
            nullable: false,
            defaultValue: 0.0);
        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Products",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);
        migrationBuilder.AddColumn<string>(
            name: "DiscontinuedOn",
            table: "Products",
            type: "TEXT",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "LeadTime",
            table: "Products",
            type: "TEXT",
            nullable: false,
            defaultValue: "2.00:00:00");
        migrationBuilder.AddColumn<string>(
            name: "Tags",
            table: "Products",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql("""UPDATE "Products" SET "Sku" = 'SKU-' || "Id" WHERE "Sku" = '';""");
        migrationBuilder.Sql(
            """
            UPDATE "Products"
            SET "PublicId" = '00000000-0000-0000-0000-' || printf('%012d', "Id")
            WHERE "PublicId" = '00000000-0000-0000-0000-000000000000';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Products_PublicId",
            table: "Products",
            column: "PublicId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Products_Sku",
            table: "Products",
            column: "Sku",
            unique: true);

        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PublicId = table.Column<string>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                DateOfBirth = table.Column<string>(type: "TEXT", nullable: false),
                IsVip = table.Column<bool>(type: "INTEGER", nullable: false),
                CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_Customers_PublicId", table: "Customers", column: "PublicId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Customers_Email", table: "Customers", column: "Email", unique: true);

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Number = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                OrderedAt = table.Column<string>(type: "TEXT", nullable: false),
                ShipBy = table.Column<string>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.ForeignKey(
                    name: "FK_Orders_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Orders_Number", table: "Orders", column: "Number", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId", table: "Orders", column: "CustomerId");

        migrationBuilder.CreateTable(
            name: "OrderLines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderLines_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrderLines_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_OrderLines_OrderId", table: "OrderLines", column: "OrderId");
        migrationBuilder.CreateIndex(name: "IX_OrderLines_ProductId", table: "OrderLines", column: "ProductId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderLines");
        migrationBuilder.DropTable(name: "Orders");
        migrationBuilder.DropTable(name: "Customers");
        migrationBuilder.DropIndex(name: "IX_Products_PublicId", table: "Products");
        migrationBuilder.DropIndex(name: "IX_Products_Sku", table: "Products");
        migrationBuilder.DropColumn(name: "PublicId", table: "Products");
        migrationBuilder.DropColumn(name: "Sku", table: "Products");
        migrationBuilder.DropColumn(name: "WeightKg", table: "Products");
        migrationBuilder.DropColumn(name: "IsActive", table: "Products");
        migrationBuilder.DropColumn(name: "DiscontinuedOn", table: "Products");
        migrationBuilder.DropColumn(name: "LeadTime", table: "Products");
        migrationBuilder.DropColumn(name: "Tags", table: "Products");
        migrationBuilder.DropColumn(name: "Color", table: "Categories");
    }
}
