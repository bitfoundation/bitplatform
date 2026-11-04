namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    extension(HttpContext context)
    {
        // The rate limits key on this, so behind a proxy it relies on UseLandingForwardedHeaders running first
        public string GetClientIp() => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public Uri GetSiteUrl()
        {
            var settings = context.RequestServices.GetRequiredService<LandingServerSettings>();

            return settings.Site.Url ?? new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/");
        }
    }
}
