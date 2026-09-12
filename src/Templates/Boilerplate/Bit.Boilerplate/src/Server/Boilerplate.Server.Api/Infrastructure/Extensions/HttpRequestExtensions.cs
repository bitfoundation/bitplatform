using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsFromCDN()
        {
            return request.Headers.ContainsKey("CDN-Loop");
        }

        /// <summary>
        /// A browser navigating to a page, as opposed to a client calling the api. A json error answered to one of
        /// these renders as raw text in the address bar's tab, so a page request belongs to the app, which has its own
        /// ui for the same refusal.
        /// </summary>
        public bool IsPageRequest()
        {
            // What a browser sets on a navigation. The Accept fallback covers the clients that do not send it.
            if (request.Headers.TryGetValue("Sec-Fetch-Dest", out var destination))
                return destination.Contains("document") || destination.Contains("iframe") || destination.Contains("frame");

            return request.Headers.Accept.Any(accept => accept?.Contains("text/html", StringComparison.OrdinalIgnoreCase) is true);
        }

        public Uri GetWebAppUrl()
        {
            var settings = request.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

            var serverUrl = request.GetBaseUrl();

            var candidate = request.Query["origin"].Union(request.Headers["X-Origin"])
                                                   .FirstOrDefault(o => string.IsNullOrWhiteSpace(o) is false);

            if (candidate is null)
            {
                // Nothing said where the web app is. Configuration knows when the api stands alone; otherwise the two
                // share a host, and this server is it.
                return Uri.TryCreate(settings.WebAppUrl, UriKind.Absolute, out var configuredWebAppUrl) ? configuredWebAppUrl : serverUrl;
            }

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var origin) is false)
                throw new BadRequestException($"Invalid origin {candidate}");

            if (origin == serverUrl || settings.IsTrustedOrigin(origin))
                return origin;

            throw new BadRequestException($"Invalid origin {origin}");
        }
    }
}
