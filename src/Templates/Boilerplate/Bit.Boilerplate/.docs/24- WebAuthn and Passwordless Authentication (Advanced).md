# Stage 24: WebAuthn and Passwordless Authentication (Advanced)

Welcome to Stage 24! In this stage, you'll learn about the WebAuthn and passwordless authentication implementation in the project. This advanced feature enables users to sign in using biometric authentication (fingerprint, Face ID) and platform authenticators, providing a more secure and convenient authentication method than traditional passwords.

---

## What is WebAuthn?

**WebAuthn (Web Authentication)** is a web standard published by the W3C that enables passwordless authentication using public key cryptography. It allows users to authenticate using:

- **Biometric authentication**: Fingerprint readers, Face ID, Windows Hello
- **Hardware security keys**: YubiKey, Titan Security Key
- **Platform authenticators**: Built-in device security features

### Security Benefits

WebAuthn provides several security advantages over traditional password-based authentication:

1. **Phishing Resistant**: Credentials are bound to the origin, making phishing attacks ineffective
2. **No Shared Secrets**: Public key cryptography means the server never stores sensitive private keys
3. **Strong Authentication**: Combines something you have (device) with something you are (biometric)
4. **Replay Attack Protection**: Each authentication generates a unique signature

### How It Works (High-Level)

1. **Registration (Credential Creation)**:
   - Server generates a challenge
   - Client creates a key pair using the device's authenticator
   - Public key is sent to the server and stored
   - Private key stays securely on the device

2. **Authentication (Credential Assertion)**:
   - Server generates a challenge
   - Client signs the challenge with the private key
   - Server verifies the signature using the stored public key
   - User is authenticated without ever transmitting a password

---

## Project Architecture Overview

The project implements WebAuthn across all platforms: Web (Blazor WebAssembly/Server), Windows, and MAUI (iOS, Android, macOS). The implementation is more secure than native biometric authentication because:

- **Standards-Based**: Uses W3C WebAuthn standard with FIDO2 protocol
- **Server-Side Verification**: All authentication is verified on the server
- **Cross-Platform**: Works consistently across all platforms
- **Cryptographic Security**: Uses public key cryptography instead of simple biometric checks

**Important Note**: While the implementation works across all platforms, **Face ID is not yet supported on Android** due to platform limitations.

---

## Core Components

### 1. Server-Side Components

#### WebAuthnCredential Entity

**Location**: [`/src/Server/Boilerplate.Server.Api/Models/Identity/WebAuthnCredential.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Server\Boilerplate.Server.Api\Models\Identity\WebAuthnCredential.cs)

This entity stores the WebAuthn credentials for each user:

```csharp
public class WebAuthnCredential
{
    public required byte[] Id { get; set; }              // Credential ID
    public Guid UserId { get; set; }                     // User reference
    public byte[]? PublicKey { get; set; }               // Public key for verification
    public uint SignCount { get; set; }                  // Counter to detect cloned credentials
    public AuthenticatorTransport[]? Transports { get; set; }  // How credential was used
    public bool IsBackupEligible { get; set; }          // Can be backed up
    public bool IsBackedUp { get; set; }                // Is backed up
    public DateTimeOffset RegDate { get; set; }         // Registration date
    public Guid AaGuid { get; set; }                    // Authenticator GUID
    // ... more properties for attestation data
}
```

#### IdentityController.WebAuthn.cs

**Location**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.WebAuthn.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Server\Boilerplate.Server.Api\Controllers\Identity\IdentityController.WebAuthn.cs)

Handles WebAuthn authentication (sign-in) flow:

```csharp
[HttpPost]
public async Task<AssertionOptions> GetWebAuthnAssertionOptions(
    WebAuthnAssertionOptionsRequestDto request, 
    CancellationToken cancellationToken)
{
    // 1. Retrieve existing credentials for the user(s)
    var existingCredentials = await DbContext.WebAuthnCredential
        .Where(c => request.UserIds.Contains(c.UserId))
        .ToArrayAsync(cancellationToken);
    
    // 2. Generate assertion options with challenge
    var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
    {
        AllowedCredentials = existingKeys,
        UserVerification = UserVerificationRequirement.Required,
    });
    
    // 3. Cache the challenge for later verification
    await cache.SetAsync(key, options.ToJson(), 
        new() { SlidingExpiration = TimeSpan.FromMinutes(3) }, 
        cancellationToken);
    
    return options;
}

