//+:cnd:noEmit
using Boilerplate.Client.Web;

namespace Boilerplate.Server.Web;

public partial class ServerWebSettings : ClientWebSettings
{
    public ForwardedHeadersOptions ForwardedHeaders { get; set; } = default!;

    public ResponseCachingOptions ResponseCaching { get; set; } = default!;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        Validator.TryValidateObject(ForwardedHeaders, new ValidationContext(ForwardedHeaders), validationResults, true);

        Validator.TryValidateObject(ResponseCaching, new ValidationContext(ResponseCaching), validationResults, true);

        return validationResults;
    }
}

public class ResponseCachingOptions
{
    /// <summary>
    /// Enables ASP.NET Core's response output caching
    /// </summary>
    public bool EnableOutputCaching { get; set; }

    /// <summary>
    /// Enables CDNs' edge servers caching
    /// </summary>
    public bool EnableCDNsEdgeCaching { get; set; }
}
