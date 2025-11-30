using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boilerplate.Client.Core.Data.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DatasyncDeltaTokens",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DatasyncDeltaTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DatasyncOperationsQueue",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Kind = table.Column<int>(type: "INTEGER", nullable: false),
                State = table.Column<int>(type: "INTEGER", nullable: false),
                LastAttempt = table.Column<long>(type: "INTEGER", nullable: true),
                HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                EntityType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                ItemId = table.Column<string>(type: "TEXT", maxLength: 126, nullable: false),
                EntityVersion = table.Column<string>(type: "TEXT", maxLength: 126, nullable: false),
                Item = table.Column<string>(type: "TEXT", nullable: false),
                Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DatasyncOperationsQueue", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TodoItems",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false),
                Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                Version = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItems", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DatasyncOperationsQueue_ItemId_EntityType",
            table: "DatasyncOperationsQueue",
            columns: new[] { "ItemId", "EntityType" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DatasyncDeltaTokens");

        migrationBuilder.DropTable(
            name: "DatasyncOperationsQueue");

        migrationBuilder.DropTable(
            name: "TodoItems");
    }
}
