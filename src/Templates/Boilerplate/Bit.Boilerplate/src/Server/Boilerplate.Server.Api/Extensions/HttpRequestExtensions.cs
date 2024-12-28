using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    internal static Uri GetWebAppUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

        if (req.Headers["X-Origin"].Union(req.Query["origin"]).FirstOrDefault(origin => string.IsNullOrEmpty(origin) is false && settings.IsAllowedOrigin(origin)) is string validOrigin)
        {
            return new Uri(validOrigin);
        }

        return req.GetBaseUrl(); // API server and web app are hosted on the same location.
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
