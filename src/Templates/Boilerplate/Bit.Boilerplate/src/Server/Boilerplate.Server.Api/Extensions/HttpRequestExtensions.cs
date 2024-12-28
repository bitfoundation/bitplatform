using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    internal static Uri GetClientUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

        if (req.Headers["X-Origin"].Union(req.Headers.Origin).Union(req.Headers.Referer)
            .FirstOrDefault(origin => string.IsNullOrEmpty(origin) is false && settings.IsAllowedOrigin(origin)) is string validOrigin)
        {
            return new Uri(validOrigin);
        }

        return req.GetBaseUrl();
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
