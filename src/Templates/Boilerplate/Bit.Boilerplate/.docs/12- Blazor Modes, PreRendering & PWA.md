# Stage 12: Blazor Modes, PreRendering & PWA

Welcome to Stage 12! In this stage, we'll explore Blazor rendering modes, pre-rendering functionality, and Progressive Web App (PWA) features in the project.

## Topics Covered

- App.razor and index.html Files
- Blazor Mode & PreRendering Configuration
- PWA & Service Workers
- IPrerenderStateService

---

## 1. App.razor and index.html Files

The project uses different files depending on the hosting model:

### Key Files

1. **Server-side App.razor** (for Blazor Server and Auto modes):
   - **Location**: [`/src/Server/Boilerplate.Server.Web/Components/App.razor`](/src/Server/Boilerplate.Server.Web/Components/App.razor)
   - This is the main entry point when the app runs in **Blazor Server** or **Blazor Auto** mode
   - Contains the HTML structure, render mode configuration, and prerendering logic

2. **Client.Web index.html** (for standalone WebAssembly):
   - **Location**: [`/src/Client/Boilerplate.Client.Web/wwwroot/index.html`](/src/Client/Boilerplate.Client.Web/wwwroot/index.html)
   - This is used when the app runs in **Blazor WebAssembly** mode as a standalone app
   - Contains the static HTML structure and loading indicators

### Important Consistency Rule

**⚠️ CRITICAL**: Changes to `App.razor` usually need similar changes in both `index.html` files.

**Why?** Both files serve as the entry point for the application:
- `App.razor` is rendered on the server (Server/Auto modes)
- `index.html` is used for client-side startup (WebAssembly mode)

If you add a new CSS file, JavaScript library, or meta tag to one, you typically need to add it to the other to ensure consistent behavior across all rendering modes.

### Example from App.razor

```aspnetcorerazor
@{
    var noPrerender = HttpContext.Request.Query["no-prerender"].Count > 0;
    var renderMode = noPrerender ? noPrerenderBlazorWebAssembly : serverWebSettings.WebAppRender.RenderMode;
    if (HttpContext.AcceptsInteractiveRouting() is false)
    {
        // Static SSR mode
        renderMode = null;
    }
}

<!DOCTYPE html>
<html bit-theme="dark">
<head>
    <base href="/" />
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
    <Link rel="stylesheet" Href="_content/Bit.BlazorUI/styles/bit.blazorui.css" />
    <Link rel="stylesheet" Href="_content/Boilerplate.Client.Core/styles/app.css" />
    <!-- More resources... -->
</head>
<body>
    <Routes @rendermode=renderMode />
    <Script Src="_framework/bit.blazor.web.es2019.js" autostart="false"></Script>
    <Script Src="_content/Bit.Bswup/bit-bswup.js"></Script>
    <!-- More scripts... -->
</body>
</html>
```

---

## 2. Blazor Mode & PreRendering Configuration

### Configuration Location

