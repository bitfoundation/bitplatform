# Stage 7: ASP.NET Core Identity - Authentication & Authorization

Welcome to **Stage 7** of the Boilerplate project tutorial! In this stage, you will explore the comprehensive **authentication and authorization system** built into the project. This production-ready identity system includes JWT-based authentication, role and permission management, session handling, two-factor authentication, and much more.

---

## Table of Contents

1. [Understanding Authentication Methods](#understanding-authentication-methods)
   - [The Two Fundamental Methods](#the-two-fundamental-methods)
   - [Two-Factor Authentication (2FA)](#two-factor-authentication-2fa)
   - [Other Sign-In Methods Are Built on OTP](#other-sign-in-methods-are-built-on-otp)
2. [Authentication Architecture](#authentication-architecture)
   - [JWT Token-Based Authentication](#jwt-token-based-authentication)
   - [Session Management](#session-management)
   - [External Identity Support](#external-identity-support)
3. [Authorization and Access Control](#authorization-and-access-control)
   - [Role-Based and Permission-Based Authorization](#role-based-and-permission-based-authorization)
   - [Policy-Based Authorization](#policy-based-authorization)
   - [Custom Claim Types](#custom-claim-types)
4. [Identity Configuration](#identity-configuration)
5. [Security Best Practices](#security-best-practices)
6. [One-Time Token System](#one-time-token-system)
7. [Advanced Topics](#advanced-topics)
   - [JWT Token Signing with PFX Certificates](#jwt-token-signing-with-pfx-certificates)
   - [Keycloak Integration](#keycloak-integration)
8. [Hands-On Exploration](#hands-on-exploration)
9. [Video Tutorial](#video-tutorial)

**Important**: All topics related to WebAuthn, passkeys, and passwordless authentication are explained in [Stage 24](/.docs/24-%20WebAuthn%20and%20Passwordless%20Authentication%20(Advanced).md).

---

## Understanding Authentication Methods

Before diving into the technical architecture, it's important to understand that the Boilerplate project fundamentally supports only **two authentication methods**. All other sign-in options are built on top of these core methods.

### The Two Fundamental Methods

**1. Identifier + Password**
- **Identifier**: Username, Email, or Phone Number
- **Password**: User's secret password
- This is the traditional authentication method

**2. Identifier + OTP (One-Time Password)**
- **Identifier**: Username, Email, or Phone Number  
- **OTP**: A 6-digit code sent to the user
- Also known as "Magic Link" authentication when delivered via email

> **Note about Username field**: In the default UI, the Username field is commented out to keep the interface clean and simple. If your business requires username-based authentication, you can easily re-enable it in the sign-in/sign-up components.

### Two-Factor Authentication (2FA)

Depending on user settings, **Two-Factor Authentication** may be triggered after the initial sign-in.

### Other Sign-In Methods Are Built on OTP

All alternative authentication methods in the Boilerplate ultimately generate a 6-digit OTP code and perform an **automatic OTP sign-in** behind the scenes:

| Method | How It Works |
|--------|-------------|
| **External Providers** (Google, Facebook, GitHub, etc.) | After successful OAuth flow, generates an OTP and auto-signs in |
| **WebAuthn / Passkeys** (Fingerprint, Face ID) | After biometric verification, generates an OTP and auto-signs in |
| **Magic Link** (Email link) | Clicking the link triggers OTP sign-in with the embedded token |

This unified approach means:
- ✅ **2FA is always respected** regardless of how the user signs in
- ✅ **Session management works the same way** for all sign-in methods
- ✅ **Simplified codebase** with a single authentication pipeline

---

## Authentication Architecture

### JWT Token-Based Authentication

The project uses **JWT (JSON Web Tokens)** for stateless authentication. Here's how it works:

#### Key Characteristics

- **Access Tokens**: Short-lived tokens (default: **5 minutes**) included in every API request via the `Authorization: Bearer <token>` header
- **Refresh Tokens**: Long-lived tokens (default: **14 days**) stored securely on the client to obtain new access tokens without requiring the user to sign in again
- **Token Generation**: Uses ASP.NET Core Identity's built-in bearer token authentication with the **HS512 algorithm**
- **Security**: Tokens are signed using a secret key configured in [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

#### Token Configuration

You can configure token expiration in [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):

```json
"Identity": {
    "BearerTokenExpiration": "0.00:05:00",  // Format: D.HH:mm:ss (5 minutes)
    "RefreshTokenExpiration": "14.00:00:00", // 14 days
    "JwtIssuerSigningKeySecret": "VeryLongJWTIssuerSigningKeySecretThatIsMoreThan64BytesToEnsureCompatibilityWithHS512Algorithm"
}
```

---

### Session Management

Unlike traditional session cookies, this project implements **server-side session tracking** in the database while still using JWT tokens.

#### The UserSession Entity

User sessions are persisted in the database through the [`UserSession`](/src/Server/Boilerplate.Server.Api/Models/Identity/UserSession.cs) entity:

```csharp
public partial class UserSession
{
    public Guid Id { get; set; }
    public string? IP { get; set; }
    public string? Address { get; set; }
    public bool Privileged { get; set; }
    public long StartedOn { get; set; }       // Unix timestamp
    public long? RenewedOn { get; set; }      // Unix timestamp
    public Guid UserId { get; set; }
    public string? SignalRConnectionId { get; set; }
    public string? DeviceInfo { get; set; }
    public AppPlatformType? PlatformType { get; set; }
    public string? CultureName { get; set; }
    public string? AppVersion { get; set; }
    // ... additional properties
}
```

#### Key Features

- **One Session Per Device**: Each active device/browser has its own `UserSession` record
- **Detailed Tracking**: Sessions track IP address, device info, platform type, app version, and culture
- **Real-Time Communication**: Sessions include SignalR connection IDs for push notifications
- **User Control**: Users can view and manage all their active sessions from the Settings page

#### Session ID in Tokens

The session ID is embedded in both access and refresh tokens as a claim (`AppClaimTypes.SESSION_ID`). This enables:

- **Session Revocation**: When a user revokes a session, its associated refresh token becomes invalid
- **Forced Re-authentication**: Deleted sessions require users to sign in again on that device

#### Session Creation Example

From [`IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs):

```csharp
private async Task<UserSession> CreateUserSession(Guid userId, CancellationToken cancellationToken)
{
    var userSession = new UserSession
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        StartedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
        // Relying on Cloudflare CDN to retrieve location
        Address = $"{Request.Headers["cf-ipcountry"]}, {Request.Headers["cf-ipcity"]}"
    };

    await UpdateUserSessionPrivilegeStatus(userSession, cancellationToken);
    return userSession;
}
```

---

### External Identity Support

The project supports **external authentication providers** for Single Sign-On (SSO) and Social sign-in purposes.

#### Supported Providers

The following external identity providers have been already configured:

- **Google**
- **Microsoft/Azure Entra/Azure AD B2C**
- **Twitter (X)**
- **Apple**
- **GitHub**
- **Facebook**
- **Keycloak** Free, Open Source Identity and Access Management

#### Configuration

External provider settings are configured in [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) under the `Authentication` section:

```json
"Authentication": {
    "Google": {
        "ClientId": null,
        "ClientSecret": null
    },
    "GitHub": {
        "ClientId": null,
        "ClientSecret": null
    },
    "AzureAD": {
        "Instance": "https://login.microsoftonline.com/",
        "TenantId": null,
        "ClientId": null,
        "ClientSecret": null,
        "CallbackPath": "/signin-oidc"
    }
    // ... other providers
}
```

#### External Sign-In Flow

From [`IdentityController.ExternalSignIn.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.ExternalSignIn.cs):

```csharp
[HttpGet]
public async Task<ActionResult> ExternalSignIn(string provider, string? returnUrl = null, 
    int? localHttpPort = null, [FromQuery] string? origin = null)
{
    var redirectUrl = Url.Action(nameof(ExternalSignInCallback), "Identity", 
        new { returnUrl, localHttpPort, origin });
    var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    return new ChallengeResult(provider, properties);
}
```

When a external sign-in is successful, the system:
1. Retrieves user information from the external provider
2. Either finds an existing user or creates a new one
3. Automatically confirms email/phone if the provider provides them
4. Generates a magic link for automatic sign-in (supports 2FA if enabled)

---

## Authorization and Access Control

### Role-Based and Permission-Based Authorization

The project implements a **hybrid authorization model** combining roles and permissions (claims).

#### Users and Roles

- Based on **ASP.NET Core Identity's** built-in `User` and `Role` entities
- Users can be assigned to **multiple roles**
- Roles are managed through the **Roles page** in the Admin Panel
- Users are managed through the **Users page** in the Admin Panel

#### Permissions (Claims)

Fine-grained permission system using **claims**:

- **User Claims**: Permissions assigned directly to individual users
- **Role Claims**: Permissions assigned to roles (inherited by all users in that role)
- Claims are added to the **JWT token payload** for efficient authorization checks (no database lookup needed)

---

### Policy-Based Authorization

The project defines **authorization policies** that can be used throughout the application.

#### Policy Configuration

Policies are defined in [`ISharedServiceCollectionExtensions.cs`](/src/Shared/Extensions/ISharedServiceCollectionExtensions.cs):

```csharp
public static void ConfigureAuthorizationCore(this IServiceCollection services)
{
    services.AddAuthorizationCore(options =>
    {
        // Built-in policies
        options.AddPolicy(AuthPolicies.TFA_ENABLED, 
            x => x.RequireClaim("amr", "mfa"));
        options.AddPolicy(AuthPolicies.PRIVILEGED_ACCESS, 
            x => x.RequireClaim(AppClaimTypes.PRIVILEGED_SESSION, "true"));
        options.AddPolicy(AuthPolicies.ELEVATED_ACCESS, 
            x => x.RequireClaim(AppClaimTypes.ELEVATED_SESSION, "true"));

        // Feature-based policies (automatically generated based on `AppFeatures`)
        foreach (var feat in AppFeatures.GetAll())
        {
            options.AddPolicy(feat.Value, policy => 
                policy.AddRequirements(new AppFeatureRequirement(
                    FeatureName: $"{feat.Group.Name}.{feat.Name}", 
                    FeatureValue: feat.Value)));
        }
    });
}
```

#### Built-in Authorization Policies

Defined in [`AuthPolicies.cs`](/src/Shared/Services/AuthPolicies.cs):

**1. TFA_ENABLED**
```csharp
public const string TFA_ENABLED = nameof(TFA_ENABLED);
```
- Requires the user to have **two-factor authentication enabled**
- Useful for pages that require enhanced security

**2. PRIVILEGED_ACCESS**
```csharp
/// <summary>
/// Having this policy/claim in access token means the user is allowed to view pages that require privileged access.
/// Currently, this policy applies only to the Todo and AdminPanel specific pages like dashboard page. 
/// However, it can be extended to cover additional pages as needed. 
/// 
/// By default, each user is limited to 3 active sessions.
/// The user's max privileged sessions' value is stored in <see cref="AppClaimTypes.MAX_PRIVILEGED_SESSIONS"/> claim.
/// 
/// Important: Do not apply this policy to the settings page, as users need access to manage and revoke their sessions there.
/// </summary>
public const string PRIVILEGED_ACCESS = nameof(PRIVILEGED_ACCESS);
```
- **Limits concurrent sessions** per user (default: **3 sessions**)
- Prevents resource abuse by limiting active sessions
- Applied to high-value pages like Dashboard, Products, Categories
- Users can manage sessions from the Settings page

**3. ELEVATED_ACCESS**
```csharp
/// <summary>
/// Enables the user to execute potentially harmful operations, like account removal.
/// This limited-time policy is activated upon successful verification via a secure 6-digit code
/// or during the initial minutes of a sign-in session of users with 2FA enabled.
/// </summary>
public const string ELEVATED_ACCESS = nameof(ELEVATED_ACCESS);
```
- Requires **recent re-authentication** for sensitive operations
- Used for dangerous actions like **account deletion**
- Activated by entering a 6-digit code or during initial sign-in with 2FA enabled
- **Time-limited** elevation (expires after a short period)

#### Privileged Session Logic

From [`IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs):

```csharp
private async Task UpdateUserSessionPrivilegeStatus(UserSession userSession, 
    CancellationToken cancellationToken)
{
    var userId = userSession.UserId;

    // Check if user has custom max session limit claim
    var maxPrivilegedSessionsClaimValues = await userClaimsService
        .GetClaimValues<int?>(userId, AppClaimTypes.MAX_PRIVILEGED_SESSIONS, cancellationToken);

    // -1 means unlimited sessions
    var hasUnlimitedPrivilegedSessions = maxPrivilegedSessionsClaimValues.Any(v => v == -1);

    var maxPrivilegedSessionsCount = hasUnlimitedPrivilegedSessions 
        ? -1 
        : maxPrivilegedSessionsClaimValues.Max() ?? AppSettings.Identity.MaxPrivilegedSessionsCount;

    // Determine if this session is privileged
    var isPrivileged = hasUnlimitedPrivilegedSessions ||
        userSession.Privileged is true || // Once privileged, stays privileged
        await DbContext.UserSessions.CountAsync(
            us => us.UserId == userSession.UserId && us.Privileged == true, 
            cancellationToken) < maxPrivilegedSessionsCount;

    // Add claims to the token
    userClaimsPrincipalFactory.SessionClaims.Add(
        new(AppClaimTypes.PRIVILEGED_SESSION, isPrivileged ? "true" : "false"));
    userClaimsPrincipalFactory.SessionClaims.Add(
        new(AppClaimTypes.MAX_PRIVILEGED_SESSIONS, 
            maxPrivilegedSessionsCount.ToString(CultureInfo.InvariantCulture)));

    userSession.Privileged = isPrivileged;
}
```

#### Feature-Based Policies

Defined in [`AppFeatures.cs`](/src/Shared/Services/AppFeatures.cs):

```csharp
public class AppFeatures
{
    public class Management
    {
        public const string ManageAiPrompt = "1.0";
        public const string ManageRoles = "1.1";
        public const string ManageUsers = "1.2";
    }

    public class System
    {
        public const string ManageLogs = "2.0";
        public const string ManageJobs = "2.1";
    }

    public class AdminPanel
    {
        public const string Dashboard = "3.0";
        public const string ManageProductCatalog = "3.1";
    }

    public class Todo
    {
        public const string ManageTodo = "4.0";
    }
}
```

The project **automatically creates policies** for all features defined in `AppFeatures`. Each feature value (e.g., `"1.0"`) becomes a policy name.

The reason behind small feature values is that they're stored in jwt token, so in order to keep jwt token payload small, such short, unique values have been assigned.

#### Policy Usage Examples

From actual pages in the project:

**TodoPage.razor**:
```csharp
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.Todo.ManageTodo)]
```

**UsersPage.razor**:
```csharp
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.Management.ManageUsers)]
```

**DashboardPage.razor**:
```csharp
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.AdminPanel.Dashboard)]
```

**Important**: When multiple `[Authorize]` attributes are used, **ALL policies must be satisfied** for the user to access the page.

---

### Custom Claim Types

#### System Claim Types

Defined in [`AppClaimTypes.cs`](/src/Shared/Services/AppClaimTypes.cs):

```csharp
/// <summary>
/// These claims may not be added to RoleClaims/UserClaims tables.
/// The system itself will assign these claims to the user based on AuthPolicies.
/// </summary>
public class AppClaimTypes
{
    public const string SESSION_ID = "s-id";
    public const string PRIVILEGED_SESSION = "p-s";
    public const string MAX_PRIVILEGED_SESSIONS = "mx-p-s";
    public const string ELEVATED_SESSION = "e-s";
    public const string FEATURES = "features";
}
```

**Important**: These claims are **automatically added by the system** and should **not** be manually added to the database:

- **`SESSION_ID`**: Unique identifier for the current user session => Guid value stored in UserSessions table
- **`MAX_PRIVILEGED_SESSIONS`**: Maximum allowed privileged sessions for this user => -1 (Unlimited) or a positive number
- **`PRIVILEGED_SESSION`**: Indicates if this session is privileged => "true" or "false"
- **`ELEVATED_SESSION`**: Indicates the user has recently authenticated for sensitive operations => "true" or "false"
- **`FEATURES`**: Contains the list of features/permissions values granted to the user => Array of AppFeature's values, for example ["1.1", "2.1"]

---

## Identity Configuration

### IdentitySettings in appsettings.json

**Location**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

**Configuration Options**:

```json
"Identity": {
    "JwtIssuerSigningKeySecret": "VeryLongJWTIssuerSigningKeySecretThatIsMoreThan64BytesToEnsureCompatibilityWithHS512Algorithm",
    "Issuer": "Boilerplate",
    "Audience": "Boilerplate",
    "BearerTokenExpiration": "0.00:05:00",
    "RefreshTokenExpiration": "14.00:00:00",
    "EmailTokenLifetime": "0.00:02:00",
    "PhoneNumberTokenLifetime": "0.00:02:00",
    "ResetPasswordTokenLifetime": "0.00:02:00",
    "TwoFactorTokenLifetime": "0.00:02:00",
    "OtpTokenLifetime": "0.00:02:00",
    "MaxPrivilegedSessionsCount": 3,
    "Password": {
        "RequireDigit": "false",
        "RequiredLength": "6",
        "RequireNonAlphanumeric": "false",
        "RequireUppercase": "false",
        "RequireLowercase": "false"
    },
    "SignIn": {
        "RequireConfirmedAccount": true
    }
}
```

#### Key Settings Explained

- **BearerTokenExpiration**: How long access tokens remain valid (default: 5 minutes)
- **RefreshTokenExpiration**: How long refresh tokens remain valid (default: 14 days)
- **EmailTokenLifetime**: Email confirmation token lifetime (default: 2 minutes)
- **TwoFactorTokenLifetime**: 2FA code validity period (default: 2 minutes)
- **MaxPrivilegedSessionsCount**: Default limit for privileged sessions per user
- **Password Requirements**: Configures password complexity rules
- **RequireConfirmedAccount**: Whether email/phone confirmation is required for sign-in

---

## Security Best Practices

The identity system follows **industry-standard security best practices**:

### 1. Password Hashing
- Uses **PBKDF2 with HMAC-SHA512**
- **100,000 iterations** for strong protection against brute-force attacks

### 2. Concurrency Stamps
- **Prevents concurrent modification conflicts**
- Each entity has a `ConcurrencyStamp` that changes on every update

### 3. Security Stamps
- **Invalidates all tokens** when changed
- Updated automatically after 2FA configuration, password changes, etc.

### 4. Account Lockout
- **Protects against brute-force attacks**
- Exponential backoff with increasing lockout duration

### 5. One-Time Tokens
- **All tokens can only be used once**
- Tokens expire after their configured lifetime
- See detailed explanation in the next section

### 6. Token Expiration
- **Short-lived access tokens** (5 minutes) minimize the window for token theft
- Refresh tokens allow seamless user experience without compromising security

### 7. Google reCAPTCHA
- **Protects sign-up endpoints** from bot attacks
- Configurable in appsettings.json

---

## One-Time Token System

The project implements a **secure one-time token system** with automatic expiration and invalidation.

### How It Works

#### Token Request Tracking

Each token type has a corresponding `RequestedOn` timestamp in the [`User`](/src/Server/Boilerplate.Server.Api/Models/Identity/User.cs) entity:

```csharp
public partial class User : IdentityUser<Guid>
{
    /// <summary>
    /// The date and time of the last token request. 
    /// Ensures only the latest generated token is valid and can only be used once.
    /// </summary>
    public DateTimeOffset? EmailTokenRequestedOn { get; set; }
    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }
    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }
    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }
    public DateTimeOffset? OtpRequestedOn { get; set; }
    public DateTimeOffset? ElevatedAccessTokenRequestedOn { get; set; }
}
```

#### Token Generation

When a token is generated, the `RequestedOn` timestamp is set to the **current time** and **embedded in the token purpose string**.

From [`IdentityController.EmailConfirmation.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.EmailConfirmation.cs):

```csharp
private async Task SendConfirmEmailToken(User user, string? returnUrl, 
    CancellationToken cancellationToken)
{
    // Set the request timestamp
    user.EmailTokenRequestedOn = DateTimeOffset.Now;
    var result = await userManager.UpdateAsync(user);

    if (result.Succeeded is false)
        throw new ResourceValidationException(result.Errors
            .Select(e => new LocalizedString(e.Code, e.Description)).ToArray())
            .WithData("UserId", user.Id);

    var email = user.Email!;
    
    // Generate token with embedded timestamp
    var token = await userManager.GenerateUserTokenAsync(
        user, 
        TokenOptions.DefaultPhoneProvider, 
        FormattableString.Invariant($"VerifyEmail:{email},{user.EmailTokenRequestedOn?.ToUniversalTime()}")
    );
    
    var link = new Uri(HttpContext.Request.GetWebAppUrl(), 
        $"{PageUrls.Confirm}?email={Uri.EscapeDataString(email)}&emailToken={Uri.EscapeDataString(token)}&culture={CultureInfo.CurrentUICulture.Name}");

    await emailService.SendEmailToken(user, email, token, link, cancellationToken);
}
```

#### Token Validation

When validating a token, the system checks:

1. **Expiration**: Has the token lifetime elapsed since `RequestedOn`?
2. **One-Time Use**: Does the token match the **latest** `RequestedOn` timestamp?
3. **Invalidation**: Is the `RequestedOn` timestamp set to `null` (invalidated)?

From [`IdentityController.EmailConfirmation.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.EmailConfirmation.cs):

```csharp
[HttpPost, Produces<TokenResponseDto>()]
public async Task ConfirmEmail(ConfirmEmailRequestDto request, CancellationToken cancellationToken)
{
    var user = await userManager.FindByEmailAsync(request.Email!)
        ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)])
            .WithData("Email", request.Email);

    // Check if token has expired
    var expired = (DateTimeOffset.Now - user.EmailTokenRequestedOn) 
        > AppSettings.Identity.EmailTokenLifetime;

    if (expired)
        throw new BadRequestException(nameof(AppStrings.ExpiredToken))
            .WithData("UserId", user.Id);

    // Verify the token with embedded timestamp
    var tokenIsValid = await userManager.VerifyUserTokenAsync(
        user, 
        TokenOptions.DefaultPhoneProvider, 
        FormattableString.Invariant($"VerifyEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"), 
        request.Token!
    );

    if (tokenIsValid is false)
    {
        await userManager.AccessFailedAsync(user);
        throw new BadRequestException(nameof(AppStrings.InvalidToken))
            .WithData("UserId", user.Id);
    }

    // Confirm the email
    await userEmailStore.SetEmailConfirmedAsync(user, true, cancellationToken);
    await userManager.UpdateAsync(user);

    // Invalidate the token by setting RequestedOn to null
    user.EmailTokenRequestedOn = null;
    var updateResult = await userManager.UpdateAsync(user);
    if (updateResult.Succeeded is false)
        throw new ResourceValidationException(updateResult.Errors
            .Select(e => new LocalizedString(e.Code, e.Description)).ToArray())
            .WithData("UserId", user.Id);

    // Auto sign-in after confirmation
    var token = await userManager.GenerateUserTokenAsync(user, 
        TokenOptions.DefaultPhoneProvider, 
        FormattableString.Invariant($"Otp_Email,{user.OtpRequestedOn?.ToUniversalTime()}"));

    await SignIn(new() { Email = request.Email, Otp = token }, cancellationToken);
}
```

### Key Benefits

✅ Only the **most recently requested token** is valid  
✅ Tokens are **automatically invalidated** after successful use  
✅ Tokens **expire** after their configured lifetime  
✅ If a user requests a new token, **all previous tokens become invalid**  
✅ User can't ask for a token frequently

### Example Scenario

Let's walk through a password reset scenario:

1. **10:00 AM**: User requests a password reset
   - Token A is generated
   - `ResetPasswordTokenRequestedOn = 10:00`

2. **10:05 AM**: User requests another password reset (maybe they didn't receive the email)
   - Token B is generated
   - `ResetPasswordTokenRequestedOn = 10:05`

3. **Token A is now invalid**
   - Token A was generated at 10:00
   - But the current `ResetPasswordTokenRequestedOn` is 10:05
   - Token A will fail validation

4. **User uses Token B successfully**
   - Token B is validated
   - Password is reset
   - `ResetPasswordTokenRequestedOn` is set to `null`

5. **Token B can never be used again**
   - Even if someone intercepts Token B, it's already invalidated

---

## Advanced Topics

### JWT Token Signing with PFX Certificates

By default, the Bit Boilerplate uses a string-based secret (`JwtIssuerSigningKeySecret`) for signing JWT tokens in the [`AppJwtSecureDataFormat`](/src/Server/Boilerplate.Server.Api/Services/Identity/AppJwtSecureDataFormat.cs) class. While this approach is valid and secure, using a **PFX certificate** is considered best practice for production environments, especially when:

- You need to share JWT validation across multiple backend services
- You want to follow industry-standard cryptographic practices
- You're deploying to enterprise environments with strict security requirements

**Why We Didn't Use PFX by Default**

We chose the string-based secret as the default because:
- **Easier Deployment**: PFX certificates require additional configuration on shared hosting providers
- **Simplified Development**: Developers can get started immediately without certificate management
- **Good Security**: String-based secrets with HS512 are still cryptographically secure

**How to Migrate to PFX Certificates**

If you want to use PFX certificates, you'll need to modify [`AppJwtSecureDataFormat`](/src/Server/Boilerplate.Server.Api/Services/Identity/AppJwtSecureDataFormat.cs) to use `AsymmetricSecurityKey` instead of `SymmetricSecurityKey`:

```csharp
// Instead of:
IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Identity.JwtIssuerSigningKeySecret))

// Use:
var certificate = new X509Certificate2("path/to/certificate.pfx", "password");
IssuerSigningKey = new X509SecurityKey(certificate)
```

**Protecting ASP.NET Core Data Protection Keys**

Additionally, you should protect the Data Protection keys stored in the database. In [`Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs), update the following code:

```csharp
services.AddDataProtection()
   .PersistKeysToDbContext<AppDbContext>()
   .ProtectKeysWithCertificate(certificate); // Add this line
```

**Cross-Service JWT Validation**

When using PFX certificates, you can share the **public key** with other backend services to validate JWTs issued by your ASP.NET Core Identity system. Other services can use the `AddJwtAuthentication` method to validate tokens without needing the private key.

This enables scenarios where:
- Multiple microservices validate the same JWT
- Third-party services can verify your tokens

---

### Keycloak Integration

Bit Boilerplate includes built-in support for **Keycloak**, a free, open-source identity and access management solution. Keycloak provides enterprise-grade features like:

- Centralized user management
- Single Sign-On (SSO) across multiple applications
- Fine-grained authorization
- User federation (LDAP, Active Directory)

#### Keycloak in .NET Aspire

When you run the project with .NET Aspire enabled (default configuration), Keycloak is automatically started as a containerized service. This provides a complete identity server for development and testing without any manual setup.

#### Demo User Accounts

The Keycloak instance comes pre-configured with the following demo accounts (Provided by [src\Server\Boilerplate.Server.AppHost\Realms\dev-realm.json](..\src\Server\Boilerplate.Server.AppHost\Realms\dev-realm.json)):

| Username | Password | Role | Description |
|----------|----------|------|-------------|
| test | 123456 | Admin | Full administrative access |
| bob | bob | Demo | Standard demo user |
| alice | alice | Demo | Standard demo user |

#### How Keycloak Mapping Works

The Boilerplate template integrates Keycloak with ASP.NET Core Identity through a custom mapping system in [`AppUserClaimsPrincipalFactory`](/src/Server/Boilerplate.Server.Api/Services/Identity/AppUserClaimsPrincipalFactory.cs):

**1. Groups → Roles**
- Keycloak **groups** are mapped to ASP.NET Core Identity **roles**
- Users inherit all roles from their group memberships

**2. Attributes → Claims**
- **User attributes** in Keycloak become individual claims
- **Group attributes** are also mapped to claims

**3. Keycloak Roles → Custom Mapping** (Not required with the current project setup)
- Keycloak's built-in **roles** (distinct from groups) are **not automatically mapped**
- These roles follow a different structure than ASP.NET Core Identity roles

#### Real-Time Claim Synchronization

The `AppUserClaimsPrincipalFactory` retrieves the latest claims from Keycloak on every ASP.NET Core Identity token refresh:

**Security Benefits:**
- **Automatic Deactivation**: If a user is disabled or deleted in Keycloak, access is immediately revoked
- **Fresh Claims**: Every token refresh fetches the latest permissions from Keycloak
- **Session Validation**: Expired or revoked Keycloak sessions trigger `UnauthorizedException`

#### JWT Token Issuance Flow

Despite using Keycloak for authentication, the final JWT tokens are still issued by **ASP.NET Core Identity**:

1. User signs in through Keycloak (via OpenID Connect)
2. `AppUserClaimsPrincipalFactory` retrieves claims from Keycloak
3. ASP.NET Core Identity merges Keycloak claims with local claims
4. A new JWT is issued and signed using the configured secret (or PFX certificate)
5. The JWT is sent to the client and used for all subsequent API requests

**Validation:**
- When the JWT is sent back to the backend, **ASP.NET Core Identity validates it**

#### Using JWTs with Other Backend Services

If you want to share the JWT with other backend services (e.g., microservices), follow these steps:

1. **Switch to PFX Certificates** (as described in the previous section)
2. **Share the Public Key** with other services
3. Other services use `AddJwtAuthentication` to validate the token

---

## Hands-On Exploration

**Run the Project**: To fully understand the identity features, run the project and test the following:

### 1. Sign Up
- Create a new account with email or phone number
- Test the email/phone confirmation flow
- Try signing up with an existing email/phone (should fail)

### 2. Sign In
- Test **password-based authentication**
- Test **OTP authentication** (magic link)
- Test **account lockout** after multiple failed attempts

### 3. Two-Factor Authentication (2FA)
- Enable 2FA in Settings using an authenticator app
- Test the **two-step sign-in** process
- Try entering incorrect 2FA codes
- Test **recovery codes**

### 4. Session Management
- Sign in from multiple devices/browsers
- View all active sessions in the **Settings page**
- **Revoke specific sessions** and observe forced re-authentication

### 5. Password Reset
- Test the **forgot password** flow
- Request a reset token via email/phone
- Try using an expired token
- Try using an already-used token

### 6. External Providers
- Try **External sign-in** with the configured demo provider
- Observe how email confirmation works with external providers

### 7. Permissions and Policies
- Create a user with different roles
- Test access to pages with **PRIVILEGED_ACCESS** policy
- Test feature-based policies (e.g., `ManageUsers`, `Dashboard`)

### 8. Privileged Session Limiting
- Sign in from more than 3 devices
- Observe which sessions become "privileged"
- Test accessing pages with `PRIVILEGED_ACCESS` policy from non-privileged sessions

---

## Video Tutorial

**Highly Recommended**: To see all these features in action and understand the complete identity system, watch this **15-minute video tutorial**:

### 📺 [Watch: Comprehensive Identity System Walkthrough](https://youtu.be/0ySKLPJm-fw)

**This video demonstrates**:
- User registration and email/phone confirmation
- Password and OTP-based authentication
- Two-factor authentication setup and usage
- Session management and revocation
- Password reset flow
- Role and permission management
- External provider integration
- Privileged session limiting
- Elevated access for sensitive operations

---

### AI Wiki: Answered Questions
* [How does a `refresh token` function in a Boilerplate project template?](https://deepwiki.com/search/how-does-a-refresh-token-funct_6a75fa66-ab98-4367-bd1a-24b081fbf88c)
* [What would happen when I use [AuthorizedApi]](https://deepwiki.com/search/what-would-happen-when-i-use-a_c525d59d-5c55-489b-8f95-69f6df7c743d)
* [Give me high level overview of two factor auth setup and usage flows](https://deepwiki.com/search/give-me-high-level-overview-of_1883503f-2e34-41ca-821a-1246d332990f)

Ask your own question [here](https://wiki.bitplatform.dev)

---
