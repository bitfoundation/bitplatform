#nullable disable

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
                Color = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<byte[]>(type: "BLOB", nullable: false)
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
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
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
                EmailTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                PhoneNumberTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                ResetPasswordTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                TwoFactorTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                OtpRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                ElevatedAccessTokenRequestedOn = table.Column<long>(type: "INTEGER", nullable: true),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
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
                Number = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Price = table.Column<decimal>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                CreatedOn = table.Column<long>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                ConcurrencyStamp = table.Column<byte[]>(type: "BLOB", nullable: false),
                ImageFileName = table.Column<string>(type: "TEXT", nullable: true)
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
            name: "UserSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                IP = table.Column<string>(type: "TEXT", nullable: true),
                DeviceInfo = table.Column<string>(type: "TEXT", nullable: true),
                Address = table.Column<string>(type: "TEXT", nullable: true),
                Privileged = table.Column<bool>(type: "INTEGER", nullable: false),
                StartedOn = table.Column<long>(type: "INTEGER", nullable: false),
                RenewedOn = table.Column<long>(type: "INTEGER", nullable: true),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                SignalRConnectionId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserSessions_Users_UserId",
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

        migrationBuilder.CreateTable(
            name: "PushNotificationSubscriptions",
            columns: table => new
            {
                DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                Platform = table.Column<string>(type: "TEXT", nullable: false),
                PushChannel = table.Column<string>(type: "TEXT", nullable: false),
                P256dh = table.Column<string>(type: "TEXT", nullable: true),
                Auth = table.Column<string>(type: "TEXT", nullable: true),
                Endpoint = table.Column<string>(type: "TEXT", nullable: true),
                UserSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                Tags = table.Column<string>(type: "TEXT", nullable: false),
                ExpirationTime = table.Column<long>(type: "INTEGER", nullable: false),
                RenewedOn = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PushNotificationSubscriptions", x => x.DeviceId);
                table.ForeignKey(
                    name: "FK_PushNotificationSubscriptions_UserSessions_UserSessionId",
                    column: x => x.UserSessionId,
                    principalTable: "UserSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "Id", "Color", "ConcurrencyStamp", "Name" },
            values: new object[,]
            {
                { new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), "#FFCD56", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "Ford" },
                { new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), "#FF6384", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "Nissan" },
                { new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), "#4BC0C0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "Benz" },
                { new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), "#2B88D8", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "Tesla" },
                { new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), "#FF9124", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "BMW" }
            });

        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "AccessFailedCount", "BirthDate", "ConcurrencyStamp", "ElevatedAccessTokenRequestedOn", "Email", "EmailConfirmed", "EmailTokenRequestedOn", "FullName", "Gender", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "OtpRequestedOn", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "PhoneNumberTokenRequestedOn", "ProfileImageName", "ResetPasswordTokenRequestedOn", "SecurityStamp", "TwoFactorEnabled", "TwoFactorTokenRequestedOn", "UserName" },
            values: new object[] { new Guid("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), 0, 1306790461440000000L, "315e1a26-5b3a-4544-8e91-2760cd28e231", null, "test@bitplatform.dev", true, 1306790461440000000L, "Boilerplate test account", 0, true, null, "TEST@BITPLATFORM.DEV", "TEST", null, "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", "+31684207362", true, null, null, null, "959ff4a9-4b07-4cc1-8141-c5fc033daf83", false, null, "test" });

        migrationBuilder.InsertData(
            table: "Products",
            columns: new[] { "Id", "CategoryId", "ConcurrencyStamp", "CreatedOn", "Description", "ImageFileName", "Name", "Number", "Price" },
            values: new object[,]
            {
                { new Guid("01d223a3-182d-406a-9722-19dab083f961"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 1", 9036, 77790m },
                { new Guid("01d223a3-182d-406a-9722-19dab083f962"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 2", 9059, 77790m },
                { new Guid("01d223a3-182d-406a-9722-19dab083f963"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 3", 9082, 77790m },
                { new Guid("01d223a3-182d-406a-9722-19dab083f964"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 4", 9105, 77790m },
                { new Guid("01d223a3-182d-406a-9722-19dab083f965"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 5", 9128, 77790m },
                { new Guid("01d223a3-182d-406a-9722-19dab083f966"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 6", 9151, 77790m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 1", 9042, 87000m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e2"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 2", 9065, 87000m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e3"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 3", 9088, 87000m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e4"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 4", 9111, 87000m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 5", 9134, 87000m },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e6"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 6", 9157, 87000m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a331"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 1", 9032, 28100m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a332"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 2", 9055, 28100m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a333"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 3", 9078, 28100m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a334"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 4", 9101, 28100m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a335"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 5", 9124, 28100m },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a336"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 6", 9147, 28100m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 1", 9027, 53205m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 2", 9050, 53205m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 3", 9073, 53205m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 4", 9096, 53205m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 5", 9119, 53205m },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 6", 9142, 53205m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 1", 9041, 77980m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d2"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 2", 9064, 77980m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d3"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 3", 9087, 77980m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d4"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 4", 9110, 77980m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 5", 9133, 77980m },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d6"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 6", 9156, 77980m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253281"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 1", 9035, 2000000m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253282"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 2", 9058, 2000000m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253283"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 3", 9081, 2000000m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253284"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 4", 9104, 2000000m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253285"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 5", 9127, 2000000m },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253286"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 6", 9150, 2000000m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a641"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 1", 9037, 60945m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a642"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 2", 9060, 60945m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a643"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 3", 9083, 60945m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a644"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 4", 9106, 60945m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a645"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 5", 9129, 60945m },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a646"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 6", 9152, 60945m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb31"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 1", 9030, 24550m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb32"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 2", 9053, 24550m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb33"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 3", 9076, 24550m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb34"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 4", 9099, 24550m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb35"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 5", 9122, 24550m },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb36"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 6", 9145, 24550m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb91"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 1", 9044, 135000m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb92"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 2", 9067, 135000m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb93"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 3", 9090, 135000m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb94"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 4", 9113, 135000m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb95"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 5", 9136, 135000m },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb96"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 6", 9159, 135000m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659411"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 1", 9045, 138890m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659412"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 2", 9068, 138890m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659413"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 3", 9091, 138890m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659414"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 4", 9114, 138890m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659415"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 5", 9137, 138890m },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659416"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 6", 9160, 138890m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b61"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 1", 9033, 54950m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b62"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 2", 9056, 54950m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b63"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 3", 9079, 54950m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b64"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 4", 9102, 54950m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b65"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 5", 9125, 54950m },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b66"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 6", 9148, 54950m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898811"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 1", 9043, 61990m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898812"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 2", 9066, 61990m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898813"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 3", 9089, 61990m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898814"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 4", 9112, 61990m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898815"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 5", 9135, 61990m },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898816"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 6", 9158, 61990m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 1", 9024, 27155m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 2", 9047, 27155m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 3", 9070, 27155m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 4", 9093, 27155m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 5", 9116, 27155m },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 6", 9139, 27155m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043031"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 1", 9034, 103360m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043032"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 2", 9057, 103360m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043033"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 3", 9080, 103360m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043034"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 4", 9103, 103360m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043035"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 5", 9126, 103360m },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043036"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 6", 9149, 103360m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 1", 9025, 500000m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 2", 9048, 500000m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 3", 9071, 500000m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 4", 9094, 500000m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 5", 9117, 500000m },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 6", 9140, 500000m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 1", 9038, 56545m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb2"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 2", 9061, 56545m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb3"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 3", 9084, 56545m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb4"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 4", 9107, 56545m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 5", 9130, 56545m },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb6"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 6", 9153, 56545m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c791"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 1", 9046, 67790m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c792"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 2", 9069, 67790m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c793"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 3", 9092, 67790m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c794"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 4", 9115, 67790m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c795"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 5", 9138, 67790m },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c796"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 6", 9161, 67790m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca21"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 1", 9029, 42800m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca22"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 2", 9052, 42800m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca23"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 3", 9075, 42800m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca24"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 4", 9098, 42800m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca25"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 5", 9121, 42800m },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca26"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 6", 9144, 42800m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf91"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 1", 9040, 100045m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf92"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 2", 9063, 100045m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf93"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 3", 9086, 100045m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf94"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 4", 9109, 100045m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf95"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 5", 9132, 100045m },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf96"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 6", 9155, 100045m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f61"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 1", 9031, 113540m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f62"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 2", 9054, 113540m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f63"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 3", 9077, 113540m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f64"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 4", 9100, 113540m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f65"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 5", 9123, 113540m },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f66"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 6", 9146, 113540m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7551"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 1", 9028, 22470m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7552"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 2", 9051, 22470m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7553"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 3", 9074, 22470m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7554"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 4", 9097, 22470m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7555"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 5", 9120, 22470m },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7556"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 6", 9143, 22470m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 1", 9026, 25000m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 2", 9049, 25000m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 3", 9072, 25000m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 4", 9095, 25000m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 5", 9118, 25000m },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 6", 9141, 25000m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec501"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 1", 9039, 55195m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec502"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 2", 9062, 55195m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec503"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 3", 9085, 55195m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec504"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 4", 9108, 55195m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec505"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 5", 9131, 55195m },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec506"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 6", 9154, 55195m }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            table: "Products",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Products_Name",
            table: "Products",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_Number",
            table: "Products",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PushNotificationSubscriptions_UserSessionId",
            table: "PushNotificationSubscriptions",
            column: "UserSessionId",
            unique: true,
            filter: "[UserSessionId] IS NOT NULL");

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

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_UserId",
            table: "UserSessions",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DataProtectionKeys");

        migrationBuilder.DropTable(
            name: "Products");

        migrationBuilder.DropTable(
            name: "PushNotificationSubscriptions");

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
            name: "UserSessions");

        migrationBuilder.DropTable(
            name: "Roles");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
