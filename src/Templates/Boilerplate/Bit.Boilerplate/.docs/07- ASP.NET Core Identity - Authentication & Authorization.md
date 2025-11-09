# Stage 7: ASP.NET Core Identity and Authentication

In this stage, we will explore the comprehensive authentication and authorization system built into the Boilerplate project.

## Authentication Architecture

### JWT Token-Based Authentication

The project uses **JWT (JSON Web Tokens)** for authentication:

- **Access Tokens**: Short-lived tokens (5 minutes by default) included in every API request via the `Authorization: Bearer <token>` header
- **Refresh Tokens**: Long-lived tokens (14 days by default) stored securely on the client to obtain new access tokens
- **Token Generation**: Uses ASP.NET Core Identity's built-in bearer token authentication with HS512 algorithm
- **Security**: Tokens are signed using a secret key configured in [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

### Session Management

**Server-Side Session Storage**: User sessions are persisted in the database through the [`UserSession`](/src/Server/Boilerplate.Server.Api/Models/Identity/UserSession.cs) entity:

- Each active device/browser has its own `UserSession` record
- Sessions track IP address, device info, platform type, app version, and culture
- Sessions include SignalR connection IDs for real-time communication
- Users can view and manage all their active sessions from the Settings page

The session ID is embedded in both access and refresh tokens as a claim (`AppClaimTypes.SESSION_ID`). When a user revokes a session, its associated refresh token becomes invalid, forcing re-authentication on that device.

### Single Sign-On (SSO) Support

**External Identity Providers**: The project supports integration with external authentication providers.

**Duende Identity Server Demo**: By default, the project is configured to connect to a demo Duende Identity Server 8 instance:
- This demonstrates how to integrate external OpenID Connect providers
- Configuration is in [`IdentityController.SocialSignIn.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.SocialSignIn.cs)

**Supported Providers**: You can easily configure SSO with:
- **Google** - OAuth 2.0 authentication
- **GitHub** - OAuth authentication for developers
- **Microsoft/Azure AD** - Enterprise identity integration
- **Twitter** - Social authentication
- **Apple** - Sign in with Apple
- **Facebook** - Social media authentication
- **Duende Identity Server** - OpenID Connect provider
- And many other OAuth/OpenID Connect providers

**Configuration**: External provider settings are configured in [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) under the `Authentication` section.

## Authorization and Access Control

### Role-Based and Permission-Based Authorization

**Users and Roles**: The project includes complete user and role management:
- ASP.NET Core Identity's built-in `User` and `Role` entities
- Users can be assigned to multiple roles
- Managed through the Users and Roles pages in the Admin Panel

**Permissions**: Fine-grained permission system using **claims**:
- **User Claims**: Permissions assigned directly to individual users
- **Role Claims**: Permissions assigned to roles (inherited by all users in that role)
- Claims are added to the JWT token payload for efficient authorization checks

### Policy-Based Authorization

**Custom Policies**: The project defines authorization policies in the [`ISharedServiceCollectionExtensions.cs`](/src/Shared/Extensions/ISharedServiceCollectionExtensions.cs) file's `AddAuthorizationCore` method:

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

**Built-in Authorization Policies** defined in [`AuthPolicies.cs`](/src/Shared/Services/AuthPolicies.cs):

1. **`TFA_ENABLED`**: Requires the user to have two-factor authentication enabled
2. **`PRIVILEGED_ACCESS`**: Limits concurrent sessions per user (default: 3 sessions)
   - Prevents resource abuse by limiting active sessions
   - Applied to high-value pages like Dashboard, Products, Categories
   - Users can manage sessions from the Settings page
3. **`ELEVATED_ACCESS`**: Requires recent re-authentication for sensitive operations
   - Used for dangerous actions like account deletion
   - Activated by entering a 6-digit code or during initial sign-in with 2FA enabled
   - Time-limited elevation (expires after a short period)

**Feature-Based Policies** defined in [`AppFeatures.cs`](/src/Shared/Services/AppFeatures.cs):

The project automatically creates policies for all features defined in `AppFeatures`:

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

**Policy Examples from Project**:

Pages using multiple authorization policies:

```csharp
// TodoPage.razor
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.Todo.ManageTodo)]

// UsersPage.razor
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.Management.ManageUsers)]

