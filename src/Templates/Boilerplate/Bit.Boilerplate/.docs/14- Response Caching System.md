# Stage 14: Response Caching System

Welcome to Stage 14! In this stage, you'll learn about the comprehensive **4-layer response caching system** built into this project. This advanced caching architecture dramatically improves application performance, reduces server load, and provides an excellent user experience.

---

## Overview

The project implements a sophisticated caching strategy that spans **four distinct layers**, each serving a specific purpose in the overall performance optimization strategy:

1. **Client In-Memory Cache** - Fastest, application-level caching
2. **Browser Cache** - Client-side HTTP caching
3. **CDN Edge Cache** - Distributed caching at edge locations
4. **ASP.NET Core Output Cache** - Server-side response caching

---

## Core Components

### 1. AppResponseCacheAttribute

The `AppResponseCacheAttribute` is the primary interface for configuring caching behavior. Located in `src/Shared/Attributes/AppResponseCacheAttribute.cs`, it can be applied to both **Blazor pages** and **API controller actions**.

**Key Properties:**

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AppResponseCacheAttribute : Attribute
{
    /// <summary>
    /// Cache duration in seconds for all cache layers (client in-memory, browser, CDN edge, and output cache).
    /// Note: Browser and in-memory caches cannot be purged automatically, so use with caution.
    /// </summary>
    public int MaxAge { get; set; } = -1;

    /// <summary>
    /// Cache duration in seconds for shared caches (CDN edge and ASP.NET Core output cache).
    /// The cache can be purged at any time using ResponseCacheService.
    /// </summary>
    public int SharedMaxAge { get; set; } = -1;

    /// <summary>
    /// Set to true if the response is not affected by the authenticated user.
    /// Allows caching responses on CDN edge and output cache even for authenticated requests.
    /// </summary>
    public bool UserAgnostic { get; set; }
}
```

**Usage Examples:**

```csharp
// Example 1: Caching public statistics for 24 hours
[AllowAnonymous]
[HttpGet("{packageId}")]
[AppResponseCache(MaxAge = 3600 * 24, UserAgnostic = true)]
public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
{
    return await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
}

// Example 2: Caching attachments for 7 days
[AllowAnonymous]
[HttpGet("{attachmentId}/{kind}")]
[AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true)]
public async Task<IActionResult> GetAttachment(Guid attachmentId, AttachmentKind kind, CancellationToken cancellationToken)
{
    var filePath = GetFilePath(attachmentId, kind);
    // ... implementation
}

