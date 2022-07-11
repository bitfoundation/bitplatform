using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPanelTemplate.Api.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    BirthDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProfileImageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmationEmailRequestedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResetPasswordEmailRequestedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Price = table.Column<decimal>(type: "money", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "Date", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
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
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
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
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    { 1, "#ff0000", "Ford" },
                    { 2, "#0300ff", "Nissan" },
                    { 3, "#00f800", "Benz" },
                    { 4, "#fefe00", "BMW" },
                    { 5, "#fe04fe", "Tesla" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CreateDate", "Desc", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2021, 5, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "The Ford Mustang is ranked #1 in Sports Cars", "Mustang", 27155m },
                    { 2, 1, new DateTime(2021, 5, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", "GT", 500000m },
                    { 3, 1, new DateTime(2021, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", "Ranger", 25000m },
                    { 4, 1, new DateTime(2022, 2, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Raptor is a SCORE off-road trophy truck living in a asphalt world", "Raptor", 53205m },
                    { 5, 1, new DateTime(2021, 5, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", "Maverick", 22470m },
                    { 6, 2, new DateTime(2022, 1, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "A powerful convertible sports car", "Roadster", 42800m },
                    { 7, 2, new DateTime(2021, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "A perfectly adequate family sedan with sharp looks", "Altima", 24550m },
                    { 8, 2, new DateTime(2022, 3, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", "GT-R", 113540m },
                    { 9, 2, new DateTime(2022, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "A new smart SUV", "Juke", 28100m },
                    { 10, 3, new DateTime(2021, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "H247", 54950m },
                    { 11, 3, new DateTime(2022, 4, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "V297", 103360m },
                    { 12, 3, new DateTime(2021, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "R50", 2000000m },
                    { 13, 4, new DateTime(2022, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "M550i", 77790m },
                    { 14, 4, new DateTime(2022, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "540i", 60945m },
                    { 15, 4, new DateTime(2021, 5, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "530e", 56545m },
                    { 16, 4, new DateTime(2022, 2, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "530i", 55195m },
                    { 17, 4, new DateTime(2021, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "M850i", 100045m },
                    { 18, 4, new DateTime(2022, 3, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "X7", 77980m },
                    { 19, 4, new DateTime(2021, 3, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "IX", 87000m },
                    { 20, 5, new DateTime(2022, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "rapid acceleration and dynamic handling", "Model 3", 61990m },
                    { 21, 5, new DateTime(2021, 5, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "finishes near the top of our luxury electric car rankings.", "Model S", 135000m },
                    { 22, 5, new DateTime(2022, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Heart-pumping acceleration, long drive range", "Model X", 138890m },
                    { 23, 5, new DateTime(2021, 3, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "extensive driving range, lots of standard safety features", "Model Y", 67790m }
                });

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
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

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
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "RoleClaims");

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
}