**File**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
{
  "WebAppRender": {
    "BlazorMode": "BlazorServer",
    "BlazorMode_Comment": "BlazorServer, BlazorWebAssembly and BlazorAuto.",
    "PrerenderEnabled": false,
    "PrerenderEnabled_Comment": "for apps with Prerender enabled, follow the instructions in the Client.Web/wwwroot/service-worker.published.js file"
  }
}
```

### Available Blazor Modes

1. **BlazorServer**
   - Runs entirely on the server
   - Real-time SignalR connection between client and server
   - Smaller initial download size
   - Requires persistent server connection

2. **BlazorWebAssembly**
   - Runs entirely in the browser using WebAssembly
   - Offline-capable after initial load
   - Larger initial download
   - No server connection needed after startup

3. **BlazorAuto**
   - Starts with Blazor Server for fast initial load
   - Automatically transitions to WebAssembly in the background
   - Best of both worlds: fast startup + offline capability

### PreRendering Configuration

**`PrerenderEnabled`** controls whether pages are pre-rendered on the server before being sent to the client.

#### When `PrerenderEnabled: true`

**Benefits:**
- ✅ **Faster perceived load time**: Users see content immediately
- ✅ **Better SEO**: Search engines can crawl the pre-rendered HTML
- ✅ **Improved user experience**: No blank screen during Blazor startup

**Trade-offs:**
- Components run twice: once on the server (pre-render), once on the client (interactive)
- Need to handle pre-render state properly

#### When `PrerenderEnabled: false`

**Benefits:**
- ✅ **Simpler lifecycle**: Components run only once
- ✅ **Easier debugging**: No pre-render/hydration complexity

**Trade-offs:**
- Users see a loading screen during Blazor startup
- Poorer SEO (search engines see loading spinner)

### Important: Service Worker Configuration

**⚠️ If you enable PreRendering**, you must update the service worker configuration in:

**File**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)

Change the `self.mode` setting to match your pre-rendering choice (explained in the next section).

---

## 3. PWA & Service Workers

### All Modes are PWAs

The project is a **Progressive Web App (PWA)** in all rendering modes:
- ✅ Blazor Server
- ✅ Blazor WebAssembly
- ✅ Blazor Auto
- ✅ Blazor Hybrid

**PWA Features Available:**
- 📦 **Installable**: Users can install the app on their device
- 🔔 **Push Notifications**: Send notifications even when the app is closed
- 📴 **Offline Mode**: Continue working without internet connection
- 🔄 **Background Sync**: Sync data when connection is restored

### Service Worker Files

The service worker is the heart of PWA functionality.

#### Development Service Worker
**File**: `/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.js` (not in the repo)
- Used during development
- Automatically generated

#### Production Service Worker
**File**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)
- Used in production/staging environments
- Manually configured

### Service Worker Modes

The service worker has different **modes** that control caching behavior.

#### Mode 1: `NoPrerender` (Default)

```javascript
self.mode = 'NoPrerender';
```

**Behavior:**
- ✅ **Starts immediately**: App launches instantly
- ✅ **Lazy-loads assets**: Downloads JS/WASM/images as needed
- ❌ **Can break offline**: If network is lost while navigating to a new page that requires lazy-loaded files

**Recommended for:**
- Apps using PWA for installability and push notifications (not primarily for offline support)
- Apps with good network connectivity

**Live Demo**: [https://adminpanel.bitplatform.dev/](https://adminpanel.bitplatform.dev/)

#### Mode 2: `FullOffline`

```javascript
self.mode = 'FullOffline';
```

**Behavior:**
- 📥 **Downloads all assets first**: Complete app download before launch
- ✅ **True offline mode**: App never breaks due to missing files
- ❌ **Slower startup**: Users wait for full download

**Recommended for:**
- Apps prioritizing offline support
- Apps with local databases (IndexedDB, SQLite/Bit.Besql)

**Live Demo**: [https://todo-offline.bitplatform.cc/offline-database-demo](https://todo-offline.bitplatform.cc/offline-database-demo)

#### Mode 3: `InitialPrerender`

```javascript
self.mode = 'InitialPrerender';
```

**Behavior:**
- 📄 **Fetches pre-rendered HTML on first load only**: Shows content while Blazor loads
- ⚡ **Subsequent loads skip pre-rendering**: Reduces server load

**Recommended for:**
- SEO-friendly apps
- Apps where first impression matters

**Requirements:**
- ✅ `PrerenderEnabled: true` in `appsettings.json`

**Live Demo**: [https://todo.bitplatform.dev/](https://todo.bitplatform.dev/)

#### Mode 4: `AlwaysPrerender`

```javascript
self.mode = 'AlwaysPrerender';
```

**Behavior:**
- 📄 **Fetches pre-rendered HTML on every load**: Always shows content immediately
- 🔄 **Continuous pre-rendering**: Even on page refresh or navigation

**Recommended for:**
- Apps targeting low-end mobile devices where Blazor runtime takes time to start
- Apps prioritizing perceived performance over server load

**Trade-offs:**
- ⚠️ **Increases server load**: Every page load pre-renders on the server

**Requirements:**
- ✅ `PrerenderEnabled: true` in `appsettings.json`

**Live Demo**: [https://sales.bitplatform.dev/](https://sales.bitplatform.dev/)

### Service Worker Configuration Details

Let's examine the key configuration options in `service-worker.published.js`:

```javascript
// Assets to include/exclude from caching
self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,
    /pdfjs-4\.7\.76\.js$/,
    /pdfjs-4\.7\.76-worker\.js$/,
    /chartjs-2\.9\.4\.js$/,
    /quill-2\.0\.3\.js$/,
    /_content\/Bit\.BlazorUI\.Extras\/flags/ // country flags
];

// External assets to cache (outside _framework folder)
self.externalAssets = [
    { "url": "/" },
    { "url": "_framework/bit.blazor.web.es2019.js" },
    { "url": "Boilerplate.Server.Web.styles.css" },
    { "url": "Boilerplate.Client.Web.bundle.scp.css" }
];

