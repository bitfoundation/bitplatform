# Stage 24: WebAuthn and Passwordless Authentication (Advanced)

Welcome to Stage 24! In this advanced stage, you'll learn about the **WebAuthn (Web Authentication)** and **passwordless authentication** system built into the project. This feature allows users to sign in using biometric authentication (fingerprint, Face ID, Windows Hello) instead of traditional passwords.

---

## What is WebAuthn?

**WebAuthn** is a web standard published by the W3C and FIDO Alliance that enables passwordless authentication using:
- **Biometric authentication**: Fingerprint, Face ID, Touch ID
- **Hardware security keys**: YubiKey, USB authenticators
- **Platform authenticators**: Windows Hello, Android biometrics, iOS Face ID/Touch ID

### Benefits of WebAuthn:
- **Enhanced Security**: No passwords to steal or phish
- **Better UX**: Faster login with biometrics
- **Privacy**: Biometric data never leaves the device
- **Phishing-Resistant**: Credentials are bound to specific domains

---

## Architecture Overview

The WebAuthn implementation in this project consists of three main layers:

### 1. **Server-Side (Backend)**
- **Entity Model**: `WebAuthnCredential` stores credential data
- **Controllers**: Handle credential registration and verification
- **FIDO2 Library**: Uses `Fido2NetLib` for cryptographic operations

### 2. **Client-Side (Frontend)**
- **Platform-Specific Services**: Web, MAUI, Windows implementations
- **Storage Service**: Tracks configured users locally
- **UI Components**: Enable/disable passwordless, sign-in with biometrics

### 3. **Bridge Layer (Hybrid Apps)**
- **LocalHttpServer**: Provides proper origin for WebView-based platforms
- **WebInteropApp**: Lightweight HTML page for WebAuthn operations

---

## Server-Side Implementation

### Entity Model: WebAuthnCredential

The `WebAuthnCredential` entity stores all data required for FIDO2 authentication:

**Location**: [`/src/Server/Boilerplate.Server.Api/Models/Identity/WebAuthnCredential.cs`](/src/Server/Boilerplate.Server.Api/Models/Identity/WebAuthnCredential.cs)

```csharp
public class WebAuthnCredential
{
    public required byte[] Id { get; set; }
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    
    public byte[]? PublicKey { get; set; }
    public uint SignCount { get; set; }
    public AuthenticatorTransport[]? Transports { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp { get; set; }
    public byte[]? AttestationObject { get; set; }
    public byte[]? AttestationClientDataJson { get; set; }
    public byte[]? UserHandle { get; set; }
    public string? AttestationFormat { get; set; }
    public DateTimeOffset RegDate { get; set; }
    public Guid AaGuid { get; set; }
}
```

**Key Properties**:
- `Id`: The credential identifier (byte array)
- `PublicKey`: Public key for signature verification
- `SignCount`: Counter to detect cloned authenticators
- `Transports`: How the authenticator communicates (platform, USB, etc.)
- `UserHandle`: Maps credentials back to users

**Relationship**: Each user can have multiple WebAuthn credentials:

```csharp
public partial class User : IdentityUser<Guid>
{
    public List<WebAuthnCredential> WebAuthnCredentials { get; set; } = [];
}
```

### FIDO2 Configuration

The server uses **Fido2NetLib** to handle WebAuthn operations:

**Location**: [`/src/Server/Boilerplate.Server.Api/Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs)

```csharp
services.AddFido2(options => { });

services.AddScoped(sp =>
{
    var webAppUrl = sp.GetRequiredService<IHttpContextAccessor>()
        .HttpContext!.Request.GetWebAppUrl();

    var options = new Fido2Configuration
    {
        ServerDomain = webAppUrl.Host,
        TimestampDriftTolerance = 1000,
        ServerName = "Boilerplate WebAuthn",
        Origins = new HashSet<string>([webAppUrl.AbsoluteUri]),
        ServerIcon = new Uri(webAppUrl, "images/icons/bit-logo.png").ToString()
    };

    return options;
});
```

**Configuration Options**:
- `ServerDomain`: The domain hosting your application
- `Origins`: Allowed origins for WebAuthn operations
- `ServerName`: Display name shown during registration
- `TimestampDriftTolerance`: Tolerance for timestamp verification (milliseconds)

### Controller Endpoints

#### UserController - Credential Registration

**Location**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.WebAuthn.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.WebAuthn.cs)

**1. Get Credential Options** (Step 1 of Registration)

```csharp
[HttpGet]
public async Task<CredentialCreateOptions> GetWebAuthnCredentialOptions(CancellationToken cancellationToken)
{
    var userId = User.GetUserId();
    var user = await userManager.FindByIdAsync(userId.ToString());
    
    var fidoUser = new Fido2User
    {
        Id = Encoding.UTF8.GetBytes(userId.ToString()),
        Name = user.DisplayUserName,
        DisplayName = user.DisplayName,
    };

    var authenticatorSelection = new AuthenticatorSelection
    {
        RequireResidentKey = false,
        ResidentKey = ResidentKeyRequirement.Discouraged,
        UserVerification = UserVerificationRequirement.Required,
        AuthenticatorAttachment = AuthenticatorAttachment.Platform
    };

    var options = fido2.RequestNewCredential(new RequestNewCredentialParams
    {
        User = fidoUser,
        ExcludeCredentials = [],
        AuthenticatorSelection = authenticatorSelection,
        AttestationPreference = AttestationConveyancePreference.None,
    });

    // Cache options for verification in next step
    var key = GetWebAuthnCacheKey(userId);
    await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), 
        new() { SlidingExpiration = TimeSpan.FromMinutes(3) }, cancellationToken);

    return options;
}
```

**2. Create Credential** (Step 2 of Registration)

```csharp
[HttpPut]
public async Task CreateWebAuthnCredential(AuthenticatorAttestationRawResponse attestationResponse, 
    CancellationToken cancellationToken)
{
    var userId = User.GetUserId();
    
    // Retrieve cached options from step 1
    var key = GetWebAuthnCacheKey(userId);
    var cachedBytes = await cache.GetAsync(key, cancellationToken);
    var options = CredentialCreateOptions.FromJson(Encoding.UTF8.GetString(cachedBytes));

    // Verify the attestation response
    var credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
    {
        AttestationResponse = attestationResponse,
        OriginalOptions = options,
        IsCredentialIdUniqueToUserCallback = IsCredentialIdUniqueToUser
    }, cancellationToken);

    // Store the credential
    var newCredential = new WebAuthnCredential
    {
        UserId = userId,
        Id = credential.Id,
        PublicKey = credential.PublicKey,
        UserHandle = credential.User.Id,
        SignCount = credential.SignCount,
        RegDate = DateTimeOffset.UtcNow,
        AaGuid = credential.AaGuid,
        Transports = credential.Transports,
        // ... other properties
    };

    await DbContext.WebAuthnCredential.AddAsync(newCredential, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
}
```

**3. Delete Credential**

```csharp
[HttpDelete]
public async Task DeleteWebAuthnCredential(AuthenticatorAssertionRawResponse assertionResponse, 
    CancellationToken cancellationToken)
{
    var userId = User.GetUserId();
    
    var affectedRows = await DbContext.WebAuthnCredential
        .Where(c => c.Id == assertionResponse.RawId)
        .ExecuteDeleteAsync(cancellationToken);

    if (affectedRows == 0)
        throw new ResourceNotFoundException();
}
```

#### IdentityController - Authentication

**Location**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.WebAuthn.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.WebAuthn.cs)

**1. Get Assertion Options** (Start Authentication)

```csharp
[HttpPost]
public async Task<AssertionOptions> GetWebAuthnAssertionOptions(
    WebAuthnAssertionOptionsRequestDto request, CancellationToken cancellationToken)
{
    var existingKeys = new List<PublicKeyCredentialDescriptor>();

    if (request.UserIds is not null)
    {
        var existingCredentials = await DbContext.WebAuthnCredential
            .Where(c => request.UserIds.Contains(c.UserId))
            .OrderByDescending(c => c.RegDate)
            .Select(c => new { c.Id, c.Transports })
            .ToArrayAsync(cancellationToken);
            
        existingKeys.AddRange(existingCredentials.Select(c => 
            new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey, c.Id, c.Transports)));
    }

    var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
    {
        AllowedCredentials = existingKeys,
        UserVerification = UserVerificationRequirement.Required,
    });

    // Cache challenge for verification
    var key = new string([.. options.Challenge.Select(b => (char)b)]);
    await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), 
        new() { SlidingExpiration = TimeSpan.FromMinutes(3) }, cancellationToken);

    return options;
}
```

**2. Verify and Sign In** (Complete Authentication)

```csharp
[HttpPost, Produces<SignInResponseDto>()]
public async Task VerifyWebAuthAndSignIn(
    VerifyWebAuthnAndSignInRequestDto<AuthenticatorAssertionRawResponse> request, 
    CancellationToken cancellationToken)
{
    var (verifyResult, credential) = await Verify(request.ClientResponse, cancellationToken);

    var user = await userManager.FindByIdAsync(credential.UserId.ToString());

    // Generate OTP for automatic sign-in
    var (otp, _) = await GenerateAutomaticSignInLink(user, null, "WebAuthn");

    // Handle 2FA if enabled
    if (user.TwoFactorEnabled is false || request.TfaCode is not null)
    {
        credential.SignCount = verifyResult.SignCount;
        DbContext.WebAuthnCredential.Update(credential);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    await SignIn(new() { Otp = otp, TwoFactorCode = request.TfaCode }, user, cancellationToken);
}
```

**Verify Method** (Shared Logic)

```csharp
private async Task<(VerifyAssertionResult, WebAuthnCredential)> Verify(
    AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
{
    // Parse client response
    var response = JsonSerializer.Deserialize(
        clientResponse.Response.ClientDataJson, 
        jsonSerializerOptions.GetTypeInfo<AuthenticatorResponse>());

    // Retrieve cached options
    var key = new string([.. response.Challenge.Select(b => (char)b)]);
    var cachedBytes = await cache.GetAsync(key, cancellationToken);
    var options = AssertionOptions.FromJson(Encoding.UTF8.GetString(cachedBytes));

    // Get stored credential
    var credential = await DbContext.WebAuthnCredential
        .FirstOrDefaultAsync(c => c.Id == clientResponse.RawId, cancellationToken);

    // Verify signature
    var verifyResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
    {
        AssertionResponse = clientResponse,
        OriginalOptions = options,
        StoredPublicKey = credential.PublicKey!,
        StoredSignatureCounter = credential.SignCount,
        IsUserHandleOwnerOfCredentialIdCallback = IsUserHandleOwnerOfCredentialId
    }, cancellationToken);

    return (verifyResult, credential);
}
```

---

## Client-Side Implementation

### IWebAuthnService Interface

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/Contracts/IWebAuthnService.cs`](/src/Client/Boilerplate.Client.Core/Services/Contracts/IWebAuthnService.cs)

