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

### Common Elements Across Files

**Both `App.razor` and `index.html` files include:**

**Meta tags and viewport settings:**
```html
<meta charset="utf-8" />
<meta name="theme-color">
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
```

**Performance optimization - Preconnect links:**
```html
<link rel="preconnect" href="https://www.google.com">
<link rel="preconnect" href="https://www.gstatic.com" crossorigin>
<link rel="preconnect" href="https://js.monitor.azure.com" crossorigin>
```

**PWA support:**
```html
<link rel="icon" href="favicon.ico" type="image/x-icon" />
<link rel="apple-touch-icon" sizes="512x512" href="images/icons/bit-icon-512.png" />
<link rel="manifest" href="manifest.json" />
```

**Bit.BlazorUI stylesheets:**
```html
<link href="_content/Bit.BlazorUI/styles/bit.blazorui.css" rel="stylesheet" />
<link href="_content/Bit.BlazorUI.Icons/styles/bit.blazorui.icons.css" rel="stylesheet" />
<link href="_content/Bit.BlazorUI.Assets/styles/bit.blazorui.assets.css" rel="stylesheet" />
<link href="_content/Bit.BlazorUI.Extras/styles/bit.blazorui.extras.css" rel="stylesheet" />
<link href="_content/Boilerplate.Client.Core/styles/app.css" rel="stylesheet" />
```

**Service worker and application scripts:**
```html
<!-- Bit.Bswup for PWA service worker management -->
<script src="_content/Bit.Bswup/bit-bswup.js"></script>
<script src="_content/Bit.Bswup/bit-bswup.progress.js"></script>

<!-- Core application scripts -->
<script src="_content/Bit.Butil/bit-butil.js"></script>
<script src="_content/Bit.Besql/bit-besql.js"></script>
<script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>
<script src="_content/Boilerplate.Client.Core/scripts/app.js"></script>
<script src="_content/Bit.BlazorUI.Extras/scripts/bit.blazorui.extras.js"></script>
```

**Application Insights initialization:**
Both files include the Application Insights snippet for telemetry and monitoring.

### Key Differences Between Files

**App.razor (Server-side):**
- Uses Razor syntax with `@` directives
- Has access to `HttpContext` and server-side services
- Dynamically determines render mode based on configuration
- Can conditionally show `LoadingComponent` based on prerendering settings

```csharp
@{
    var noPrerender = HttpContext.Request.Query["no-prerender"].Count > 0;
    var renderMode = noPrerender ? noPrerenderBlazorWebAssembly : serverWebSettings.WebAppRender.RenderMode;
    if (HttpContext.AcceptsInteractiveRouting() is false)
    {
        // Static SSR for non-interactive scenarios
        renderMode = null;
    }
}
```

**index.html (Client-side):**
- Pure HTML file
- Has static loading component HTML/CSS inline
- Uses `blazor.webassembly.js` or `blazor.webview.js` (for Hybrid)
- No dynamic server-side logic

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

#### 1. BlazorServer
**How it works:** Runs on the server with SignalR connection to the client

**Pros:**
- ✅ Small initial download
- ✅ Server-side processing power
- ✅ Immediate load
- ✅ Works on older browsers

**Cons:**
- ❌ Requires persistent connection
- ❌ Higher server load
- ❌ Network latency affects UX
- ❌ Scalability challenges

**Best for:** Internal apps, admin panels, apps requiring real-time server state

#### 2. BlazorWebAssembly
**How it works:** Downloads and runs entirely in the browser

**Pros:**
- ✅ Offline support (with PWA)
- ✅ Reduced server load
- ✅ Rich client-side experience
- ✅ No persistent connection needed

**Cons:**
- ❌ Larger initial download (~2-3 MB)
- ❌ Browser compatibility requirements
- ❌ Slower initial startup

**Best for:** Public apps, offline-capable apps, apps with minimal server interaction

#### 3. BlazorAuto
**How it works:** Starts with Server mode, then switches to WebAssembly after download

**Pros:**
- ✅ Fast initial load (server-side)
- ✅ Eventually gets offline support (WASM)
- ✅ Best of both worlds

**Cons:**
- ❌ More complex configuration
- ❌ Requires both server and client resources
- ❌ Transition between modes can be noticeable

**Best for:** Public-facing apps that need fast initial load AND offline support

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
- ✅ Landing pages with important content