// DashboardPage.razor
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.AdminPanel.Dashboard)]
```

All policies must be satisfied for the user to access the page.

### Custom Claim Types

**System Claim Types** defined in [`AppClaimTypes.cs`](/src/Shared/Services/AppClaimTypes.cs):

These claims are automatically added by the system and should not be manually added to the database:

```csharp
public class AppClaimTypes
{
    public const string SESSION_ID = "s-id";
    public const string PRIVILEGED_SESSION = "p-s";
    public const string MAX_PRIVILEGED_SESSIONS = "mx-p-s";
    public const string ELEVATED_SESSION = "e-s";
    public const string FEATURES = "feat";
}
```

- **`SESSION_ID`**: Unique identifier for the current user session
- **`PRIVILEGED_SESSION`**: Indicates if this session counts toward the privileged session limit
- **`MAX_PRIVILEGED_SESSIONS`**: Maximum allowed privileged sessions for this user
- **`ELEVATED_SESSION`**: Indicates the user has recently authenticated for sensitive operations
- **`FEATURES`**: Contains the list of features/permissions granted to the user

## Identity Configuration

### IdentitySettings in appsettings.json

**Location**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

**Configuration Options**: Key identity settings that can be customized:

```json
"Identity": {
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

- **Token Expiration**: Controls how long various tokens remain valid
- **Password Requirements**: Configures password complexity rules
- **Account Confirmation**: Determines if email/phone confirmation is required

## Security Best Practices

**Built-in Security**: The identity system follows security best practices:

- **Password Hashing**: Uses PBKDF2 with HMAC-SHA512, 100,000 iterations
- **Concurrency Stamps**: Prevents concurrent modification conflicts
- **Security Stamps**: Invalidates all tokens when changed (e.g., after 2FA configuration)
- **Account Lockout**: Protects against brute-force attacks with exponential backoff
- **One-Time Tokens**: All tokens can only be used once and expire after use
- **Token Expiration**: Short-lived tokens minimize the window for token theft

## One-Time Token System

The project implements a secure one-time token system with automatic expiration and invalidation.

### How It Works

**Token Request Tracking**: Each token type has a corresponding `RequestedOn` timestamp in the [`User`](/src/Server/Boilerplate.Server.Api/Models/Identity/User.cs) entity:

```csharp
public partial class User : IdentityUser<Guid>
{
    public DateTimeOffset? EmailTokenRequestedOn { get; set; }
    public DateTimeOffset? PhoneNumberTokenRequestedOn { get; set; }
    public DateTimeOffset? ResetPasswordTokenRequestedOn { get; set; }
    public DateTimeOffset? TwoFactorTokenRequestedOn { get; set; }
    public DateTimeOffset? OtpRequestedOn { get; set; }
    public DateTimeOffset? ElevatedAccessTokenRequestedOn { get; set; }
}
```

**Token Generation**: When a token is generated, the `RequestedOn` timestamp is set to the current time and embedded in the token purpose string:

```csharp
// From IdentityController.EmailConfirmation.cs
user.EmailTokenRequestedOn = DateTimeOffset.Now;
await userManager.UpdateAsync(user);

var token = await userManager.GenerateUserTokenAsync(
    user, 
    TokenOptions.DefaultPhoneProvider, 
    FormattableString.Invariant($"VerifyEmail:{email},{user.EmailTokenRequestedOn?.ToUniversalTime()}")
);
```

**Token Validation**: When validating a token, the system checks:

1. **Expiration**: Has the token lifetime elapsed since `RequestedOn`?
2. **One-Time Use**: Does the token match the latest `RequestedOn` timestamp?
3. **Invalidation**: Is the `RequestedOn` timestamp set to `null` (invalidated)?

```csharp
// From IdentityController.EmailConfirmation.cs
var expired = (DateTimeOffset.Now - user.EmailTokenRequestedOn) > AppSettings.Identity.EmailTokenLifetime;

if (expired)
    throw new BadRequestException(nameof(AppStrings.ExpiredToken));

var tokenIsValid = await userManager.VerifyUserTokenAsync(
    user, 
    TokenOptions.DefaultPhoneProvider, 
    FormattableString.Invariant($"VerifyEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"), 
    request.Token!
);

if (tokenIsValid)
{
    user.EmailTokenRequestedOn = null; // Invalidates the token
    await userManager.UpdateAsync(user);
}
```

**Key Benefits**:
- Only the **most recently requested token** is valid
- Tokens are **automatically invalidated** after successful use
- Tokens **expire** after their configured lifetime
- If a user requests a new token, **all previous tokens become invalid**

**Example Scenario**:
1. User requests a password reset at 10:00 AM → Token A is generated with `ResetPasswordTokenRequestedOn = 10:00`
2. User requests another password reset at 10:05 AM → Token B is generated with `ResetPasswordTokenRequestedOn = 10:05`
3. Token A is now invalid because it was generated at 10:00, but the current `ResetPasswordTokenRequestedOn` is 10:05
4. User uses Token B successfully → `ResetPasswordTokenRequestedOn` is set to `null`, invalidating Token B
5. Token B can never be used again

## Code Examples

### Controller Method with Authorization

From [`IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs):

```csharp
[HttpPost]
public async Task SignUp(SignUpRequestDto request, CancellationToken cancellationToken)
{
    // Validate Google reCAPTCHA
    if (await googleRecaptchaService.Verify(request.GoogleRecaptchaResponse, cancellationToken) is false)
        throw new BadRequestException(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaResponse)]);

    // Check for existing user
    var existingUser = await userManager.FindUserAsync(new() { Email = request.Email, PhoneNumber = request.PhoneNumber });
    if (existingUser is not null)
    {
        if (await userConfirmation.IsConfirmedAsync(userManager, existingUser) is false)
        {
            await SendConfirmationToken(existingUser, request.ReturnUrl, cancellationToken);
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]).WithData("UserId", existingUser.Id);
        }
        else
        {
            throw new BadRequestException(Localizer[nameof(AppStrings.DuplicateEmailOrPhoneNumber)]).WithData("UserId", existingUser.Id);
        }
    }

    // Create new user
    var userToAdd = new User { LockoutEnabled = true };
    await userStore.SetUserNameAsync(userToAdd, request.UserName!, cancellationToken);
    
    if (string.IsNullOrEmpty(request.Email) is false)
    {
        await userEmailStore.SetEmailAsync(userToAdd, request.Email!, cancellationToken);
    }
    
    await userManager.CreateUserWithDemoRole(userToAdd, request.Password!);
    await SendConfirmationToken(userToAdd, request.ReturnUrl, cancellationToken);
}
```

### Session Creation

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

### Privileged Session Logic

From [`IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs):

