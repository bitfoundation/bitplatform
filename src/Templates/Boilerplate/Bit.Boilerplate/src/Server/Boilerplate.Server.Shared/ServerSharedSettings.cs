//+:cnd:noEmit
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Server.Shared;

public partial class ServerSharedSettings : SharedSettings
{
    public ForwardedHeadersOptions? ForwardedHeaders { get; set; } = default!;

    /// <summary>
    /// Specifies the allowed origins for CORS requests, URLs returned after external sign-in and email confirmation, and permitted origins for Web Auth, as well as forwarded headers middleware in ASP.NET Core.
    /// </summary>
    public Uri[] TrustedOrigins { get; set; } = [];

    public ResponseCachingOptions? ResponseCaching { get; set; } = default!;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (ForwardedHeaders is not null)
        {
            Validator.TryValidateObject(ForwardedHeaders, new ValidationContext(ForwardedHeaders), validationResults, true);
        }

        if (ResponseCaching is not null)
        {
            Validator.TryValidateObject(ResponseCaching, new ValidationContext(ResponseCaching), validationResults, true);
        }

        return validationResults;
    }

    public bool IsTrustedOrigin(Uri origin)
    {
        return TrustedOrigins.Any(trustedOrigin => trustedOrigin == origin)
            || TrustedOriginsRegex().IsMatch(origin.ToString());
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
