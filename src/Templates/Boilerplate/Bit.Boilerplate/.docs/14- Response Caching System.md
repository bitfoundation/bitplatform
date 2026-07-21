# Stage 14: Response Caching System

Welcome to Stage 14! In this stage, you'll learn about the comprehensive **4-layer response caching system** built into this project. This advanced caching architecture dramatically improves application performance, reduces server load, and provides an excellent user experience.

---

## Overview

The project implements a sophisticated caching strategy that spans **four distinct layers**, each serving a specific purpose in the overall performance optimization strategy:

1. **Client In-Memory Cache** - Fastest, application-level caching (synchronous, instant)
2. **Browser HTTP Cache** - Client-side HTTP caching (fast, persists across sessions)
3. **CDN Edge Cache** - Distributed caching at edge locations (Cloudflare)
4. **ASP.NET Core Output Cache** - Server-side response caching (Memory or Redis)

---

## Core Components

### Key Benefit: Zero Server Overhead for Cached Content

**Real-World Impact:**
- Every page refresh on cached pages (like https://sales.bitplatform.dev product pages) adds **zero overhead** to the server
- The complete response is served directly from Cloudflare's edge servers (CDN)
- This dramatically reduces server load, database queries, and infrastructure costs
- Enables handling millions of requests with minimal server resources

**Important Security Note:**
- Responses for authenticated/logged-in users are **not cached** on CDN or output cache for security/privacy reasons
- User-specific data is only cached in the user's own browser/memory (safe)

---

### 1. AppResponseCacheAttribute

The `AppResponseCacheAttribute` is the primary interface for configuring caching behavior. Located in `/src/Shared/Infrastructure/Attributes/AppResponseCacheAttribute.cs`, it can be applied to:
- **Blazor pages** (e.g., `HomePage.razor`, `AboutPage.razor`)
- **Web API controller actions** (e.g., methods in controllers)
- **Minimal API endpoints** (e.g., sitemap endpoints)

This attribute caches **HTML, JSON, XML, and other response types** in multiple cache layers.

**Key Properties:**

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AppResponseCacheAttribute : Attribute
{
    /// <summary>
    /// Specifies the cache duration in seconds. This setting caches the response in:
    /// - ASP.NET Core's output cache
    /// - CDN edge servers
    /// - Browser's cache
    /// - App's in-memory cache
    /// 
    /// Note: Browser and in-memory caches cannot be purged automatically, so use with caution.
    /// </summary>
    public int MaxAge { get; set; } = -1;

    /// <summary>
    /// Specifies the cache duration in seconds for shared caches. This setting caches the response in:
    /// - ASP.NET Core's output cache
    /// - CDN edge servers
    /// 
    /// The cache can be purged at any time using ResponseCacheService.
    /// </summary>
    public int SharedMaxAge { get; set; } = -1;

    /// <summary>
    /// Set to true if the response is not affected by the authenticated user.
    /// Allows caching responses on CDN edge and output cache even for authenticated requests.
    /// 
    /// WARNING: If your page/API includes user-specific data (user's name, roles, tenant), 
    /// setting this to true could leak that data to other users via shared caches.
    /// Only set to true if the response is identical for ALL users.
    /// </summary>
    public bool UserAgnostic { get; set; }
}
```

**Usage Examples:**

```csharp
// Example 1: Caching a Blazor page (HomePage.razor)
@page "/"
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24, MaxAge = 60 * 5)]

// SharedMaxAge = 24 hours on CDN/output cache (purgeable)
// MaxAge = 5 minutes on browser/in-memory cache (not purgeable) which improves page navigations when the user navigates back to the locally cached page

// Note: StreamRendering is incompatible with response caching.
// AppResponseCachePolicy automatically disables streaming when current request is configured for response caching.
```

```csharp
// Example 2: Caching Terms page for a week
@page "/terms"
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]

