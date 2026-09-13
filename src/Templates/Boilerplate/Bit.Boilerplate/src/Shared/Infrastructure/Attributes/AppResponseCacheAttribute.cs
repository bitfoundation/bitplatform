namespace Boilerplate.Shared.Infrastructure.Attributes;

/// <summary>
/// Applying this attribute to Blazor pages or API actions will cache them in ASP.NET Core's output cache,
/// CDN edge servers, the browser's cache storage and app's in-memory cache. The cache key is based on the current request path and query,
/// and the duration specified in <see cref="MaxAge"/> and <see cref="SharedMaxAge"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AppResponseCacheAttribute : Attribute
{
    /// <summary>
    /// Specifies the cache duration in seconds. This setting caches the response in ASP.NET Core's output cache,
    /// CDN edge servers, the browser's cache and app's in-memory cache. Note that browser and in-memory caches cannot be purged automatically, so use it with caution.
    /// </summary>
    public int MaxAge { get; set; } = -1;

    /// <summary>
    /// Specifies the cache duration in seconds for shared caches. This setting caches the response in ASP.NET Core's output cache
    /// and CDN edge servers. The cache can be purged at any time using the ResponseCacheService.
    /// </summary>
    public int SharedMaxAge { get; set; } = -1;

    /// <summary>
    /// Keeps the response out of ASP.NET Core's output cache only, leaving the CDN edge and the client caches as
    /// <see cref="MaxAge"/> and <see cref="SharedMaxAge"/> asked for. Set it when the body is a FILE: the output
    /// cache holds whole bodies in the app's byte-bounded memory cache (See <c>AppMemoryCache</c>), where a few
    /// multi-megabyte downloads evict every pre-rendered page. An edge cache is where those bytes belong.
    /// </summary>
    public bool SkipOutputCache { get; set; }

    /// <summary>
    /// The tag both caches store this response under, with <c>{routeValue}</c> placeholders filled from the request's
    /// route values - <c>"Attachment-{attachmentId}"</c> on <c>[HttpGet("{attachmentId}/{kind}")]</c> tags every kind
    /// and every query string of one attachment alike, so a single purge clears them all.
    /// Unset means the tag is the request path, which is the right default: one url, one tag.
    /// </summary>
    public string? CacheTagTemplate { get; set; }

    /// <summary>
    /// If the current request is authenticated, the pre-rendered HTML response might include the user's name,
    /// or the JSON content of API calls might be based on the user's roles or tenant.
    /// Caching such a response on a CDN's edge or in the output cache of ASP.NET Core
    /// could result in serving that response to other users.
    ///
    /// If you are certain that your page or API is not affected by the user,
    /// you can set this property to true to cache those responses and improve performance.
    /// </summary>
    public bool UserAgnostic { get; set; }
}
