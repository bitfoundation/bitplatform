using Boilerplate.Server.Shared;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsFromCDN()
        {
            return request.Headers.ContainsKey("CDN-Loop");
        }

        public Uri GetWebAppUrl()
        {
            var settings = request.HttpContext.RequestServices.GetRequiredService<ServerSharedSettings>();

            var serverUrl = request.GetBaseUrl();

            var origin = request.Query["origin"].Union(request.Headers["X-Origin"]).Select(o => new Uri(o)).FirstOrDefault();

            if (origin is null)
                return serverUrl; // Assume that web app and server are hosted in one place.

            if (origin == serverUrl || settings.IsTrustedOrigin(origin))
                return origin;

            throw new BadRequestException($"Invalid origin {origin}");
        }
    }
}
