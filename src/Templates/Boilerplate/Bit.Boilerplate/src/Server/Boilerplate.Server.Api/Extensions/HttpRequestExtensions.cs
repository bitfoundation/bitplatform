using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    internal static Uri GetWebAppUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

        var serverUrl = req.GetBaseUrl();

        var origins = req.Headers["X-Origin"].Union(req.Query["origin"]);

        if (origins.Any() is false)
            return serverUrl;

        if (origins.FirstOrDefault(origin => string.Equals(origin, serverUrl) || settings.IsAllowedOrigin(origin)) is string validOrigin)
        {
            return new Uri(validOrigin);
        }

        throw new BadRequestException($"No valid origin found among {string.Join(',', origins)}");
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
