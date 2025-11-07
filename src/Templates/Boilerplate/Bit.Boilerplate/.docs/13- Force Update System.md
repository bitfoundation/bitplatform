# Stage 13: Force Update System

Welcome to Stage 13! In this stage, you will learn about the **Force Update System** - a critical feature that ensures users are always running a compatible version of your application.

## What is the Force Update System?

The Force Update System is a mechanism that:
- **Validates client app versions** on every API request
- **Forces users to update** if they're running an outdated version
- **Prevents incompatible clients** from accessing the backend
- **Works across all platforms**: Web, Windows, Android, iOS, and macOS

### Why is this important?

Unlike traditional desktop applications where version checks happen only at startup, this system validates the version on **every single request**. This means:
- Even users who are **actively using** the application will be forced to update
- No grace period or "update later" option for critical updates
- Ensures backend API changes don't break outdated clients
- Maintains security by enforcing minimum supported versions

---

## How It Works: The Complete Flow

### 1. Client Sends Version Header

Every HTTP request from the client includes version information in the headers.

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/RequestHeadersDelegatingHandler.cs`](/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/RequestHeadersDelegatingHandler.cs)

```csharp
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
    // ... other code ...
    
    var isInternalRequest = request.HasExternalApiAttribute() is false;
    if (isInternalRequest)
    {
        request.Headers.Add("X-App-Version", telemetryContext.AppVersion);
        request.Headers.Add("X-App-Platform", AppPlatform.Type.ToString());
    }
    
    return await base.SendAsync(request, cancellationToken);
}
```

**Key Points**:
- The `X-App-Version` header contains the current app version (e.g., "1.0.0")
- The `X-App-Platform` header identifies the platform (Web, Windows, Android, iOS, macOS)
- These headers are **automatically added** to all internal API requests

---

### 2. Server Validates Version via Middleware

The server checks the incoming version against configured minimum supported versions.

**Location**: [`/src/Server/Boilerplate.Server.Api/RequestPipeline/ForceUpdateMiddleware.cs`](/src/Server/Boilerplate.Server.Api/RequestPipeline/ForceUpdateMiddleware.cs)

```csharp
public class ForceUpdateMiddleware(RequestDelegate next, ServerApiSettings settings)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-App-Version", out var appVersionHeaderValue)
            && appVersionHeaderValue.Any())
        {
            var appVersion = appVersionHeaderValue.Single()!;
            var appPlatformType = Enum.Parse<AppPlatformType>(context.Request.Headers["X-App-Platform"].Single()!);
            var minVersion = settings.SupportedAppVersions!.GetMinimumSupportedAppVersion(appPlatformType);
            
            if (minVersion != null && Version.Parse(appVersion) < minVersion)
            {
                throw new ClientNotSupportedException();
            }
        }

        await next(context);
    }
}
```

**How It Works**:
1. Extracts `X-App-Version` and `X-App-Platform` from request headers
2. Retrieves the minimum supported version for that platform from settings
3. Compares the client's version with the minimum required version
4. If the client version is too old, throws `ClientNotSupportedException`

---

### 3. Minimum Supported Versions Configuration

The minimum supported versions are configured in the server's `appsettings.json`.

**Location**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
{
  "SupportedAppVersions": {
    "MinimumSupportedAndroidAppVersion": "1.0.0",
    "MinimumSupportedIosAppVersion": "1.0.0",
    "MinimumSupportedMacOSAppVersion": "1.0.0",
    "MinimumSupportedWindowsAppVersion": "1.0.0",
    "MinimumSupportedWebAppVersion": "1.0.0",
    "SupportedAppVersions__Comment": "Enabling `AutoReload` (Disabled by default) ensure the latest app version is always applied in Web & Windows apps. Refer to `Client.Web/Components/AppBswupProgressBar.razor`, `Client.Web/wwwroot/index.html` and `Client.Windows/appsettings.json` for details."
  }
}
```

**Settings Class**: [`/src/Server/Boilerplate.Server.Api/ServerApiSettings.cs`](/src/Server/Boilerplate.Server.Api/ServerApiSettings.cs)