**When to disable PreRendering:**
- ❌ Internal admin panels (no SEO needed)
- ❌ Apps that show loading progress for better UX
- ❌ When you want simpler behavior (no double-render)
- ❌ Apps with complex client-side initialization

#### How PreRendering Works in App.razor

From [`/src/Server/Boilerplate.Server.Web/Components/App.razor`](/src/Server/Boilerplate.Server.Web/Components/App.razor):

```csharp
@{
    var noPrerender = HttpContext.Request.Query["no-prerender"].Count > 0;
    var renderMode = noPrerender ? noPrerenderBlazorWebAssembly : serverWebSettings.WebAppRender.RenderMode;
    if (HttpContext.AcceptsInteractiveRouting() is false)
    {
        // https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-9.0#add-static-server-side-rendering-ssr-pages-to-a-globally-interactive-blazor-web-app
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
- You can temporarily disable prerendering via `?no-prerender` query parameter for debugging
- The `AcceptsInteractiveRouting()` check enables static SSR for non-interactive scenarios

### ⚠️ Important: Service Worker Configuration

**If you enable PreRendering**, you **MUST** update the service worker file accordingly:

**File**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)

The service worker has different modes that correspond to your PreRendering settings (explained in detail in the next section).

---

## PWA & Service Workers

This project is a **full-featured Progressive Web App (PWA)** in ALL modes - Server, WebAssembly, Auto, and Hybrid.

### PWA Benefits Available in ALL Modes

✅ **Offline Support** - App continues working without internet connection  
✅ **Installability** - Users can install the app on their device (desktop, mobile)  
✅ **Push Notifications** - Send notifications even when app is closed  
✅ **App-Like Experience** - Runs in standalone window without browser chrome  
✅ **Performance** - Service worker caching for faster subsequent loads  
✅ **Background Sync** - Can sync data when connection returns  
✅ **Add to Home Screen** - Users can add app icon to their device home screen  

### Service Worker Files

The project uses **Bit.Bswup** (BitPlatform Service Worker Update) for managing the service worker lifecycle.

**File**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)

This file controls:
- Asset caching strategies
- Offline behavior
- Pre-rendering integration
- Update detection and handling
- Push notification handling

### Understanding Service Worker Modes

The service worker has **four different modes** to match your app's PreRendering configuration. Let's explore each one:

#### Mode 1: FullOffline (Traditional PWA)

```javascript
// self.mode = 'FullOffline';
```

**How it works:** Downloads ALL assets first, THEN runs the app.

**Behavior:**
- On first load, downloads and caches all app assets (JS, CSS, images, etc.)
- Shows download progress indicator
- Once all assets are cached, the app starts
- Subsequent visits work completely offline

**Recommended for:**
- Offline-first apps with local database (IndexedDB, SQLite via Bit.Besql)
- Apps that MUST work without network connectivity
- Field service apps, medical apps, emergency response apps
- Apps where guaranteed offline navigation is critical

**Demo:** https://todo-offline.bitplatform.cc/offline-database-demo

**Pros:**
- ✅ Guaranteed offline functionality
- ✅ No broken pages if network lost during navigation
- ✅ Consistent performance after first load

**Cons:**
- ❌ Longer initial load time
- ❌ Large bandwidth usage on first visit
- ❌ Users must wait for all downloads before using app

#### Mode 2: NoPrerender (Modern PWA) - DEFAULT

```javascript
self.mode = 'NoPrerender';
```

**How it works:** Starts immediately and lazy-loads assets as needed.

**Behavior:**
- App starts instantly
- Assets are loaded on-demand as user navigates
- Gradually caches assets for offline use
- If network lost, only visited pages work offline

**Recommended for:**
- Modern apps that use PWA for installability, not offline
- Apps with push notifications
- Admin panels and internal tools
- Apps where instant startup is priority

**Demo:** https://adminpanel.bitplatform.dev/

**Pros:**
- ✅ Instant app startup
- ✅ Minimal initial bandwidth usage
- ✅ Simple and predictable behavior
- ✅ Best for apps that require network anyway

**Cons:**
- ❌ Network required for unvisited pages
- ❌ App might break if connectivity lost during navigation

**Use when:**
- You set `PrerenderEnabled: false` in appsettings.json
- PWA features are for installability/notifications, not offline
- You're okay with network dependency for lazy-loaded pages

#### Mode 3: InitialPrerender

```javascript
// self.mode = 'InitialPrerender';
```

**How it works:** Fetches pre-rendered HTML ONLY on first visit.

**Behavior:**
- First visit: Fetches and displays pre-rendered HTML from server
- Shows content while Blazor runtime loads in background
- Subsequent visits: Uses cached app, no server pre-rendering
- Reduces server load after first visit

**Recommended for:**
- SEO-friendly apps with fast first-load experience
- Public-facing apps where first impression matters
- Apps balancing SEO benefits and server resources

**Demo:** https://todo.bitplatform.dev/

**Pros:**
- ✅ Fast perceived load on first visit
- ✅ SEO benefits (search engines see content)
- ✅ Reduced server load on subsequent visits
- ✅ Better user experience for new visitors

**Cons:**
- ❌ Slightly more complex behavior
- ❌ First visit requires server round-trip

**Use when:**
- You enabled `PrerenderEnabled: true` in appsettings.json
- You want SEO benefits but don't want server load on every refresh
- First visit experience is critical for user conversion

#### Mode 4: AlwaysPrerender

```javascript
// self.mode = 'AlwaysPrerender';
```

**How it works:** Fetches pre-rendered HTML on EVERY app load.

**Behavior:**
- Every page load fetches pre-rendered HTML from server
- Content shows immediately on every visit
- Helps low-end devices with slow WASM startup
- Highest server load of all modes

**Recommended for:**
- Low-end mobile device targeting
- Critical SEO needs (every visit counts)
- Apps where Blazor WASM startup is too slow on target devices

**Demo:** https://sales.bitplatform.dev/

**Pros:**
- ✅ Content visible immediately on every load
- ✅ Works well on slow devices
- ✅ Maximum SEO benefit
- ✅ Consistent fast perceived load

**Cons:**
- ❌ High server load (pre-renders every request)
- ❌ More bandwidth usage
- ❌ Server must handle pre-rendering at scale

**Use when:**
- You enabled `PrerenderEnabled: true` in appsettings.json
- Target audience has low-end mobile devices
- Blazor WebAssembly startup time is too slow on target devices
- SEO is critical and you can handle the server load

### Matching Service Worker Mode to Your Configuration

Here's the simple decision matrix:

| appsettings.json Setting | App Priority | Service Worker Mode |
|-------------------------|--------------|---------------------|
| `PrerenderEnabled: false` | Instant startup | `NoPrerender` (default) |
| `PrerenderEnabled: true` | SEO + first load | `InitialPrerender` |
| `PrerenderEnabled: true` | SEO + low-end devices | `AlwaysPrerender` |
| `PrerenderEnabled: false` | Offline-first | `FullOffline` |

### Service Worker Asset Configuration

The service worker controls which assets to cache:

```javascript
self.assetsInclude = [];
self.assetsExclude = [
    /bit\.blazorui\.fluent\.css$/,
    /bit\.blazorui\.fluent-dark\.css$/,
    /bit\.blazorui\.fluent-light\.css$/,

    // If PDF reader is needed in PWA, remove these lines:
    /pdfjs-4\.7\.76\.js$/,
    /pdfjs-4\.7\.76-worker\.js$/,

    // country flags (large files)
    /_content\/Bit\.BlazorUI\.Extras\/flags/
];
```

**Asset Exclusion Strategy:**
- `assetsExclude`: Prevents caching of large files you don't need offline
- Themes: Only the active theme is cached, not all variants
- PDF.js: Excluded by default (remove if you need PDF reader offline)
- Country flags: Excluded due to size (hundreds of flag images)

**Customization tip:** Review your app's offline requirements and adjust exclusions accordingly.

```javascript
self.externalAssets = [
    { "url": "/" },
    { url: "_framework/bit.blazor.web.es2019.js" },
    { "url": "Boilerplate.Server.Web.styles.css" },
    { "url": "Boilerplate.Client.Web.bundle.scp.css" }
];
```

**External assets** are resources that must be cached separately because they're not discovered automatically by the service worker.

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

**Server-handled URLs** bypass the service worker and always go directly to the server:
- API endpoints (`/api/`, `/odata/`)
- Administrative interfaces (`/hangfire`, `/healthchecks-ui`)
- Health check endpoints (`/healthz`, `/health`, `/alive`)
- Authentication callbacks (`/signin-*`)
- Static server resources (sitemap, well-known files)

**Why bypass these?** These URLs need real-time server responses and shouldn't be cached.

### Web Push Notifications

The service worker handles push notifications, even when the app is closed:

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

**How it works:**
1. Server sends push notification to browser
2. Service worker receives push event (even if app closed)
3. Shows notification with title, message, and icon
4. When user clicks notification, app opens to specified `pageUrl`

**Key benefits:**
- ✅ Works across **all Blazor modes** (Server, WASM, Auto, Hybrid)
- ✅ Notifications work even when app is completely closed
- ✅ Can navigate to specific app pages on notification click
- ✅ Cross-platform support (Windows, macOS, Android via browser)

**Configuration needed:**
- Set up push notification server (see AdsPushVapid in appsettings.json)
- Request notification permission from user
- Subscribe to push notifications in your app

---

## IPrerenderStateService

When you use direct `HttpClient` calls (instead of the recommended `IAppController` interfaces), you need to manage pre-render state manually to avoid duplicate API calls.

### The Problem: Double API Calls During PreRendering

When PreRendering is enabled, your component renders **twice**:

1. **First render (Server)**: Runs on the server to generate HTML
   - Executes `OnInitAsync`, `OnParametersSetAsync`, etc.
   - Fetches data from API/database
   - Generates HTML to send to client

2. **Second render (Client)**: Runs in the browser after Blazor initializes
   - Executes same lifecycle methods again
   - Would fetch same data again (duplicate call!)
   - Hydrates the pre-rendered HTML into interactive components

Without `IPrerenderStateService`, any API calls in `OnInitAsync` would execute **twice**:

```csharp
// ❌ BAD: This calls the API twice during pre-rendering
protected override async Task OnInitAsync()
{
    products = await HttpClient.GetFromJsonAsync<ProductDto[]>("api/products/");
    // First call: Server-side during pre-rendering
    // Second call: Client-side during hydration
    // Result: Wasted bandwidth, slower load, unnecessary server load
}
```

### The Solution: IPrerenderStateService

**File**: [`/src/Shared/Services/Contracts/IPrerenderStateService.cs`](/src/Shared/Services/Contracts/IPrerenderStateService.cs)

```csharp
/// <summary>
/// The Client.Core codebase is designed to support various Blazor hosting models, including Hybrid and WebAssembly, 
/// which may or may not enable pre-rendering. To ensure consistent behavior across all scenarios, 
/// the `IPrerenderStateService` interface is introduced.
/// </summary>
public interface IPrerenderStateService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value, executing the factory function only once during pre-rendering.
    /// Uses caller info to generate a unique key automatically.
    /// </summary>
    Task<T?> GetValue<T>(Func<Task<T?>> factory,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "");

    /// <summary>
    /// Gets a value with an explicit key for more control.
    /// </summary>
    Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
}
```

### How It Works

`IPrerenderStateService` ensures data is fetched **only once** during pre-rendering:

**Server render (first render):**
1. Calls the factory function (executes API call)
2. Stores result in `PersistentComponentState`
3. Serializes state as JSON in the HTML

**Client render (second render):**
1. Checks if value exists in persisted state
2. If yes: Returns cached value (NO API call)
3. If no: Falls back to executing factory function

```csharp
// ✅ GOOD: This calls the API only once, even during pre-rendering
protected override async Task OnInitAsync()
{
    products = await PrerenderStateService.GetValue(() => 
        HttpClient.GetFromJsonAsync<ProductDto[]>("api/products/")
    );
    // First render (server): Executes factory, stores result
    // Second render (client): Returns stored result, NO API call
}
```

### When Do You Need IPrerenderStateService?

| Scenario | Need IPrerenderStateService? | Why? |
|----------|----------------------------|------|
| Using `IAppController` interfaces | ❌ No | Pre-render state handled automatically |
| Using direct `HttpClient` calls | ✅ Yes | Prevents duplicate calls |
| PreRendering is disabled | ❌ No | Component only renders once |
| PreRendering enabled + direct HTTP | ✅ Yes | Required to avoid double API calls |
| Blazor Hybrid (MAUI, Windows) | ❌ No | No pre-rendering in Hybrid apps |
| Fetching non-API data during pre-render | ✅ Yes | For any data that should persist across renders |

### Real Example from MainLayout

**File**: [`/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.cs)

