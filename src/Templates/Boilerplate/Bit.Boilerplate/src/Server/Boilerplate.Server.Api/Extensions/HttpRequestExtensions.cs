using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    internal static Uri GetWebAppUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

        var serverUrl = req.GetBaseUrl();

        var origins = req.Query["origin"].Union(req.Headers["X-Origin"]);

        if (origins.Any() is false)
            return serverUrl; // Assume that web app and server are hosted in one place.

        if (origins.SingleOrDefault(origin => string.Equals(origin, serverUrl) || settings.IsAllowedOrigin(origin)) is string validOrigin)
        {
            return new Uri(validOrigin);
        }

        throw new BadRequestException($"None of the provided origins are valid: {string.Join(", ", origins)}");
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
