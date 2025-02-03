using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    /// <summary>
    /// https://blog.elmah.io/how-to-get-base-url-in-asp-net-core/
    /// </summary>
    internal static Uri GetBaseUrl(this HttpRequest req)
    {
        var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);
        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.Uri;
    }

    public static Uri GetUri(this HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Scheme))
        {
            throw new ArgumentException("Http request Scheme is not specified");
        }

        return new Uri($"{request.Scheme}://{((!request.Host.HasValue) ? "UNKNOWN-HOST" : ((request.Host.Value.IndexOf(',') > 0) ? "MULTIPLE-HOST" : request.Host.Value))}{(request.PathBase.HasValue ? request.PathBase.Value : string.Empty)}{(request.Path.HasValue ? request.Path.Value : string.Empty)}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}");
    }

    public static bool DisableStreamPrerendering(this HttpRequest request)
    {
        if (request.HttpContext.Items.TryGetValue("AppResponseCachePolicy__DisableStreamPrerendering", out var val) && val is true)
            return true;  // The response from streaming pre-rendering is not suitable for caching in ASP.NET Core's output caching mechanism or on CDN edge servers.

        return request.IsCrawlerClient();
    }

    public static bool IsCrawlerClient(this HttpRequest request)
    {
        var agent = GetLoweredUserAgent(request);

        if (agent.Contains("google")) return true;

        if (agent.Contains("bing")) return true;

        if (agent.Contains("yahoo")) return true;

        if (agent.Contains("duckduck")) return true;

        if (agent.Contains("yandex")) return true;

        return false;
    }

    private static string GetLoweredUserAgent(HttpRequest request)
    {
        var userAgent = request.Headers[HeaderNames.UserAgent].ToString();

        if (string.IsNullOrEmpty(userAgent)) return string.Empty;

        return userAgent.ToLowerInvariant();
    }

    public static bool IsFromCDN(this HttpRequest request)
    {
        return request.Headers.ContainsKey("CDN-Loop");
    }
}