// SharedMaxAge = 7 days on CDN/output cache
// MaxAge = 5 minutes on browser/in-memory cache
```

```csharp
// Example 3: Caching a minimal API endpoint (SiteMapsEndpoint.cs)
app.MapGet("/sitemap_index.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
{
    // Generate sitemap XML
    // Cached for 7 days on CDN and output cache
})
.CacheOutput("AppResponseCachePolicy")
.WithTags("Sitemaps");
```

```csharp
// Example 4: Minimal API with public, user-agnostic data
app.MapGet("/api/minimal-api-sample/{routeParameter}", 
    [AppResponseCache(MaxAge = 3600 * 24)] 
    (string routeParameter, [FromQuery] string queryStringParameter) => new
    {
        RouteParameter = routeParameter,
        QueryStringParameter = queryStringParameter
    })
.WithTags("Test")
.CacheOutput("AppResponseCachePolicy");
```

---

### 2. AppResponseCachePolicy

The `AppResponseCachePolicy` class (located in `/src/Server/Boilerplate.Server.Shared/Infrastructure/Services/AppResponseCachePolicy.cs`) implements the actual caching logic. It's an implementation of ASP.NET Core's `IOutputCachePolicy` interface.

**Key Features:**

- **Intelligent Cache Layer Selection**: Automatically determines which cache layers to use based on context
- **User-Aware Caching**: Prevents authenticated user data from being cached in shared caches
- **Culture Variation**: Handles multi-language caching with culture-specific cache keys
- **Development Mode Handling**: Disables client cache in development for easier debugging
- **Request Type Detection**: Different behavior for Blazor pages vs API requests

Note: **Multi-Language Limitation**: For non-invariant globalization, client and edge caching are disabled for pre-rendered Blazor pages.
It's because it doesn't work with the free Tier of Cloudflare CDN and needs Enterprise plan that supports tag based purging with multiple dimensions (culture + URL).
You can switch to AWS CloudFront or Azure Frontdoor which support this feature for lower/free plans.
Output cache still works correctly for multi-language scenarios.

**Cache Duration Logic:**

```csharp
public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
{
    var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();

    if (responseCacheAtt is null) return;

    context.AllowLocking = true;
    context.EnableOutputCaching = true;

    // What the output cache keys on, besides the request path:
    context.CacheVaryByRules.QueryKeys = "*";
    context.CacheVaryByRules.VaryByHost = true;
    context.CacheVaryByRules.HeaderNames = new[] { HeaderNames.Origin, "X-Origin" };
    context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);

    // Multi-tenant: an authenticated request resolves its tenant from the user's claim rather than from the
    // host, and tenant scoped entities are filtered by it, so two tenants on one host must not share an entry.
    if (context.HttpContext.User.GetTenantId() is Guid currentTenantId)
        context.CacheVaryByRules.VaryByValues.Add("Tenant", currentTenantId.ToString());

    // SharedMaxAge falls back to MaxAge when it isn't set
    var sharedMaxAge = responseCacheAtt.SharedMaxAge == -1 ? responseCacheAtt.MaxAge : responseCacheAtt.SharedMaxAge;

    var clientCacheTtl = responseCacheAtt.MaxAge;  // In-memory + Browser
    var edgeCacheTtl = sharedMaxAge;               // CDN Edge
    var outputCacheTtl = sharedMaxAge;             // ASP.NET Core Output Cache

    // Disable CDN edge if configured
    if (settings.ResponseCaching?.EnableCdnEdgeCaching is false)
        edgeCacheTtl = -1;

    // Disable output cache if configured
    if (settings.ResponseCaching?.EnableOutputCaching is false)
        outputCacheTtl = -1;

    // Disable client cache in development
    if (env.IsDevelopment())
        clientCacheTtl = -1;

    // Security: Disable shared caches for user-specific responses
    if (context.HttpContext.User.IsAuthenticated() && responseCacheAtt.UserAgnostic is false)
    {
        edgeCacheTtl = -1;
        outputCacheTtl = -1;
    }

    // The entry is tagged with the request path only, without the query string: purging is done by bare path
    // ("/product/5"), while QueryKeys = "*" gives every query string variant its own entry. Tagging those with
    // their full PathAndQuery would leave "/product/5?utm_source=x" unpurgeable for the rest of its lifetime.
    context.Tags.Add(new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).AbsolutePath.ToLowerInvariant());

    // ... set cache headers and output cache policy
}
```

**Responses that are never stored:** a response is kept out of every cache unless it is a `200 OK` that hands out no
cookies (the culture cookie is exempt, since the cache varies by culture anyway). That keeps a 404 for a product created
a minute later from surviving on the edge for days, and keeps one caller's cookies from being replayed to everybody else.
This is enforced twice - by an `OnStarting` callback that downgrades `Cache-Control` to `no-store, private` for browsers
and CDNs, and by clearing `AllowCacheStorage` in `ServeResponseAsync` for the output cache.

**Telling shared caches what to vary on:**

The output cache keys on `Origin` and `X-Origin`, so the response advertises them too:

```
Vary: Origin, X-Origin
```

- `Origin` - the CORS middleware runs before the output cache middleware and echoes the caller's origin into
  `Access-Control-Allow-Origin`. Without the vary, the first caller's value would be replayed to every other origin and
  their browsers would reject it.
- `X-Origin` - the header a Blazor Hybrid / standalone WASM client sends to tell the backend which web app url it is
  running under (See `HttpRequestExtensions.GetWebAppUrl`), which can end up embedded in the response.

> **A CDN may ignore `Vary`.** Cloudflare does not consider it in caching decisions unless the header is
> `Accept-Encoding`, or a **Cache Rules → Vary** setting naming `origin` / `x-origin` has been configured on the zone.
> Without that rule the edge keeps a single variant per URL and hands it to callers of every origin. Configure it before
> turning `EnableCdnEdgeCaching` on.

**Important Security Note:**

The `UserAgnostic` property is critical for security. If a response contains user-specific data (e.g., user's name, roles, or tenant information), it **must not** be cached in shared caches (CDN edge or output cache). Setting `UserAgnostic = true` is only safe when the response is identical for all users.

> **Multi-tenant + CDN edge:** the `Tenant` discriminator above is part of the **ASP.NET Core output cache key only** -
> `VaryByValues` never becomes a response header, so a CDN cannot see it. The output cache therefore keeps tenants apart
> correctly, but an edge cache keyed on host + path does not. Until the tenant is part of the URL or the host, treat
> `UserAgnostic = true` together with `EnableCdnEdgeCaching` as unsafe for any response whose body is tenant-filtered.

---

### 3. ResponseCacheService

The `ResponseCacheService` (located in `/src/Server/Boilerplate.Server.Api/Infrastructure/Services/ResponseCacheService.cs`) provides methods to **purge/invalidate cached responses** when data changes.

**Purpose**: When you update data on the server (e.g., edit a product in the admin panel), you need to invalidate the cached versions of pages/APIs that display that data. Otherwise, users will continue to see stale/outdated information until the cache expires naturally.

**Real-World Example - Product Page Caching:**

1. **Initial State**: A product page like `https://sales.bitplatform.dev/product/10036` is viewed and cached on Cloudflare CDN
2. **Data Update**: Admin updates the product at `https://adminpanel.bitplatform.dev/add-edit-product/e7f8a9b0-c1d2-e3f4-5678-9012a3b4c5d6`
3. **Cache Purge**: The server automatically sends a request to Cloudflare to purge/remove that page from the Edge Cache
4. **Next Request**: The next user who visits the product page gets the updated version (which is then cached again)