```csharp
[AutoInject] private IPrerenderStateService prerenderStateService = default!;

protected override async Task OnInitializedAsync()
{
    await base.OnInitializedAsync();

    try
    {
        var inPrerenderSession = RendererInfo.IsInteractive is false;
        // During pre-rendering, if any API calls are made, the `isOnline` value will be set 
        // using PubSub's `ClientPubSubMessages.IS_ONLINE_CHANGED`, depending on the success 
        // or failure of the API call. However, if a pre-rendered page has no HTTP API call 
        // dependencies, its value remains null. 
        // Even though Server.Web and Server.Api may be deployed on different servers, 
        // we can still assume that if the client is displaying a pre-rendered result, it is online.
        isOnline = await prerenderStateService.GetValue<bool?>(
            nameof(isOnline), 
            async () => isOnline ?? inPrerenderSession is true ? true : null
        );
    }
    catch (Exception exp)
    {
        exceptionHandler.Handle(exp);
    }
}
```

**What this example demonstrates:**
- Uses `GetValue` with a named key (`nameof(isOnline)`)
- Determines online status during pre-rendering
- Assumes online if displaying pre-rendered content
- State persists from server render to client render

### Platform-Specific Implementations

The project has **three implementations** of `IPrerenderStateService` to support different hosting models:

#### 1. WebServerPrerenderStateService

**Location**: [`/src/Server/Boilerplate.Server.Web/Services/WebServerPrerenderStateService.cs`](/src/Server/Boilerplate.Server.Web/Services/WebServerPrerenderStateService.cs)

**Used in:** `Boilerplate.Server.Web` during server-side pre-rendering

**How it works:**
- Stores data in `PersistentComponentState` during server render
- Registers callback to persist state as JSON in HTML
- Data is embedded in the HTML sent to client
- Only active when pre-rendering is enabled

```csharp
public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
{
    if (NoPersistent) return await factory();

    // Try to retrieve from state first
    if (persistentComponentState!.TryTakeFromJson(key, out T? value)) 
        return value;

    // Not found, execute factory and persist result
    var result = await factory();
    Persist(key, result);
    return result;
}
```

#### 2. WebClientPrerenderStateService

**Location**: [`/src/Client/Boilerplate.Client.Web/Services/WebClientPrerenderStateService.cs`](/src/Client/Boilerplate.Client.Web/Services/WebClientPrerenderStateService.cs)

**Used in:** `Boilerplate.Client.Web` during client-side hydration

**How it works:**
- Retrieves data from `PersistentComponentState` that was persisted by server
- Extracts JSON data from HTML
- Falls back to executing factory if data not found

