using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    public static bool ShouldRenderStaticMode(this HttpRequest request)
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

        return userAgent.ToLower();
    }
}