**Key Methods:**

```csharp
public partial class ResponseCacheService
{
    /// <summary>
    /// Purges cache for specific URL paths from both ASP.NET Core output cache and CDN edge cache
    /// </summary>
    public async Task PurgeCache(params string[] relativePaths)
    {
        // Purge from ASP.NET Core output cache. Lowercased to match the tag the policy writes.
        foreach (var relativePath in relativePaths)
        {
            await outputCacheStore.EvictByTagAsync(relativePath.ToLowerInvariant(), default);
        }
        
        // Purge from Cloudflare CDN
        await PurgeCloudflareCache(relativePaths);
    }

    /// <summary>
    /// Convenience method to purge all product-related caches
    /// </summary>
    public async Task PurgeProductCache(int shortId)
    {
        await PurgeCache(
            "/",                                     // Home page (may list products)
            $"/product/{shortId}",                   // Product detail page
            $"/api/v1/ProductView/Get/{shortId}"     // Product API endpoint
        );
    }
}
```

**Usage in Controllers:**

```csharp
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);

    // Purge all caches for this product
    await responseCacheService.PurgeProductCache(entityToUpdate.ShortId);

    return entityToUpdate.Map();
}

[HttpDelete("{id}/{version}")]
public async Task Delete(Guid id, string version, CancellationToken cancellationToken)
{
    // ... delete logic ...
    await DbContext.SaveChangesAsync(cancellationToken);

    // Purge all caches for this product
    await responseCacheService.PurgeProductCache(entityToDelete.ShortId);
}
```

