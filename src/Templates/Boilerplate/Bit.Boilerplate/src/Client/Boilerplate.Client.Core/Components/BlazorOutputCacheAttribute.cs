namespace Boilerplate.Client.Core.Components;

/// <summary>
/// Applying this attribute enables caching of your page's pre-rendered HTML response in both  
/// ASP.NET Core's output caching and on CDN edge servers. (Refer to `ResponseCacheService`)  
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BlazorOutputCacheAttribute : Attribute
{
    /// <summary>
    /// In seconds.
    /// </summary>
    public int Duration { get; set; } = 3600 * 24; // 1 Day
}