```csharp
public interface IWebAuthnService
{
    ValueTask<bool> IsWebAuthnAvailable();
    ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options);
    ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options);
    ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null);
    ValueTask<Guid[]> GetWebAuthnConfiguredUserIds();
    ValueTask SetWebAuthnConfiguredUserId(Guid userId);
    ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId = null);
}
```

### WebAuthnServiceBase

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/WebAuthnServiceBase.cs`](/src/Client/Boilerplate.Client.Core/Services/WebAuthnServiceBase.cs)

The base service handles **client-side storage** of which users have configured WebAuthn:

```csharp
public abstract partial class WebAuthnServiceBase : IWebAuthnService
{
    private const string STORE_KEY = "bit-webauthn";

    [AutoInject] protected IStorageService storageService = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    // Abstract methods - platform-specific
    public abstract ValueTask<bool> IsWebAuthnAvailable();
    public abstract ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options);
    public abstract ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options);

    // Concrete methods - shared logic
    public virtual async ValueTask<Guid[]> GetWebAuthnConfiguredUserIds()
    {
        var userIdsAsString = await storageService.GetItem(STORE_KEY);
        if (string.IsNullOrEmpty(userIdsAsString))
            return [];
        return JsonSerializer.Deserialize(userIdsAsString, 
            jsonSerializerOptions.GetTypeInfo<Guid[]>())!;
    }

    public async ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null)
    {
        var userIds = await GetWebAuthnConfiguredUserIds();
        return userId is not null ? userIds.Contains(userId.Value) : userIds.Any();
    }

    public async ValueTask SetWebAuthnConfiguredUserId(Guid userId)
    {
        var userIds = await GetWebAuthnConfiguredUserIds();
        await storageService.SetItem(STORE_KEY, 
            JsonSerializer.Serialize([.. userIds.Union([userId])], 
            jsonSerializerOptions.GetTypeInfo<Guid[]>()));
    }
}
```

**Why Store User IDs Locally?**
- The client needs to know which users can use passwordless sign-in
- This avoids a server round-trip just to check if WebAuthn is available
- The UI can show/hide the "Sign in with biometrics" button accordingly

### Platform-Specific Implementations

#### Web Implementation

**Location**: [`/src/Client/Boilerplate.Client.Web/Services/WebAuthnService.cs`](/src/Client/Boilerplate.Client.Web/Services/WebAuthnService.cs)

For **Blazor WebAssembly** and **Blazor Server**, the implementation uses **Bit.Butil.WebAuthn** to directly call the browser's WebAuthn API:

```csharp
public partial class WebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private Bit.Butil.WebAuthn webAuthn = default!;

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        return await webAuthn.IsAvailable();
    }

    public override async ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options)
    {
        return await webAuthn.CreateCredential<JsonElement, JsonElement>(options);
    }

    public override async ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options)
    {
        return await webAuthn.GetCredential<JsonElement, JsonElement>(options);
    }
}
```

**Bit.Butil.WebAuthn**: This is a C# wrapper around the browser's `navigator.credentials` API, eliminating the need for custom JavaScript interop.

#### MAUI Implementation

**Location**: [`/src/Client/Boilerplate.Client.Maui/Services/MauiWebAuthnService.cs`](/src/Client/Boilerplate.Client.Maui/Services/MauiWebAuthnService.cs)

For **Blazor Hybrid (MAUI)**, WebView has IP-based origins (`http://0.0.0.1`) which don't work with WebAuthn. The solution uses a **local HTTP server** and **in-app browser**:

```csharp
public partial class MauiWebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    public TaskCompletionSource<JsonElement>? CreateWebAuthnCredentialTcs;
    
    public override async ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options)
    {
        CreateWebAuthnCredentialOptions = options;
        CreateWebAuthnCredentialTcs = new();

        ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

        // Open in-app browser pointing to local HTTP server
        await externalNavigationService.NavigateToAsync(
            $"http://localhost:{localHttpServer.Port}/{PageUrls.WebInteropApp}?actionName=CreateWebAuthnCredential");

        return await CreateWebAuthnCredentialTcs.Task;
    }

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        var osVersion = Environment.OSVersion.Version;

        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)
            || true; // Check SupportedOSPlatformVersion in Directory.Build.props
    }
}
```

**How It Works**:
1. The method creates a `TaskCompletionSource` to await the result
2. It navigates to `WebInteropApp` via an in-app browser with `localhost` origin
3. `WebInteropApp` performs the WebAuthn operation in JavaScript
4. The result is passed back to complete the `TaskCompletionSource`

#### WebInteropApp for Hybrid

**Location**: [`/src/Client/Boilerplate.Client.Web/wwwroot/web-interop-app.html`](/src/Client/Boilerplate.Client.Web/wwwroot/web-interop-app.html)

This is a **lightweight HTML page** (no Blazor runtime) that:
- Loads `bit-butil.js` for WebAuthn JavaScript functions
- Loads `app.js` for custom interop logic
- Runs `WebInteropApp.run()` to execute the WebAuthn operation
- Returns the result to the calling MAUI app

```html
<html>
<head>
    <title>Boilerplate</title>
    <style>/* Loading spinner styles */</style>
</head>
<body>
    <div class="title">Please wait</div>
    <div class="loader"><!-- Animated loader --></div>

    <script src="_content/Bit.Butil/bit-butil.js"></script>
    <script src="_content/Boilerplate.Client.Core/scripts/app.js"></script>
    <script type="text/javascript">
        WebInteropApp.run();
    </script>
</body>
</html>
```

