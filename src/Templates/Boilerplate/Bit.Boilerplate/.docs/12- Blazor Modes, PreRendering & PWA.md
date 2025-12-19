# Stage 12: Blazor Modes, PreRendering & PWA

Welcome to Stage 12 of the Boilerplate project tutorial! In this stage, you'll learn about Blazor rendering modes, pre-rendering capabilities, Progressive Web App (PWA) features, and how they all work together in this project.

---

## Table of Contents
1. [App.razor and index.html Files](#apprazor-and-indexhtml-files)
2. [Blazor Mode & PreRendering Configuration](#blazor-mode--prerendering-configuration)
3. [PWA & Service Workers](#pwa--service-workers)

---

## App.razor and index.html Files

The project uses different files depending on the hosting model:

### Key Files

1. **[`/src/Server/Boilerplate.Server.Web/Components/App.razor`](/src/Server/Boilerplate.Server.Web/Components/App.razor)** - The main entry point for Blazor Server, WebAssembly, Auto and Static SSR
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

Blazor Server, Auto, WebAssembly, Blazor WebAssembly Standalone and Blazor Hybrid.

This [article](https://www.reddit.com/r/Blazor/comments/1kq5eyu/this_is_not_yet_just_another_incorrect_comparison/) is a good resource to compare Blazor modes.
In a nutshell:
 - Use Blazor Server for development purposes only.
 - Use Blazor WebAssembly or Blazor WebAssembly Standalone for production
 - Use Blazor Hybrid for Android, iOS, Windows and macOS apps

### PreRendering: Faster Load & Better SEO

**PreRendering** means the server generates the initial HTML content before sending it to the browser. This provides two major benefits:

1. **Faster Perceived Load Time**: Users see content immediately, even while Blazor is initializing
2. **Better SEO**: Search engines can crawl the fully-rendered HTML content

#### PrerenderEnabled Settings

```json
"PrerenderEnabled": true   // Shows content immediately + SEO benefits
"PrerenderEnabled": false  // Shows loading screen while app initializes
```

---

## PWA & Service Workers

This project is a **full-featured Progressive Web App (PWA)** in Blazor Server, WebAssembly, and Auto.

### PWA Benefits Available in ALL Modes

✅ **Installability** - Users can install the app on their device (desktop, mobile)  
✅ **Push Notifications** - Send notifications even when app is closed  
✅ **App-Like Experience** - Runs in standalone window without browser chrome  
✅ **Performance** - Service worker caching for faster subsequent loads  
✅ **Background Sync** - Can sync data when connection returns  
✅ **Add to Home Screen** - Users can add app icon to their device home screen  
✅ **Offline Support** - App continues working without internet connection (WebAssembly and WebAssembly Standalone only)

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
- Field service apps, medical apps
- Apps where guaranteed offline navigation is critical

**Demo:** https://todo-offline.bitplatform.cc/offline-todo

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
- ✅ Best for apps that require network anyway

**Cons:**
- ❌ Network required for unvisited pages' assets
- ❌ App might break if connectivity lost during navigation

**Use when:**
- You set `PrerenderEnabled: false` in appsettings.json
- PWA features are for installability/notifications, not offline
- You're okay with network dependency for pages lazy-loaded assets

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

**Demo:** https://sales.bitplatform.dev/

**Initial Prerender** vs **Always Prerender**:
If pre-rendering is enabled, `Always Prerender` fetches the site's document on every load of the app. The reason behind fetching the document on every app load is that Blazor WebAssembly's runtime might take some time to kick in on low-end Android devices, so if the user refreshes the page or visits a new page, it shows the pre-rendered document while the Blazor WebAssembly runtime is loading. Downside? It increases server load due to frequent pre-rendering which can be reduced by response caching which will be covered in upcoming stages.

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
    ...
];
```

**Server-handled URLs** bypass the service worker and let the server handle these endpoints:
- API endpoints (`/api/`, `/odata/`)
- Administrative interfaces (`/hangfire`, `/healthchecks-ui`)
- Health check endpoints (`/healthz`, `/health`, `/alive`)
- Authentication callbacks (`/signin-*`)
- Static server resources (sitemap, well-known files)

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
4. When user clicks notification, app opens to specified `pageUrl` (If applicable)

---