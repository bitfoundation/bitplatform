using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

internal class CacheDelegatingHandler(IMemoryCache memoryCache, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        var memoryCacheStatus = "DYNAMIC";
        var useCache = AppEnvironment.IsDevelopment() is false && AppPlatform.IsBlazorHybridOrBrowser;

        try
        {
            var cacheKey = $"{request.Method}-{request.RequestUri}";

            if (useCache && memoryCache.TryGetValue(cacheKey, out ResponseMemoryCacheItems? cachedResponse))
            {
                memoryCacheStatus = "HIT";
                var cachedHttpResponse = new HttpResponseMessage(cachedResponse!.StatusCode)
                {
                    Content = new ByteArrayContent(cachedResponse.Content)
                };
                foreach (var (key, values) in cachedResponse.ResponseHeaders)
                {
                    cachedHttpResponse.Headers.TryAddWithoutValidation(key, values);
                }
                foreach (var (key, values) in cachedResponse.ContentHeaders)
                {
                    cachedHttpResponse.Content.Headers.TryAddWithoutValidation(key, values);
                }
                foreach (var l in cachedResponse.LogScopeData)
                {
                    logScopeData[l.Key] = l.Value;
                }
                request.Options.Set(new(RequestOptionNames.LogLevel), LogLevel.Information);
                return cachedHttpResponse;
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (useCache && response.IsSuccessStatusCode && response.Headers.CacheControl?.MaxAge is TimeSpan maxAge && maxAge > TimeSpan.Zero)
            {
                memoryCacheStatus = "MISS";
                var responseContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                memoryCache.Set(cacheKey, new ResponseMemoryCacheItems
                {
                    Content = responseContent,
                    StatusCode = response.StatusCode,
                    ResponseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
                    ContentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
                    LogScopeData = logScopeData.ToDictionary()
                }, maxAge);
            }

            return response;
        }
        finally
        {
            logScopeData["MemoryCacheStatus"] = memoryCacheStatus;
        }
    }

    public class ResponseMemoryCacheItems
    {
        public required byte[] Content { get; set; }

        public required HttpStatusCode StatusCode { get; set; }

        public required Dictionary<string, string[]> ResponseHeaders { get; set; }
        public required Dictionary<string, string[]> ContentHeaders { get; set; }

        public required Dictionary<string, object?> LogScopeData { get; set; }
    }
}
