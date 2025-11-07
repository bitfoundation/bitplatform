# Stage 14: Response Caching System

Welcome to Stage 14! In this stage, you'll learn about the comprehensive multi-layer response caching system built into the Boilerplate project. This system dramatically improves performance by caching responses at multiple levels: browser cache, CDN edge servers, and ASP.NET Core's output cache.

---

## 📋 Overview

The Boilerplate project implements a sophisticated caching strategy that includes:

1. **Static file caching** on browsers and CDN edge servers
2. **JSON and dynamic API response caching** using the `AppResponseCache` attribute
3. **Pre-rendered HTML caching** for Blazor pages
4. **Cache purging** capabilities to invalidate stale data

All of this is controlled through a simple attribute-based API that makes caching easy to implement and maintain.

---

## 🏗️ Key Caching Components

### 1. AppResponseCacheAttribute

**Location**: [`/src/Shared/Attributes/AppResponseCacheAttribute.cs`](/src/Shared/Attributes/AppResponseCacheAttribute.cs)

This attribute is the main interface for developers to control caching behavior. You can apply it to:
- Blazor pages (as a Razor attribute)
- API controller actions
- Minimal API endpoints

#### Properties

```csharp
public class AppResponseCacheAttribute : Attribute
{
    /// <summary>
    /// Cache duration in seconds for browser and in-memory caches.
    /// Use with caution as these caches cannot be purged automatically.
    /// </summary>
    public int MaxAge { get; set; } = -1;

    /// <summary>
    /// Cache duration in seconds for ASP.NET Core output cache and CDN edge servers.
    /// Can be purged at any time using ResponseCacheService.
    /// </summary>
    public int SharedMaxAge { get; set; } = -1;

    /// <summary>
    /// Set to true if the response is the same for all users.
    /// Allows caching in CDN and output cache even for authenticated requests.
    /// </summary>
    public bool UserAgnostic { get; set; }
}
```

#### Understanding MaxAge vs SharedMaxAge

- **MaxAge**: Controls caching in the **browser's cache** and **app's in-memory cache**
  - These caches **cannot be purged** by the server
  - Use shorter durations or avoid entirely for frequently changing data
  - In Development mode, this is automatically set to -1 (disabled)

- **SharedMaxAge**: Controls caching in **CDN edge servers** and **ASP.NET Core's output cache**
  - These caches **can be purged** using `ResponseCacheService`
  - Safe to use longer durations since you can invalidate when data changes
  - If not specified, defaults to `MaxAge` value

### 2. AppResponseCachePolicy

**Location**: [`/src/Server/Boilerplate.Server.Shared/Services/AppResponseCachePolicy.cs`](/src/Server/Boilerplate.Server.Shared/Services/AppResponseCachePolicy.cs)

This class implements `IOutputCachePolicy` and is the engine that processes the `AppResponseCache` attribute. It:

- Reads the `AppResponseCache` attribute from the current request
- Determines appropriate cache durations based on:
  - Attribute settings
  - Authentication status
  - Environment (Development/Production)
  - Request type (Blazor page vs API)
  - Configuration settings
- Sets HTTP cache headers (`Cache-Control`)
- Configures ASP.NET Core's output cache
- Adds cache tags for purging

#### Key Logic

```csharp
// Authenticated users and non-UserAgnostic endpoints
if (context.HttpContext.User.IsAuthenticated() && responseCacheAtt.UserAgnostic is false)
{
    // Don't cache in shared caches to avoid serving user-specific data to others
    edgeCacheTtl = -1;
    outputCacheTtl = -1;
}

// Blazor pages with cultures
if (context.HttpContext.IsBlazorPageContext() && CultureInfoManager.InvariantGlobalization is false)
{
    // Disable edge and client cache due to culture variations
    edgeCacheTtl = -1;
    clientCacheTtl = -1;
}
```

### 3. ResponseCacheService

**Location**: [`/src/Server/Boilerplate.Server.Api/Services/ResponseCacheService.cs`](/src/Server/Boilerplate.Server.Api/Services/ResponseCacheService.cs)

This service provides cache purging capabilities. When data changes on the server, you need to invalidate cached responses to ensure users see fresh data.

#### Core Methods

```csharp
public partial class ResponseCacheService
{
    // Generic cache purging for any URLs
    public async Task PurgeCache(params string[] relativePaths)
    {
        // Purge from ASP.NET Core output cache
        foreach (var relativePath in relativePaths)
        {
            await outputCacheStore.EvictByTagAsync(relativePath, default);
        }
        
        // Purge from CDN edge servers (e.g., Cloudflare)
        await PurgeCloudflareCache(relativePaths);
    }

    // Domain-specific purging for products
    public async Task PurgeProductCache(int shortId)
    {
        await PurgeCache(
            "/",                              // Home page (may list products)
            $"/product/{shortId}",           // Product detail page
            $"/api/ProductView/Get/{shortId}" // Product API endpoint
        );
    }
}
```

