# Stage 13: Force Update System

Welcome to **Stage 13** of the Boilerplate project getting started guide! In this stage, we'll explore the **Force Update System** - a critical feature that ensures all users are running compatible versions of your application.

---

## Overview

The Force Update System is designed to maintain version compatibility between the client applications and the backend server. When you deploy a breaking change to your API, you need a way to ensure that older client versions stop working and prompt users to update.

**Key Concept**: Unlike typical version checks that happen only at app startup, this system validates the client version **on every single request**. This means even users who are actively using the app will be forced to update if they fall below the minimum supported version.

---

## How It Works: Architecture Overview

The Force Update System consists of four main components:

1. **Client-Side Version Header**: Every HTTP request includes client-app version information
2. **Server-Side Middleware**: Validates the version and throws an exception if unsupported
3. **Exception Handling**: Catches the exception and publishes a force update message
4. **Platform-Specific Update Logic**: Each platform handles the update differently

Let's explore each component in detail.

---

## 1. Client-Side: Sending Version Headers

Every HTTP request from the client automatically includes two critical headers:
- `X-App-Version`: The current application version (e.g., "1.2.0")
- `X-App-Platform`: The platform type (Android, iOS, Windows, Web, macOS)

This happens in the **`RequestHeadersDelegatingHandler`** class:

**File**: [`src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/RequestHeadersDelegatingHandler.cs`](/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/RequestHeadersDelegatingHandler.cs)

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

**Important Notes**:
- These headers are **only added for internal API calls** (calls to your own backend)
- External API calls don't include these headers to avoid CORS issues
- This applies to **both HTTP requests and SignalR connections** (SignalR uses `HttpMessageHandlerFactory` for negotiation)

### SignalR Integration

SignalR connections also benefit from this system because they use the same `HttpMessageHandlerFactory`:

