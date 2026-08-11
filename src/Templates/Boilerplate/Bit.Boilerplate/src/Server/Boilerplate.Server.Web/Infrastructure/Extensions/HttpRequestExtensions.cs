namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsStreamPrerenderingSuppressed()
        {
            if (request.HttpContext.IsSharedResponseCacheEnabled())
                return true; // The response from streaming pre-rendering is not suitable for caching in ASP.NET Core's output caching mechanism or on CDN edge servers.

            // Both predicates, because App.razor omits the scripts for both - and everything streaming defers is
            // delivered in <blazor-ssr> blocks that only the scripts can apply. A caller without them would be handed
            // a document whose deferred half never arrives.
            return request.IsCrawlerClient() || request.IsLightHouseRequest();
        }
    }
}
