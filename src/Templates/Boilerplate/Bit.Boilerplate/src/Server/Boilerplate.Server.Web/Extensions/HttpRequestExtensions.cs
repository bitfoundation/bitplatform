namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    public static bool DisableStreamPrerendering(this HttpRequest request)
    {
        if (request.HttpContext.Items.TryGetValue("AppResponseCachePolicy__DisableStreamPrerendering", out var val) && val is true)
            return true;  // The response from streaming pre-rendering is not suitable for caching in ASP.NET Core's output caching mechanism or on CDN edge servers.

        return request.IsCrawlerClient();
    }

    public static bool IsCrawlerClient(this HttpRequest request)
    {
        var agent = request.GetLoweredUserAgent();

        if (agent.Contains("google")) return true;

        if (agent.Contains("bing")) return true;

        if (agent.Contains("yahoo")) return true;

        if (agent.Contains("duckduck")) return true;

        if (agent.Contains("yandex")) return true;

        return false;
    }
}