// Example 3: Different cache durations for edge and client
[AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
public async Task<ActionResult<string>> GetGoogleSocialSignInUri()
{
    // SharedMaxAge = 7 days on CDN/output cache (purgeable)
    // MaxAge = 5 minutes on browser/in-memory cache (not purgeable)
}
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
        await PurgeCache("/", $"/product/{shortId}", $"/api/ProductView/Get/{shortId}");
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

**Important Note:** For successful cache purging, the request URL must **exactly match** the URL passed to `PurgeCache()`. Query strings and route parameters must match precisely.

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
        // Return cached response
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
- Only works for **Blazor Hybrid and Browser** platforms (not server-side)
- Respects the `Cache-Control: max-age` header from server responses
- Stores entire HTTP response (content, status code, and headers)
- Provides fastest possible response time for repeated requests

---

## The 4-Layer Caching Architecture

### Comparison Table

| Layer | Location | Speed | Scope | Purgeable | Best For |
|-------|----------|-------|-------|-----------|----------|
| **1. Client In-Memory** | Client app memory | ⚡ Fastest (microseconds) | Single user, single session | ❌ No | Frequently accessed API responses in same session |
| **2. Browser Cache** | Browser's HTTP cache | 🚀 Very Fast (milliseconds) | Single user, across sessions | ❌ No | Static assets, images, user-specific data |
| **3. CDN Edge** | Cloudflare/CDN edge | 💨 Fast (10-50ms) | Global, shared | ✅ Yes | Public content, images, API responses |
| **4. Output Cache** | ASP.NET Core server | ⏱️ Medium (50-100ms) | Server-level, shared | ✅ Yes | Dynamic content, pre-rendered pages |

### When Each Layer is Used

**Client In-Memory Cache:**
- Only if `MaxAge > 0`
- Only in non-development environments
- Only for Blazor Hybrid and Browser platforms
- Response must be successful (2xx status code)
- **Cannot be purged** - use cautiously

**Browser Cache:**
- Only if `MaxAge > 0`
- Respects HTTP `Cache-Control` headers
- Stored per browser/user
- **Cannot be purged by server** - use cautiously
- Disabled in development mode

**CDN Edge Cache:**
- Only if `SharedMaxAge > 0` (or `MaxAge > 0` if SharedMaxAge not set)
- Only if `EnableCdnEdgeCaching = true` in settings
- Only if `UserAgnostic = true` OR user is not authenticated
- **Can be purged** via `ResponseCacheService`

**ASP.NET Core Output Cache:**
- Only if `SharedMaxAge > 0` (or `MaxAge > 0` if SharedMaxAge not set)
- Only if `EnableOutputCaching = true` in settings
- Only if `UserAgnostic = true` OR user is not authenticated
- **Can be purged** via `ResponseCacheService`

### Visual Flow

```
┌─────────────────────────────────────────────────────────────┐
│  Client makes request: GET /api/ProductView/Get/123         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  1. Client In-Memory Cache Check (CacheDelegatingHandler)   │
│     - Fastest (microseconds)                                 │
│     - Not purgeable                                          │
└─────────────────────────────────────────────────────────────┘
        │ MISS                          │ HIT
        ▼                               └──────► Return from memory
┌─────────────────────────────────────────────────────────────┐
│  2. Browser Cache Check (HTTP Cache-Control)                │
│     - Very fast (milliseconds)                               │
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

---

## Key Benefits

### 1. Dramatic Performance Improvement

With all 4 cache layers working together:
- **First request**: ~200ms (database query + rendering)
- **Subsequent requests**:
  - From in-memory cache: **< 1ms**
  - From browser cache: **~5ms**
  - From CDN edge: **~20ms**
  - From output cache: **~80ms**

### 2. Reduced Server Load

- CDN edge cache handles most static and public content
- Output cache prevents redundant database queries
- Server CPU and database are freed up for other operations

### 3. Global Performance

- CDN edge locations serve content from the closest geographic location to the user
- Users in Asia, Europe, and Americas all get fast response times

### 4. Cost Savings

- Reduced database queries
- Lower server CPU usage
- Reduced bandwidth costs (CDN serves cached content)

### 5. Scalability

- Application can handle significantly more concurrent users
- Cache layers absorb traffic spikes
- Database remains responsive under high load

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

```csharp
// ❌ BAD: User-specific content with MaxAge
[AppResponseCache(MaxAge = 3600)] // Browser will cache user's data!
public async Task<UserDto> GetCurrentUser() { }

// ✅ GOOD: User-specific content with SharedMaxAge only
[AppResponseCache(SharedMaxAge = 60)] // Only CDN/output cache (purgeable)
public async Task<UserDto> GetCurrentUser() { }

// ✅ GOOD: Public content with both
[AppResponseCache(MaxAge = 3600 * 24, UserAgnostic = true)]
public async Task<ProductDto[]> GetProducts() { }
```

### 2. Always Purge Cache After Mutations

```csharp
// ✅ GOOD: Purge cache after update
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    await responseCacheService.PurgeCache(
        "/products",                           // List page
        $"/api/ProductView/Get/{dto.ShortId}", // API endpoint
        $"/product/{dto.ShortId}"              // Detail page
    );
    
    return entityToUpdate.Map();
}
```

### 3. Use UserAgnostic Carefully

```csharp
// ❌ BAD: User-specific data marked as UserAgnostic
[AppResponseCache(MaxAge = 3600, UserAgnostic = true)]
public async Task<OrderDto[]> GetMyOrders() { } // Different per user!

