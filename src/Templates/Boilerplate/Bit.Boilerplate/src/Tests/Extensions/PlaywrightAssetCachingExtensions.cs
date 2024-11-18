using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Extensions;

public static partial class PlaywrightAssetCachingExtensions
{
    public static ConcurrentDictionary<string, (byte[] Body, Dictionary<string, string> Headers)> CachedResponses { get; } = [];

    public static Task EnableBlazorWasmCaching(this IPage page) => page.EnableAssetCaching(BlazorWasmRegex());

    public static Task EnableBlazorWasmCaching(this IBrowserContext context) => context.EnableAssetCaching(BlazorWasmRegex());

    public static Task EnableAssetCaching(this IPage page, string routePattern) => page.RouteAsync(routePattern, CacheHandler);

    public static Task EnableAssetCaching(this IPage page, Regex routePattern) => page.RouteAsync(routePattern, CacheHandler);

    public static Task EnableAssetCaching(this IPage page, Func<string, bool> predicate) => page.RouteAsync(predicate, CacheHandler);

    public static Task EnableAssetCaching(this IBrowserContext context, string routePattern) => context.RouteAsync(routePattern, CacheHandler);

    public static Task EnableAssetCaching(this IBrowserContext context, Regex routePattern) => context.RouteAsync(routePattern, CacheHandler);

    public static Task EnableAssetCaching(this IBrowserContext context, Func<string, bool> predicate) => context.RouteAsync(predicate, CacheHandler);

    private static async Task CacheHandler(IRoute route)
    {
        var url = new Uri(route.Request.Url).PathAndQuery;

        if (CachedResponses.TryGetValue(url, out var cachedResponse) is false)
        {
            var response = await route.FetchAsync();
            var body = await response.BodyAsync();
            cachedResponse = (body, response.Headers);
            CachedResponses[url] = cachedResponse;
        }

        await route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            BodyBytes = cachedResponse.Body,
            Headers = cachedResponse.Headers
        });
    }

    public static bool ContainsAsset(Regex regex) => CachedResponses.Keys.Any(regex.IsMatch);

    public static bool ContainsAsset(string url) => CachedResponses.Keys.Any(url.Contains);

    public static void ClearBlazorWasmCache() => ClearCache(BlazorWasmRegex());

    public static void ClearCache() => CachedResponses.Clear();

    public static void ClearCache(Regex regex) => CachedResponses.Where(x => regex.IsMatch(x.Key)).ToList().ForEach(key => CachedResponses.TryRemove(key));

    public static void ClearCache(string url) => CachedResponses.Where(x => url.Contains(x.Key)).ToList().ForEach(key => CachedResponses.TryRemove(key));

    //Glob pattern: /_framework/*.{wasm|pdb|dat}?v=sha256-*
    [GeneratedRegex(@"\/_framework\/[\w\.]+\.((wasm)|(pdb)|(dat))\?v=sha256-.+")]
    public static partial Regex BlazorWasmRegex();
}