```csharp
public async Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
{
    // Try to retrieve persisted data from server
    if (persistentComponentState!.TryTakeFromJson(key, out T? value)) 
        return value;

    // Not found (shouldn't happen if server persisted correctly), execute factory
    var result = await factory();
    return result;
}
```

#### 3. NoOpPrerenderStateService

**Location**: [`/src/Client/Boilerplate.Client.Core/Services/NoOpPrerenderStateService.cs`](/src/Client/Boilerplate.Client.Core/Services/NoOpPrerenderStateService.cs)

**Used in:** 
- `Boilerplate.Client.Maui` (Blazor Hybrid)
- `Boilerplate.Client.Windows` (Blazor Hybrid)
- Any scenario where pre-rendering is not applicable

**How it works:**
- Simply executes the factory function
- No state persistence
- Lightweight, no overhead

```csharp
public class NoOpPrerenderStateService : IPrerenderStateService
{
    public Task<T?> GetValue<T>(Func<Task<T?>> factory, 
        [CallerLineNumber] int lineNumber = 0, 
        [CallerMemberName] string memberName = "", 
        [CallerFilePath] string filePath = "")
    {
        return factory(); // Just execute and return
    }

    public Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
    {
        return factory(); // Just execute and return
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

**Design benefit:** This design allows the same `Client.Core` code to work seamlessly across all platforms without conditional logic!

### Best Practice: Use IAppController Interfaces

**⭐ STRONGLY RECOMMENDED**: Instead of using `HttpClient` directly, use the strongly-typed `IAppController` interfaces.

```csharp
// ✅ BEST: No need for IPrerenderStateService, everything handled automatically
[AutoInject] private IProductController productController = default!;

