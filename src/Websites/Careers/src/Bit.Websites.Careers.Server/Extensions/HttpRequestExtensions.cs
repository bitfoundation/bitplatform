using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    /// <summary>
    /// https://blog.elmah.io/how-to-get-base-url-in-asp-net-core/
    /// </summary>
    public static string GetBaseUrl(this HttpRequest req)
    {
        var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);
        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.Uri.AbsoluteUri;
    }

    public static bool IsCrawlerClient(this HttpRequest request)
    {
        var agent = GetLoweredUserAgent(request);

        if (agent.Contains("google")) return true;

        if (agent.Contains("bing")) return true;

        if (agent.Contains("lighthouse")) return true;

        return false;
    }

    private static string GetLoweredUserAgent(HttpRequest request)
    {
        var userAgent = request.Headers[HeaderNames.UserAgent].ToString();

        if (string.IsNullOrEmpty(userAgent)) return string.Empty;

        return userAgent.ToLowerInvariant();
    }
}
