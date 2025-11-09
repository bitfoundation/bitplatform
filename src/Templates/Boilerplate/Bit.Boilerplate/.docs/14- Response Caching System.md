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
- This only applies to responses where `UserAgnostic` is not false (default)
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
// MaxAge = 5 minutes on browser/in-memory cache (not purgeable)
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

[HttpDelete("{id}/{concurrencyStamp}")]
public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
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
1. **First visit** to Product A: Server request, data cached in memory
2. **Navigate** to Product B: Server request, data cached in memory
3. **Navigate back** to Product A: **Instant load** from memory cache - no loading indicator, no spinner, no shimmer - the page appears instantly!

This creates an exceptionally smooth user experience because the app feels native and responsive.

**Important Notes:**
- **Client-side memory cache** is cleared when the app is closed (doesn't persist across sessions)
- **Browser HTTP cache** persists even after closing the browser, but it's asynchronous (shows loading briefly)
- The combination of both provides the best user experience:
  - Instant loads during the current session (memory cache)
  - Fast loads on return visits (browser cache)

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

### When Each Layer is Used

**Client In-Memory Cache:**
- ✅ Only if `MaxAge > 0`
- ✅ Only in non-development environments
- ✅ Only for Blazor Hybrid and Browser platforms (not server-side rendering)
- ✅ Response must be successful (2xx status code)
- ⚠️ **Cannot be purged** - cache clears when app closes
- 🎯 **Unique benefit**: Returns data **synchronously** - no loading indicators!

**Browser HTTP Cache:**
- ✅ Only if `MaxAge > 0`
- ✅ Respects HTTP `Cache-Control` headers
- ✅ Stored per browser/user
- ✅ Persists across browser restarts
- ⚠️ **Cannot be purged by server** - use cautiously for data that may change
- ❌ Disabled in development mode

**CDN Edge Cache:**
- ✅ Only if `SharedMaxAge > 0` (or `MaxAge > 0` if SharedMaxAge not set)
- ✅ Only if `EnableCdnEdgeCaching = true` in settings
- ✅ Only if `UserAgnostic = true` OR user is not authenticated
- ✅ **Can be purged** via `ResponseCacheService`
- 🎯 **Global reach**: Serves content from nearest geographic location

**ASP.NET Core Output Cache:**
- ✅ Only if `SharedMaxAge > 0` (or `MaxAge > 0` if SharedMaxAge not set)
- ✅ Only if `EnableOutputCaching = true` in settings
- ✅ Only if `UserAgnostic = true` OR user is not authenticated
- ✅ **Can be purged** via `ResponseCacheService`
- 🎯 **Flexible backend**: Can use Memory (single server) or Redis (distributed)

### Important Security Note

**User-Specific Content Protection:**
- If a user is authenticated AND `UserAgnostic = false`, the response is **NOT cached** in:
  - ❌ CDN Edge Cache
  - ❌ ASP.NET Core Output Cache
- But it **CAN still be cached** in:
  - ✅ Browser HTTP Cache (user's own browser)
  - ✅ Client In-Memory Cache (user's own app instance)

This prevents accidentally serving User A's data to User B through shared caches.

### Central Control: AppResponseCache Attribute

**All four cache layers are controlled by the single `AppResponseCache` attribute:**

- **MaxAge property** controls:
  - Client-side memory cache (`CacheDelegatingHandler.cs`)
  - Browser's HTTP cache (standard browser caching)

- **SharedMaxAge property** controls:
  - ASP.NET Core Output Cache (server-side, can use Memory or Redis as `IDistributedCache`)
  - CDN Edge Cache (e.g., Cloudflare)

This unified approach makes cache configuration simple and consistent across all layers.

---

## Key Benefits

### 1. Exceptional Performance Improvement

With all 4 cache layers working together:
- **First request**: ~200ms (database query + rendering)
- **Subsequent requests from same client**:
  - From in-memory cache: **< 1ms** (synchronous, instant, no loading UI)
  - From browser cache: **~5ms** (async, brief loading indicator)
  - From CDN edge: **~20ms** (network latency to nearest edge location)
  - From output cache: **~80ms** (server processing without DB query)

**Real-World Example:**
Every page refresh on `https://sales.bitplatform.dev` product pages adds **zero overhead** to the server. The complete response is served directly from Cloudflare's edge servers (CDN), providing:
- Lightning-fast response times globally
- Zero database queries
- Zero server CPU usage
- Zero server memory usage

### 2. Instant Navigation Experience

The client-side in-memory cache provides a **native app-like experience**:
- When navigating back to previously visited pages, content appears **instantly**
- No loading spinners, no skeleton UIs, no shimmers
- Response is **synchronous** rather than **asynchronous**
- Users perceive the app as incredibly fast and responsive

Example: Navigate through products on `https://sales.bitplatform.dev` and notice how returning to a previously viewed product is instantaneous.

### 3. Reduced Server Load

- CDN edge cache handles most static and public content
- Output cache prevents redundant database queries
- Server CPU and database are freed up for other operations
- Enables handling significantly more concurrent users with the same hardware

### 4. Global Performance

- CDN edge locations serve content from the closest geographic location to the user
- Users in Asia, Europe, and Americas all get fast response times
- Reduces latency for international users from hundreds of milliseconds to tens of milliseconds

### 5. Cost Savings

- Reduced database queries → lower database costs
- Lower server CPU usage → smaller server instances needed
- Reduced bandwidth costs → CDN serves cached content
- Better resource utilization → same infrastructure handles more traffic

### 6. Scalability

- Application can handle significantly more concurrent users
- Cache layers absorb traffic spikes gracefully
- Database remains responsive under high load
- Reduced need for expensive horizontal scaling

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
        "https://www.example.com",
        "https://api.example.com"
      ]
    }
  }
}
```

---

## Best Practices

### 1. Use MaxAge vs SharedMaxAge Appropriately

**Understanding the Difference:**
- `MaxAge`: Caches in **client-side memory** AND **browser cache** (not purgeable)
- `SharedMaxAge`: Caches in **CDN edge** AND **ASP.NET Core output cache** (purgeable)

```csharp
// ❌ BAD: User-specific content with MaxAge
[AppResponseCache(MaxAge = 3600)] // Browser will cache user's data!
public async Task<UserDto> GetCurrentUser() { }
// Problem: User logs out and back in as different user, browser shows old cached data