protected override async Task OnInitAsync()
{
    products = await productController.GetProducts(CurrentCancellationToken);
    // Pre-render state management: ✅ Automatic
    // Cancellation token: ✅ Automatic
    // Error handling: ✅ Automatic
    // Type safety: ✅ Guaranteed
}
```

**What `IAppController` interfaces provide:**
- ✅ **Automatic pre-render state management** - No manual `IPrerenderStateService` needed
- ✅ **Type safety** - Compile-time checking of API calls
- ✅ **Cancellation support** - Proper handling of abandoned requests
- ✅ **Consistent error handling** - Centralized exception management
- ✅ **Clear contract** - Explicit API interface definition

**Use `IPrerenderStateService` directly only when:**
- ❌ Calling third-party APIs that don't have controller interfaces
- ❌ Working with legacy code that uses direct `HttpClient`
- ❌ Fetching data from non-API sources during pre-rendering (e.g., browser APIs, local storage)
- ❌ Accessing platform-specific services that return async data

---

## Summary

In this stage, you learned about:

### 1. App.razor and index.html Files
- **`App.razor`** is the entry point for Blazor Server/SSR scenarios
- **`index.html`** files are entry points for WebAssembly and Hybrid
- Changes to one often require mirroring in the other
- Both files share common elements: styles, scripts, PWA configuration
- Key difference: `App.razor` has server-side logic and dynamic render mode selection

### 2. Blazor Modes
- **BlazorServer**: Runs on server, SignalR connection, immediate load
- **BlazorWebAssembly**: Runs in browser, offline support, larger download
- **BlazorAuto**: Starts server, switches to WASM, best of both worlds
- All configured in `appsettings.json` under `WebAppRender` section
- Each mode has distinct pros/cons for different use cases

### 3. PreRendering
- Generates HTML on server before sending to client
- **Benefits**: Faster perceived load time, better SEO
- Configure with `PrerenderEnabled` setting in `appsettings.json`
- Must match service worker mode configuration
- Can be bypassed with `?no-prerender` query parameter for debugging
- `LoadingComponent` shows when pre-rendering is disabled

### 4. PWA Features
- **ALL** modes support PWA capabilities (Server, WASM, Auto, Hybrid)
- Service worker has **4 modes**: `FullOffline`, `NoPrerender`, `InitialPrerender`, `AlwaysPrerender`
- Must match service worker mode to your PreRendering setting
- Asset caching, push notifications, offline support all built-in
- **Bit.Bswup** manages service worker lifecycle automatically
- Push notifications work even when app is closed

### 5. IPrerenderStateService
- Prevents duplicate API calls during pre-rendering
- Only needed when using direct `HttpClient` calls
- `IAppController` interfaces handle this automatically (RECOMMENDED)
- Three implementations for different platforms:
  - `WebServerPrerenderStateService`: Server-side state persistence
  - `WebClientPrerenderStateService`: Client-side state retrieval
  - `NoOpPrerenderStateService`: No-op for Hybrid apps
- Uses `PersistentComponentState` to transfer data from server to client

### Configuration Quick Reference

**For apps without PreRendering (DEFAULT):**
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

**For apps with PreRendering (always):**
```json
// appsettings.json
"WebAppRender": {
    "BlazorMode": "BlazorWebAssembly", 
    "PrerenderEnabled": true
}
```
```javascript
// service-worker.published.js
self.mode = 'AlwaysPrerender';
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

✅ **Experiment with different `BlazorMode` settings** to see how they affect your app's behavior and performance

✅ **Enable/disable PreRendering** and observe the differences in load time and SEO

✅ **Modify the service worker mode** to match your app's specific needs and target audience

✅ **Review how `IPrerenderStateService` is used** throughout the codebase for best practices

✅ **Customize PWA features** like icons, manifest, splash screens, and push notifications

✅ **Test offline functionality** by disconnecting network and navigating your app

✅ **Monitor service worker updates** using Bit.Bswup's built-in update detection

✅ **Configure asset caching** based on your app's offline requirements

**Ready for Stage 13?** Learn about the **Force Update System** that ensures users always run the latest version of your app!