[HttpPost]
public async Task VerifyWebAuthAndSignIn(
    VerifyWebAuthnAndSignInRequestDto<AuthenticatorAssertionRawResponse> request, 
    CancellationToken cancellationToken)
{
    // 1. Verify the assertion using Fido2NetLib
    var (verifyResult, credential) = await Verify(request.ClientResponse, cancellationToken);
    
    // 2. Find the user
    var user = await userManager.FindByIdAsync(credential.UserId.ToString());
    
    // 3. Update sign count (prevents credential cloning attacks)
    credential.SignCount = verifyResult.SignCount;
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // 4. Sign the user in
    await SignIn(new() { Otp = otp, TwoFactorCode = request.TfaCode }, user, cancellationToken);
}
```

**Key Points**:
- Uses **Fido2NetLib** for WebAuthn protocol implementation
- Caches challenges in **IDistributedCache** with 3-minute expiration
- Supports **Two-Factor Authentication (2FA)** in addition to WebAuthn
- Updates **SignCount** to detect cloned credentials

#### UserController.WebAuthn.cs

**Location**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.WebAuthn.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Server\Boilerplate.Server.Api\Controllers\Identity\UserController.WebAuthn.cs)

Handles WebAuthn credential management (registration/deletion):

```csharp
[HttpGet]
public async Task<CredentialCreateOptions> GetWebAuthnCredentialOptions(
    CancellationToken cancellationToken)
{
    var userId = User.GetUserId();
    var user = await userManager.FindByIdAsync(userId.ToString());
    
    // Configure authenticator requirements
    var authenticatorSelection = new AuthenticatorSelection
    {
        RequireResidentKey = false,
        ResidentKey = ResidentKeyRequirement.Discouraged,
        UserVerification = UserVerificationRequirement.Required,
        AuthenticatorAttachment = AuthenticatorAttachment.Platform  // Use device's built-in authenticator
    };
    
    // Generate credential creation options
    var options = fido2.RequestNewCredential(new RequestNewCredentialParams
    {
        User = fidoUser,
        ExcludeCredentials = [],  // Allow multiple credentials
        AuthenticatorSelection = authenticatorSelection,
        AttestationPreference = AttestationConveyancePreference.None,
    });
    
    // Cache options for verification
    await cache.SetAsync(key, options.ToJson(), 
        new() { SlidingExpiration = TimeSpan.FromMinutes(3) }, 
        cancellationToken);
    
    return options;
}

[HttpPut]
public async Task CreateWebAuthnCredential(
    AuthenticatorAttestationRawResponse attestationResponse, 
    CancellationToken cancellationToken)
{
    // 1. Retrieve cached options
    var options = CredentialCreateOptions.FromJson(cachedJson);
    
    // 2. Verify the attestation
    var credential = await fido2.MakeNewCredentialAsync(
        new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = IsCredentialIdUniqueToUser
        }, 
        cancellationToken);
    
    // 3. Store the credential
    var newCredential = new WebAuthnCredential
    {
        UserId = userId,
        Id = credential.Id,
        PublicKey = credential.PublicKey,
        SignCount = credential.SignCount,
        RegDate = DateTimeOffset.UtcNow,
        // ... other properties
    };
    
    await DbContext.WebAuthnCredential.AddAsync(newCredential, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
}
```

### 2. Client-Side Components

#### IWebAuthnService Interface

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/Contracts/IWebAuthnService.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Services\Contracts\IWebAuthnService.cs)

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

#### WebAuthnServiceBase

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/WebAuthnServiceBase.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Services\WebAuthnServiceBase.cs)

Base class that handles local storage of configured user IDs:

```csharp
public abstract partial class WebAuthnServiceBase : IWebAuthnService
{
    private const string STORE_KEY = "bit-webauthn";

    [AutoInject] protected IStorageService storageService = default!;