// ✅ GOOD: User-specific content with SharedMaxAge only
[AppResponseCache(SharedMaxAge = 60)] // Only CDN/output cache (purgeable)
public async Task<UserDto> GetCurrentUser() { }
// Better: If user changes, server can purge the cache

// ✅ GOOD: Public content with both MaxAge and SharedMaxAge
@page "/"
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24, MaxAge = 60 * 5)]
// Home page is same for everyone, safe to cache everywhere
// SharedMaxAge = 24 hours (purgeable when content changes)
// MaxAge = 5 minutes (acceptable staleness in browser/memory)

// ✅ GOOD: Static content with long MaxAge
[AppResponseCache(MaxAge = 3600 * 24 * 365, UserAgnostic = true)]
public IActionResult GetVersionedAsset() { }
// Static files with versioned URLs can be cached for a year
```

### 2. Always Purge Cache After Mutations

Every time you UPDATE, DELETE, or CREATE data that affects cached responses, you **must** purge the cache:

```csharp
// ✅ GOOD: Purge cache after update
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // Purge ALL URLs that display this product
    await responseCacheService.PurgeCache(
        "/",                                   // Home page (may list products)
        "/products",                           // Product list page
        $"/api/ProductView/Get/{dto.ShortId}", // API endpoint
        $"/product/{dto.ShortId}"              // Product detail page
    );
    
    return entityToUpdate.Map();
}