**Why This Approach?**
- **WebView Limitation**: WebView has `http://0.0.0.1` origin which WebAuthn rejects
- **Local HTTP Server**: Provides proper `http://localhost:{port}` origin
- **In-App Browser**: Opens the URL in a native browser component (not WebView)
- **Result**: WebAuthn works with platform authenticators (Face ID, Fingerprint)

---

## UI Components

### Enabling Passwordless (Settings Page)

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/Account/PasswordlessTab.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/Account/PasswordlessTab.razor.cs)

```csharp
public partial class PasswordlessTab
{
    private bool isConfigured;

    [AutoInject] IUserController userController = default!;
    [AutoInject] IWebAuthnService webAuthnService = default!;
    [AutoInject] ILocalHttpServer localHttpServer = default!;

    [Parameter] public UserDto? User { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();
        if (User?.UserName is null) return;
        
        // Check if user has already configured WebAuthn
        isConfigured = await webAuthnService.IsWebAuthnConfigured(User.Id);
    }

    private async Task EnablePasswordless()
    {
        if (User?.UserName is null) return;

        // Step 1: Get options from server
        var options = await userController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .GetWebAuthnCredentialOptions(CurrentCancellationToken);

        JsonElement attestationResponse;
        try
        {
            // Step 2: Call browser/platform WebAuthn API
            attestationResponse = await webAuthnService.CreateWebAuthnCredential(options);
        }
        catch (JSException ex)
        {
            // User cancelled or timeout
            ExceptionHandler.Handle(ex, AppEnvironment.IsDevelopment() ? 
                ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
            return;
        }

        // Step 3: Send attestation to server
        await userController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

        // Step 4: Store user ID locally
        await webAuthnService.SetWebAuthnConfiguredUserId(User.Id);

        isConfigured = true;
        SnackBarService.Success(Localizer[nameof(AppStrings.EnablePasswordlessSucsessMessage)]);
    }
}
```

**UI (Razor)**:

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/Account/PasswordlessTab.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/Account/PasswordlessTab.razor)

```xml
@inherits AppComponentBase

<section>
    <BitStack FillContent HorizontalAlign="BitAlignment.Center" Class="max-width">
        <BitText Typography="BitTypography.H6" Align="BitTextAlign.Center">
            @Localizer[nameof(AppStrings.PasswordlessTitle)]
        </BitText>

        <br />

        @if (isConfigured)
        {
            <BitButton AutoLoading OnClick="WrapHandled(DisablePasswordless)" 
                       Variant="BitVariant.Outline" Color="BitColor.Warning">
                @Localizer[nameof(AppStrings.DisablePasswordless)]
            </BitButton>
        }
        else
        {
            <BitButton AutoLoading OnClick="WrapHandled(EnablePasswordless)">
                @Localizer[nameof(AppStrings.EnablePasswordless)]
            </BitButton>
        }
    </BitStack>
</section>
```

### Passwordless Sign-In

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor.cs) (lines 250-310)

```csharp
private async Task PasswordlessSignIn()
{
    isWaiting = true;

    try
    {
        // Step 1: Get configured user IDs from local storage
        var userIds = await webAuthnService.GetWebAuthnConfiguredUserIds();

        if (AppPlatform.IsBlazorHybrid)
        {
            localHttpServer.EnsureStarted();
        }

        // Step 2: Request assertion options from server
        var options = await identityController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .GetWebAuthnAssertionOptions(new() { UserIds = userIds }, CurrentCancellationToken);

        try
        {
            // Step 3: Get credential from authenticator
            webAuthnAssertion = await webAuthnService.GetWebAuthnCredential(options);
        }
        catch (Exception ex)
        {
            // User cancelled or timeout
            ExceptionHandler.Handle(ex, AppEnvironment.IsDevelopment() ? 
                ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
            webAuthnAssertion = null;
            return;
        }

        // Step 4: Sign in with the assertion
        await DoSignIn();
    }
    catch (KnownException e)
    {
        webAuthnAssertion = null;
        SnackBarService.Error(e.Message);
    }
    finally
    {
        isWaiting = false;
    }
}
```

---

## Complete Authentication Flow

Let's walk through the complete passwordless sign-in flow:

### Registration Flow (First-Time Setup)