    // Abstract methods implemented by platform-specific services
    public abstract ValueTask<bool> IsWebAuthnAvailable();
    public abstract ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options);
    public abstract ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options);

    // Shared implementation for tracking configured users
    public virtual async ValueTask<Guid[]> GetWebAuthnConfiguredUserIds()
    {
        var userIdsAsString = await storageService.GetItem(STORE_KEY);
        if (string.IsNullOrEmpty(userIdsAsString))
            return [];
        return JsonSerializer.Deserialize<Guid[]>(userIdsAsString)!;
    }

    public async ValueTask SetWebAuthnConfiguredUserId(Guid userId)
    {
        var userIds = (await GetWebAuthnConfiguredUserIds()).ToList();
        if (!userIds.Contains(userId))
        {
            userIds.Add(userId);
            await storageService.SetItem(STORE_KEY, 
                JsonSerializer.Serialize(userIds.ToArray()));
        }
    }
}
```

**Purpose**: This base class provides a consistent way to track which users have configured WebAuthn on the device, stored in local storage.

---

## Platform-Specific Implementations

### 1. Web Implementation (Blazor WebAssembly/Server)

**Location**: [`/src/Client/Boilerplate.Client.Web/Services/WebAuthnService.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Web\Services\WebAuthnService.cs)

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

**How It Works**:
- Uses **Bit.Butil.WebAuthn** - a wrapper around the browser's native WebAuthn API
- Bit.Butil uses JavaScript interop to call `navigator.credentials.create()` and `navigator.credentials.get()`
- Works directly in the browser without any special workarounds
- Fully supports all WebAuthn features available in the browser

### 2. Blazor Hybrid Implementation (MAUI & Windows)

**The Challenge**: Blazor Hybrid apps run in a WebView with an IP-based origin like `http://0.0.0.1`. WebAuthn requires a proper origin (like `https://example.com` or `http://localhost`) for security reasons.

**The Solution**: Use a combination of:
1. **Local HTTP Server** - Serves content on `http://localhost:{port}`
2. **Web Interop App** - A lightweight HTML page that can access WebAuthn APIs
3. **External Navigation Service** - Opens the Web Interop App in an in-app browser
4. **Communication Bridge** - Passes data between the main app and the interop page

#### Step-by-Step Flow

Let's trace through the flow for enabling passwordless authentication in a MAUI app:

**Step 1: User Requests to Enable Passwordless**

In [`PasswordlessTab.razor.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Components\Pages\Settings\Account\PasswordlessTab.razor.cs):

```csharp
private async Task EnablePasswordless()
{
    // 1. Get credential creation options from server
    var options = await userController
        .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
        .GetWebAuthnCredentialOptions(CurrentCancellationToken);

    // 2. Create credential using platform-specific service
    JsonElement attestationResponse = await webAuthnService.CreateWebAuthnCredential(options);

    // 3. Send credential to server for storage
    await userController.CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

    // 4. Mark user as configured locally
    await webAuthnService.SetWebAuthnConfiguredUserId(User.Id);
}
```

**Step 2: MAUI WebAuthn Service**

**Location**: [`/src/Client/Boilerplate.Client.Maui/Services/MauiWebAuthnService.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Maui\Services\MauiWebAuthnService.cs)

```csharp
public override async ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options)
{
    // 1. Store the options so the local HTTP server can serve them
    CreateWebAuthnCredentialOptions = options;

    // 2. Create a TaskCompletionSource to wait for the result
    CreateWebAuthnCredentialTcs = new();

    // 3. Set the service reference on the local HTTP server
    ((MauiLocalHttpServer)localHttpServer).WebAuthnService = this;

    // 4. Open the Web Interop App in an in-app browser
    await externalNavigationService.NavigateToAsync(
        $"http://localhost:{localHttpServer.Port}/{PageUrls.WebInteropApp}?actionName=CreateWebAuthnCredential");

    // 5. Wait for the result (will be set by the HTTP server callback)
    return await CreateWebAuthnCredentialTcs.Task;
}
```

**Step 3: Local HTTP Server**

