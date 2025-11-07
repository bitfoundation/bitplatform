# Stage 7: ASP.NET Core Identity and Authentication

Welcome to Stage 7! In this stage, you'll learn about the comprehensive authentication and authorization system built into this project. This includes JWT token-based authentication, session management, Single Sign-On (SSO), role-based and permission-based authorization, and various security features.

---

## 1. Authentication Architecture

### 1.1 JWT Token-Based Authentication

The project uses **JSON Web Tokens (JWT)** for authentication, providing a stateless, secure, and scalable authentication mechanism.

#### How It Works:
- When a user signs in successfully, the server generates two tokens:
  - **Access Token (Bearer Token)**: Short-lived JWT token (default: 5 minutes) used to authenticate API requests
  - **Refresh Token**: Long-lived token (default: 14 days) used to obtain new access tokens without re-authentication
- These tokens are sent to the client and included in subsequent requests via the `Authorization` header
- The server validates the JWT signature and claims on each request to authenticate the user

#### Key Files:
- **Token Generation**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs)
- **Token Validation**: Handled by ASP.NET Core Identity's Bearer Token authentication scheme

#### Configuration in appsettings.json:
Located at [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):

```json
"Identity": {
    "JwtIssuerSigningKeySecret": "VeryLongJWTIssuerSiginingKeySecretThatIsMoreThan64BytesToEnsureCompatibilityWithHS512Algorithm",
    "Issuer": "Boilerplate",
    "Audience": "Boilerplate",
    "BearerTokenExpiration": "0.00:05:00",
    "RefreshTokenExpiration": "14.00:00:00"
}
```

**⚠️ Security Note**: In production, change the `JwtIssuerSigningKeySecret` to a strong, randomly generated secret and store it securely (e.g., using environment variables or Azure Key Vault).

---

### 1.2 Session Management

The project implements **server-side session tracking** that persists user sessions in the database, providing enhanced security and session management capabilities.

#### UserSession Model:
Located at [`/src/Server/Boilerplate.Server.Api/Models/Identity/UserSession.cs`](/src/Server/Boilerplate.Server.Api/Models/Identity/UserSession.cs):

```csharp
public partial class UserSession
{
    public Guid Id { get; set; }
    public string? IP { get; set; }
    public string? Address { get; set; }
    public bool Privileged { get; set; }
    public long StartedOn { get; set; }
    public long? RenewedOn { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? DeviceInfo { get; set; }
    public AppPlatformType? PlatformType { get; set; }
    public string? CultureName { get; set; }
    public string? AppVersion { get; set; }
    // ... additional properties
}
```

#### How Session Management Works:
1. **Session Creation**: When a user signs in, a new `UserSession` record is created in the database with a unique session ID
2. **Session ID in Tokens**: The session ID is embedded as a claim in both access and refresh tokens
3. **Session Tracking**: Every API request includes the session ID, allowing the server to track active sessions
4. **Session Revocation**: Sessions can be revoked by deleting them from the database (e.g., "Sign out from all devices")
5. **Session Monitoring**: Users can view and manage their active sessions from the settings page

#### Benefits:
- **Instant Token Revocation**: Unlike pure JWT authentication, sessions can be invalidated server-side immediately
- **Security Monitoring**: Track login locations, devices, and suspicious activities
- **Concurrent Session Limits**: Enforce maximum active session counts per user
- **Device Management**: Users can see and revoke sessions from specific devices

