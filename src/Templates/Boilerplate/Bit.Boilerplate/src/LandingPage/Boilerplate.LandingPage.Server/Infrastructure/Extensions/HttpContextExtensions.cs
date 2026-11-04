namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    // [mirror] crawler and benchmark detection - keep in sync with:
    // - src/Server/Boilerplate.Server.Shared/Infrastructure/Extensions/HttpRequestExtensions.cs
    private static readonly string[] CrawlerUserAgentTokens =
    [
        "googlebot", "storebot-google", "google-inspectiontool", "googleother", "google-extended",
        "adsbot-google", "mediapartners-google",
        "bingbot", "adidxbot", "msnbot", "bingpreview",
        "slurp", // Yahoo
        "duckduckbot", "duckassistbot",
        "yandexbot", "yandexmobilebot", "yandeximages",
        "baiduspider", "applebot", "petalbot", "seznambot"
    ];

    extension(HttpRequest request)
    {
        public bool IsLightHouseRequest()
        {
            return request.GetLoweredUserAgent().Contains("lighthouse");
        }

        public bool IsCrawlerClient()
        {
            var agent = request.GetLoweredUserAgent();

            return CrawlerUserAgentTokens.Any(agent.Contains);
        }

        public string GetLoweredUserAgent()
        {
            return request.Headers.UserAgent.ToString().ToLowerInvariant();
        }
    }

    extension(HttpContext context)
    {
        // The rate limits key on this, so behind a proxy it relies on UseLandingForwardedHeaders running first
        public string GetClientIp() => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public Uri GetSiteUrl()
        {
            return new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/");
        }
    }
}
