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

The `AppResponseCacheAttribute` is the primary interface for configuring caching behavior. Located in `src/Shared/Attributes/AppResponseCacheAttribute.cs`, it can be applied to:
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
@attribute [StreamRendering(enabled: true)]
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24, MaxAge = 60 * 5)]

// SharedMaxAge = 24 hours on CDN/output cache (purgeable)
// MaxAge = 5 minutes on browser/in-memory cache (not purgeable) which improves page naviagations when the user navigates back to the locally cached page
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

The `AppResponseCachePolicy` class (located in `src/Server/Boilerplate.Server.Shared/Services/AppResponseCachePolicy.cs`) implements the actual caching logic. It's an implementation of ASP.NET Core's `IOutputCachePolicy` interface.

**Key Features:**

- **Intelligent Cache Layer Selection**: Automatically determines which cache layers to use based on context
- **User-Aware Caching**: Prevents authenticated user data from being cached in shared caches
- **Culture Variation**: Handles multi-language caching with culture-specific cache keys
- **Development Mode Handling**: Disables client cache in development for easier debugging
- **Request Type Detection**: Different behavior for Blazor pages vs API requests

**Cache Duration Logic:**

```csharp
public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
{
    var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();
    
    if (responseCacheAtt is null) return;

    // Default: SharedMaxAge = MaxAge if not specified
    if (responseCacheAtt.SharedMaxAge == -1)
    {
        responseCacheAtt.SharedMaxAge = responseCacheAtt.MaxAge;
    }

    var clientCacheTtl = responseCacheAtt.MaxAge;      // In-memory + Browser
    var edgeCacheTtl = responseCacheAtt.SharedMaxAge;  // CDN Edge
    var outputCacheTtl = responseCacheAtt.SharedMaxAge; // ASP.NET Core Output Cache

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

    // ... set cache headers and output cache policy
}
```

**Important Security Note:**

The `UserAgnostic` property is critical for security. If a response contains user-specific data (e.g., user's name, roles, or tenant information), it **must not** be cached in shared caches (CDN edge or output cache). Setting `UserAgnostic = true` is only safe when the response is identical for all users.

---

### 3. ResponseCacheService

The `ResponseCacheService` (located in `src/Server/Boilerplate.Server.Api/Services/ResponseCacheService.cs`) provides methods to **purge/invalidate cached responses** when data changes.

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
        // Purge from ASP.NET Core output cache
        foreach (var relativePath in relativePaths)
        {
            await outputCacheStore.EvictByTagAsync(relativePath, default);
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
            "/",                                  // Home page (may list products)
            $"/product/{shortId}",                // Product detail page
            $"/api/ProductView/Get/{shortId}"     // Product API endpoint
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
- **Browser cache** and **in-memory cache** cannot be purged remotely (this is why `MaxAge` should be used cautiously)

---

### 4. Client-Side In-Memory Cache (CacheDelegatingHandler)

The `CacheDelegatingHandler` (located in `src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/CacheDelegatingHandler.cs`) implements client-side in-memory caching for HTTP responses.

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
- **Client-side memory cache** is cleared when the app is closed (doesn't persist across sessions)
- **Browser HTTP cache** persists even after closing the browser, but it's asynchronous (shows loading briefly)
- The combination of both provides the best user experience:
  - Instant loads during the current session (memory cache)
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
| **1. Client In-Memory** | Client app memory | ⚡ Fastest (microseconds, **sync**) | Single user, current session only | ❌ No | `MaxAge` | Instant navigation between pages user already visited |
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
  "ServerApiSettings": {
    "ResponseCaching": {
      "EnableOutputCaching": true,  // ASP.NET Core output cache
      "EnableCdnEdgeCaching": true  // CDN edge caching
    },
    "Cloudflare": {
      "Configured": true,
      "ZoneId": "your-cloudflare-zone-id",
      "ApiToken": "your-cloudflare-api-token",
      "AdditionalDomains": [
        "https://sales.bitplatform.ai",
        "https://sales.bitplatform.com",
        "https://sales.bitplatform.uk",
      ]
    }
  }
}
```

---

## FusionCache Library

The project uses the **FusionCache** library for server-side caching:

- **Output Cache Backend**: Powers the ASP.NET Core Output Cache implementation (Layer 4)
- **Data Caching**: Provides data caching via `IFusionCache` interface for caching arbitrary data (database query results, computed values, etc.) in addition to HTTP responses
- **Flexible Storage**: Supports multiple backends (in-memory, Redis, hybrid etc) for both response and data caching

---

### Monitor Cache Headers

The system adds custom headers to help debug caching:

```
App-Cache-Response: Output:3600,Edge:3600,Client:3600
```

This shows the TTL (in seconds) for each cache layer. Use browser DevTools Network tab to inspect:

```
Cache-Control: public, max-age=300, s-maxage=3600
App-Cache-Response: Output:3600,Edge:3600,Client:300
```

Interpretation:
- `max-age=300`: Browser and in-memory cache for 5 minutes
- `s-maxage=3600`: CDN edge and output cache for 1 hour
- `public`: Can be cached in shared caches (CDN)

---

### AI Wiki: Answered Questions
* [How does the bit Boilerplate AttachmentController interact with response caching? Why do users always see the latest profile pictures, even though no PurgeCache has been called and these assets are stored in the browser cache, which cannot be automatically purged?](https://deepwiki.com/search/how-does-the-bit-boilerplate-a_4f042d5f-3ffb-4c14-b661-bb923825c21d)
* [Why response caching doesn't work with stream pre-rendering in bit Boilerplate?](https://deepwiki.com/search/why-response-caching-doesnt-wo_2de1ba6c-1017-4c77-96f5-33c8ed001760)

Ask your own question [here](https://wiki.bitplatform.dev)

---