```csharp
public class SupportedAppVersionsOptions
{
    public Version? MinimumSupportedAndroidAppVersion { get; set; }
    public Version? MinimumSupportedIosAppVersion { get; set; }
    public Version? MinimumSupportedMacOSAppVersion { get; set; }
    public Version? MinimumSupportedWindowsAppVersion { get; set; }
    public Version? MinimumSupportedWebAppVersion { get; set; }

    public Version? GetMinimumSupportedAppVersion(AppPlatformType platformType)
    {
        return platformType switch
        {
            AppPlatformType.Android => MinimumSupportedAndroidAppVersion,
            AppPlatformType.Ios => MinimumSupportedIosAppVersion,
            AppPlatformType.MacOS => MinimumSupportedMacOSAppVersion,
            AppPlatformType.Windows => MinimumSupportedWindowsAppVersion,
            AppPlatformType.Web => MinimumSupportedWebAppVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(platformType), platformType, null)
        };
    }
}
```

**Example Scenario**:
If you set `"MinimumSupportedWebAppVersion": "2.0.0"`, any Web client running version 1.x.x will be blocked and forced to update.

---

### 4. Client Handles the Update Notification

When the server throws `ClientNotSupportedException`, the client catches it and triggers the force update flow.

#### Exception Handler

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/ExceptionDelegatingHandler.cs`](/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/ExceptionDelegatingHandler.cs)

```csharp
catch (ClientNotSupportedException)
{
    pubSubService.Publish(ClientPubSubMessages.FORCE_UPDATE, persistent: true);
    throw;
}
```

The exception handler publishes a `FORCE_UPDATE` message to the pub-sub service with `persistent: true`, ensuring the message is delivered even if no subscribers are currently listening.

#### Force Update Snack Bar

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor.cs)

```csharp
public partial class ForceUpdateSnackBar
{
    [AutoInject] private IAppUpdateService appUpdateService = default!;

    private bool isShown;
    private Action? unsubscribe;
    private BitSnackBar bitSnackBar = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (InPrerenderSession) return;

        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.FORCE_UPDATE, async (_) =>
        {
            if (isShown) return;

            isShown = true;
            await bitSnackBar.Error(string.Empty);
        });
    }

    private async Task Update()
    {
        await appUpdateService.ForceUpdate();
    }
}
```

**UI Component**: [`/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor)

```xml
@inherits AppComponentBase

<BitSnackBar @ref="bitSnackBar" AutoDismiss="false" Persistent>
    <TitleTemplate>
        <BitStack FitHeight>
            <BitText Typography="BitTypography.H5">
                @Localizer[nameof(AppStrings.ForceUpdateTitle)]
            </BitText>
        </BitStack>
    </TitleTemplate>
    <BodyTemplate>
        <BitStack>
            <BitText Typography="BitTypography.Body1">
                @Localizer[nameof(AppStrings.ForceUpdateBody)]
            </BitText>
            <BitButton Color="BitColor.Tertiary"
                       OnClick="WrapHandled(Update)"
                       IconName="@BitIconName.Download">
                @Localizer[nameof(AppStrings.Update)]
            </BitButton>
        </BitStack>
    </BodyTemplate>
</BitSnackBar>
```

When the user clicks the "Update" button, the platform-specific update service is triggered.

---

### 5. Platform-Specific Update Behavior

Different platforms handle updates differently based on their capabilities and app distribution models.

#### Web App Update (Progressive Web App)

**Location**: [`/src/Client/Boilerplate.Client.Web/Services/WebAppUpdateService.cs`](/src/Client/Boilerplate.Client.Web/Services/WebAppUpdateService.cs)

```csharp
public partial class WebAppUpdateService : IAppUpdateService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    public async Task ForceUpdate()
    {
        const bool autoReload = true;
        await jsRuntime.InvokeVoidAsync("App.tryUpdatePwa", autoReload);
    }
}
```

**Behavior**: 
- Triggers the PWA service worker update mechanism
- Automatically reloads the page with the new version
- No user download required - updates happen instantly

---

#### Windows App Update (Velopack)

**Location**: [`/src/Client/Boilerplate.Client.Windows/Services/WindowsAppUpdateService.cs`](/src/Client/Boilerplate.Client.Windows/Services/WindowsAppUpdateService.cs)