1. **User navigates to Settings → Account → Passwordless**
2. **Client checks** if WebAuthn is available: `webAuthnService.IsWebAuthnAvailable()`
3. **User clicks** "Enable passwordless sign-in"
4. **Client requests options** from server: `GetWebAuthnCredentialOptions()`
5. **Server generates** `CredentialCreateOptions` with:
   - Challenge (random bytes)
   - User information
   - RP (Relying Party) information
   - Caches the options for 3 minutes
6. **Client calls** `CreateWebAuthnCredential(options)`:
   - On Web: Directly calls `navigator.credentials.create()`
   - On MAUI/Windows: Opens in-app browser to WebInteropApp
7. **Browser/Platform prompts** user for biometric (Face ID, fingerprint, etc.)
8. **Authenticator creates** a new key pair and returns attestation
9. **Client sends attestation** to server: `CreateWebAuthnCredential(attestationResponse)`
10. **Server verifies attestation** using FIDO2 library
11. **Server stores credential** in `WebAuthnCredential` table
12. **Client stores user ID** locally in storage
13. **UI updates** to show "Disable passwordless sign-in" button

### Authentication Flow (Sign In)

1. **User navigates to Sign-In page**
2. **Client checks** local storage for configured user IDs
3. **UI shows** "Sign in with biometrics" button if any users are configured
4. **User clicks** the biometrics button
5. **Client requests assertion options** from server: `GetWebAuthnAssertionOptions(userIds)`
6. **Server generates** `AssertionOptions` with:
   - Challenge
   - List of allowed credentials for those user IDs
   - Caches the options for 3 minutes
7. **Client calls** `GetWebAuthnCredential(options)`:
   - On Web: Directly calls `navigator.credentials.get()`
   - On MAUI/Windows: Opens in-app browser to WebInteropApp
8. **Browser/Platform prompts** user for biometric
9. **Authenticator signs the challenge** with private key
10. **Client sends assertion** to server: `VerifyWebAuthAndSignIn(assertion)`
11. **Server verifies signature** using stored public key
12. **Server checks** sign count (to detect cloned authenticators)
13. **Server generates OTP** and calls regular sign-in flow
14. **User is signed in** successfully!

---

## Security Features

### Anti-Cloning Detection

The `SignCount` property prevents cloned authenticators:

```csharp
var verifyResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
{
    StoredSignatureCounter = credential.SignCount,
    // ...
});

// Update sign count after successful authentication
credential.SignCount = verifyResult.SignCount;
DbContext.WebAuthnCredential.Update(credential);
```

If an authenticator is cloned, the sign counter will be out of sync, indicating a security issue.

### Challenge Caching

Challenges are cached temporarily to prevent replay attacks:

```csharp
await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), 
    new() { SlidingExpiration = TimeSpan.FromMinutes(3) }, cancellationToken);
```

After 3 minutes, the cached challenge expires and cannot be used.

### Origin Validation

The FIDO2 library automatically validates that the origin matches the configured origins:

```csharp
var options = new Fido2Configuration
{
    Origins = new HashSet<string>([webAppUrl.AbsoluteUri]),
};
```

This prevents phishing attacks where a malicious site tries to use credentials registered for your domain.

### Two-Factor Authentication Support

WebAuthn can work alongside 2FA:

```csharp
public async Task VerifyWebAuthAndSignIn(VerifyWebAuthnAndSignInRequestDto request, ...)
{
    var (verifyResult, credential) = await Verify(request.ClientResponse, cancellationToken);
    var user = await userManager.FindByIdAsync(credential.UserId.ToString());
    
    // If 2FA is enabled AND no 2FA code provided
    if (user.TwoFactorEnabled is false || request.TfaCode is not null)
    {
        // Complete sign-in
    }
    else
    {
        // Prompt for 2FA code
        await SendTwoFactorToken(new() { Otp = otp }, user, cancellationToken);
    }
}
```

---

## Platform Support Matrix