**Important Note:** 
- For successful cache purging, the request URL must **exactly match** the URL passed to `PurgeCache()`. 
- Query strings and route parameters must match precisely.
- This only purges **CDN edge cache** and **ASP.NET Core output cache** (the purgeable layers)
- **Browser cache** and **Client In-Memory Cache** cannot be purged remotely (this is why `MaxAge` should be used cautiously)

**Cache-Busting Strategy for Non-Purgeable Caches:**

Since browser cache and Client In-Memory Cache cannot be purged remotely, use **versioned URLs** (cache-busting) to ensure users see updated content. This technique appends a version parameter to the URL that changes when data is updated.

```csharp
// Example from ProductDto.cs - Product image URL with version parameter
public string? GetPrimaryMediumImageUrl(Uri absoluteServerAddress)
{
    return HasPrimaryImage is false
        ? null
        : new Uri(absoluteServerAddress, 
            $"/api/v1/Attachment/GetAttachment/{Id}/{AttachmentKind.ProductPrimaryImageMedium}?v={Version}")
            .ToString();
}
```

**How it works:**
- The `Version` property (a `long` used for optimistic concurrency) changes every time the entity is updated
- When the product is updated, the version changes, creating a **new URL** that bypasses all cached versions
- The browser/Client In-Memory Cache treats this as a completely new resource and fetches fresh data

This pattern is ideal for assets like images, documents, or any content where you want aggressive caching but also need immediate updates when data changes.

---

### 4. Client-Side In-Memory Cache (CacheDelegatingHandler)

The `CacheDelegatingHandler` (located in `/src/Client/Boilerplate.Client.Core/Infrastructure/Services/HttpMessageHandlers/CacheDelegatingHandler.cs`) implements client-side in-memory caching for HTTP responses.

**How It Works:**

```csharp
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
    var cacheKey = $"{request.Method}-{request.RequestUri}";
    var useCache = AppEnvironment.IsDevelopment() is false && AppPlatform.IsBlazorHybridOrBrowser;

    // Try to get from cache
    if (useCache && memoryCache.TryGetValue(cacheKey, out ResponseMemoryCacheItems? cachedResponse))
    {
        // Return cached response SYNCHRONOUSLY (instant, no loading indicators!)
        memoryCacheStatus = "HIT";
        return CreateHttpResponseFromCache(cachedResponse);
    }

    // Make actual request
    var response = await base.SendAsync(request, cancellationToken);

    // Cache if response has Cache-Control: max-age
    if (useCache && response.IsSuccessStatusCode && 
        response.Headers.CacheControl?.MaxAge is TimeSpan maxAge && maxAge > TimeSpan.Zero)
    {
        memoryCacheStatus = "MISS";
        var responseContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        memoryCache.Set(cacheKey, new ResponseMemoryCacheItems
        {
            Content = responseContent,
            StatusCode = response.StatusCode,
            ResponseHeaders = response.Headers.ToDictionary(),
            ContentHeaders = response.Content.Headers.ToDictionary()
        }, maxAge);
    }

    return response;
}
```