// ✅ GOOD: Truly public data marked as UserAgnostic
[AppResponseCache(MaxAge = 3600 * 24, UserAgnostic = true)]
public async Task<ProductDto[]> GetAllProducts() { } // Same for everyone
```

### 4. Vary Cache Duration by Content Type

```csharp
// Images: Long cache (rarely change)
[AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true)] // 7 days
public async Task<IActionResult> GetAttachment() { }

// API data: Short cache (changes frequently)
[AppResponseCache(SharedMaxAge = 300)] // 5 minutes
public async Task<ProductDto[]> GetProducts() { }

// Static resources: Very long cache (versioned URLs)
[AppResponseCache(MaxAge = 3600 * 24 * 365, UserAgnostic = true)] // 1 year
public IActionResult GetScript() { }
```

### 5. Monitor Cache Headers

The system adds a custom header to help debug caching:

```
App-Cache-Response: Output:3600,Edge:3600,Client:3600
```

This shows the TTL (in seconds) for each cache layer. Use browser DevTools to inspect these headers.

---

## Debugging Tips

### Check if Caching is Active

1. Open browser DevTools → Network tab
2. Look for `App-Cache-Response` header in response
3. Check `Cache-Control` header: `public, max-age=3600, s-maxage=3600`

### Check Cache Status

The client logs include `MemoryCacheStatus`:
- `"DYNAMIC"` - Response not cached (no Cache-Control header)
- `"MISS"` - Request made to server, response cached
- `"HIT"` - Response served from in-memory cache

### Verify Cache Purging

After calling `PurgeCache()`:
1. Make a request - should be slow (cache miss)
2. Make the same request again - should be fast (cache hit)
3. Update data and purge
4. First request should be slow again (cache was purged)

---

## Common Pitfalls

### ❌ Pitfall 1: Forgetting to Purge Cache

```csharp
// BAD: Cache is never purged, users see stale data
[HttpPut]
public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
{
    // ... update logic ...
    await DbContext.SaveChangesAsync(cancellationToken);
    
    // ❌ Forgot to purge cache!
    
    return entityToUpdate.Map();
}
```

### ❌ Pitfall 2: Wrong UserAgnostic Setting

```csharp
// BAD: User-specific data cached in shared caches
[Authorize]
[AppResponseCache(MaxAge = 3600, UserAgnostic = true)] // ❌ Wrong!
public async Task<OrderDto[]> GetMyOrders() 
{
    var userId = User.GetUserId();
    return await DbContext.Orders.Where(o => o.UserId == userId).ToArrayAsync();
    // This will cache User A's orders and show them to User B!
}
```

### ❌ Pitfall 3: Using MaxAge for Frequently Changing Data

```csharp
// BAD: Browser caches data that changes often
[AppResponseCache(MaxAge = 3600)] // ❌ Bad! Browser will cache for 1 hour
public async Task<int> GetUnreadNotificationCount()
{
    // This changes frequently, browser cache can't be purged!
}

// GOOD: Use SharedMaxAge only
[AppResponseCache(SharedMaxAge = 60)] // ✅ Good! Only CDN/output cache (purgeable)
public async Task<int> GetUnreadNotificationCount()
{
    // Can be purged when new notification arrives
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

✅ **Exceptional Performance**: Sub-millisecond response times for cached content  
✅ **Scalability**: Handle massive traffic with minimal server resources  
✅ **Flexibility**: Fine-grained control over cache behavior per endpoint  
✅ **Security**: Prevents caching of user-specific data in shared caches  
✅ **Maintainability**: Easy cache purging when data changes  
✅ **Global Reach**: CDN edge caching provides fast responses worldwide  

By understanding and leveraging all four cache layers, you can build applications that are both incredibly fast and highly scalable.

---