| Platform | Implementation | Availability Check | Notes |
|----------|---------------|-------------------|-------|
| **Blazor WebAssembly** | `Bit.Butil.WebAuthn` → Browser API | `await webAuthn.IsAvailable()` | Direct browser access |
| **Blazor Server** | `Bit.Butil.WebAuthn` → Browser API | `await webAuthn.IsAvailable()` | Direct browser access |
| **MAUI Android** | LocalHttpServer + In-App Browser | OS version check | Uses Android biometrics |
| **MAUI iOS** | LocalHttpServer + In-App Browser | OS version check | Uses Face ID / Touch ID |
| **MAUI Windows** | LocalHttpServer + In-App Browser | Windows 10 v1903+ (build 18362) | Uses Windows Hello |
| **Windows Forms Hybrid** | Similar to MAUI | Windows 10 v1903+ (build 18362) | Uses Windows Hello |

**Browser Support** (for Web platforms):
- ✅ Chrome/Edge 67+
- ✅ Firefox 60+
- ✅ Safari 13+
- ✅ Opera 54+

---

## Configuration and Customization

### Server-Side Customization

You can customize FIDO2 configuration in [`Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs):

```csharp
services.AddScoped(sp =>
{
    var webAppUrl = sp.GetRequiredService<IHttpContextAccessor>()
        .HttpContext!.Request.GetWebAppUrl();

    var options = new Fido2Configuration
    {
        ServerDomain = webAppUrl.Host,
        ServerName = "Your App Name", // Change this
        ServerIcon = "https://yourapp.com/icon.png", // Change this
        TimestampDriftTolerance = 1000,
        Origins = new HashSet<string>([webAppUrl.AbsoluteUri]),
    };

    return options;
});
```

### Authenticator Selection

You can customize which authenticators are allowed:

```csharp
var authenticatorSelection = new AuthenticatorSelection
{
    // Only allow platform authenticators (Face ID, Windows Hello)
    AuthenticatorAttachment = AuthenticatorAttachment.Platform, 
    
    // Or allow any authenticator (including USB keys)
    // AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform,
    
    // Require user verification (biometric/PIN)
    UserVerification = UserVerificationRequirement.Required,
    
    // Don't require discoverable credentials (resident keys)
    ResidentKey = ResidentKeyRequirement.Discouraged,
};
```

**Authenticator Types**:
- **Platform**: Built-in (Face ID, fingerprint sensor, Windows Hello)
- **CrossPlatform**: External (YubiKey, USB security keys)

### Client-Side Storage Key

If you want to change where user IDs are stored locally:

```csharp
public abstract partial class WebAuthnServiceBase : IWebAuthnService
{
    private const string STORE_KEY = "bit-webauthn"; // Change this if needed
}
```

---

## Testing and Debugging

### Testing WebAuthn Locally

1. **Blazor WebAssembly/Server**: Must use HTTPS or `localhost` (HTTP is allowed for localhost only)
2. **Blazor Hybrid**: Uses local HTTP server on `localhost` which is allowed

### Common Issues

**Issue**: WebAuthn not available in browser
- **Cause**: Not using HTTPS (except localhost)
- **Solution**: Configure HTTPS in development or use `dotnet dev-certs https --trust`

**Issue**: "NotAllowedError" in browser
- **Cause**: User cancelled, timed out, or browser doesn't support WebAuthn
- **Solution**: Handle the exception gracefully (see `PasswordlessTab.razor.cs`)

**Issue**: Origin mismatch error
- **Cause**: FIDO2 configuration origins don't match the actual origin
- **Solution**: Check `Origins` in `Fido2Configuration` matches your URL

**Issue**: WebAuthn not working in MAUI
- **Cause**: WebView IP-based origin `http://0.0.0.1`
- **Solution**: Already handled by LocalHttpServer + WebInteropApp approach

### Debugging Tips

1. **Check if WebAuthn is available**:
```csharp
var isAvailable = await webAuthnService.IsWebAuthnAvailable();
```

2. **Check configured users**:
```csharp
var userIds = await webAuthnService.GetWebAuthnConfiguredUserIds();
```

3. **Enable detailed logging**:
```json
"Logging": {
  "LogLevel": {
    "Fido2NetLib": "Debug"
  }
}
```

4. **Inspect stored credentials in database**:
```sql
SELECT Id, UserId, SignCount, RegDate FROM WebAuthnCredential;
```

---

## Best Practices

### 1. Always Handle Exceptions Gracefully

```csharp
try
{
    attestationResponse = await webAuthnService.CreateWebAuthnCredential(options);
}
catch (JSException ex)
{
    // User cancelled or timeout - don't show error
    ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
    return;
}
```

### 2. Use HTTPS in Production

WebAuthn requires HTTPS except for `localhost`. Always use HTTPS in production.

### 3. Implement Fallback Authentication

Never make WebAuthn the *only* authentication method. Always provide:
- Password-based sign-in
- Email OTP sign-in
- SMS OTP sign-in (if phone number is available)

### 4. Update Sign Counter After Successful Authentication

```csharp
if (user.TwoFactorEnabled is false || request.TfaCode is not null)
{
    credential.SignCount = verifyResult.SignCount;
    DbContext.WebAuthnCredential.Update(credential);
    await DbContext.SaveChangesAsync(cancellationToken);
}
```

### 5. Support Multiple Credentials Per User

The data model supports multiple credentials per user:

```csharp
public List<WebAuthnCredential> WebAuthnCredentials { get; set; } = [];
```

This allows users to register:
- Fingerprint on their laptop
- Face ID on their phone
- YubiKey as a backup

### 6. Clean Up Credentials on User Deletion

The EF Core configuration handles this automatically:

```csharp
builder.HasOne(t => t.User)
    .WithMany(u => u.WebAuthnCredentials)
    .HasForeignKey(t => t.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

### 7. Cache Options Temporarily

Options should expire quickly to prevent replay attacks:

```csharp
new() { SlidingExpiration = TimeSpan.FromMinutes(3) }
```

---

## Advanced Scenarios

### Supporting Hardware Security Keys (YubiKey)

Change the `AuthenticatorAttachment` to allow cross-platform authenticators:

```csharp
var authenticatorSelection = new AuthenticatorSelection
{
    AuthenticatorAttachment = null, // Allow any authenticator
    UserVerification = UserVerificationRequirement.Preferred,
};
```

### User Verification Levels

```csharp
// Required: User MUST provide biometric/PIN
UserVerification = UserVerificationRequirement.Required

// Preferred: Ask for biometric/PIN if available
UserVerification = UserVerificationRequirement.Preferred

// Discouraged: Don't ask for biometric/PIN
UserVerification = UserVerificationRequirement.Discouraged
```

### Resident Keys (Discoverable Credentials)

Resident keys allow passwordless login without entering a username:

```csharp
var authenticatorSelection = new AuthenticatorSelection
{
    ResidentKey = ResidentKeyRequirement.Required,
    RequireResidentKey = true,
};
```

**Note**: Not all authenticators support resident keys.

---

## Resources and Further Learning

### Official Documentation
- **W3C WebAuthn Spec**: https://www.w3.org/TR/webauthn-2/
- **FIDO Alliance**: https://fidoalliance.org/
- **MDN WebAuthn Guide**: https://developer.mozilla.org/en-US/docs/Web/API/Web_Authentication_API

### Libraries Used
- **Fido2NetLib**: https://github.com/passwordless-lib/fido2-net-lib
- **Bit.Butil**: Part of bitplatform (query DeepWiki for details)

### Testing Tools
- **WebAuthn.io**: https://webauthn.io/ (Test WebAuthn in your browser)
- **Chrome DevTools**: Supports virtual authenticators for testing

---

## Summary

In this stage, you learned about:

✅ **WebAuthn Architecture**: Understanding the three-layer system (server, client, bridge)  
✅ **Server Implementation**: Entity models, FIDO2 configuration, controller endpoints  
✅ **Client Implementation**: Platform-specific services (Web, MAUI, Windows)  
✅ **Complete Flows**: Registration and authentication workflows step-by-step  
✅ **Security Features**: Anti-cloning, challenge caching, origin validation  
✅ **Platform Support**: How different platforms handle WebAuthn  
✅ **UI Components**: Enabling/disabling passwordless, sign-in with biometrics  
✅ **Best Practices**: Error handling, HTTPS, fallback authentication  
✅ **Advanced Scenarios**: Hardware keys, user verification levels, resident keys  

**Key Takeaway**: WebAuthn provides a secure, user-friendly alternative to passwords by leveraging biometric authentication. The implementation in this project handles all the complexity of FIDO2/WebAuthn across multiple platforms (Web, MAUI, Windows), making it easy to add passwordless authentication to your application.

---