// URLs that should always go to the server (not cached)
self.serverHandledUrls = [
    /\/api\//,
    /\/odata\//,
    /\/core\//,
    /\/hangfire/,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/swagger/,
    /\/signin-/,
    /\/.well-known/,
    /\/sitemap.xml/
];

// Cache control mode
self.enableCacheControl = false; 
// false = respect origin's cache headers
// true = service worker manages cache headers
```

**Key Configuration Options:**

1. **`assetsExclude`**: Files you don't want cached (e.g., unused themes, PDF viewer, charts)
2. **`externalAssets`**: Files outside `_framework` that need caching
3. **`serverHandledUrls`**: URLs that bypass the service worker and always hit the server
4. **`enableCacheControl`**: Whether to respect server cache headers or override them

### Push Notifications in Service Worker

If push notifications are enabled in the project, the service worker handles them:

```javascript
self.addEventListener('push', function (event) {
    const eventData = event.data.json();
    self.registration.showNotification(eventData.title, {
        data: eventData.data,
        body: eventData.message,
        icon: '/images/icons/bit-icon-512.png'
    });
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const pageUrl = event.notification.data.pageUrl;
    if (pageUrl != null) {
        // Navigate to the page or focus existing tab
        event.waitUntil(
            clients.matchAll({ type: 'window' }).then((clientList) => {
                for (const client of clientList) {
                    if (client.url === pageUrl && 'focus' in client) {
                        return client.focus();
                    }
                }
                return clients.openWindow(pageUrl);
            })
        );
    }
});
```

---

## 4. IPrerenderStateService

### What is IPrerenderStateService?

`IPrerenderStateService` is a service that prevents **duplicate API calls** during pre-rendering.

**Problem it solves:**

When pre-rendering is enabled, component lifecycle runs **twice**:
1. **On the server** (pre-render): Generates HTML
2. **On the client** (hydration): Makes the page interactive

Without `IPrerenderStateService`, API calls would execute twice—wasting bandwidth and server resources.

### When to Use It

#### ✅ **You NEED it when:**
- Making direct `HttpClient` calls (not using `IAppController` interfaces)
- Pre-rendering is enabled (`PrerenderEnabled: true`)

#### ❌ **You DON'T need it when:**
- Using `IAppController` interfaces (pre-render state is handled automatically)
- Pre-rendering is disabled

### How It Works

**Interface Definition** ([`/src/Shared/Services/Contracts/IPrerenderStateService.cs`](/src/Shared/Services/Contracts/IPrerenderStateService.cs)):

```csharp
public interface IPrerenderStateService : IAsyncDisposable
{
    Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "");

    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
```

### Implementations

The project has **three implementations** of `IPrerenderStateService`:

#### 1. WebServerPrerenderStateService

**Location**: [`/src/Server/Boilerplate.Server.Web/Services/WebServerPrerenderStateService.cs`](/src/Server/Boilerplate.Server.Web/Services/WebServerPrerenderStateService.cs)

**Purpose**: Used during **server-side pre-rendering** to persist data

**Key Features:**
- Stores data in `PersistentComponentState`
- Data is serialized to JSON and embedded in the HTML
- Automatically generates unique keys using `[CallerLineNumber]`, `[CallerMemberName]`, and `[CallerFilePath]`

**Code Snippet:**

```csharp
public partial class WebServerPrerenderStateService : IPrerenderStateService
{
    private readonly PersistentComponentState? persistentComponentState;
    private readonly ConcurrentDictionary<string, object?> values = new();

    public async Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        if (NoPersistent) return await factory();

        string key = $"{filePath.Split('\\').LastOrDefault()} {memberName} {lineNumber}";
        
        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) 
            return value;

        var result = await factory();
        Persist(key, result);
        return result;
    }

    void Persist<T>(string key, T value)
    {
        values.AddOrUpdate(key, value, (_, _) => value);
    }
}
```

#### 2. WebClientPrerenderStateService

**Location**: [`/src/Client/Boilerplate.Client.Web/Services/WebClientPrerenderStateService.cs`](/src/Client/Boilerplate.Client.Web/Services/WebClientPrerenderStateService.cs)

**Purpose**: Used during **client-side hydration** to retrieve persisted data

**Key Features:**
- Reads data from `PersistentComponentState` (embedded in HTML by server)
- Returns cached data if available
- Falls back to executing the factory function if data not found

**Code Snippet:**

```csharp
public partial class WebClientPrerenderStateService : IPrerenderStateService
{
    private readonly PersistentComponentState? persistentComponentState;