```csharp
public partial class WindowsAppUpdateService : IAppUpdateService
{
    [AutoInject] private ClientWindowsSettings settings = default!;

    public async Task ForceUpdate()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrEmpty(windowsUpdateSettings?.FilesUrl))
            return;
        windowsUpdateSettings.AutoReload = true; // Force update to reload the app after update
        await Update();
    }

    public async Task Update()
    {
        var windowsUpdateSettings = settings.WindowsUpdate;
        if (string.IsNullOrEmpty(windowsUpdateSettings?.FilesUrl))
            return;
        var updateManager = new UpdateManager(windowsUpdateSettings.FilesUrl);
        var updateInfo = await updateManager.CheckForUpdatesAsync();
        if (updateInfo is not null)
        {
            await updateManager.DownloadUpdatesAsync(updateInfo);
            if (windowsUpdateSettings.AutoReload)
            {
                updateManager.ApplyUpdatesAndRestart(updateInfo, Environment.GetCommandLineArgs());
            }
        }
    }
}
```

**Behavior**:
- Uses Velopack to download and install updates
- Automatically restarts the application after update
- Updates are downloaded from a configured URL

---

#### Mobile App Update (Android/iOS/macOS)

**Location**: [`/src/Client/Boilerplate.Client.Maui/Services/MauiAppUpdateService.cs`](/src/Client/Boilerplate.Client.Maui/Services/MauiAppUpdateService.cs)

```csharp
public partial class MauiAppUpdateService : IAppUpdateService
{
    public async Task ForceUpdate()
    {
        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
    }
}
```

**Behavior**:
- Opens the app's page in the platform's app store:
  - **Android**: Google Play Store
  - **iOS**: Apple App Store
  - **macOS**: Mac App Store
- User must manually download and install the update from the store

---

## SignalR Support

The Force Update system also works with SignalR connections!

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs`](/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs)

```csharp
if (exception is HubException)
{
    if (exception.Message.EndsWith(nameof(AppStrings.UnauthorizedException)))
    {
        await AuthManager.RefreshToken(requestedBy: nameof(HubException));
    }
    else if (exception.Message.EndsWith(nameof(AppStrings.ForceUpdateTitle)))
    {
        PubSubService.Publish(ClientPubSubMessages.FORCE_UPDATE);
    }
}
```

When a SignalR hub method is called from an outdated client, the server throws `ClientNotSupportedException`, which gets translated into a `HubException` on the client side. The client detects this and triggers the force update flow.

---

## Key Differences from Traditional Update Systems

### ⚠️ CRITICAL: Version Check on EVERY Request

Most applications check the version only at startup. This system is different:

| Traditional Systems | This Force Update System |
|---------------------|--------------------------|
| Check version at app launch | Check version on **every API request** |
| User can continue using outdated version | User is **immediately blocked** |

**Example Scenario**:
1. User is actively using version 1.0.0
2. Admin updates `MinimumSupportedWebAppVersion` to 1.1.0 on the server
3. User clicks "Save" on a form
4. Request is sent to server with version 1.0.0
5. Server rejects the request with `ClientNotSupportedException`
6. User sees the Force Update snackbar **immediately**
7. User **cannot continue** until they update

This ensures that breaking API changes or critical security updates are enforced instantly across all active users.

---

## The IAppUpdateService Interface

All platform-specific update services implement this simple interface:

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/Contracts/IAppUpdateService.cs`](/src/Client/Boilerplate.Client.Core/Services/Contracts/IAppUpdateService.cs)

```csharp
public interface IAppUpdateService
{
    Task ForceUpdate();
}
```

This abstraction allows the force update logic to work seamlessly across all platforms without knowing the platform-specific implementation details.

---

## How to Use This System in Production

### 1. Deploy a New Version

When you release a breaking change or critical security update:

1. **Build and deploy** the new client versions (Web, Windows, Android, iOS, macOS)
2. **Update the server** with the new backend code
3. **Update `appsettings.json`** to set the new minimum version:

```json
{
  "SupportedAppVersions": {
    "MinimumSupportedAndroidAppVersion": "2.0.0",
    "MinimumSupportedIosAppVersion": "2.0.0",
    "MinimumSupportedMacOSAppVersion": "2.0.0",
    "MinimumSupportedWindowsAppVersion": "2.0.0",
    "MinimumSupportedWebAppVersion": "2.0.0"
  }
}
```

### 2. Gradual Rollout (Optional)

You can enforce updates on specific platforms while allowing others to continue:

```json
{
  "SupportedAppVersions": {
    "MinimumSupportedAndroidAppVersion": "2.0.0",  // ← Force Android to update
    "MinimumSupportedIosAppVersion": "2.0.0",      // ← Force iOS to update
    "MinimumSupportedMacOSAppVersion": "1.9.0",    // ← macOS can still use 1.9.x
    "MinimumSupportedWindowsAppVersion": "1.9.0",  // ← Windows can still use 1.9.x
    "MinimumSupportedWebAppVersion": "2.0.0"       // ← Force Web to update
  }
}
```

### 3. Monitor User Sessions

You can track which versions users are running by examining the user sessions table (if session tracking is enabled). This helps you decide when to enforce version updates.

---

## Best Practices

### ✅ DO

- **Update the minimum version** only after you've deployed the new client versions to app stores/update servers
- **Test the update flow** in staging before updating production settings
- **Use semantic versioning** (e.g., 1.0.0, 1.1.0, 2.0.0) for clarity
- **Communicate updates** to users through release notes or notifications
- **Monitor metrics** to ensure users are successfully updating

### ❌ DON'T

- **Don't set** a minimum version higher than what's available in app stores
- **Don't update** minimum versions too frequently - reserve this for breaking changes or critical security updates
- **Don't forget** to publish mobile app updates to stores before updating the minimum version
- **Don't use** non-standard version formats (stick to `major.minor.patch`)

---

## Troubleshooting

### Issue: Users stuck in update loop

**Symptom**: Users keep seeing the force update message even after updating.

**Common Causes**:
1. The new version wasn't properly deployed
2. Users are behind a caching proxy
3. Service worker isn't updating (Web apps)

**Solution**:
- For Web: Clear service worker cache
- For Windows: Verify the update files are accessible at the configured URL
- For Mobile: Confirm the new version is live in the app store

### Issue: Some platforms can't update

**Symptom**: Android users can't update but iOS users can.

**Common Causes**:
1. Platform-specific build failed
2. App store review pending

**Solution**:
- Temporarily lower the minimum version for that platform
- Fix the build/review issue
- Re-raise the minimum version once the update is available

---

## Summary

The Force Update System is a powerful mechanism that:

✅ **Validates client versions** on every request (not just at startup)  
✅ **Enforces immediate updates** when minimum version requirements change  
✅ **Works across all platforms** with platform-specific update strategies  
✅ **Integrates with both HTTP and SignalR** communication  
✅ **Prevents API incompatibilities** by blocking outdated clients  

This system gives you complete control over client version enforcement, ensuring that critical updates and breaking changes are adopted immediately by all users.

---

## Related Files Reference

Here's a quick reference to all the key files in the Force Update System:

### Server-Side
- [`/src/Server/Boilerplate.Server.Api/RequestPipeline/ForceUpdateMiddleware.cs`](/src/Server/Boilerplate.Server.Api/RequestPipeline/ForceUpdateMiddleware.cs) - Main middleware
- [`/src/Server/Boilerplate.Server.Api/ServerApiSettings.cs`](/src/Server/Boilerplate.Server.Api/ServerApiSettings.cs) - Settings classes
- [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json) - Version configuration

### Client-Side - Core
- [`/src/Client/Boilerplate.Client.Core/Services/Contracts/IAppUpdateService.cs`](/src/Client/Boilerplate.Client.Core/Services/Contracts/IAppUpdateService.cs) - Interface
- [`/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/RequestHeadersDelegatingHandler.cs`](/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/RequestHeadersDelegatingHandler.cs) - Version header injection
- [`/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/ExceptionDelegatingHandler.cs`](/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/ExceptionDelegatingHandler.cs) - Exception handling
- [`/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor) - UI component
- [`/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs`](/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs) - SignalR support

### Client-Side - Platform Implementations
- [`/src/Client/Boilerplate.Client.Web/Services/WebAppUpdateService.cs`](/src/Client/Boilerplate.Client.Web/Services/WebAppUpdateService.cs) - Web/PWA
- [`/src/Client/Boilerplate.Client.Windows/Services/WindowsAppUpdateService.cs`](/src/Client/Boilerplate.Client.Windows/Services/WindowsAppUpdateService.cs) - Windows
- [`/src/Client/Boilerplate.Client.Maui/Services/MauiAppUpdateService.cs`](/src/Client/Boilerplate.Client.Maui/Services/MauiAppUpdateService.cs) - Android/iOS/macOS

### Shared
- [`/src/Shared/Exceptions/ClientNotSupportedException.cs`](/src/Shared/Exceptions/ClientNotSupportedException.cs) - Exception class

---