---

## 🎯 Practical Examples

### Example 1: Caching API Responses

**File**: [`/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductViewController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductViewController.cs)

```csharp
[ApiController, Route("api/[controller]/[action]")]
[AllowAnonymous]
public partial class ProductViewController : AppControllerBase, IProductViewController
{
    // List products with 5-minute browser cache, no CDN cache
    [HttpGet, EnableQuery]
    [AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products.Project();
    }

    // Get single product with 7-day CDN cache and 5-minute browser cache
    [HttpGet("{id}")]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5, UserAgnostic = true)]
    public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
    {
        var product = await Get().FirstOrDefaultAsync(t => t.ShortId == id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return product;
    }
}
```

**Why these settings?**
- `UserAgnostic = true`: Product data is the same for all users
- **List endpoint**: Short browser cache (5 min), no CDN cache - users expect fresh product lists
- **Detail endpoint**: Long CDN cache (7 days), short browser cache (5 min) - product details change rarely

### Example 2: Caching Blazor Pages

**File**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/ProductPage.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/ProductPage.razor)

```xml
@page "/product/{id:int}"
@inherits AppPageBase
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24, MaxAge = 60 * 5)]

<PageTitle>@(product?.Name ?? Localizer[nameof(AppStrings.Product)])</PageTitle>

@* Page content *@
```

This caches the **pre-rendered HTML** of the product page:
- **CDN cache**: 24 hours
- **Browser cache**: 5 minutes

### Example 3: Cache Purging After Updates

**File**: [`/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs)

```csharp
public partial class ProductController : AppControllerBase, IProductController
{
    [AutoInject] private ResponseCacheService responseCacheService = default!;

    [HttpPut]
    public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
    {
        // ... update logic ...
        
        await DbContext.SaveChangesAsync(cancellationToken);

        // 🔥 Purge all cached responses related to this product
        await responseCacheService.PurgeProductCache(entityToUpdate.ShortId);

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}/{concurrencyStamp}")]
    public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
    {
        // ... delete logic ...
        
        await DbContext.SaveChangesAsync(cancellationToken);

        // 🔥 Purge cache again after deletion
        await responseCacheService.PurgeProductCache(entityToDelete.ShortId);
    }
}
```

**Important**: The URL passed to `PurgeCache()` must **exactly match** the cached URL, including query parameters.

### Example 4: Minimal API with Caching

**File**: [`/src/Server/Boilerplate.Server.Web/Program.Middlewares.cs`](/src/Server/Boilerplate.Server.Web/Program.Middlewares.cs)

```csharp
app.MapGet("/api/minimal-api-sample/{routeParameter}", 
    [AppResponseCache(MaxAge = 3600 * 24)] 
    (string routeParameter, [FromQuery] string queryStringParameter) => new
    {
        routeParameter,
        queryStringParameter
    })
    .WithTags("Test")
    .CacheOutput("AppResponseCachePolicy");
```

For minimal APIs, you need both:
1. `[AppResponseCache]` attribute on the handler
2. `.CacheOutput("AppResponseCachePolicy")` in the builder

---

## 🎛️ Configuration

### Output Caching Setup

**File**: [`/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs`](/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs)

```csharp
services.AddOutputCache(options =>
{
    options.AddPolicy("AppResponseCachePolicy", policy =>
    {
        var builder = policy.AddPolicy<AppResponseCachePolicy>();
    }, excludeDefaultPolicy: true);
});
```

This registers the `AppResponseCachePolicy` with ASP.NET Core's output caching system.

### Middleware Registration

**File**: [`/src/Server/Boilerplate.Server.Web/Program.Middlewares.cs`](/src/Server/Boilerplate.Server.Web/Program.Middlewares.cs)

```csharp
app.UseOutputCache(); // Enable output caching middleware
```

This must be placed in the correct order in the middleware pipeline.

### Cache Settings in appsettings.json

