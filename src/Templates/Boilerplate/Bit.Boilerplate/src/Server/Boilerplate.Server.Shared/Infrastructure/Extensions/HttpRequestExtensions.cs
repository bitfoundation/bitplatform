using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    private static readonly string[] CrawlerUserAgentTokens =
    [
        "googlebot", "storebot-google", "google-inspectiontool", "googleother", "google-extended",
        "adsbot-google", "mediapartners-google",
        "bingbot", "adidxbot", "msnbot", "bingpreview",
        "slurp", // Yahoo
        "duckduckbot", "duckassistbot",
        "yandexbot", "yandexmobilebot", "yandeximages",
        "baiduspider", "applebot", "petalbot", "seznambot"
    ];

    extension(HttpRequest request)
    {
        public Uri GetUri()
        {
            if (string.IsNullOrWhiteSpace(request.Scheme))
            {
                throw new ArgumentException("Http request Scheme is not specified");
            }

            return new Uri($"{request.Scheme}://{((!request.Host.HasValue) ? "UNKNOWN-HOST" : ((request.Host.Value.IndexOf(',') > 0) ? "MULTIPLE-HOST" : request.Host.Value))}{(request.PathBase.HasValue ? request.PathBase.Value : string.Empty)}{(request.Path.HasValue ? request.Path.Value : string.Empty)}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}");
        }

        /// <summary>
        /// https://blog.elmah.io/how-to-get-base-url-in-asp-net-core/
        /// </summary>
        public Uri GetBaseUrl()
        {
            var uriBuilder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1);
            if (uriBuilder.Uri.IsDefaultPort)
            {
                uriBuilder.Port = -1;
            }

            return uriBuilder.Uri;
        }

        /// <summary>
        /// The OAuth issuer for this request: the origin the caller reached, no trailing slash, as RFC 8414 requires.
        /// Token minting and the discovery document must both come through here, or a conforming client rejects the
        /// mismatch - as does the RFC 9207 check on the authorization response.
        /// </summary>
        public string GetIssuer()
        {
            return request.GetBaseUrl().ToString().TrimEnd('/');
        }

        public bool IsLightHouseRequest()
        {
            return GetLoweredUserAgent(request).Contains("lighthouse");
        }

        public bool IsCrawlerClient()
        {
            var agent = GetLoweredUserAgent(request);

            return CrawlerUserAgentTokens.Any(agent.Contains);
        }

        public string GetLoweredUserAgent()
        {
            var userAgent = request.Headers[HeaderNames.UserAgent].ToString();

            if (string.IsNullOrWhiteSpace(userAgent)) return string.Empty;

            return userAgent.ToLowerInvariant();
        }
    }
}