**Location**: [`/src/Client/Boilerplate.Client.Maui/Services/MauiLocalHttpServer.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Maui\Services\MauiLocalHttpServer.cs)

The local HTTP server hosts endpoints that the Web Interop App will call:

```csharp
public int EnsureStarted()
{
    // Start HTTP server on an available port
    port = GetAvailableTcpPort();
    
    localHttpServer = new WebServer(o => o.WithUrlPrefix($"http://localhost:{port}"))
        // Serve the options to the Web Interop App
        .WithModule(new ActionModule("/api/GetCreateWebAuthnCredentialOptions", HttpVerbs.Get, async ctx =>
        {
            await ctx.SendStringAsync(
                JsonSerializer.Serialize(WebAuthnService!.CreateWebAuthnCredentialOptions!));
        }))
        // Receive the credential back from the Web Interop App
        .WithModule(new ActionModule("/api/CreateWebAuthnCredential", HttpVerbs.Post, async ctx =>
        {
            var result = JsonSerializer.Deserialize<JsonElement>(
                await ctx.GetRequestBodyAsStringAsync());
            WebAuthnService!.CreateWebAuthnCredentialTcs!.SetResult(result);
            // Close the in-app browser and return to the main app
            await GoBackToApp();
        }));
    
    localHttpServer.Start();
    return port;
}
```

**Step 4: Web Interop App**

**Location**: [`/src/Client/Boilerplate.Client.Web/wwwroot/web-interop-app.html`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Web\wwwroot\web-interop-app.html)

This is a lightweight HTML page that loads only the necessary JavaScript (app.js) without Blazor:

```xml
<!DOCTYPE html>
<html>
<head>
    <title>Boilerplate</title>
    <base href="/" />
</head>
<body>
    <div class="title">Please wait</div>
    <!-- Loading animation -->
    
    <script src="_content/Bit.Butil/bit-butil.js"></script>
    <script src="_content/Boilerplate.Client.Core/scripts/app.js"></script>
    <script type="text/javascript">
        WebInteropApp.run();
    </script>
</body>
</html>
```

**Step 5: JavaScript Interop (app.js)**

The `app.js` file contains the `WebInteropApp.run()` function that:

1. Reads the `actionName` from the query string (e.g., `CreateWebAuthnCredential`)
2. Fetches the options from the local HTTP server (`/api/GetCreateWebAuthnCredentialOptions`)
3. Calls the browser's WebAuthn API (`navigator.credentials.create()`)
4. Posts the result back to the local HTTP server (`/api/CreateWebAuthnCredential`)
5. The local HTTP server completes the TaskCompletionSource, unblocking the original call
6. The in-app browser closes and returns to the main app

#### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ MAUI App (WebView - http://0.0.0.1)                           │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ PasswordlessTab Component                                │  │
│  │  - User clicks "Enable Passwordless"                     │  │
│  └─────────────┬────────────────────────────────────────────┘  │
│                │                                                │
│                v                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ MauiWebAuthnService                                      │  │
│  │  - Stores options                                        │  │
│  │  - Creates TaskCompletionSource                          │  │
│  │  - Opens in-app browser → http://localhost:12345/web... │  │
│  └─────────────┬────────────────────────────────────────────┘  │
│                │                                                │
└────────────────┼────────────────────────────────────────────────┘
                 │
                 v
┌─────────────────────────────────────────────────────────────────┐
│ In-App Browser (http://localhost:12345)                       │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ web-interop-app.html                                     │  │
│  │  - Loads app.js                                          │  │
│  │  - Runs WebInteropApp.run()                              │  │
│  └─────────────┬────────────────────────────────────────────┘  │
│                │                                                │
│                v                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ JavaScript (app.js)                                      │  │
│  │  1. GET /api/GetCreateWebAuthnCredentialOptions         │  │
│  │  2. navigator.credentials.create(options)                │  │
│  │  3. POST /api/CreateWebAuthnCredential (result)          │  │
│  └─────────────┬────────────────────────────────────────────┘  │
│                │                                                │
└────────────────┼────────────────────────────────────────────────┘
                 │
                 v
┌─────────────────────────────────────────────────────────────────┐
│ Local HTTP Server (http://localhost:12345)                    │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ MauiLocalHttpServer                                      │  │
│  │  - Serves options via GET endpoint                       │  │
│  │  - Receives result via POST endpoint                     │  │
│  │  - Completes TaskCompletionSource                        │  │
│  │  - Closes in-app browser                                 │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

#### Why This Architecture?

This complex architecture is necessary because:

1. **WebView Limitation**: WebViews in MAUI/Windows use IP-based origins (`http://0.0.0.1`) which WebAuthn APIs reject for security
2. **Security Requirement**: WebAuthn requires proper origins (`http://localhost` or HTTPS domains)
3. **User Experience**: Must maintain app state and avoid restarting the entire app
4. **Platform Consistency**: The same approach works for both credential creation and authentication

---

## Key Interfaces

### ILocalHttpServer

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/Contracts/ILocalHttpServer.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Services\Contracts\ILocalHttpServer.cs)