**File**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
{
  "ResponseCaching": {
    "EnableOutputCaching": false,
    "EnableCdnEdgeCaching": false
  },
  "Cloudflare": {
    "ApiToken": null,
    "ZoneId": null,
    "AdditionalDomains": []
  }
}
```

**Important Configuration Notes**:
- Both caching options are **disabled by default** for safety
- Enable them in **Production** after understanding the implications
- If using Cloudflare, configure the API token and zone ID for cache purging
- `AdditionalDomains`: If multiple domains point to your backend, list them here to purge cache across all domains

---

## 🏛️ Multi-Layer Caching Architecture

The caching system works across **four distinct layers**, each with different characteristics:

### Layer 1: Browser Cache (Private Cache)
- **Duration**: Controlled by `MaxAge`
- **Scope**: Individual user's browser
- **Can be purged?**: ❌ No (user controls this)
- **Best for**: Static resources, assets
- **Risk**: User might see stale data until cache expires

### Layer 2: In-Memory Cache (Application Cache)
- **Duration**: Controlled by `MaxAge`
- **Scope**: Application's memory
- **Can be purged?**: ❌ No (automatic eviction only)
- **Best for**: Frequently accessed data
- **Risk**: Memory usage, stale data

### Layer 3: CDN Edge Cache (Shared Cache)
- **Duration**: Controlled by `SharedMaxAge`
- **Scope**: CDN servers worldwide (e.g., Cloudflare)
- **Can be purged?**: ✅ Yes (via `ResponseCacheService`)
- **Best for**: Public content, API responses
- **Benefit**: Fastest response times globally

### Layer 4: ASP.NET Core Output Cache (Server Cache)
- **Duration**: Controlled by `SharedMaxAge`
- **Scope**: Your server's memory/distributed cache
- **Can be purged?**: ✅ Yes (via `ResponseCacheService`)
- **Best for**: Frequently accessed endpoints
- **Benefit**: Reduces database queries

### Caching Flow

```
User Request
    ↓
1. Browser Cache? → [HIT] Return cached response
    ↓ [MISS]
2. CDN Edge Cache? → [HIT] Return cached response
    ↓ [MISS]
3. Output Cache? → [HIT] Return cached response
    ↓ [MISS]
4. Execute code → Generate response → Cache at all layers → Return
```

---

## 🎨 HTTP Cache-Control Headers

The `AppResponseCachePolicy` generates standard HTTP cache headers that control browser and CDN behavior:

```http
Cache-Control: public, max-age=300, s-maxage=86400
App-Cache-Response: Output:86400,Edge:86400,Client:300
```

**Header breakdown**:
- `public`: Response can be cached by CDN (shared cache)
- `private`: Response can only be cached by browser (private cache)
- `max-age=300`: Browser can cache for 300 seconds (5 minutes)
- `s-maxage=86400`: Shared caches (CDN) can cache for 86400 seconds (24 hours)
- `App-Cache-Response`: Custom header showing all three cache durations for debugging

---

## 🛡️ Security Considerations

### The UserAgnostic Property

This is a **critical security setting**. Let's understand why:

#### ❌ Unsafe Example (Missing UserAgnostic)

```csharp
[HttpGet]
[AppResponseCache(SharedMaxAge = 3600)] // DANGER!
public async Task<UserProfileDto> GetProfile(CancellationToken cancellationToken)
{
    var userId = User.GetUserId();
    return await DbContext.Users
        .Where(u => u.Id == userId)
        .ProjectToDto()
        .FirstAsync(cancellationToken);
}
```

**Problem**: If this cached response is stored in the CDN or output cache, **User A's profile could be served to User B**! 

#### ✅ Safe Example 1 (UserAgnostic = false, default)

```csharp
[HttpGet]
[AppResponseCache(MaxAge = 300)] // Only browser cache
public async Task<UserProfileDto> GetProfile(CancellationToken cancellationToken)
{
    // SharedMaxAge will be automatically set to -1 for authenticated users
    // CDN and output cache disabled automatically
}
```

#### ✅ Safe Example 2 (UserAgnostic = true)

```csharp
[HttpGet]
[AppResponseCache(SharedMaxAge = 3600, UserAgnostic = true)] // OK!
public async Task<ProductDto[]> GetProducts(CancellationToken cancellationToken)
{
    // Products are the same for all users
    return await DbContext.Products.ProjectToDto().ToArrayAsync(cancellationToken);
}
```

**Rule of thumb**: Only set `UserAgnostic = true` if the response is **identical for all users**, regardless of authentication status, roles, or tenant.

---

## 🔧 Advanced Topics

### Cache Vary Rules

The caching system automatically varies cache by:

1. **Request URL and Query String**: Different URLs = different cache entries
2. **Culture**: Localized responses are cached separately (if not using invariant globalization)

```csharp
context.CacheVaryByRules.QueryKeys = "*";
if (CultureInfoManager.InvariantGlobalization is false)
{
    context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);
}
```

### Conditional Caching

Certain conditions automatically disable caching:

```csharp
// Don't cache responses with cookies (except culture cookie)
if (response.GetTypedHeaders().SetCookie.Any(sc => 
    sc.Name != CookieRequestCultureProvider.DefaultCookieName))
{
    context.AllowCacheStorage = false;
}

// Don't cache non-200 responses
if (response.StatusCode is not StatusCodes.Status200OK)
{
    context.AllowCacheStorage = false;
}

