//+:cnd:noEmit
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Server.Shared;

public partial class ServerSharedSettings : SharedSettings
{
    /// <summary>
    /// Specifies the allowed origins for CORS requests, URLs returned after external sign-in and email confirmation, and permitted origins for Web Auth, as well as forwarded headers middleware in ASP.NET Core.
    /// Each entry may contain the <c>*</c> wildcard (e.g. <c>https://*.myapp.com</c>) so a single entry can trust every tenant subdomain.
    /// It is stored as <see cref="string"/>[] rather than <see cref="Uri"/>[] because <see cref="Uri"/> rejects <c>*</c> in the host.
    /// </summary>
    public string[] TrustedOrigins { get; set; } = [];

    public ResponseCachingOptions? ResponseCaching { get; set; } = default!;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (ResponseCaching is not null)
        {
            Validator.TryValidateObject(ResponseCaching, new ValidationContext(ResponseCaching), validationResults, true);
        }

        return validationResults;
    }

    public bool IsTrustedOrigin(Uri origin)
    {
        // scheme://host[:port] with no path or trailing slash, which is what an origin actually is.
        var requestOrigin = origin.GetLeftPart(UriPartial.Authority);

        return TrustedOrigins.Any(trustedOrigin => MatchTrustedOrigin(trustedOrigin, requestOrigin))
            || TrustedOriginsRegex().IsMatch(origin.ToString());
    }

    /// <summary>
    /// Matches a configured <see cref="TrustedOrigins"/> entry against the requesting origin.
    /// A <c>*</c> in the entry matches any run of characters within the authority (e.g. <c>https://*.myapp.com</c>
    /// trusts every tenant subdomain). Matching is case-insensitive and ignores a trailing slash.
    /// </summary>
    private static bool MatchTrustedOrigin(string trustedOrigin, string requestOrigin)
    {
        trustedOrigin = trustedOrigin.TrimEnd('/');

        if (trustedOrigin.Contains('*') is false)
            return string.Equals(trustedOrigin, requestOrigin, StringComparison.OrdinalIgnoreCase);

        var pattern = $"^{Regex.Escape(trustedOrigin).Replace("\\*", "[^/]*")}$";
        return Regex.IsMatch(requestOrigin, pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Extracts the host (which may still contain a <c>*</c> wildcard) from a <see cref="TrustedOrigins"/> entry
    /// so it can be supplied to <c>ForwardedHeadersOptions.AllowedHosts</c>. <see cref="Uri"/> can't be used here
    /// because it rejects <c>*</c> in the host.
    /// Note: AllowedHosts only understands the <c>*</c> (all hosts) and <c>*.domain</c> (subdomain) forms; a wildcard
    /// placed in the middle of a label (e.g. <c>tenant-*.myapp.com</c>) is treated as a literal host by ASP.NET Core
    /// and will not match forwarded hosts.
    /// </summary>
    public static string GetTrustedOriginHost(string trustedOrigin)
    {
        var host = trustedOrigin;

        var schemeSeparatorIndex = host.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparatorIndex >= 0)
            host = host[(schemeSeparatorIndex + 3)..];

        host = host.Split('/')[0]; // remove path
        host = host.Split(':')[0]; // remove port

        return host;
    }

    //-:cnd:noEmit
    /// <summary>
    /// Blazor Hybrid's webview, localhost, devtunnels, github codespaces.
    /// </summary>
#if Development
    [GeneratedRegex(@"^(http|https|app):\/\/(localhost|0\.0\.0\.0|0\.0\.0\.1|127\.0\.0\.1|.*?devtunnels\.ms|.*?github\.dev)(:\d+)?(\/.*)?$")]
#else
    [GeneratedRegex(@"^(http|https|app):\/\/(localhost|0\.0\.0\.0|0\.0\.0\.1|127\.0\.0\.1)(:\d+)?(\/.*)?$")]
#endif
    //+:cnd:noEmit
    public partial Regex TrustedOriginsRegex();
}

public class ResponseCachingOptions
{
    /// <summary>
    /// Enables ASP.NET Core's response output caching
    /// </summary>
    public bool EnableOutputCaching { get; set; }

    /// <summary>
    /// Enables CDN's edge servers caching
    /// </summary>
    public bool EnableCdnEdgeCaching { get; set; }
}
