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
    /// CDN edge servers, and the browser's cache. Note that browser's cache cannot be purged automatically, so use it with caution.
    /// </summary>
    public int MaxAge { get; set; } = -1;

    /// <summary>
    /// Specifies the cache duration in seconds for shared caches. This setting caches the response in ASP.NET Core's output cache
    /// and CDN edge servers. The cache can be purged at any time using the ResponseCacheService.
    /// Default value is 86400 seconds (1 day).
    /// </summary>
    public int SharedMaxAge { get; set; } = -1;

    /// <summary>
    /// If the current request is authenticated, the pre-rendered HTML response might include the user's name, 
    /// or the JSON content of API calls might be based on the user's roles or tenant. 
    /// Storing such a response in the browser's cache is generally not an issue, 
    /// but caching the response on a CDN's edge or in the output cache of ASP.NET Core 
    /// could result in serving that response to other users. 
    /// 
    /// If you are certain that your page or API is not affected by the user, 
    /// you can set this property to true to cache those responses and improve performance.
    /// </summary>
    public bool UserAgnostic { get; set; }
}
