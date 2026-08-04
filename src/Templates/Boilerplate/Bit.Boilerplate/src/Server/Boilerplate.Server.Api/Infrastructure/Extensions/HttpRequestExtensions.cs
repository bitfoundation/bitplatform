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

            var candidate = request.Query["origin"].Union(request.Headers["X-Origin"])
                                                   .FirstOrDefault(o => string.IsNullOrWhiteSpace(o) is false);

            if (candidate is null)
                return serverUrl; // Assume that web app and server are hosted in one place.

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var origin) is false)
                throw new BadRequestException($"Invalid origin {candidate}");

            if (origin == serverUrl || settings.IsTrustedOrigin(origin))
                return origin;

            throw new BadRequestException($"Invalid origin {origin}");
        }
    }
}
