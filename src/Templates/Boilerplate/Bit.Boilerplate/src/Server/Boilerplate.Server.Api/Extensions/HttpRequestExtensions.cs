using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
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

    internal static Uri GetWebClientUrl(this HttpRequest req)
    {
        var configuration = req.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (string.IsNullOrEmpty(configuration["WebClientUrl"]) is false)
            return new Uri(configuration["WebClientUrl"]!);

        return req.GetBaseUrl();
    }
}
