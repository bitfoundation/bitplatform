#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Boilerplate.Server.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "dbo");

        migrationBuilder.CreateTable(
            name: "Categories",
            schema: "dbo",
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
            name: "Roles",
            schema: "dbo",
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
            name: "SystemPrompts",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PromptKind = table.Column<int>(type: "INTEGER", nullable: false),
                Markdown = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SystemPrompts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "dbo",
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
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ShortId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Price = table.Column<decimal>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
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
                    principalSchema: "dbo",
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoleClaims",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TodoItems",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserClaims",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserLogins",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserSessions",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserTokens",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WebAuthnCredential",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                PublicKey = table.Column<byte[]>(type: "BLOB", nullable: true),
                SignCount = table.Column<uint>(type: "INTEGER", nullable: false),
                Transports = table.Column<string>(type: "TEXT", nullable: true),
                IsBackupEligible = table.Column<bool>(type: "INTEGER", nullable: false),
                IsBackedUp = table.Column<bool>(type: "INTEGER", nullable: false),
                AttestationObject = table.Column<byte[]>(type: "BLOB", nullable: true),
                AttestationClientDataJson = table.Column<byte[]>(type: "BLOB", nullable: true),
                UserHandle = table.Column<byte[]>(type: "BLOB", nullable: true),
                AttestationFormat = table.Column<string>(type: "TEXT", nullable: true),
                RegDate = table.Column<long>(type: "INTEGER", nullable: false),
                AaGuid = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WebAuthnCredential", x => x.Id);
                table.ForeignKey(
                    name: "FK_WebAuthnCredential_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PushNotificationSubscriptions",
            schema: "dbo",
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
                    principalSchema: "dbo",
                    principalTable: "UserSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.InsertData(
            schema: "dbo",
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
            schema: "dbo",
            table: "Roles",
            columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
            values: new object[] { new Guid("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"), "8ff71671-a1d6-5f97-abb9-d87d7b47d6e7", "SuperAdmin", "SUPERADMIN" });

        migrationBuilder.InsertData(
            schema: "dbo",
            table: "SystemPrompts",
            columns: new[] { "Id", "Markdown", "PromptKind" },
            values: new object[] { new Guid("a8c94d94-0004-4dd0-921c-255e0a581424"), "\r\nYou are a support assistant for Boilerplate app. Below, you will find a markdown document containing information about the app, and then the user's query.\r\n\r\n# Boilerplate app - Features and usage guide\r\n\r\n**[[[GENERAL_INFORMATION_BEGIN]]]**\r\n\r\n*   **Platforms:** The application is available on Android, iOS, Windows, macOS, and as a Web (PWA) application.\r\n*   **Languages:** The app supports multiple languages: English, Dutch, and Persian.\r\n\r\n* Website address: https://adminpanel.bitplatform.dev/\r\n* Google Play: https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template\r\n* Apple Store: https://apps.apple.com/us/app/bit-adminpanel/id6450611349\r\n* Windows EXE installer: https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe\r\n\r\n## 1. Account Management & Authentication\r\n\r\nThese features cover user sign-up, sign-in, account recovery, and security settings.\r\n\r\n### 1.1. Sign Up\r\n*   **Description:** Allows new users to create an account. Users can sign up using their email address, phone number, or via social providers.\r\n*   **How to Use:**\r\n        - Navigate to the [Sign Up page](/sign-up).\r\n\r\n### 1.2. Sign In\r\n*   **Description:** Allows existing users to sign into their accounts using various methods.\r\n*   **How to Use:**\r\n        - Navigate to the [Sign In page](/sign-in).\r\n\r\n### 1.3. Confirm Account\r\n*   **Description:** Verifies a user's email address or phone number after sign-up, typically by entering a code sent to them.\r\n*   **How to Use:**\r\n        - Navigate to the [Confirmation page](/confirm) (often automatic redirection after sign-up).\r\n\r\n### 1.4. Forgot Password\r\n*   **Description:** Initiates the password reset process by sending a reset token (code) to the user's registered email or phone number.\r\n*   **How to Use:**\r\n        - Navigate to the [Forgot Password page](/forgot-password), often linked from the Sign In page.\r\n\r\n### 1.5. Reset Password\r\n*   **Description:** Allows users to set a new password after requesting a reset token via the Forgot Password flow.\r\n*   **How to Use:**\r\n        - Navigate to the [Reset Password page](/reset-password).\r\n\r\n## 2. User Settings\r\n\r\nAccessible after signin in, these pages allow users to manage their profile, account details, security settings, and active sessions.\r\n\r\n### 2.1. Profile Settings\r\n*   **Description:** Manage personal user information like name, profile picture, birthdate, and gender.\r\n*   **How to Use:**\r\n        - Navigate to the [profile page](/settings/profile).\r\n\r\n### 2.2. Account Settings\r\n*   **Description:** Manage account-specific details like email, phone number, enable passwordless sign-in, and account deletion.\r\n*   **How to Use:**\r\n        - Navigate to the [account page](/settings/account).\r\n\r\n### 2.3. Two-Factor Authentication (2FA)\r\n*   **Description:** Enhance account security by requiring a second form of verification (typically a code from an authenticator app) during sign-in.\r\n*   **How to Use:**\r\n        - Navigate to the [two factor authentication page](/settings/tfa).\r\n\r\n### 2.4. Session Management\r\n*   **Description:** View all devices and browsers where the user is currently signed in and provides the ability to sign out (revoke) specific sessions remotely.\r\n*   **How to Use:**\r\n        - Navigate to the [sessions page](/settings/sessions).\r\n\r\n## 3. Core Application Features\r\n\r\nThese are the primary functional areas of the application beyond account management.\r\n\r\n//#if (module == 'Admin')\r\n### 3.1. Dashboard\r\n*   **Description:** Provides a high-level overview and analytics of key application data, such as categories and products.\r\n*   **How to Use:**\r\n        - Navigate to the [dashboard page](/dashboard).\r\n\r\n### 3.2. Categories Management\r\n*   **Description:** Allows users to view, create, edit, and delete categories, often used to organize products.\r\n*   **How to Use:**\r\n        - Navigate to the [categories page](/categories).\r\n\r\n### 3.3. Products Management\r\n*   **Description:** Allows users to view, create, edit, and delete products.\r\n*   **How to Use:**\r\n        - Navigate to the [products page](/products).\r\n\r\n### 3.4. Add/Edit Product\r\n*   **Description:** A form page for creating a new product or modifying an existing one.\r\n*   **How to Use:**\r\n        - Navigate to the [add/edit products page](/add-edit-product).\r\n//#endif\r\n\r\n//#if (module == 'Sales')\r\n### 3.5. View Product\r\n*   **Description:** Displays the details of a single product in a read-only view.\r\n*   **How to Use:**\r\n        - Navigate to the [view products page](/).\r\n//#endif\r\n\r\n//#if (sample == true)\r\n### 3.6. Todo List\r\n*   **Description:** A simple task management feature to keep track of personal tasks.\r\n*   **How to Use:**\r\n        - Navigate to the [todo page](/todo).\r\n//#endif\r\n\r\n## 4. Informational Pages\r\n\r\n### 4.1. About Page\r\n*   **Description:** Provides information about the application itself.\r\n*   **How to Use:**\r\n        - Navigate to the [about page](/about).\r\n\r\n### 4.2. Terms Page\r\n*   **Description:** Displays the legal terms and conditions, including the End-User License Agreement (EULA) and potentially the Privacy Policy.\r\n*   **How to Use:**\r\n        - Navigate to the [terms page](/terms).\r\n\r\n---\r\n\r\n**[[[GENERAL_INFORMATION_END]]]**\r\n\r\n**[[[INSTRUCTIONS_BEGIN]]]**\r\n\r\n- ### Language:\r\n    - Always respond in the {{UserCulture}} language or the language specified by the user.\r\n\r\n- ### User's device info:\r\n        - User's device is {{DeviceInfo}} or the one specifid by user. Tailor your platform specific responses accordingly.\r\n	\r\n- ### Relevance:  \r\n    - Before answering, determine if the user's query is related to the Boilerplate app. A query is considered related only if it pertains to the features, usage, or support topics. A query is considered related only if it pertains to the features, usage, or support topics covered in the provided markdown document.\r\n\r\n- ## App-Related Queries:  \r\n    - Use the provided markdown document to deliver accurate and concise answers in {{UserCulture}} language or the language specified by the user.  \r\n\r\n    - When mentioning specific app pages, include the relative URL from the markdown document, formatted in markdown (e.g., [sign-up page](/sign-up)).  \r\n\r\n    - Maintain a helpful and professional tone throughout your response.  \r\n\r\n    - Structure your response clearly, utilizing bullet points or numbered steps where appropriate.\r\n	\r\n	- If the user asks multiple questions, address each one separately with clear headings or bullet points. If needed, ask them to prioritize: \"I see you have multiple questions. Which issue would you like me to address first?\"\r\n	  \r\n	- Never request sensitive information (e.g., passwords, PINs). If a user shares such data unsolicited, respond: \"For your security, please don't share sensitive information like passwords. Rest assured, your data is safe with us\"\r\n	  \r\n- ## User Feedback and Suggestions:\r\n  - If a user provides feedback or suggests a feature, respond: \"Thank you for your feedback! It's valuable to us, and I'll pass it on to the product team\"\r\n  \r\n  - If a user seems frustrated or confused, use calming language and offer to clarify: \"I'm sorry if this is confusing. I'm here to help—would you like me to explain it again?\"\r\n\r\n- ## Unresolved Issues:  \r\n    - If you cannot resolve the user's issue, respond with: \"I'm sorry I couldn't resolve your issue. I understand how frustrating this must be for you. Please provide your email address so a human operator can follow up with you soon\"\r\n\r\n    - After the user provides their email address, save the user's email and their conversation history. Then ask if they have any other issues. For example: \"Thank you for providing your email. Do you have any other issues you'd like me to assist with?\"\r\n\r\n**[[[INSTRUCTIONS_END]]]**\r\n", 0 });

        migrationBuilder.InsertData(
            schema: "dbo",
            table: "Users",
            columns: new[] { "Id", "AccessFailedCount", "BirthDate", "ConcurrencyStamp", "ElevatedAccessTokenRequestedOn", "Email", "EmailConfirmed", "EmailTokenRequestedOn", "FullName", "Gender", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "OtpRequestedOn", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "PhoneNumberTokenRequestedOn", "ProfileImageName", "ResetPasswordTokenRequestedOn", "SecurityStamp", "TwoFactorEnabled", "TwoFactorTokenRequestedOn", "UserName" },
            values: new object[] { new Guid("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), 0, 1306790461440000000L, "315e1a26-5b3a-4544-8e91-2760cd28e231", null, "test@bitplatform.dev", true, 1306790461440000000L, "Boilerplate test account", 0, true, null, "TEST@BITPLATFORM.DEV", "TEST", null, "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", "+31684207362", true, null, null, null, "959ff4a9-4b07-4cc1-8141-c5fc033daf83", false, null, "test" });

        migrationBuilder.InsertData(
            schema: "dbo",
            table: "Products",
            columns: new[] { "Id", "CategoryId", "ConcurrencyStamp", "CreatedOn", "Description", "ImageFileName", "Name", "Price", "ShortId" },
            values: new object[,]
            {
                { new Guid("01d223a3-182d-406a-9722-19dab083f961"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 1", 77790m, 9036 },
                { new Guid("01d223a3-182d-406a-9722-19dab083f962"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 2", 77790m, 9059 },
                { new Guid("01d223a3-182d-406a-9722-19dab083f963"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 3", 77790m, 9082 },
                { new Guid("01d223a3-182d-406a-9722-19dab083f964"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 4", 77790m, 9105 },
                { new Guid("01d223a3-182d-406a-9722-19dab083f965"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 5", 77790m, 9128 },
                { new Guid("01d223a3-182d-406a-9722-19dab083f966"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful, sporty variant of the BMW 5 Series", null, "M550i - 6", 77790m, 9151 },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 1", 87000m, 9042 },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e2"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 2", 87000m, 9065 },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e3"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 3", 87000m, 9088 },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e4"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 4", 87000m, 9111 },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 5", 87000m, 9134 },
                { new Guid("1b22319e-0a58-471e-82b6-75cd8b9d98e6"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306413563904000000L, "Luxury crossover SUV that combines cutting-edge technology", null, "IX - 6", 87000m, 9157 },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a331"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 1", 28100m, 9032 },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a332"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 2", 28100m, 9055 },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a333"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 3", 28100m, 9078 },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a334"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 4", 28100m, 9101 },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a335"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 5", 28100m, 9124 },
                { new Guid("362a6638-0031-485d-825f-e8aeae63a336"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A new smart SUV", null, "Juke - 6", 28100m, 9147 },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 1", 53205m, 9027 },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 2", 53205m, 9050 },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 3", 53205m, 9073 },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 4", 53205m, 9096 },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 5", 53205m, 9119 },
                { new Guid("43a82ec1-aab6-445f-83af-a85028417cf6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "Raptor is a SCORE off-road trophy truck living in a asphalt world", null, "Raptor - 6", 53205m, 9142 },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 1", 77980m, 9041 },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d2"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 2", 77980m, 9064 },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d3"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 3", 77980m, 9087 },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d4"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 4", 77980m, 9110 },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 5", 77980m, 9133 },
                { new Guid("4d9cb0f4-1f32-45d5-8c84-d7f15bc569d6"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", null, "X7 - 6", 77980m, 9156 },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253281"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 1", 2000000m, 9035 },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253282"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 2", 2000000m, 9058 },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253283"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 3", 2000000m, 9081 },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253284"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 4", 2000000m, 9104 },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253285"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 5", 2000000m, 9127 },
                { new Guid("59eea437-bdf2-4c11-b262-06643b253286"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Ultra-rare and powerful sports car", null, "R50 - 6", 2000000m, 9150 },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a641"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 1", 60945m, 9037 },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a642"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 2", 60945m, 9060 },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a643"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 3", 60945m, 9083 },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a644"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 4", 60945m, 9106 },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a645"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 5", 60945m, 9129 },
                { new Guid("64a2616f-3af6-4248-86cf-4a605095a646"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Luxurious and powerful sedan that combines elegant design with impressive performance", null, "540i - 6", 60945m, 9152 },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb31"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 1", 24550m, 9030 },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb32"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 2", 24550m, 9053 },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb33"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 3", 24550m, 9076 },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb34"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 4", 24550m, 9099 },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb35"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 5", 24550m, 9122 },
                { new Guid("74bb268f-18cf-45ec-9f2f-30b34b18fb36"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "A perfectly adequate family sedan with sharp looks", null, "Altima - 6", 24550m, 9145 },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb91"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 1", 135000m, 9044 },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb92"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 2", 135000m, 9067 },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb93"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 3", 135000m, 9090 },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb94"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 4", 135000m, 9113 },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb95"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 5", 135000m, 9136 },
                { new Guid("840ba759-bde9-4821-b49b-c981c082bb96"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Finishes near the top of our luxury electric car rankings.", null, "Model S - 6", 135000m, 9159 },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659411"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 1", 138890m, 9045 },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659412"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 2", 138890m, 9068 },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659413"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 3", 138890m, 9091 },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659414"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 4", 138890m, 9114 },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659415"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 5", 138890m, 9137 },
                { new Guid("840e113b-5074-4b1c-86bd-e9affb659416"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Heart-pumping acceleration, long drive range", null, "Model X - 6", 138890m, 9160 },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b61"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 1", 54950m, 9033 },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b62"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 2", 54950m, 9056 },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b63"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 3", 54950m, 9079 },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b64"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 4", 54950m, 9102 },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b65"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 5", 54950m, 9125 },
                { new Guid("8629931e-e26e-4885-b561-e447197d4b66"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Subcompact luxury crossover SUV", null, "H247 - 6", 54950m, 9148 },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898811"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 1", 61990m, 9043 },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898812"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 2", 61990m, 9066 },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898813"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 3", 61990m, 9089 },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898814"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 4", 61990m, 9112 },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898815"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 5", 61990m, 9135 },
                { new Guid("96c73b9c-03df-4f70-ac8d-75c32b898816"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "Rapid acceleration and dynamic handling", null, "Model 3 - 6", 61990m, 9158 },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 1", 27155m, 9024 },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 2", 27155m, 9047 },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 3", 27155m, 9070 },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 4", 27155m, 9093 },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 5", 27155m, 9116 },
                { new Guid("9a59dda2-7b12-4cc1-9658-d2586eef91d6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "The Ford Mustang is ranked #1 in Sports Cars", null, "Mustang - 6", 27155m, 9139 },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043031"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 1", 103360m, 9034 },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043032"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 2", 103360m, 9057 },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043033"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 3", 103360m, 9080 },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043034"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 4", 103360m, 9103 },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043035"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 5", 103360m, 9126 },
                { new Guid("a1c1987d-ee6c-41ad-9647-18de45043036"), new Guid("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "Battery-electric full-size luxury liftback", null, "V297 - 6", 103360m, 9149 },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 1", 500000m, 9025 },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 2", 500000m, 9048 },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 3", 500000m, 9071 },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 4", 500000m, 9094 },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 5", 500000m, 9117 },
                { new Guid("a42914e2-92da-4f0b-aab0-b9572c9671b6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306457800704000000L, "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", null, "GT - 6", 500000m, 9140 },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb1"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 1", 56545m, 9038 },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb2"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 2", 56545m, 9061 },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb3"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 3", 56545m, 9084 },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb4"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 4", 56545m, 9107 },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb5"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 5", 56545m, 9130 },
                { new Guid("ac50dc29-4b7e-4d4d-b23a-4227d91f2bb6"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306448953344000000L, "Combines class, spaciousness, and a well-built cabin", null, "530e - 6", 56545m, 9153 },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c791"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 1", 67790m, 9046 },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c792"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 2", 67790m, 9069 },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c793"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 3", 67790m, 9092 },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c794"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 4", 67790m, 9115 },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c795"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 5", 67790m, 9138 },
                { new Guid("b2db9074-a0a9-4054-87e2-206b7a55c796"), new Guid("747f6d66-7524-40ca-8494-f65e85b5ee5d"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "Extensive driving range, lots of standard safety features", null, "Model Y - 6", 67790m, 9161 },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca21"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 1", 42800m, 9029 },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca22"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 2", 42800m, 9052 },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca23"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 3", 42800m, 9075 },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca24"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 4", 42800m, 9098 },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca25"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 5", 42800m, 9121 },
                { new Guid("d53bb159-f4f9-493a-b4dc-215fd765ca26"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306466648064000000L, "A powerful convertible sports car", null, "Roadster - 6", 42800m, 9144 },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf91"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 1", 100045m, 9040 },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf92"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 2", 100045m, 9063 },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf93"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 3", 100045m, 9086 },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf94"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 4", 100045m, 9109 },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf95"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 5", 100045m, 9132 },
                { new Guid("e159b1ad-12aa-4e02-a39b-d5e4a32eaf96"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306431258624000000L, "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", null, "M850i - 6", 100045m, 9155 },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f61"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 1", 113540m, 9031 },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f62"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 2", 113540m, 9054 },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f63"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 3", 113540m, 9077 },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f64"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 4", 113540m, 9100 },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f65"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 5", 113540m, 9123 },
                { new Guid("eb787e1a-7ba8-4708-924b-9f7964fa0f66"), new Guid("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", null, "GT-R - 6", 113540m, 9146 },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7551"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 1", 22470m, 9028 },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7552"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 2", 22470m, 9051 },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7553"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 3", 22470m, 9074 },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7554"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 4", 22470m, 9097 },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7555"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 5", 22470m, 9120 },
                { new Guid("f01b32bb-eccd-43be-aaf3-3c788a7d7556"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306422411264000000L, "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", null, "Maverick - 6", 22470m, 9143 },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee1"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 1", 25000m, 9026 },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee2"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 2", 25000m, 9049 },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee3"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 3", 25000m, 9072 },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee4"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 4", 25000m, 9095 },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee5"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 5", 25000m, 9118 },
                { new Guid("f75325c8-a213-470b-ab93-4677ca4caee6"), new Guid("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", null, "Ranger - 6", 25000m, 9141 },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec501"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 1", 55195m, 9039 },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec502"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 2", 55195m, 9062 },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec503"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 3", 55195m, 9085 },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec504"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 4", 55195m, 9108 },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec505"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 5", 55195m, 9131 },
                { new Guid("fb41cc51-9abd-4b45-b0d9-ea8f565ec506"), new Guid("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 1306440105984000000L, "Zippy and fuel-efficient powertrain, and sure-footed handling", null, "530i - 6", 55195m, 9154 }
            });

        migrationBuilder.InsertData(
            schema: "dbo",
            table: "RoleClaims",
            columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
            values: new object[] { 1, "edit-ai-system-prompts", "true", new Guid("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7") });

        migrationBuilder.InsertData(
            schema: "dbo",
            table: "UserRoles",
            columns: new[] { "RoleId", "UserId" },
            values: new object[] { new Guid("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7"), new Guid("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7") });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            schema: "dbo",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            schema: "dbo",
            table: "Products",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Products_Name",
            schema: "dbo",
            table: "Products",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_ShortId",
            schema: "dbo",
            table: "Products",
            column: "ShortId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PushNotificationSubscriptions_UserSessionId",
            schema: "dbo",
            table: "PushNotificationSubscriptions",
            column: "UserSessionId",
            unique: true,
            filter: "[UserSessionId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_RoleClaims_RoleId_ClaimType",
            schema: "dbo",
            table: "RoleClaims",
            columns: new[] { "RoleId", "ClaimType" });

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Name",
            schema: "dbo",
            table: "Roles",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            schema: "dbo",
            table: "Roles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TodoItems_UserId",
            schema: "dbo",
            table: "TodoItems",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserClaims_UserId_ClaimType",
            schema: "dbo",
            table: "UserClaims",
            columns: new[] { "UserId", "ClaimType" });

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_UserId",
            schema: "dbo",
            table: "UserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId_UserId",
            schema: "dbo",
            table: "UserRoles",
            columns: new[] { "RoleId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            schema: "dbo",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            schema: "dbo",
            table: "Users",
            column: "Email",
            unique: true,
            filter: "[Email] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_PhoneNumber",
            schema: "dbo",
            table: "Users",
            column: "PhoneNumber",
            unique: true,
            filter: "[PhoneNumber] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            schema: "dbo",
            table: "Users",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_UserId",
            schema: "dbo",
            table: "UserSessions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_WebAuthnCredential_UserId",
            schema: "dbo",
            table: "WebAuthnCredential",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Products",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "PushNotificationSubscriptions",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "RoleClaims",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "SystemPrompts",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "TodoItems",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "UserClaims",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "UserLogins",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "UserRoles",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "UserTokens",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "WebAuthnCredential",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Categories",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "UserSessions",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Roles",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "dbo");
    }
}