**File**: [`src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs)

```csharp
var hubConnection = new HubConnectionBuilder()
    .WithUrl(new Uri(absoluteServerAddressProvider.GetAddress(), "app-hub"), options =>
    {
        options.HttpMessageHandlerFactory = httpClientHandler => 
            sp.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(httpClientHandler);
        // ... other code ...
    })
    .Build();
```

This ensures that even SignalR connections are version-validated.

---

## 2. Server-Side: Version Validation Middleware

On the server, the **`ForceUpdateMiddleware`** intercepts every request and validates the client version.

**File**: [`src/Server/Boilerplate.Server.Api/RequestPipeline/ForceUpdateMiddleware.cs`](/src/Server/Boilerplate.Server.Api/RequestPipeline/ForceUpdateMiddleware.cs)

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

### How It Works:

1. Extracts the `X-App-Version` and `X-App-Platform` headers
2. Gets the minimum supported version for that platform from configuration
3. Compares the client's version with the minimum supported version
4. If the client version is **less than** the minimum, throws a `ClientNotSupportedException`
5. Otherwise, allows the request to proceed

### Configuration

The minimum supported versions are configured in **appsettings.json**:

**File**: [`src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
"SupportedAppVersions": {
    "MinimumSupportedAndroidAppVersion": "1.0.0",
    "MinimumSupportedIosAppVersion": "1.0.0",
    "MinimumSupportedMacOSAppVersion": "1.0.0",
    "MinimumSupportedWindowsAppVersion": "1.0.0",
    "MinimumSupportedWebAppVersion": "1.0.0"
}
```

**When to Update These Values**:
- When you deploy a breaking API change
- When you need to force users to update for security reasons
- When you want to discontinue support for old versions

### Settings Class

The configuration is strongly-typed in the **`ServerApiSettings`** class:

**File**: [`src/Server/Boilerplate.Server.Api/ServerApiSettings.cs`](/src/Server/Boilerplate.Server.Api/ServerApiSettings.cs)

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
            _ => throw new ArgumentOutOfRangeException(nameof(platformType))
        };
    }
}
```

---

## 3. Exception Handling: Triggering the Update Flow

When `ClientNotSupportedException` is thrown, it follows a special handling path.

### The Exception Class

**File**: [`src/Shared/Exceptions/ClientNotSupportedException.cs`](/src/Shared/Exceptions/ClientNotSupportedException.cs)

```csharp
public partial class ClientNotSupportedException : BadRequestException
{
    public ClientNotSupportedException()
        : this(nameof(AppStrings.ForceUpdateTitle))
    {
    }
    // ... other constructors ...
}
```

### Client-Side Exception Handling

When the client receives this exception, it's caught in the **`ExceptionDelegatingHandler`**:

**File**: [`src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/ExceptionDelegatingHandler.cs`](/src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/ExceptionDelegatingHandler.cs)

```csharp
catch (ClientNotSupportedException)
{
    pubSubService.Publish(ClientAppMessages.FORCE_UPDATE, persistent: true);
    throw;
}
```

**Key Point**: The exception is **published as a persistent message** using the PubSub system. This ensures that the force update UI is shown to the user.

### Exception Logging

The `ClientNotSupportedException` is explicitly ignored from logging to prevent noise:

**File**: [`src/Shared/Services/SharedExceptionHandler.cs`](/src/Shared/Services/SharedExceptionHandler.cs)

```csharp
public virtual bool IgnoreException(Exception exception)
{
    if (exception is ClientNotSupportedException)
        return true; // See ExceptionDelegatingHandler

    // ... other code ...
}
```

---

## 4. Platform-Specific Update Logic

Each platform implements the **`IAppUpdateService`** interface to handle updates differently.

### Interface Definition

**File**: [`src/Client/Boilerplate.Client.Core/Services/Contracts/IAppUpdateService.cs`](/src/Client/Boilerplate.Client.Core/Services/Contracts/IAppUpdateService.cs)

```csharp
public interface IAppUpdateService
{
    Task ForceUpdate();
}
```

### Web Platform: Auto-Update via Service Worker

**File**: [`src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebAppUpdateService.cs`](/src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebAppUpdateService.cs)

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

**How it works**:
- Calls a JavaScript function that triggers the service worker to update
- The `autoReload = true` parameter forces the page to reload after the update is downloaded
- The new version is fetched from the server and cached by the service worker
- The page automatically reloads with the new version

### Windows Platform: Auto-Update via Velopack

**File**: [`src/Client/Boilerplate.Client.Windows/Services/WindowsAppUpdateService.cs`](/src/Client/Boilerplate.Client.Windows/Services/WindowsAppUpdateService.cs)

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

**How it works**:
- Uses **Velopack** to check for updates from a configured URL
- Downloads the new version in the background
- Automatically restarts the application with the new version

### Android, iOS, macOS: Open App Store

**File**: [`src/Client/Boilerplate.Client.Maui/Services/MauiAppUpdateService.cs`](/src/Client/Boilerplate.Client.Maui/Services/MauiAppUpdateService.cs)

```csharp
public partial class MauiAppUpdateService : IAppUpdateService
{
    public async Task ForceUpdate()
    {
        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
    }
}
```

**How it works**:
- Opens the corresponding app store (Google Play Store, Apple App Store, or Mac App Store)
- Navigates directly to your app's page
- The user must manually download and install the update

---

## 5. User Interface: Force Update Snackbar

When the force update message is published, a snackbar is shown to the user.

### Snackbar Component

**File**: [`src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor)

```xml
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

### Snackbar Code-Behind

**File**: [`src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/ForceUpdateSnackBar.razor.cs)

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

        unsubscribe = PubSubService.Subscribe(ClientAppMessages.FORCE_UPDATE, async (_) =>
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

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();
        await base.DisposeAsync(disposing);
    }
}
```

**Key Features**:
- Subscribes to the `FORCE_UPDATE` pub-sub message
- Shows the snackbar when the message is received
- Prevents multiple snackbars from showing (`isShown` flag)
- The snackbar is persistent and cannot be dismissed by the user
- Clicking the "Update" button calls the platform-specific update logic

---

## The Critical Difference: Always-On Validation

**🚨 Most Important Concept**: This system validates the client version **on every single request**, not just at app startup.

### Why This Matters

Imagine this scenario:

1. A user opens your app at 9:00 AM with version 1.0.0
2. At 10:00 AM, you deploy a breaking change and set `MinimumSupportedWebAppVersion` to 1.1.0
3. The user is still actively using the app at 10:05 AM

**What happens?**
- The very next API request the user makes will fail with `ClientNotSupportedException`
- The force update snackbar will appear immediately
- The user **must** update to continue using the app

**Traditional approach** (validation only at startup):
- The user could continue using version 1.0.0 for hours or days
- They would only see the update prompt when they restart the app
- This could lead to data corruption or errors if the API has changed

---

## Configuration Best Practices

### When to Increment Minimum Versions

✅ **DO increment when**:
- You change API response/request DTOs in a breaking way
- You remove or rename endpoints
- You change authentication/authorization logic
- You fix critical security vulnerabilities
- You migrate the database schema in an incompatible way

❌ **DON'T increment when**:
- You add new optional fields to DTOs
- You add new endpoints (without removing old ones)
- You fix bugs that don't affect the API contract
- You change UI-only code

### Version Number Strategy

Use semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (force update required)
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

Example:
```json
"SupportedAppVersions": {
    "MinimumSupportedWebAppVersion": "2.0.0",  // Major version change
    "MinimumSupportedAndroidAppVersion": "1.5.0", // Android can lag behind
    "MinimumSupportedIosAppVersion": "1.5.0"     // iOS can lag behind
}
```

---

## Advanced Scenarios

### Gradual Rollout

You can set different minimum versions for different platforms to enable gradual rollouts:

```json
"SupportedAppVersions": {
    "MinimumSupportedWebAppVersion": "2.0.0",      // Already rolled out
    "MinimumSupportedAndroidAppVersion": "1.5.0",  // Still on old version
    "MinimumSupportedIosAppVersion": "1.5.0"       // Still on old version
}
```

This allows you to:
1. Deploy breaking changes to the web app first
2. Test with web users before forcing mobile users to update
3. Give mobile users more time to update (app store approval delays)

---