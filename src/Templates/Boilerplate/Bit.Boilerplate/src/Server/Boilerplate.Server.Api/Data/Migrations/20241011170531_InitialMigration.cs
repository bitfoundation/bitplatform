﻿#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Boilerplate.Server.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Color = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DataProtectionKeys",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FriendlyName = table.Column<string>(type: "TEXT", nullable: true),
                Xml = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", nullable: true),
                Gender = table.Column<int>(type: "INTEGER", nullable: true),
                BirthDate = table.Column<long>(type: "INTEGER", nullable: true),
                ProfileImageName = table.Column<string>(type: "TEXT", nullable: true),
                Sessions = table.Column<string>(type: "TEXT", nullable: false),
                EmailTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                PhoneNumberTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                ResetPasswordTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                TwoFactorTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                OtpRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", rowVersion: true, nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LockoutEnd = table.Column<long>(type: "INTEGER", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Price = table.Column<decimal>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                CreatedOn = table.Column<long>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey(
                    name: "FK_Products_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_RoleClaims_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DeviceInstallations",
            columns: table => new
            {
                InstallationId = table.Column<string>(type: "TEXT", nullable: false),
                Platform = table.Column<string>(type: "TEXT", nullable: false),
                PushChannel = table.Column<string>(type: "TEXT", nullable: false),
                P256dh = table.Column<string>(type: "TEXT", nullable: true),
                Auth = table.Column<string>(type: "TEXT", nullable: true),
                Endpoint = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                Tags = table.Column<string>(type: "TEXT", nullable: false),
                ExpirationTime = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeviceInstallations", x => x.InstallationId);
                table.ForeignKey(
                    name: "FK_DeviceInstallations_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "TodoItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false),
                Date = table.Column<long>(type: "INTEGER", nullable: false),
                IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_TodoItems_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserClaims_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_UserLogins_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                RoleId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserTokens",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_UserTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "Id", "Color", "Name" },
            values: new object[,]
            {
                { new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), "#FFCD56", "Ford" },
                { new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), "#FF6384", "Nissan" },
                { new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), "#4BC0C0", "Benz" },
                { new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), "#2B88D8", "Tesla" },
                { new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), "#FF9124", "BMW" }
            });

        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "AccessFailedCount", "BirthDate", "Email", "EmailConfirmed", "EmailTokenRequestedOn", "FullName", "Gender", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "OtpRequestedOn", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "PhoneNumberTokenRequestedOn", "ProfileImageName", "ResetPasswordTokenRequestedOn", "SecurityStamp", "Sessions", "TwoFactorEnabled", "TwoFactorTokenRequestedOn", "UserName" },
            values: new object[] { new Guid("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), 0, 1306790461440000000L, "test@bitplatform.dev", true, 1306790461440000000L, "Boilerplate test account", 2, true, null, "TEST@BITPLATFORM.DEV", "TEST", null, "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", "+31684207362", true, null, null, null, "959ff4a9-4b07-4cc1-8141-c5fc033daf83", "[]", false, null, "test" });

        migrationBuilder.InsertData(
            table: "Products",
            columns: new[] { "Id", "CategoryId", "CreatedOn", "Description", "Name", "Price" },
            values: new object[,]
            {
                { new Guid("01d223a3-182d-406a-9722-19dab083f96e"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306466648064000000L, "", "M550i", 77790m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306413563904000000L, "", "IX", 87000m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a334"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), 1306422411264000000L, "A new smart SUV", "Juke", 28100m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf7"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", "Raptor", 53205m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306422411264000000L, "", "X7", 77980m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253288"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), 1306422411264000000L, "", "R50", 2000000m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a644"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306457800704000000L, "", "540i", 60945m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb3c"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", "Altima", 24550m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb96"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), 1306457800704000000L, "finishes near the top of our luxury electric car rankings.", "Model S", 135000m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659412"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), 1306448953344000000L, "Heart-pumping acceleration, long drive range", "Model X", 138890m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b69"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), 1306466648064000000L, "", "H247", 54950m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b89881a"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), 1306466648064000000L, "rapid acceleration and dynamic handling", "Model 3", 61990m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", "Mustang", 27155m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de4504303a"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), 1306457800704000000L, "", "V297", 103360m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", "GT", 500000m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb0"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306448953344000000L, "", "530e", 56545m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c793"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), 1306422411264000000L, "extensive driving range, lots of standard safety features", "Model Y", 67790m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca25"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), 1306466648064000000L, "A powerful convertible sports car", "Roadster", 42800m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf99"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306431258624000000L, "", "M850i", 100045m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f64"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", "GT-R", 113540m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7558"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", "Maverick", 22470m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caeef"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", "Ranger", 25000m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec502"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 1306440105984000000L, "", "530i", 55195m }
            });

        migrationBuilder.CreateIndex(
            name: "IX_DeviceInstallations_UserId",
            table: "DeviceInstallations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            table: "Products",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_RoleClaims_RoleId",
            table: "RoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "Roles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TodoItems_UserId",
            table: "TodoItems",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserClaims_UserId",
            table: "UserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_UserId",
            table: "UserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true,
            filter: "[Email] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_PhoneNumber",
            table: "Users",
            column: "PhoneNumber",
            unique: true,
            filter: "[PhoneNumber] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "Users",
            column: "NormalizedUserName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DataProtectionKeys");

        migrationBuilder.DropTable(
            name: "DeviceInstallations");

        migrationBuilder.DropTable(
            name: "Products");

        migrationBuilder.DropTable(
            name: "RoleClaims");

        migrationBuilder.DropTable(
            name: "TodoItems");

        migrationBuilder.DropTable(
            name: "UserClaims");

        migrationBuilder.DropTable(
            name: "UserLogins");

        migrationBuilder.DropTable(
            name: "UserRoles");

        migrationBuilder.DropTable(
            name: "UserTokens");

        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropTable(
            name: "Roles");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