// ✅ GOOD: Purge cache after delete
[HttpDelete("{id}/{concurrencyStamp}")]
public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
{
    // ... delete logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // Purge cache for this product
    await responseCacheService.PurgeProductCache(entityToDelete.ShortId);
    
    // Also purge list pages since product is removed
    await responseCacheService.PurgeCache("/", "/products");
}

// ❌ BAD: Forgot to purge cache
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // ❌ No cache purge! Users will see old data until cache expires naturally
    
    return entityToUpdate.Map();
}
```

### 3. Use UserAgnostic Carefully

The `UserAgnostic` property determines whether authenticated user data can be cached in shared caches (CDN and output cache).

```csharp
// ❌ BAD: User-specific data marked as UserAgnostic
[Authorize]
[AppResponseCache(MaxAge = 3600, UserAgnostic = true)] // ❌ DANGEROUS!
public async Task<OrderDto[]> GetMyOrders() 
{
    var userId = User.GetUserId();
    return await DbContext.Orders.Where(o => o.UserId == userId).ToArrayAsync();
    // This will cache User A's orders on CDN and show them to User B!
}

// ✅ GOOD: User-specific data WITHOUT UserAgnostic
[Authorize]
[AppResponseCache(SharedMaxAge = 60)] // UserAgnostic defaults to false
public async Task<OrderDto[]> GetMyOrders() 
{
    var userId = User.GetUserId();
    return await DbContext.Orders.Where(o => o.UserId == userId).ToArrayAsync();
    // Automatically prevented from caching in CDN/output cache
    // Can still cache in user's browser/memory (safe since it's their own cache)
}

// ✅ GOOD: Truly public data marked as UserAgnostic
[AllowAnonymous]
[AppResponseCache(MaxAge = 3600 * 24, UserAgnostic = true)]
public async Task<ProductDto[]> GetAllProducts() 
{
    return await DbContext.Products.ToArrayAsync();
    // Same for everyone, safe to cache on CDN
}

// ✅ GOOD: Public sitemap
app.MapGet("/sitemap.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] 
    async (context) =>
{
    // Sitemap is same for all users
    // No UserAgnostic needed for anonymous endpoints
});
```

### 4. Vary Cache Duration by Content Type

Different types of content should have different cache durations:

```csharp
// Images/Attachments: Long cache (rarely change)
[AllowAnonymous]
[HttpGet("{attachmentId}/{kind}")]
[AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true)] // 7 days
public async Task<IActionResult> GetAttachment(Guid attachmentId, AttachmentKind kind)
{
    // Images and files rarely change
    // Safe to cache for a week
}

// API data: Short cache (changes frequently)
[HttpGet]
[AppResponseCache(SharedMaxAge = 300)] // 5 minutes
public IQueryable<ProductDto> GetProducts()
{
    // Product data changes often
    // Cache briefly, can be purged when data changes
}

// Static pages: Medium cache
@page "/terms"
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
// Terms page changes infrequently
// SharedMaxAge = 7 days (purgeable)
// MaxAge = 5 minutes (acceptable staleness)

// Home page: Balanced cache
@page "/"
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24, MaxAge = 60 * 5)]
// SharedMaxAge = 24 hours on CDN (can purge when content changes)
// MaxAge = 5 minutes in browser (acceptable staleness)