// Don't cache Lighthouse requests (for accurate performance audits)
if (context.HttpContext.Request.IsLightHouseRequest())
{
    edgeCacheTtl = -1;
    outputCacheTtl = -1;
}
```

### CDN Integration

The system includes built-in Cloudflare integration. For other CDNs (GCore, Fastly, etc.), you need to implement cache purging:

```csharp
public async Task PurgeCache(params string[] relativePaths)
{
    // ASP.NET Core cache purging (works automatically)
    foreach (var relativePath in relativePaths)
    {
        await outputCacheStore.EvictByTagAsync(relativePath, default);
    }
    
    // If using a different CDN, implement purging here
    if (httpContextAccessor.HttpContext!.Request.IsFromCDN())
    {
        throw new NotImplementedException();
    }
}
```

---

## 🚀 Performance Impact

Proper caching can dramatically improve performance:

### Without Caching
```
User Request → CDN → Server → Database Query → Entity Mapping → DTO Mapping → JSON Serialization → Response
~200-500ms per request
```

### With Caching (Cache Hit)
```
User Request → CDN Edge Server → Cached Response
~10-50ms per request
```

**Benefits**:
- **10-50x faster responses** for cache hits
- **Reduced database load** by 80-95%
- **Lower server costs** (fewer resources needed)
- **Better user experience** (faster page loads)
- **Global performance** (CDN edge caching)

---

## ⚠️ Common Pitfalls

### 1. Caching User-Specific Data Without UserAgnostic = false

```csharp
// ❌ BAD: Will serve Alice's data to Bob!
[AppResponseCache(SharedMaxAge = 3600)]
public async Task<OrderDto[]> GetMyOrders() { ... }

// ✅ GOOD: Only browser cache
[AppResponseCache(MaxAge = 60)]
public async Task<OrderDto[]> GetMyOrders() { ... }
```

### 2. Forgetting to Purge Cache After Updates

```csharp
// ❌ BAD: Cache still has old data
public async Task Update(ProductDto dto)
{
    await DbContext.SaveChangesAsync();
    // Missing: await responseCacheService.PurgeProductCache(dto.ShortId);
}
```

### 3. Caching Dynamic User-Specific Content with Long MaxAge

```csharp
// ❌ BAD: User might see stale cart items for an hour
[AppResponseCache(MaxAge = 3600)]
public async Task<CartDto> GetCart() { ... }

// ✅ GOOD: Short duration or no cache
[AppResponseCache(MaxAge = 30)]
public async Task<CartDto> GetCart() { ... }
```

### 4. Incorrect Cache Purge URLs

```csharp
// ❌ BAD: Won't purge the cached URL
await PurgeCache("/product/123");  // Actual URL was "/products/123"

// ✅ GOOD: Match exact URLs
await PurgeCache("/products/123"); 
```

---

## 🧪 Testing Cache Behavior

### 1. Check Cache Headers

Use browser DevTools Network tab:
```http
Cache-Control: public, max-age=300, s-maxage=86400
App-Cache-Response: Output:86400,Edge:86400,Client:300
```

### 2. Verify Cache Hits

Look for the response header:
```http
X-Cache: HIT
```

(Actual header name depends on your CDN)

### 3. Test Cache Purging

1. Request a cached endpoint
2. Update the data
3. Request again - should see fresh data
4. Check that `responseCacheService.PurgeCache()` was called

---

## 📚 Best Practices

1. **Start Conservative**: Begin with short cache durations and increase gradually
2. **Use SharedMaxAge for Purgeable Caches**: Longer durations are safe because you can purge
3. **Keep MaxAge Short**: Browser cache can't be purged, so use shorter durations
4. **Set UserAgnostic Carefully**: Only for truly public, user-independent data
5. **Always Purge After Mutations**: Update, delete, create operations should purge affected caches
6. **Monitor Cache Hit Rates**: Track performance improvements with monitoring tools
7. **Test in Production-like Environment**: Caching behavior differs between Development and Production
8. **Document Cache Dependencies**: Note which caches need purging when data changes

---

## 🎓 Summary

You've learned about the Boilerplate's comprehensive response caching system:

✅ **AppResponseCacheAttribute**: Simple attribute-based caching control  
✅ **AppResponseCachePolicy**: Intelligent cache policy engine  
✅ **ResponseCacheService**: Cache purging capabilities  
✅ **Multi-layer architecture**: Browser, in-memory, CDN, and output caches  
✅ **Security considerations**: UserAgnostic property for safe caching  
✅ **Real-world examples**: API controllers, Blazor pages, minimal APIs  
✅ **Configuration**: appsettings.json and Cloudflare integration  
✅ **Performance impact**: 10-50x faster response times  

**The key benefit**: This caching system can serve cached responses from ASP.NET Core's output cache in **microseconds** and from CDN edge servers in **milliseconds**, compared to the **hundreds of milliseconds** required to query the database, map entities, and serialize responses. This dramatic performance improvement scales effortlessly even with millions of records in the database.

---
