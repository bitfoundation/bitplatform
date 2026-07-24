namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsStreamPrerenderingSuppressed()
        {
            if (request.HttpContext.IsSharedResponseCacheEnabled())
                return true; // The response from streaming pre-rendering is not suitable for caching in ASP.NET Core's output caching mechanism or on CDN edge servers.

            return request.IsCrawlerClient();
        }

        public bool IsCrawlerClient()
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
}
