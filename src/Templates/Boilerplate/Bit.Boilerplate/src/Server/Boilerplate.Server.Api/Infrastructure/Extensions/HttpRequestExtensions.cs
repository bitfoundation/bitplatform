using Boilerplate.Server.Shared;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    public static bool IsFromCDN(this HttpRequest request)
    {
        return request.Headers.ContainsKey("CDN-Loop");
    }

    public static Uri GetWebAppUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerSharedSettings>();

        var serverUrl = req.GetBaseUrl();

        var origin = req.Query["origin"].Union(req.Headers["X-Origin"]).Select(o => new Uri(o)).FirstOrDefault();

        if (origin is null)
            return serverUrl; // Assume that web app and server are hosted in one place.

        if (origin == serverUrl || settings.IsTrustedOrigin(origin))
            return origin;

        throw new BadRequestException($"Invalid origin {origin}");
    }
}
