# Stage 12: Blazor Modes, PreRendering & PWA

Welcome to Stage 12 of the Boilerplate project tutorial! In this stage, you'll learn about Blazor rendering modes, pre-rendering capabilities, Progressive Web App (PWA) features, and how they all work together in this project.

---

## Table of Contents
1. [App.razor and index.html Files](#apprazor-and-indexhtml-files)
2. [Blazor Mode & PreRendering Configuration](#blazor-mode--prerendering-configuration)
3. [PWA & Service Workers](#pwa--service-workers)
4. [IPrerenderStateService](#iprerendererstateservice)
5. [Summary](#summary)

---

## App.razor and index.html Files

The project uses different files depending on the hosting model:

### Key Files

1. **[`/src/Server/Boilerplate.Server.Web/Components/App.razor`](/src/Server/Boilerplate.Server.Web/Components/App.razor)** - The main entry point for Blazor Server and SSR (Server-Side Rendering) scenarios
2. **[`/src/Client/Boilerplate.Client.Web/wwwroot/index.html`](/src/Client/Boilerplate.Client.Web/wwwroot/index.html)** - The entry point for Blazor WebAssembly standalone mode
3. **[`/src/Client/Boilerplate.Client.Maui/wwwroot/index.html`](/src/Client/Boilerplate.Client.Maui/wwwroot/index.html)** - The entry point for Blazor Hybrid / MAUI

### Important: Synchronization Required

⚠️ **Changes to `App.razor` usually need similar changes in the `index.html` files.**

When you modify the structure, scripts, stylesheets, or metadata in `App.razor`, you should mirror those changes in `index.html` to ensure consistent behavior across both hosting models.

**Example scenarios requiring synchronization:**
- Adding a new CSS file or JavaScript library
- Updating meta tags or favicon
- Adding new scripts for third-party integrations
- Modifying the app container structure

Let's look at key similarities between these files:

**Both files include:**
- Meta tags for viewport and theme
- Preconnect links for performance optimization
- Favicon and app icons
- Bit.BlazorUI stylesheets
- Service worker scripts (Bit.Bswup)
- Application Insights initialization
- Manifest for PWA support

---

## Blazor Mode & PreRendering Configuration

The project supports multiple Blazor hosting models, all configured in a single location.

### Configuration Location

**File**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
"WebAppRender": {
    "BlazorMode": "BlazorServer",
    "BlazorMode_Comment": "BlazorServer, BlazorWebAssembly and BlazorAuto.",
    "PrerenderEnabled": false,
    "PrerenderEnabled_Comment": "for apps with Prerender enabled, follow the instructions in the Client.Web/wwwroot/service-worker.published.js file"
}
```

### Available Blazor Modes

1. **BlazorServer** - Runs on the server with SignalR connection to the client
   - **Pros**: Small initial download, server-side processing, immediate load
   - **Cons**: Requires persistent connection, higher server load, network latency

2. **BlazorWebAssembly** - Downloads and runs entirely in the browser
   - **Pros**: Offline support, reduced server load, rich client-side experience
   - **Cons**: Larger initial download, browser compatibility requirements

3. **BlazorAuto** - Starts with Server mode, then switches to WebAssembly after download
   - **Pros**: Fast initial load (server), then offline support (WASM)
   - **Cons**: More complex, requires both server and client resources

### PreRendering: Faster Load & Better SEO

**PreRendering** means the server generates the initial HTML content before sending it to the browser. This provides two major benefits:

1. **Faster Perceived Load Time**: Users see content immediately, even while Blazor is initializing
2. **Better SEO**: Search engines can crawl the fully-rendered HTML content

#### PrerenderEnabled Settings

```csharp
"PrerenderEnabled": true   // Shows content immediately + SEO benefits
"PrerenderEnabled": false  // Shows loading screen while app initializes
```

**When to enable PreRendering:**
- ✅ Public-facing marketing sites
- ✅ E-commerce product pages
- ✅ Blog/content sites that need SEO
- ✅ When first-paint performance is critical

**When to disable PreRendering:**
- ❌ Internal admin panels (no SEO needed)
- ❌ Apps that show loading progress for better UX
- ❌ When you want simpler behavior (no double-render)

#### Example from App.razor

```cshtml
@{
    var noPrerender = HttpContext.Request.Query["no-prerender"].Count > 0;
    var renderMode = noPrerender ? noPrerenderBlazorWebAssembly : serverWebSettings.WebAppRender.RenderMode;
    if (HttpContext.AcceptsInteractiveRouting() is false)
    {
        // Static SSR for non-interactive scenarios
        renderMode = null;
    }
}

@if (renderMode != null && (serverWebSettings.WebAppRender.PrerenderEnabled is false || noPrerender))
{
    <LoadingComponent @rendermode="null" />
}
```

**Key points:**
- The app checks the `PrerenderEnabled` setting at runtime
- If `false`, it shows a loading screen (`LoadingComponent`) while Blazor initializes
- If `true`, pre-rendered HTML is displayed immediately
- You can temporarily disable prerendering via `?no-prerender` query parameter

### ⚠️ Important: Service Worker Configuration

**If you enable PreRendering**, you **MUST** update the service worker file accordingly:

**File**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)

The service worker has different modes that correspond to your PreRendering settings.

---

## PWA & Service Workers

This project is a **full-featured Progressive Web App (PWA)** in ALL modes - Server, WebAssembly, Auto, and Hybrid.

### PWA Benefits Available in ALL Modes

✅ **Offline Support** - App continues working without internet connection  
✅ **Installability** - Users can install the app on their device  
✅ **Push Notifications** - Send notifications even when app is closed  
✅ **App-Like Experience** - Runs in standalone window without browser chrome  
✅ **Performance** - Service worker caching for faster subsequent loads

### Service Worker Files

The project uses **Bit.Bswup** (BitPlatform Service Worker Update) for managing the service worker lifecycle.

**File**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)

This file controls:
- Asset caching strategies
- Offline behavior
- Pre-rendering integration
- Update detection and handling

#### Understanding Service Worker Modes

The service worker has **four different modes** to match your app's PreRendering configuration:

```javascript
// Mode 1: FullOffline (Traditional PWA)
// self.mode = 'FullOffline';
// Downloads ALL assets first, THEN runs the app
// Recommended for: Offline-first apps with local database (IndexedDB, SQLite)
// Demo: https://todo-offline.bitplatform.cc/offline-database-demo
```

**Use FullOffline when:**
- Your app primarily uses PWA for offline support
- You have a local/offline database (IndexedDB, Bit.Besql SQLite)
- Users need guaranteed offline access to all features
- Network loss should NEVER break navigation

```javascript
// Mode 2: NoPrerender (Modern PWA) - DEFAULT
self.mode = 'NoPrerender';
// Starts immediately and lazy-loads assets as needed
// Recommended for: Modern apps that use PWA for installability, push notifications
// Demo: https://adminpanel.bitplatform.dev/
```

**Use NoPrerender when:**
- You want instant app startup
- PWA features are for installability/notifications, not offline
- You're okay with network dependency for lazy-loaded pages
- You set `PrerenderEnabled: false` in appsettings.json

```javascript
// Mode 3: InitialPrerender
// self.mode = 'InitialPrerender';
// Fetches pre-rendered HTML ONLY on first visit
// Recommended for: SEO-friendly apps with fast first-load experience
// Demo: https://todo.bitplatform.dev/
```

**Use InitialPrerender when:**
- You enabled `PrerenderEnabled: true` in appsettings.json
- You want SEO benefits but don't want server load on every refresh
- First visit shows pre-rendered content, subsequent visits skip server

```javascript
// Mode 4: AlwaysPrerender
// self.mode = 'AlwaysPrerender';
// Fetches pre-rendered HTML on EVERY app load
// Recommended for: Low-end mobile devices, critical SEO needs
// Demo: https://sales.bitplatform.dev/
```

**Use AlwaysPrerender when:**
- You enabled `PrerenderEnabled: true` in appsettings.json
- Target audience has low-end mobile devices
- Blazor WebAssembly startup time is too slow on target devices
- SEO is critical and you can handle the server load

**Downside**: Increases server load due to pre-rendering on every page refresh

#### Matching Service Worker Mode to Your Configuration

Here's the simple rule:

| appsettings.json Setting | Service Worker Mode |
|-------------------------|---------------------|
| `PrerenderEnabled: false` | `NoPrerender` |
| `PrerenderEnabled: true` (first load only) | `InitialPrerender` |
| `PrerenderEnabled: true` (always) | `AlwaysPrerender` |
| Offline-first app | `FullOffline` |

#### Service Worker Asset Configuration

The service worker also controls which assets to cache:

```javascript
self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,

    // If PDF reader is needed in PWA, remove these lines:
    /pdfjs-4\.7\.76\.js$/,
    /pdfjs-4\.7\.76-worker\.js$/,

    // country flags
    /_content\/Bit\.BlazorUI\.Extras\/flags/
];
```

**Key points:**
- `assetsExclude`: Prevents caching of large files you don't need offline
- You can customize this based on your app's offline requirements
- Remove PDF.js exclusions if you need the PDF reader component offline

```javascript
self.externalAssets = [
    { "url": "/" },
    { url: "_framework/bit.blazor.web.es2019.js" },
    { "url": "Boilerplate.Server.Web.styles.css" },
    { "url": "Boilerplate.Client.Web.bundle.scp.css" }
];
```

**External assets** are cached separately and must be explicitly listed.

```javascript
self.serverHandledUrls = [
    /\/api\//,
    /\/odata\//,
    /\/core\//,
    /\/hangfire/,
    /\/healthchecks-ui/,
    /\/healthz/,
    /\/health/,
    /\/alive/,
    /\/swagger/,
    /\/signin-/,
    /\/.well-known/,
    /\/sitemap.xml/,
    /\/sitemap_index.xml/,
    /\/web-interop-app.html/
];
```

**Server-handled URLs** bypass the service worker and always go to the server. This includes:
- API endpoints
- Health checks
- Authentication callbacks
- Static server resources

#### Web Push Notifications

The service worker also handles push notifications:

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
        // Navigate to specific page when notification is clicked
        event.waitUntil(
            clients.openWindow(pageUrl)
        );
    }
});
```

Push notifications work across **all Blazor modes** and even when the app is closed!

---

## IPrerenderStateService

When you use direct `HttpClient` calls (instead of the recommended `IAppController` interfaces), you need to manage pre-render state manually to avoid duplicate API calls.

### The Problem: Double API Calls During PreRendering

When PreRendering is enabled, your component renders **twice**:
1. **First render (Server)**: Runs on the server to generate HTML
2. **Second render (Client)**: Runs in the browser after Blazor initializes

Without `IPrerenderStateService`, any API calls in `OnInitAsync` would execute twice:
```csharp
// ❌ BAD: This calls the API twice during pre-rendering
protected override async Task OnInitAsync()
{
    products = await HttpClient.GetFromJsonAsync<ProductDto[]>("api/products/");
}
```

### The Solution: IPrerenderStateService

**File**: [`/src/Shared/Services/Contracts/IPrerenderStateService.cs`](/src/Shared/Services/Contracts/IPrerenderStateService.cs)

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

### How It Works

`IPrerenderStateService` ensures data is fetched **only once** during pre-rendering:

1. **Server render**: Executes the factory function, stores result in state
2. **Client render**: Retrieves stored result, skips the factory function

```csharp
// ✅ GOOD: This calls the API only once, even during pre-rendering
protected override async Task OnInitAsync()
{
    products = await PrerenderStateService.GetValue(() => 
        HttpClient.GetFromJsonAsync<ProductDto[]>("api/products/")
    );
}
```

### When Do You Need IPrerenderStateService?

| Scenario | Need IPrerenderStateService? |
|----------|----------------------------|
| Using `IAppController` interfaces | ❌ No - handled automatically |
| Using direct `HttpClient` calls | ✅ Yes - prevents duplicate calls |
| PreRendering is disabled | ❌ No - component only renders once |
| PreRendering is enabled + direct HTTP | ✅ Yes - required |

### Real Example from MainLayout

**File**: [`/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.cs)

```csharp
[AutoInject] private IPrerenderStateService prerenderStateService = default!;

protected override async Task OnInitAsync()
{
    isOnline = await prerenderStateService.GetValue<bool?>(
        nameof(isOnline), 
        async () => isOnline ?? inPrerenderSession is true ? true : null
    );
}
```

### Platform-Specific Implementations

The project has **three implementations** of `IPrerenderStateService`:

1. **`WebServerPrerenderStateService`** - Used in `Boilerplate.Server.Web` during server-side pre-rendering
   - Stores data in `PersistentComponentState` to pass to client

2. **`WebClientPrerenderStateService`** - Used in `Boilerplate.Client.Web` during client-side initialization
   - Retrieves data from `PersistentComponentState` stored by server

3. **`NoOpPrerenderStateService`** - Used in `Boilerplate.Client.Maui` and other non-prerendering scenarios
   - Simply executes the factory function without state persistence

This design allows the same `Client.Core` code to work across all platforms!

### Best Practice: Use IAppController Interfaces

**Recommendation**: Instead of using `HttpClient` directly, use the strongly-typed `IAppController` interfaces:

```csharp
// ✅ BEST: No need for IPrerenderStateService, pre-render state handled automatically
[AutoInject] private IProductController productController = default!;

protected override async Task OnInitAsync()
{
    products = await productController.GetProducts(CurrentCancellationToken);
}
```

The `IAppController` interfaces automatically handle:
- Pre-render state management
- Cancellation tokens
- Error handling
- Type safety

**Use `IPrerenderStateService` only when:**
- Calling third-party APIs that don't have controller interfaces
- Working with legacy code that uses direct `HttpClient`
- Fetching data from non-API sources during pre-rendering

---

## Summary

In this stage, you learned about:

1. **App.razor and index.html Files**
   - `App.razor` is the entry point for Blazor Server/SSR
   - `index.html` is the entry point for Blazor WebAssembly
   - Changes to one often require mirroring in the other

2. **Blazor Modes**
   - **BlazorServer**: Runs on server, SignalR connection
   - **BlazorWebAssembly**: Runs in browser, offline support
   - **BlazorAuto**: Starts server, switches to WASM
   - Configured in `appsettings.json` under `WebAppRender`

3. **PreRendering**
   - Generates HTML on server before sending to client
   - Benefits: Faster perceived load, better SEO
   - Configure with `PrerenderEnabled` setting
   - Must match service worker mode configuration

4. **PWA Features**
   - ALL modes support PWA (offline, install, notifications)
   - Service worker has 4 modes: `FullOffline`, `NoPrerender`, `InitialPrerender`, `AlwaysPrerender`
   - Must match service worker mode to your PreRendering setting
   - Asset caching, push notifications, offline support all built-in

5. **IPrerenderStateService**
   - Prevents duplicate API calls during pre-rendering
   - Only needed when using direct `HttpClient` calls
   - `IAppController` interfaces handle this automatically
   - Three implementations for different platforms

### Configuration Quick Reference

**For apps without PreRendering:**
```json
// appsettings.json
"WebAppRender": {
    "BlazorMode": "BlazorWebAssembly",
    "PrerenderEnabled": false
}
```
```javascript
// service-worker.published.js
self.mode = 'NoPrerender';
```

**For apps with PreRendering (first load only):**
```json
// appsettings.json
"WebAppRender": {
    "BlazorMode": "BlazorWebAssembly", 
    "PrerenderEnabled": true
}
```
```javascript
// service-worker.published.js
self.mode = 'InitialPrerender';
```

**For offline-first apps:**
```json
// appsettings.json
"WebAppRender": {
    "BlazorMode": "BlazorWebAssembly",
    "PrerenderEnabled": false
}
```
```javascript
// service-worker.published.js
self.mode = 'FullOffline';
```

---

## Next Steps

Now that you understand Blazor modes, pre-rendering, and PWA features, you can:

- Experiment with different `BlazorMode` settings to see how they affect your app
- Enable/disable PreRendering and observe the differences
- Modify the service worker mode to match your app's needs
- Review how `IPrerenderStateService` is used throughout the codebase
- Customize PWA features like icons, manifest, and push notifications
