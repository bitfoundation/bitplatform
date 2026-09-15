using System.Text.RegularExpressions;
using Boilerplate.Server.Api;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    [GeneratedRegex(@"^(/[A-Za-z0-9._~\-]+)*/?$")]
    private static partial Regex PathBaseRegex();

    extension(HttpRequest request)
    {
        public bool IsFromCDN()
        {
            return request.Headers.ContainsKey("CDN-Loop");
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

            // Scheme, host, port and at most a path base: anything more ends up in emails, links and the chatbot's prompt,
            // where Uri.ToString() unescapes %22 and %20 back into quotes and spaces.
            if (origin.Query.Length > 0 || origin.Fragment.Length > 0 || PathBaseRegex().IsMatch(origin.AbsolutePath) is false)
                throw new BadRequestException($"Invalid origin {origin}");

            if (origin == serverUrl || settings.IsTrustedOrigin(origin))
                return origin;

            throw new BadRequestException($"Invalid origin {origin}");
        }
    }
}
