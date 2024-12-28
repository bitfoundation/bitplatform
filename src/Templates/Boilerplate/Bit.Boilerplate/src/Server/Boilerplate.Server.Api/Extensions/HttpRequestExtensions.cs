using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    internal static Uri GetWebAppUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

        var serverUrl = req.GetBaseUrl();

        var origin = req.Query["origin"].Union(req.Headers["X-Origin"]).Select(o => new Uri(o)).SingleOrDefault();

        if (origin is null)
            return serverUrl; // Assume that web app and server are hosted in one place.

        if (origin == serverUrl || settings.IsAllowedOrigin(origin))
            return origin;

        throw new BadRequestException($"Invalid origin {origin}");
    }

    /// <summary>
    /// https://blog.elmah.io/how-to-get-base-url-in-asp-net-core/
    /// </summary>
    internal static Uri GetBaseUrl(this HttpRequest req)
    {
        var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);
        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.Uri;
    }
}
