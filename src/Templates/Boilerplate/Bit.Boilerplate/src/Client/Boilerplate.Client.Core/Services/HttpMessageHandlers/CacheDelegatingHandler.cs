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
        var useCache = AppEnvironment.IsDev() is false && AppPlatform.IsBlazorHybridOrBrowser;

        try
        {
            var cacheKey = request.RequestUri!.ToString();

            if (useCache && memoryCache.TryGetValue(cacheKey, out ResponseMemoryCacheItems? cachedResponse))
            {
                memoryCacheStatus = "HIT";
                var cachedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(cachedResponse!.Content)
                };
                foreach (var (key, values) in cachedResponse.Headers)
                {
                    cachedHttpResponse.Headers.TryAddWithoutValidation(key, values);
                }
                request.Options.Set(new(RequestOptionNames.LogLevel), LogLevel.Information);
                return cachedHttpResponse;
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (useCache && response.IsSuccessStatusCode && response.Headers.CacheControl?.MaxAge is TimeSpan maxAge)
            {
                memoryCacheStatus = "MISS";
                var responseContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                memoryCache.Set(cacheKey, new ResponseMemoryCacheItems
                {
                    Content = responseContent,
                    Headers = response.Headers.Select(h => (h.Key, h.Value.ToArray())).ToArray()
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

        public required (string key, string[] values)[] Headers { get; set; }
    }
}
