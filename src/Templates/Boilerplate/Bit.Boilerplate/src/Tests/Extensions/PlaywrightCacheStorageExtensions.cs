using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Extensions;

public static class PlaywrightCacheStorageExtensions
{
    public static async Task DeleteCacheStorage(this IPage page, CacheId? cacheId = null)
    {
        // Chrome DevTools Protocol
        // https://chromedevtools.github.io/devtools-protocol/tot/CacheStorage/

        cacheId ??= new();
        await using var client = await page.Context.NewCDPSessionAsync(page);
        await client.SendAsync("CacheStorage.deleteCache", new() { ["cacheId"] = cacheId.Value });
    }

    public static async Task<CacheEntries> GetCacheStorageEntries(this IPage page, CacheId? cacheId = null)
    {
        cacheId ??= new();
        await using var client = await page.Context.NewCDPSessionAsync(page);
        var json = await client.SendAsync("CacheStorage.requestEntries", new() { ["cacheId"] = cacheId.Value });
        return json.Value.Deserialize<CacheEntries>()!;
    }
}

public record CacheId(string Origin = "http://localhost:5000/", string CacheName = "dotnet-resources-/")
{
    public string Value { get; init; } = $"{Origin}|{CacheName}";

    public override string ToString() => Value;
}

public class CacheEntries
{
    [JsonPropertyName("cacheDataEntries")]
    public List<CacheEntry> CacheDataEntries { get; set; } = [];

    public List<CacheEntry> GetCacheEntries(Regex regex) => CacheDataEntries.Where(x => regex.IsMatch(x.RequestURL)).ToList();

    public List<CacheEntry> GetCacheEntries(string url) => CacheDataEntries.Where(x => url.Contains(x.RequestURL)).ToList();

    public bool ContainsCacheEntry(Regex regex) => CacheDataEntries.Exists(x => regex.IsMatch(x.RequestURL));

    public bool ContainsCacheEntry(string url) => CacheDataEntries.Exists(x => url.Contains(x.RequestURL));
}

public class CacheEntry
{
    [JsonPropertyName("requestURL")]
    public string RequestURL { get; set; }

    [JsonPropertyName("requestMethod")]
    public string RequestMethod { get; set; }

    [JsonPropertyName("responseTime")]
    public double ResponseTime { get; set; }

    [JsonPropertyName("responseStatus")]
    public int ResponseStatus { get; set; }

    [JsonPropertyName("responseStatusText")]
    public string ResponseStatusText { get; set; }

    [JsonPropertyName("responseType")]
    public string ResponseType { get; set; }

    [JsonPropertyName("requestHeaders")]
    public List<RequestResponseHeader> RequestHeaders { get; set; }

    [JsonPropertyName("responseHeaders")]
    public List<RequestResponseHeader> ResponseHeaders { get; set; }
}

public class RequestResponseHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}
