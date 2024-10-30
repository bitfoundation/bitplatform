using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Extensions;

public static partial class PlaywrightCacheExtensions
{
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

        if (cachedResponses.TryGetValue(url, out var cachedResponse) is false)
        {
            var response = await route.FetchAsync();
            var body = await response.BodyAsync();
            cachedResponse = (body, response.Headers);
            cachedResponses[url] = cachedResponse;
        }

        await route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            BodyBytes = cachedResponse.Body,
            Headers = cachedResponse.Headers
        });
    }
    public static void ClearCache() => cachedResponses.Clear();

    private static readonly ConcurrentDictionary<string, (byte[] Body, Dictionary<string, string> Headers)> cachedResponses = [];

    [GeneratedRegex(@"\/_framework\/[\w\.]+\.((wasm)|(pdb)|(dat))\?v=sha256-.+")]
    private static partial Regex BlazorWasmRegex();
}