```csharp
private async Task UpdateUserSessionPrivilegeStatus(UserSession userSession, CancellationToken cancellationToken)
{
    var userId = userSession.UserId;

    var maxPrivilegedSessionsClaimValues = await userClaimsService.GetClaimValues<int?>(userId, AppClaimTypes.MAX_PRIVILEGED_SESSIONS, cancellationToken);

    var hasUnlimitedPrivilegedSessions = maxPrivilegedSessionsClaimValues.Any(v => v == -1); // -1 means no limit

    var maxPrivilegedSessionsCount = hasUnlimitedPrivilegedSessions ? -1 : maxPrivilegedSessionsClaimValues.Max() ?? AppSettings.Identity.MaxPrivilegedSessionsCount;

    var isPrivileged = hasUnlimitedPrivilegedSessions ||
        userSession.Privileged is true ||
        await DbContext.UserSessions.CountAsync(us => us.UserId == userSession.UserId && us.Privileged == true, cancellationToken) < maxPrivilegedSessionsCount;

    userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.PRIVILEGED_SESSION, isPrivileged ? "true" : "false"));
    userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.MAX_PRIVILEGED_SESSIONS, maxPrivilegedSessionsCount.ToString(CultureInfo.InvariantCulture)));

    userSession.Privileged = isPrivileged;
}
```

## Hands-On Exploration

**Run the Project**: Explore the identity features by running the project and testing:

1. **Sign Up**: Create a new account with email/phone confirmation
2. **Sign In**: Test password-based and OTP authentication
3. **Two-Factor Authentication**: Enable 2FA in Settings and test the two-step sign-in
4. **Session Management**: View active sessions in Settings and revoke specific sessions
5. **Password Reset**: Test the forgot password flow
6. **External Providers**: Try social sign-in with the configured providers
7. **Permissions**: Test access to pages with different user roles and permissions

## Video Tutorial

**Highly Recommended**: To fully understand the identity features and see them in action, watch this **15-minute video tutorial**:

[**Watch: Comprehensive Identity System Walkthrough**](https://youtu.be/0ySKLPJm-fw)

This video demonstrates:
- User registration and email/phone confirmation
- Password and OTP-based authentication
- Two-factor authentication setup and usage
- Session management and revocation
- Password reset flow
- Role and permission management
- External provider integration

## Summary

The Boilerplate project provides a **production-ready authentication and authorization system** with:

✅ **JWT-based authentication** with access and refresh tokens  
✅ **Server-side session management** with device tracking  
✅ **Single Sign-On (SSO)** support for multiple providers  
✅ **Role-based and permission-based authorization**  
✅ **Policy-based authorization** with custom policies  
✅ **Feature flags** for fine-grained access control  
✅ **Two-factor authentication** with multiple methods  
✅ **Account lockout** protection against brute-force attacks  
✅ **One-time tokens** with automatic expiration and invalidation  
✅ **Privileged session limiting** to prevent resource abuse  
✅ **Elevated access** for sensitive operations  
✅ **Comprehensive security** following industry best practices  

This system is designed to handle enterprise-level authentication and authorization requirements while remaining flexible and extensible for custom business logic.

---
