namespace Boilerplate.Shared.Attributes;

/// <summary>
/// Applying this attribute to Blazor pages or API actions will cache them in ASP.NET Core's output cache,
/// CDN edge servers, and the browser's cache storage. The cache key is based on the current request path and query,
/// and the duration specified in <see cref="MaxAge"/> and <see cref="SharedMaxAge"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AppResponseCacheAttribute : Attribute
{
    /// <summary>
    /// Specifies the cache duration in seconds. This setting caches the response in ASP.NET Core's output cache,
    /// CDN edge servers, and the browser's cache. Note that this cache cannot be purged automatically, so use it with caution.
    /// </summary>
    public int MaxAge { get; set; }

    /// <summary>
    /// Specifies the cache duration in seconds for shared caches. This setting caches the response in ASP.NET Core's output cache
    /// and CDN edge servers. The cache can be purged at any time using the ResponseCacheService.
    /// Default value is 86400 seconds (1 day).
    /// </summary>
    public int SharedMaxAge { get; set; } = 3600 * 24; // 1 Day

    public ResourceKind ResourceKind { get; set; }
}

public enum ResourceKind
{
    Page, Api
}