    public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        if (persistentComponentState!.TryTakeFromJson(key, out T? value)) 
            return value;

        var result = await factory();
        return result;
    }
}
```

#### 3. NoOpPrerenderStateService

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/NoOpPrerenderStateService.cs`](/src/Client/Boilerplate.Client.Core/Services/NoOpPrerenderStateService.cs)

**Purpose**: Used when **pre-rendering is disabled** (e.g., Blazor Hybrid, WebAssembly standalone)

**Key Features:**
- Simply executes the factory function
- No state persistence
- Zero overhead

**Code Snippet:**

```csharp
public class NoOpPrerenderStateService : IPrerenderStateService
{
    public Task<T?> GetValue<T>(Func<Task<T?>> factory, 
        [CallerLineNumber] int lineNumber = 0, 
        [CallerMemberName] string memberName = "", 
        [CallerFilePath] string filePath = "")
    {
        return factory();
    }
}
```

### Usage Example

**Scenario**: Fetching user settings in a component

**Without IPrerenderStateService** (makes API call twice):

```csharp
protected override async Task OnInitAsync()
{
    // Called on server (pre-render) and client (hydration)
    // Results in 2 API calls!
    settings = await HttpClient.GetFromJsonAsync<SettingsDto>("api/settings");
}
```

**With IPrerenderStateService** (makes API call once):

```csharp
[AutoInject] protected IPrerenderStateService PrerenderStateService = default!;

protected override async Task OnInitAsync()
{
    // Called on server (pre-render) and client (hydration)
    // But API call happens only once, data is persisted!
    settings = await PrerenderStateService.GetValue(() => 
        HttpClient.GetFromJsonAsync<SettingsDto>("api/settings")
    );
}
```

**What Happens:**

1. **Server-side (pre-render)**:
   - `WebServerPrerenderStateService.GetValue()` is called
   - Executes the factory function (makes API call)
   - Stores result in `PersistentComponentState`
   - Result is embedded in HTML as JSON

2. **Client-side (hydration)**:
   - `WebClientPrerenderStateService.GetValue()` is called
   - Checks `PersistentComponentState` for cached data
   - Finds the data, returns it **without making API call**

### Automatic Handling with IAppController

**Good News**: If you use `IAppController` interfaces for API calls, pre-render state is handled **automatically**.

**Example** (no manual IPrerenderStateService needed):

```csharp
[AutoInject] protected ISettingsController SettingsController = default!;

protected override async Task OnInitAsync()
{
    // IAppController handles pre-render state automatically!
    settings = await SettingsController.GetSettings(CurrentCancellationToken);
}
```

The HTTP client infrastructure automatically uses `IPrerenderStateService` under the hood.

---

## Summary

In this stage, you learned:

✅ **App.razor and index.html**: Entry points for different Blazor modes, and the importance of keeping them synchronized

✅ **Blazor Modes**: BlazorServer, BlazorWebAssembly, and BlazorAuto, each with different trade-offs

✅ **PreRendering**: Benefits for SEO and perceived performance, and how to configure it

✅ **PWA & Service Workers**: Different caching modes (`NoPrerender`, `FullOffline`, `InitialPrerender`, `AlwaysPrerender`) and their use cases

✅ **IPrerenderStateService**: Prevents duplicate API calls during pre-rendering, with automatic handling via `IAppController`

---

## Quick Reference

### Configuration Matrix

| Setting | Location | Options |
|---------|----------|---------|
| **Blazor Mode** | `/src/Server/Boilerplate.Server.Api/appsettings.json` | `BlazorServer`, `BlazorWebAssembly`, `BlazorAuto` |
| **PreRendering** | `/src/Server/Boilerplate.Server.Api/appsettings.json` | `true` (SEO, faster load) or `false` (simpler) |
| **Service Worker Mode** | `/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js` | `NoPrerender`, `FullOffline`, `InitialPrerender`, `AlwaysPrerender` |

### Best Practices

1. **Keep App.razor and index.html in sync**: When adding CSS/JS, update both
2. **Match service worker mode to pre-render settings**: Use `InitialPrerender` or `AlwaysPrerender` mode if `PrerenderEnabled: true`
3. **Use IAppController for API calls**: Avoids manual pre-render state management
4. **Choose the right service worker mode**: Consider your offline requirements and server load
5. **Test in all modes**: Behavior differs between BlazorServer, WebAssembly, and Auto

---