#### Session Creation Code Example:
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
        Address = $"{Request.Headers["cf-ipcountry"]}, {Request.Headers["cf-ipcity"]}"
    };

    await UpdateUserSessionPrivilegeStatus(userSession, cancellationToken);

    return userSession;
}
```

---

## 2. Single Sign-On (SSO) Support

The project supports integration with **external identity providers** (OAuth/OpenID Connect), allowing users to sign in using their existing accounts from popular platforms.

### 2.1 Demo Configuration: Duende Identity Server

By default, the project is configured to connect to a **Duende Identity Server 8 demo instance** for testing SSO functionality without requiring any setup:

```csharp
// From Program.Services.cs
authenticationBuilder.AddOpenIdConnect("IdentityServerDemo", options =>
{
    options.Authority = "https://demo.duendesoftware.com";
    options.ClientId = "interactive.confidential";
    options.ClientSecret = "secret";
    options.ResponseType = "code";
    // ... additional configuration
});
```

You can test this immediately by running the project and clicking the "Identity Server Demo" button on the sign-in page.

### 2.2 Supported External Providers

The project includes built-in support for the following external authentication providers:

#### **Google**
- Configuration location: [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) → `Authentication:Google`
- Setup guide: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins

```json
"Authentication": {
    "Google": {
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
    }
}
```

#### **GitHub**
- Configuration location: [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) → `Authentication:GitHub`
- Setup guide: https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/blob/dev/docs/github.md

#### **Twitter (X)**
- Configuration location: [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) → `Authentication:Twitter`
- Setup guide: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/twitter-logins

#### **Apple**
- Configuration location: [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) → `Authentication:Apple`
- Setup guide: https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/blob/dev/docs/sign-in-with-apple.md

#### **Facebook**
- Configuration location: [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) → `Authentication:Facebook`
- Setup guide: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/facebook-logins

#### **Azure AD / Azure AD B2C / Microsoft Entra**
- Configuration location: [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) → `Authentication:AzureAD`

### 2.3 How External Authentication Works:
1. User clicks on an external provider button (e.g., "Sign in with Google")
2. User is redirected to the provider's login page
3. After successful authentication, the provider redirects back to the app with an authorization code
4. The server exchanges the code for user information
5. A local user account is created or linked (if it doesn't exist)
6. The user is signed in with a JWT token

---

## 3. Authorization and Access Control

### 3.1 Users and Roles

The project uses **ASP.NET Core Identity** for user and role management with a many-to-many relationship.

#### User Model:
Located at [`/src/Server/Boilerplate.Server.Api/Models/Identity/User.cs`](/src/Server/Boilerplate.Server.Api/Models/Identity/User.cs):

```csharp
public partial class User : IdentityUser<Guid>
{
    [PersonalData]
    public string? FullName { get; set; }
    
    public string? DisplayName => FullName ?? DisplayUserName;
    
    [PersonalData]
    public Gender? Gender { get; set; }
    
    [PersonalData]
    public DateTimeOffset? BirthDate { get; set; }
    
    public List<UserSession> Sessions { get; set; } = [];
    public List<UserRole> Roles { get; set; } = [];
    public List<UserClaim> Claims { get; set; } = [];
    // ... additional properties
}
```

#### Role Model:
Located at [`/src/Server/Boilerplate.Server.Api/Models/Identity/Role.cs`](/src/Server/Boilerplate.Server.Api/Models/Identity/Role.cs):

```csharp
public partial class Role : IdentityRole<Guid>
{
    public List<UserRole> Users { get; set; } = [];
    public List<RoleClaim> Claims { get; set; } = [];
}
```

#### Role-Based Authorization Example:
```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteUser(Guid id)
{
    // Only users with the "Admin" role can access this
}
```

---

### 3.2 Permission-Based Authorization with Features

The project implements a **fine-grained permission system** using the `AppFeatures` class and claims-based authorization.

#### AppFeatures Definition:
Located at [`/src/Shared/Services/AppFeatures.cs`](/src/Shared/Services/AppFeatures.cs):

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

#### How It Works:
- Each feature is represented by a unique string value (e.g., `"1.0"`, `"1.1"`)
- Features are stored as claims on the user's principal with the claim type `AppClaimTypes.FEATURES`
- When a feature is required, the authorization system checks if the user has the corresponding claim value
- Roles can have features assigned to them via `RoleClaim` records

#### Feature-Based Authorization Example:
```csharp
[Authorize(Policy = AppFeatures.Management.ManageUsers)]
public async Task<IActionResult> GetUsers()
{
    // Only users with the "ManageUsers" feature can access this
}
```

---

### 3.3 Policy-Based Authorization

The project defines custom authorization policies using ASP.NET Core's policy-based authorization system.

#### Authorization Policies Definition:
Located at [`/src/Shared/Services/AuthPolicies.cs`](/src/Shared/Services/AuthPolicies.cs):

```csharp
public class AuthPolicies
{
    /// <summary>
    /// Having this policy means the user has enabled 2 factor authentication.
    /// </summary>
    public const string TFA_ENABLED = nameof(TFA_ENABLED);