// Versioned static assets: Very long cache
[AppResponseCache(MaxAge = 3600 * 24 * 365, UserAgnostic = true)] // 1 year
public IActionResult GetVersionedScript(string version)
{
    // Files with version in URL never change
    // Can cache for extremely long periods
}
```

### 5. Monitor Cache Headers

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

## Debugging Tips

### Check if Caching is Active

1. **Open browser DevTools** → **Network tab**
2. **Make a request** to a cached endpoint
3. **Check Response Headers**:
   - `App-Cache-Response: Output:3600,Edge:3600,Client:300` - Shows TTL for each layer
   - `Cache-Control: public, max-age=300, s-maxage=3600` - Standard HTTP cache header

**Example Headers for Cached Response:**
```
HTTP/1.1 200 OK
Cache-Control: public, max-age=300, s-maxage=3600
App-Cache-Response: Output:3600,Edge:3600,Client:300
```

**Example Headers for Non-Cached Response:**
```
HTTP/1.1 200 OK
Cache-Control: no-cache, no-store
App-Cache-Response: Output:-1,Edge:-1,Client:-1
```

### Check Client-Side In-Memory Cache Status

The client-side logging includes `MemoryCacheStatus` in structured logs:

```json
{
  "MemoryCacheStatus": "HIT",
  "RequestPath": "/api/ProductView/Get/123",
  "Duration": "0.5ms"
}
```

Possible values:
- `"DYNAMIC"` - Response not cached (no Cache-Control header with max-age)
- `"MISS"` - Request made to server, response will be cached
- `"HIT"` - Response served from in-memory cache (instant, synchronous)

**How to view logs:**
1. Open browser DevTools → **Console tab**
2. Look for log entries with `MemoryCacheStatus`
3. Filter by "HIT" to see cached responses

**Or use Application Insights / OpenTelemetry to view structured logs in production**

### Verify Cache Purging Works

**Test the purge flow:**

1. **Initial Request (MISS)**:
   ```bash
   curl -I https://yourapp.com/product/123
   # Should be slow (200ms+) - cache MISS
   # Check: App-Cache-Response header exists
   ```

2. **Second Request (HIT)**:
   ```bash
   curl -I https://yourapp.com/product/123
   # Should be fast (<50ms) - cache HIT
   ```

3. **Update the Product** (triggers cache purge):
   ```bash
   # Use admin panel to edit product 123
   # Or call API:
   curl -X PUT https://yourapp.com/api/Product/Update -d '{...}'
   ```

4. **Third Request (MISS again)**:
   ```bash
   curl -I https://yourapp.com/product/123
   # Should be slow again (200ms+) - cache was purged!
   ```

5. **Fourth Request (HIT)**:
   ```bash
   curl -I https://yourapp.com/product/123
   # Fast again (<50ms) - re-cached after purge
   ```

### Test Client-Side In-Memory Cache

**Navigate between pages in the app:**

1. **Visit Product A** - Look for `MemoryCacheStatus: "MISS"` in logs
2. **Visit Product B** - Look for `MemoryCacheStatus: "MISS"` in logs
3. **Go back to Product A** - Look for `MemoryCacheStatus: "HIT"` in logs
4. **Notice**: No loading spinner, instant response!

### Cloudflare Cache Status

If using Cloudflare CDN, check the `CF-Cache-Status` header:

```
CF-Cache-Status: HIT
CF-Ray: 8a9b8c7d6e5f4g3h-SJC
```

Possible values:
- `HIT` - Served from Cloudflare edge cache
- `MISS` - Not in cache, fetched from origin server
- `EXPIRED` - Was in cache but expired, revalidated
- `DYNAMIC` - Marked as non-cacheable
- `BYPASS` - Origin server told Cloudflare not to cache

---

## Common Pitfalls

### ❌ Pitfall 1: Forgetting to Purge Cache

**Problem:** Cache is never purged, users see stale/outdated data

```csharp
// BAD: Cache is never purged, users see stale data
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    var entity = await DbContext.Products.FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken)
        ?? throw new ResourceNotFoundException(Localizer.GetString(nameof(AppStrings.ProductCouldNotBeFound)));
    
    dto.Patch(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // ❌ Forgot to purge cache!
    // Users will see old product name/price until cache expires naturally (could be days!)
    
    return entity.Map();
}
```

**Solution:**
```csharp
// GOOD: Cache is purged immediately after update
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    var entity = await DbContext.Products.FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken)
        ?? throw new ResourceNotFoundException(Localizer.GetString(nameof(AppStrings.ProductCouldNotBeFound)));
    
    dto.Patch(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // ✅ Purge all caches for this product
    await responseCacheService.PurgeProductCache(entity.ShortId);
    
    return entity.Map();
}
```

### ❌ Pitfall 2: Wrong UserAgnostic Setting

**Problem:** User-specific data cached in shared caches, leaked to other users

```csharp
// BAD: User-specific data cached in shared caches - SECURITY VULNERABILITY!
[Authorize]
[AppResponseCache(MaxAge = 3600, UserAgnostic = true)] // ❌ DANGEROUS!
public async Task<OrderDto[]> GetMyOrders() 
{
    var userId = User.GetUserId();
    var orders = await DbContext.Orders
        .Where(o => o.UserId == userId)
        .ProjectTo<OrderDto>()
        .ToArrayAsync();
    
    return orders;
    
    // PROBLEM: User A's orders are cached on CDN with URL like /api/Order/GetMyOrders
    // User B makes the same request to /api/Order/GetMyOrders
    // CDN returns User A's cached orders to User B!
    // SECURITY BREACH: User B sees User A's private order data!
}
```

**Solution:**
```csharp
// GOOD: User-specific data NOT cached in shared caches
[Authorize]
[AppResponseCache(SharedMaxAge = 60)] // UserAgnostic defaults to false
public async Task<OrderDto[]> GetMyOrders() 
{
    var userId = User.GetUserId();
    var orders = await DbContext.Orders
        .Where(o => o.UserId == userId)
        .ProjectTo<OrderDto>()
        .ToArrayAsync();
    
    return orders;
    
    // SAFE: Because UserAgnostic = false (default), authenticated requests are NOT cached in:
    // - CDN edge cache (prevented)
    // - Output cache (prevented)
    // 
    // But CAN still be cached in:
    // - User's own browser cache (safe - it's their own data)
    // - User's own in-memory cache (safe - it's their own app instance)
}
```

### ❌ Pitfall 3: Using MaxAge for Frequently Changing Data

**Problem:** Browser cache can't be purged, users see stale data

```csharp
// BAD: Browser caches data that changes often, can't be purged
[AppResponseCache(MaxAge = 3600)] // ❌ Bad! Browser will cache for 1 hour
public async Task<int> GetUnreadNotificationCount()
{
    var userId = User.GetUserId();
    var count = await DbContext.Notifications
        .Where(n => n.UserId == userId && !n.IsRead)
        .CountAsync();
    
    return count;
    
    // PROBLEM: User receives a new notification
    // Server can't purge the user's browser cache
    // Notification count shows "3" in browser cache, but actual count is "4"
    // User won't see the new notification until cache expires (1 hour!)
}
```

**Solution:**
```csharp
// GOOD: Use SharedMaxAge only for frequently changing data
[AppResponseCache(SharedMaxAge = 60)] // ✅ Good! Only CDN/output cache (purgeable)
public async Task<int> GetUnreadNotificationCount()
{
    var userId = User.GetUserId();
    var count = await DbContext.Notifications
        .Where(n => n.UserId == userId && !n.IsRead)
        .CountAsync();
    
    return count;
    
    // BETTER: Cached for 60 seconds on server
    // When new notification arrives, call:
    // await responseCacheService.PurgeCache("/api/Notification/GetUnreadNotificationCount");
}