**Key Features:**
- Only active in **non-development** environments
- Only works for **Blazor Hybrid and Browser** platforms (not server-side rendering)
- **Works on all client platforms**: Web browsers, .NET MAUI mobile apps, Windows desktop apps
- Respects the `Cache-Control: max-age` header from server responses
- Stores entire HTTP response (content, status code, and headers)
- **Synchronous response**: Returns cached data instantly without any async delay
- **No loading indicators**: Prevents spinners, shimmers, and skeleton UIs from appearing
- Provides fastest possible response time for repeated requests

**Real-World Example:**
If you navigate between products on `https://sales.bitplatform.dev`:
1. **Open website** by navigating to [https://sales.bitplatform.dev](https://sales.bitplatform.dev)
2. **First visit** to Product A: Server request, data cached in memory
3. **Navigate** to Product B: Server request, data cached in memory
4. **Navigate back** to Product A: **Instant load** from memory cache - no loading indicator, no spinner, no shimmer - the page appears instantly!

This creates an exceptionally smooth user experience because the app feels native and responsive.

**Important Notes:**
- **Client In-Memory Cache** is cleared when the app is closed (doesn't persist across sessions)
- **Browser HTTP cache** persists even after closing the browser, but it's asynchronous (shows loading briefly)
- The combination of both provides the best user experience:
  - Instant loads during the current session (Client In-Memory Cache)
  - Fast loads on return visits (browser cache)

When navigating back to the Home page from Page A, you may encounter loading indicators. This is expected behavior: the initial page load doesn't send any HTTP requests to the server, as it fetches all required data from the pre-rendered state. As a result, `CacheDelegatingHandler.cs` doesn't cache anything for it.

---

## The 4-Layer Caching Architecture

### Request Flow and Cache Layer Order

When a user makes a request, it flows through these layers in order:

```
┌─────────────────────────────────────────────────────────────┐
│  Client makes request: GET /api/ProductView/Get/123         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  1. Client In-Memory Cache Check (CacheDelegatingHandler)   │
│     - Fastest (microseconds - SYNCHRONOUS)                   │
│     - No loading indicators, spinners, or shimmers           │
│     - Only works during current app session                  │
│     - Not purgeable                                          │
└─────────────────────────────────────────────────────────────┘
        │ MISS                          │ HIT
        ▼                               └──────► Return from memory (INSTANT)
┌─────────────────────────────────────────────────────────────┐
│  2. Browser HTTP Cache Check (Standard Browser Cache)       │
│     - Very fast (milliseconds - ASYNCHRONOUS)                │
│     - Shows loading indicators briefly                       │
│     - Persists across app sessions/browser restarts         │
│     - Not purgeable by server                                │
└─────────────────────────────────────────────────────────────┘
        │ MISS                          │ HIT
        ▼                               └──────► Return from browser
┌─────────────────────────────────────────────────────────────┐
│  Request goes to network                                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  3. CDN Edge Cache Check (Cloudflare)                       │
│     - Fast (10-50ms)                                         │
│     - Purgeable via ResponseCacheService                     │
│     - Global distribution (serves from nearest edge)         │
└─────────────────────────────────────────────────────────────┘
        │ MISS                          │ HIT
        ▼                               └──────► Return from CDN
┌─────────────────────────────────────────────────────────────┐
│  Request reaches ASP.NET Core server                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  4. Output Cache Check (ASP.NET Core)                       │
│     - Medium speed (50-100ms)                                │
│     - Purgeable via ResponseCacheService                     │
│     - Can use Memory or Redis backend                        │
└─────────────────────────────────────────────────────────────┘
        │ MISS                          │ HIT
        ▼                               └──────► Return from output cache
┌─────────────────────────────────────────────────────────────┐
│  Execute controller action / Query database                  │
│  Generate response                                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Response flows back through all cache layers               │
│  Each layer caches according to its configuration           │
└─────────────────────────────────────────────────────────────┘
```

### Comparison Table

| Layer | Location | Speed | Scope | Purgeable | Controlled By | Best For |
|-------|----------|-------|-------|-----------|---------------|----------|
| **1. Client In-Memory Cache** | Client app memory | ⚡ Fastest (microseconds, **sync**) | Single user, current session only | ❌ No | `MaxAge` | Instant navigation between pages user already visited |
| **2. Browser HTTP Cache** | Browser's HTTP cache | 🚀 Very Fast (milliseconds, async) | Single user, persists across sessions | ❌ No | `MaxAge` | Returning to pages after closing/reopening app |
| **3. CDN Edge** | Cloudflare/CDN edge | 💨 Fast (10-50ms) | Global, shared across all users | ✅ Yes | `SharedMaxAge` | Public content served to many users worldwide |
| **4. Output Cache** | ASP.NET Core server | ⏱️ Medium (50-100ms) | Server-level, shared across users | ✅ Yes | `SharedMaxAge` | Pre-rendered pages, API responses |

### Important Security Note

**User-Specific Content Protection:**
- If a user is authenticated AND `UserAgnostic = false`, the response is **NOT cached** in:
  - ❌ CDN Edge Cache
  - ❌ ASP.NET Core Output Cache
- But it **CAN still be cached** in:
  - ✅ Browser HTTP Cache (user's own browser)
  - ✅ Client In-Memory Cache (user's own app instance)

This prevents accidentally serving User A's data to User B through shared caches.

---

## Configuration

### appsettings.json

```json
{
  "ResponseCaching": {
    "EnableOutputCaching": true,  // ASP.NET Core output cache
    "EnableCdnEdgeCaching": true  // CDN edge caching
  },
  "Cloudflare": {
    "ZoneId": "your-cloudflare-zone-id",
    "ApiToken": "your-cloudflare-api-token",
    "AdditionalDomains": [
      "https://sales.bitplatform.ai",
      "https://sales.bitplatform.com",
      "https://sales.bitplatform.uk"
    ]
  },
  // Shared/appsettings.json - shared by the server AND every client (WASM, MAUI, Windows)
  "MemoryCache": {
    "SizeLimit": 268435456  // 256 MB, in bytes
  }
}
```

Both `ResponseCaching` flags default to `false`. Turn `EnableCdnEdgeCaching` on only after reading the `Vary` and
multi-tenant notes above.

---

## The L1 Memory Budget

Layers 1 and 4 (Client In-Memory Cache and Output Cache) both live in the app's single `IMemoryCache`, which is bounded
by `MemoryCache:SizeLimit` in `Shared/appsettings.json` and implemented by `AppMemoryCache`.

**The unit is bytes, not entries.** That matters, because the three kinds of entry are charged differently:

| Entry | Charged | Set by |
|---|---|---|
| Output cache response body | its exact length | `FusionOutputCacheStore` (`AddFusionOutputCache`) |
| Client in-memory cached response | its exact length | `CacheDelegatingHandler` |
| Everything else (FusionCache data entries, 3rd party libraries) | `AppMemoryCache.EstimatedEntrySizeInBytes` (4 KB) | `WithDefaultEntryOptions` / `AppMemoryCache.CreateEntry` |

The flat 4 KB estimate is deliberately generous: charging an entry too much only means fewer of them fit, while charging
too little lets the cache outgrow the limit it exists to enforce. At 256 MB the budget holds roughly 65k estimated
entries, minus whatever the cached response bodies take.

**Why not count entries instead?** Because the output cache stores whole response bodies here. A single pre-rendered
page or attachment would otherwise cost the same one unit as a small dictionary, letting one big response quietly
consume a budget sized for tens of thousands of small ones - or, worse, be silently rejected once the limit was reached,
turning output caching into a no-op with no error anywhere.

If you raise `SizeLimit`, remember the same value ships to the clients: it also bounds the Blazor Hybrid / WASM app's
in-process cache on a phone, not just the server's.

---

## FusionCache Library

The project uses the **FusionCache** library for server-side caching:

- **Output Cache Backend**: Powers the ASP.NET Core Output Cache implementation (Layer 4)
- **Data Caching**: Provides data caching via `IFusionCache` interface for caching arbitrary data (database query results, computed values, etc.) in addition to HTTP responses
- **Flexible Storage**: Supports multiple backends (in-memory, Redis, hybrid etc) for both response and data caching

---

## Redis Infrastructure

The project uses **two separate Redis instances** for different purposes:

### 1. redis-cache Ephemeral Cache
- **No persistence** (data stored only in memory)
- **Use Cases**: 
  - **FusionCache** L2 distributed cache and backplane for multi-server cache synchronization
  - **SignalR backplane** for real-time messaging across servers
- **Why**: Cache data is regenerable, no need for disk I/O overhead

### 2. redis-persistent - Persistent Storage
- **AOF enabled** with synchronous disk writes for maximum durability
- **Use Cases**: 
  - **Hangfire** background job queues and state
  - **Distributed locking** for coordinating operations
- **Why**: Critical data that cannot be easily regenerated must survive restarts

**Benefits**: Separation allows ephemeral cache to run faster while ensuring critical infrastructure data is never lost.

---

### Monitor Cache Headers

The system adds custom headers to help debug caching:

```
App-Cache-Response: Output:3600,Edge:3600,Client:3600
```

This shows the TTL (in seconds) for each cache layer. Use browser DevTools Network tab to inspect:

```
Cache-Control: public, max-age=300, s-maxage=3600
Vary: Origin, X-Origin
App-Cache-Response: Output:3600,Edge:3600,Client:300
```

Interpretation:
- `max-age=300`: Browser and in-memory cache for 5 minutes
- `s-maxage=3600`: CDN edge and output cache for 1 hour
- `public`: Can be cached in shared caches (CDN)
- `Vary`: the request headers a shared cache must include in its key
- `Output:-1` (or `Edge:-1` / `Client:-1`) means that layer was disabled for this request - by configuration, by the
  caller being authenticated on a non-`UserAgnostic` endpoint, or by the request being a pre-rendered Blazor page

A response that turns out not to be cacheable (anything other than `200 OK`, or one that sets a cookie) is downgraded to
`Cache-Control: no-store, private` on its way out, regardless of what `App-Cache-Response` announced earlier in the
request.

---

### Automated Test

`src/Tests/Features/Caching/ProductResponseCacheTests.cs` exercises the whole loop end to end with
`EnableOutputCaching` and `PrerenderEnabled` both on: it fills the output cache from two directions (the tenant-user
calling the `UserAgnostic` product API, and an anonymous visitor loading the pre-rendered product page), has the
tenant-admin edit the product through `ProductController.Update` and asserts both readers see the change immediately,
then deletes the row straight from the database and asserts both readers keep being served the deleted product - the
proof that the responses are coming from the cache and not the database.

Note it needs `ProductController` (Admin module) and `ProductViewController` (Sales module) at the same time, and
`module` is a single-choice template parameter, so it is excluded from generated projects and only runs against the
template's own source tree.

---

### AI Wiki: Answered Questions
* [How does the bit Boilerplate AttachmentController interact with response caching? Why do users always see the latest profile pictures, even though no PurgeCache has been called and these assets are stored in the browser cache, which cannot be automatically purged?](https://deepwiki.com/search/how-does-the-bit-boilerplate-a_4f042d5f-3ffb-4c14-b661-bb923825c21d)
* [Why response caching doesn't work with stream pre-rendering in bit Boilerplate?](https://deepwiki.com/search/why-response-caching-doesnt-wo_2de1ba6c-1017-4c77-96f5-33c8ed001760)

Ask your own question [here](https://wiki.bitplatform.dev)

---