    /// <summary>
    /// By default, each user is limited to 3 active sessions.
    /// This policy can be disabled or configured to adjust the session limit dynamically, 
    /// such as by reading from application settings, the user's subscription plan, or other criteria.
    /// Currently, this policy applies only to the Todo and AdminPanel specific pages like dashboard page. 
    /// However, it can be extended to cover additional pages as needed. 
    /// 
    /// Important: Do not apply this policy to the settings page, 
    /// as users need access to manage and revoke their sessions there.
    /// </summary>
    public const string PRIVILEGED_ACCESS = nameof(PRIVILEGED_ACCESS);

    /// <summary>
    /// Enables the user to execute potentially harmful operations, like account removal. 
    /// This limited-time policy is activated upon successful verification via a secure 6-digit code or
    /// during the initial minutes of a sign-in session of users with 2fa enabled.
    /// </summary>
    public const string ELEVATED_ACCESS = nameof(ELEVATED_ACCESS);
}
```

#### Policy Configuration:
Located at [`/src/Shared/Extensions/ISharedServiceCollectionExtensions.cs`](/src/Shared/Extensions/ISharedServiceCollectionExtensions.cs):

```csharp
services.AddAuthorizationCore(options =>
{
    options.AddPolicy(AuthPolicies.TFA_ENABLED, x => x.RequireClaim("amr", "mfa"));
    options.AddPolicy(AuthPolicies.PRIVILEGED_ACCESS, x => x.RequireClaim(AppClaimTypes.PRIVILEGED_SESSION, "true"));
    options.AddPolicy(AuthPolicies.ELEVATED_ACCESS, x => x.RequireClaim(AppClaimTypes.ELEVATED_SESSION, "true"));

    foreach (var feat in AppFeatures.GetAll())
    {
        options.AddPolicy(feat.Value, policy => policy.AddRequirements(new AppFeatureRequirement(FeatureName: $"{feat.Group.Name}.{feat.Name}", FeatureValue: feat.Value)));
    }
});
```

#### Policy Usage Examples:

**1. PRIVILEGED_ACCESS Policy:**
Used to limit concurrent sessions and protect resource-intensive pages:

```csharp
// In a Blazor page
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]