// EVEN BETTER: Don't cache at all for real-time data
public async Task<int> GetUnreadNotificationCount()
{
    // No caching attribute - always fresh data
    var userId = User.GetUserId();
    var count = await DbContext.Notifications
        .Where(n => n.UserId == userId && !n.IsRead)
        .CountAsync();
    
    return count;
    
    // BEST: Use SignalR to push real-time updates to client
    // No polling needed, no caching concerns
}
```

### ❌ Pitfall 4: Forgetting .CacheOutput() on Minimal APIs

**Problem:** Minimal API endpoints don't respect AppResponseCache without .CacheOutput()

```csharp
// BAD: Attribute is present but not wired up
app.MapGet("/api/stats", [AppResponseCache(MaxAge = 3600)] () => 
{
    return new { Total = 12345 };
});
// ❌ Won't cache! Missing .CacheOutput() call
```

**Solution:**
```csharp
// GOOD: Both attribute and .CacheOutput() present
app.MapGet("/api/stats", [AppResponseCache(MaxAge = 3600)] () => 
{
    return new { Total = 12345 };
})
.CacheOutput("AppResponseCachePolicy"); // ✅ Required for minimal APIs
```

### ❌ Pitfall 5: Incorrect URL in PurgeCache()

**Problem:** Purge doesn't work because URL doesn't match exactly

```csharp
// BAD: URL mismatch
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // ❌ Wrong URL format
    await responseCacheService.PurgeCache($"/api/ProductView/Get?id={dto.ShortId}");
    
    // Actual API URL is: /api/ProductView/Get/123
    // Purge URL was:      /api/ProductView/Get?id=123
    // They don't match! Cache is NOT purged!
    
    return entityToUpdate.Map();
}
```

**Solution:**
```csharp
// GOOD: Exact URL match
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // ✅ Correct URL format matches the API route
    await responseCacheService.PurgeCache($"/api/ProductView/Get/{dto.ShortId}");
    
    return entityToUpdate.Map();
}
```

---

## Advanced: Cloudflare Integration

The `ResponseCacheService` includes automatic Cloudflare CDN cache purging:

```csharp
private async Task PurgeCloudflareCache(string[] relativePaths)
{
    if (serverApiSettings?.Cloudflare?.Configured is not true)
        return;

    var zoneId = serverApiSettings.Cloudflare.ZoneId;
    var apiToken = serverApiSettings.Cloudflare.ApiToken;

    // Build full URLs from relative paths
    var files = serverApiSettings.Cloudflare.AdditionalDomains
        .Union([httpContextAccessor.HttpContext!.Request.GetBaseUrl(), 
                httpContextAccessor.HttpContext!.Request.GetWebAppUrl()])
        .SelectMany(baseUri => relativePaths.Select(path => new Uri(baseUri, path)))
        .Distinct()
        .ToArray();

    // Call Cloudflare API to purge cache
    using var request = new HttpRequestMessage(HttpMethod.Post, $"{zoneId}/purge_cache");
    request.Headers.Add("Authorization", $"Bearer {apiToken}");
    request.Content = JsonContent.Create(new { files });
    
    using var response = await httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
}
```

**Requirements:**
- Cloudflare account with your domain
- Zone ID from Cloudflare dashboard
- API token with "Cache Purge" permission
- Configure in `appsettings.json`

---

## Summary

The 4-layer caching system provides:

✅ **Exceptional Performance**: 
   - Sub-millisecond response times from in-memory cache (synchronous, instant)
   - No loading indicators when navigating to previously visited pages
   - Native app-like experience in web browsers

✅ **Scalability**: 
   - Handle massive traffic with minimal server resources
   - Zero server overhead for cached responses served from CDN
   - Database remains responsive under high load

✅ **Flexibility**: 
   - Fine-grained control over cache behavior per endpoint
   - Separate control for purgeable (SharedMaxAge) vs non-purgeable (MaxAge) caches
   - Supports HTML, JSON, XML, and binary responses

✅ **Security**: 
   - Prevents caching of user-specific data in shared caches
   - Automatic protection for authenticated requests (unless explicitly marked UserAgnostic)
   - Safe defaults prevent accidental data leaks

✅ **Maintainability**: 
   - Easy cache purging when data changes via `ResponseCacheService`
   - Cloudflare CDN integration for global edge caching
   - Comprehensive debugging headers for troubleshooting

✅ **Global Reach**: 
   - CDN edge caching provides fast responses worldwide
   - Content served from nearest geographic location to users
   - Reduces latency from hundreds to tens of milliseconds

---

**Live Examples:**
- Sales website: https://sales.bitplatform.dev (experience instant navigation between products)
- Admin panel: https://adminpanel.bitplatform.dev (see cache purging in action after edits)


---