```csharp
public interface ILocalHttpServer : IAsyncDisposable
{
    int EnsureStarted();    // Start the server and return the port
    int Port { get; }        // Get the current port
    string? Origin { get; }  // Get the origin (e.g., "http://localhost:12345")
}
```

**Implementations**:
- **NoopLocalHttpServer** (Web): Does nothing, not needed for web browsers
- **MauiLocalHttpServer** (MAUI): EmbedIO-based HTTP server
- **WindowsLocalHttpServer** (Windows): EmbedIO-based HTTP server

### IExternalNavigationService

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/Contracts/IExternalNavigationService.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Services\Contracts\IExternalNavigationService.cs)

```csharp
public interface IExternalNavigationService
{
    Task NavigateToAsync(string url);
}
```

**Purpose**: Opens URLs in an in-app browser (MAUI/Windows) or new window/popup (Web).

**Implementations**:
- **DefaultExternalNavigationService** (Web): Opens in popup or new tab
- **MauiExternalNavigationService** (MAUI): Uses `WebAuthenticator` to open in-app browser
- **WindowsExternalNavigationService** (Windows): Creates a Form with WebView

---

## Complete Usage Examples

### 1. Sign In with Passwordless

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Components\Pages\Identity\SignIn\SignInPanel.razor.cs)

```csharp
private async Task PasswordlessSignIn()
{
    isWaiting = true;

    try
    {
        // 1. Get list of users who have configured WebAuthn on this device
        var userIds = await webAuthnService.GetWebAuthnConfiguredUserIds();

        // 2. For Blazor Hybrid, start the local HTTP server
        if (AppPlatform.IsBlazorHybrid)
        {
            localHttpServer.EnsureStarted();
        }

        // 3. Get assertion options from the server
        var options = await identityController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .GetWebAuthnAssertionOptions(new() { UserIds = userIds }, CurrentCancellationToken);

        // 4. Get the credential (will trigger biometric prompt)
        try
        {
            webAuthnAssertion = await webAuthnService.GetWebAuthnCredential(options);
        }
        catch (Exception ex)
        {
            // User cancelled or timed out - handle gracefully
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            return;
        }

        // 5. Sign in
        await DoSignIn();  // This will call VerifyWebAuthAndSignIn on the server
    }
    catch (KnownException e)
    {
        SnackBarService.Error(e.Message);
    }
    finally
    {
        isWaiting = false;
    }
}
```