// In an API controller
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
public async Task<IQueryable<ProductDto>> GetProducts()
{
    return DbContext.Products.Project();
}
```

Examples from the project:
- [`/src/Client/Boilerplate.Client.Core/Components/Pages/Dashboard/DashboardPage.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Dashboard/DashboardPage.razor)
- [`/src/Server/Boilerplate.Server.Api/Controllers/Dashboard/DashboardController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Dashboard/DashboardController.cs)

**2. ELEVATED_ACCESS Policy:**
Used for sensitive operations like account deletion:

```csharp
[Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
public async Task DeleteAccount(CancellationToken cancellationToken)
{
    // User must have recently verified with 2FA code to access this
}
```

**3. TFA_ENABLED Policy:**
Used to require two-factor authentication for specific features:

```csharp
[Authorize(Policy = AuthPolicies.TFA_ENABLED)]
public async Task AccessSensitiveData()
{
    // Only users with 2FA enabled can access this
}
```

---

## 4. Identity Configuration

### 4.1 IdentitySettings in appsettings.json

Located at [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):

```json
"Identity": {
    "JwtIssuerSigningKeySecret": "VeryLongJWTIssuerSiginingKeySecretThatIsMoreThan64BytesToEnsureCompatibilityWithHS512Algorithm",
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

#### Key Configuration Options:

**Token Lifetimes:**
- `BearerTokenExpiration`: How long the JWT access token is valid (format: `D.HH:mm:ss`)
- `RefreshTokenExpiration`: How long the refresh token is valid
- `EmailTokenLifetime`: Lifetime for email confirmation tokens
- `PhoneNumberTokenLifetime`: Lifetime for phone number confirmation tokens
- `ResetPasswordTokenLifetime`: Lifetime for password reset tokens
- `TwoFactorTokenLifetime`: Lifetime for 2FA verification codes
- `OtpTokenLifetime`: Lifetime for one-time password (OTP) codes

**Password Requirements:**
Configure password complexity requirements (all set to lenient defaults for easier development):
- `RequireDigit`: Require at least one digit (0-9)
- `RequiredLength`: Minimum password length
- `RequireNonAlphanumeric`: Require at least one special character
- `RequireUppercase`: Require at least one uppercase letter
- `RequireLowercase`: Require at least one lowercase letter

**Sign-In Options:**
- `RequireConfirmedAccount`: Whether users must confirm their email/phone before signing in

**Session Management:**
- `MaxPrivilegedSessionsCount`: Maximum number of concurrent privileged sessions per user (default: 3)
  - Set to `-1` for unlimited sessions
  - Can be overridden per-user via claims for subscription-based features

**⚠️ Production Recommendations:**
- Strengthen password requirements for production
- Use environment variables for sensitive values like `JwtIssuerSigningKeySecret`
- Adjust token lifetimes based on your security requirements

---

## 5. One-Time Tokens

The project implements **one-time token** functionality to prevent token reuse and enhance security.

### How One-Time Tokens Work:

1. **Token Request Timestamp**: When a token is requested (e.g., email confirmation, password reset), the request timestamp is stored on the user entity:

```csharp
public partial class User : IdentityUser<Guid>
{
    public DateTimeOffset? EmailTokenRequestedOn { get; set; }
    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }
    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }
    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }
    public DateTimeOffset? OtpRequestedOn { get; set; }
    public DateTimeOffset? ElevatedAccessTokenRequestedOn { get; set; }
    // ...
}
```

2. **Token Generation**: The token is generated using the timestamp as part of the token purpose:

```csharp
var token = await userManager.GenerateUserTokenAsync(
    user, 
    TokenOptions.DefaultPhoneProvider, 
    FormattableString.Invariant($"Otp_Email,{user.OtpRequestedOn?.ToUniversalTime()}")
);
```

3. **Token Validation**: When the token is verified, the timestamp must match:
   - If a new token is requested, the timestamp changes
   - Previous tokens become invalid because their embedded timestamp no longer matches

4. **Token Invalidation After Use**: After successful verification, the timestamp is cleared:

```csharp
if (result.Succeeded is true && user.OtpRequestedOn != null)
{
    user.OtpRequestedOn = null; // invalidates the OTP
    await userManager.UpdateAsync(user);
}
```

### Benefits:
- **Only Latest Token is Valid**: If a user requests multiple tokens, only the most recent one works
- **Single Use**: Once a token is used successfully, it cannot be reused
- **Automatic Expiration**: Tokens have built-in lifetime limits
- **Security Against Token Leakage**: Even if an old token is intercepted, it won't work

### Example from IdentityController:
Check out the `SendOtp` and `TwoFactorSignIn` methods in [`IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs) to see this pattern in action.

---

## 6. Video Tutorial (Highly Recommended)

To fully understand the identity features and see them in action, watch this **15-minute video tutorial**:

**🎥 Video**: https://bitplatform.dev/templates/authentication

This video demonstrates:
- Sign-up and email/phone confirmation flow
- Sign-in with password and OTP (magic link)
- Two-factor authentication (2FA) setup and usage
- Social sign-in with external providers
- Session management and device tracking
- Password reset flow
- Elevated access for sensitive operations

---

## 7. Hands-On Exploration

The best way to learn is by exploring the running application:

**🚀 Run the Project**:
1. Navigate to `src/Server/Boilerplate.Server.Web/`
2. Run: `dotnet run`
3. Open your browser to the displayed URL (usually `http://localhost:5000`)

**🔍 Explore These Features**:
1. **Sign-Up Page**: Create a new account and confirm your email
2. **Sign-In Page**: Try signing in with password and OTP
3. **Settings → Security**: 
   - Enable two-factor authentication
   - View and manage active sessions
   - Generate recovery codes
4. **Settings → Account**:
   - Try changing your password (requires elevated access)
   - Test account deletion (requires elevated access + 2FA)
5. **External Sign-In**: Try the "Identity Server Demo" button to test SSO

---

## 8. Security Best Practices

The identity system follows security best practices:

### 8.1 Built-in Security Features:
- ✅ **JWT Token Signature Verification**: All tokens are cryptographically signed
- ✅ **Password Hashing**: Passwords are hashed using ASP.NET Core Identity's default hasher (PBKDF2)
- ✅ **Account Lockout**: Protect against brute-force attacks with automatic account lockout
- ✅ **Two-Factor Authentication**: Support for TOTP authenticator apps and SMS/email codes
- ✅ **Session Management**: Track and revoke sessions across devices
- ✅ **One-Time Tokens**: Email and phone confirmation tokens can only be used once
- ✅ **HTTPS Enforcement**: Secure communication for all authentication flows
- ✅ **Anti-CSRF Protection**: Built into ASP.NET Core for form posts
- ✅ **Secure Cookie Options**: HttpOnly, Secure, and SameSite cookies

### 8.2 Elevated Access for Sensitive Operations:
Critical operations (like account deletion) require **elevated access**:
1. User must verify their identity with a 2FA code
2. System grants temporary `ELEVATED_ACCESS` claim
3. Elevated access expires after a short period
4. This prevents unauthorized actions even if someone gains access to an authenticated session

### 8.3 Privileged Session Limits:
The `PRIVILEGED_ACCESS` policy prevents resource abuse:
- Limits concurrent sessions per user (default: 3)
- Protects expensive operations (like dashboard queries)
- Can be customized per-user via claims (useful for subscription tiers)

---

## 9. Code Examples

### 9.1 Controller Method with Authorization:
From [`/src/Server/Boilerplate.Server.Api/Controllers/Dashboard/DashboardController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Dashboard/DashboardController.cs):

```csharp
[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
public partial class DashboardController : AppControllerBase
{
    [EnableQuery]
    [HttpGet]
    public IQueryable<ProductSaleStatisticsDto> GetProductsSalesStatistics()
    {
        return DbContext.Products.Project();
    }
}
```

### 9.2 Blazor Page with Authorization:
From [`/src/Client/Boilerplate.Client.Core/Components/Pages/Dashboard/DashboardPage.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Dashboard/DashboardPage.razor):

```xml
@page "/dashboard"
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@inherits AppPageBase

<PageTitle>@Localizer[nameof(AppStrings.Dashboard)]</PageTitle>

<!-- Page content -->
```

### 9.3 Checking Authorization Programmatically:
```csharp
// In a component or service
var user = await AuthenticationStateProvider.GetAuthenticationStateAsync();
var isAuthorized = await AuthorizationService.AuthorizeAsync(
    user.User, 
    AuthPolicies.PRIVILEGED_ACCESS
);

if (isAuthorized.Succeeded)
{
    // User has privileged access
}
```

---

## 10. Summary

The project provides a **production-ready authentication and authorization system** with:

✅ **JWT token-based authentication** with access and refresh tokens  
✅ **Server-side session management** for instant revocation and tracking  
✅ **Single Sign-On (SSO)** support for Google, GitHub, Twitter, Apple, Facebook, Azure AD, and custom providers  
✅ **Role-based authorization** for coarse-grained access control  
✅ **Permission-based authorization** with `AppFeatures` for fine-grained control  
✅ **Policy-based authorization** for advanced scenarios (2FA, elevated access, session limits)  
✅ **Two-factor authentication (2FA)** with TOTP, SMS, and email  
✅ **One-time tokens** for email/phone confirmation and password reset  
✅ **Elevated access** for sensitive operations  
✅ **Privileged session limits** to prevent resource abuse  
✅ **Account lockout** to prevent brute-force attacks  
✅ **Security best practices** built-in  

This system is highly customizable and can be extended to meet your specific requirements.

---