### 2. Enable Passwordless for a User

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/Account/PasswordlessTab.razor.cs`](c:\Workspace\bitplatform\src\Templates\Boilerplate\Bit.Boilerplate\src\Client\Boilerplate.Client.Core\Components\Pages\Settings\Account\PasswordlessTab.razor.cs)

```csharp
private async Task EnablePasswordless()
{
    if (User?.UserName is null) return;

    // 1. Get credential creation options from server
    var options = await userController
        .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
        .GetWebAuthnCredentialOptions(CurrentCancellationToken);

    // 2. Create credential (will trigger biometric enrollment)
    JsonElement attestationResponse;
    try
    {
        attestationResponse = await webAuthnService.CreateWebAuthnCredential(options);
    }
    catch (JSException ex)
    {
        // User cancelled or error occurred
        ExceptionHandler.Handle(ex, AppEnvironment.IsDevelopment() 
            ? ExceptionDisplayKind.NonInterrupting 
            : ExceptionDisplayKind.None);
        return;
    }

    // 3. Send credential to server
    await userController
        .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
        .CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

    // 4. Mark as configured locally
    await webAuthnService.SetWebAuthnConfiguredUserId(User.Id);

    isConfigured = true;

    SnackBarService.Success(Localizer[nameof(AppStrings.EnablePasswordlessSucsessMessage)]);
}
```

### 3. Disable Passwordless for a User

```csharp
private async Task DisablePasswordless()
{
    if (User?.UserName is null) return;

    // 1. Get assertion options (to verify it's really the user)
    var options = await identityController
        .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
        .GetWebAuthnAssertionOptions(new() { UserIds = [User.Id] }, CurrentCancellationToken);

    // 2. Verify with biometric
    JsonElement assertion;
    try
    {
        assertion = await webAuthnService.GetWebAuthnCredential(options);
    }
    catch (Exception ex)
    {
        ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
        return;
    }

    // 3. Verify the assertion on the server
    await identityController
        .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
        .VerifyWebAuthAssertion(assertion, CurrentCancellationToken);

    // 4. Delete the credential
    await userController
        .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
        .DeleteWebAuthnCredential(assertion, CurrentCancellationToken);

    // 5. Remove from local storage
    await webAuthnService.RemoveWebAuthnConfiguredUserId(User.Id);

    isConfigured = false;

    SnackBarService.Success(Localizer[nameof(AppStrings.DisablePasswordlessSucsessMessage)]);
}
```

---

## Two-Factor Authentication (2FA) Integration

WebAuthn can be used in combination with Two-Factor Authentication. The server-side flow supports this:

```csharp
[HttpPost]
public async Task VerifyWebAuthAndSendTwoFactorToken(
    AuthenticatorAssertionRawResponse clientResponse, 
    CancellationToken cancellationToken)
{
    // Verify the WebAuthn assertion
    var (verifyResult, credential) = await Verify(clientResponse, cancellationToken);

    var user = await userManager.FindByIdAsync(credential.UserId.ToString());

    // Generate OTP for the user
    var (otp, _) = await GenerateAutomaticSignInLink(user, null, "WebAuthn");

    // Send 2FA token via email/SMS
    await SendTwoFactorToken(new() { Otp = otp }, user, cancellationToken);
}
```

When signing in with WebAuthn, if the user has 2FA enabled, they'll need to provide the 2FA code after the biometric verification.

---

## Testing WebAuthn

### Web Browser
1. Navigate to the sign-in page
2. If you have WebAuthn configured, you'll see a "Sign in with Fingerprint/Face ID" button
3. Click it to sign in without a password

### MAUI/Windows App
1. Run the app
2. Go to Settings → Account → Passwordless
3. Click "Enable" - an in-app browser will open
4. Complete the biometric enrollment
5. Return to the app - passwordless is now enabled
6. Sign out and try signing in with biometric authentication

---

## Additional Resources

### Server-Side Library

The project uses **Fido2NetLib** for server-side WebAuthn/FIDO2 implementation:
- GitHub: https://github.com/passwordless-lib/fido2-net-lib
- Handles all the complex cryptographic operations
- Validates attestations and assertions
- Manages credential storage and verification

### Client-Side Library

For web browsers, the project uses **Bit.Butil.WebAuthn**:
- Part of the Bit.Butil library
- Provides C# wrapper around browser's WebAuthn API
- Documentation: https://blazorui.bitplatform.dev/butil/webauthn

### Specifications

- **WebAuthn Specification**: https://www.w3.org/TR/webauthn-2/
- **FIDO2 Overview**: https://fidoalliance.org/fido2/

---

## Summary

In this stage, you learned:

1. ✅ **What WebAuthn is** and why it's more secure than traditional authentication
2. ✅ **How the project implements WebAuthn** across all platforms (Web, Windows, MAUI)
3. ✅ **The architecture for Blazor Hybrid** using Local HTTP Server and Web Interop App
4. ✅ **Key components** on both server and client side
5. ✅ **Complete flows** for registration, authentication, and credential management
6. ✅ **Security considerations** including origin validation, challenge caching, and sign count tracking
7. ✅ **Integration with 2FA** for additional security
8. ✅ **Testing and troubleshooting** common issues

WebAuthn is an advanced feature that provides a superior user experience while maintaining the highest security standards. The implementation in this project is production-ready and demonstrates best practices for cross-platform passwordless authentication.

---
