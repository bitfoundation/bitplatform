//+:cnd:noEmit

namespace Boilerplate.Client.Core;

public partial class ClientCoreSettings : SharedSettings
{
    /// <summary>
    /// If you're running Boilerplate.Server.Web project, then you can also use relative urls such as / for Blazor Server and WebAssembly
    /// </summary>
    [Required]
    public string ServerAddress { get; set; } = default!;

    //#if (captcha == "reCaptcha")
    [Required]
    public string GoogleRecaptchaSiteKey { get; set; } = default!;
    //#endif

    //#if (ads == true)
    /// <summary>
    /// The advertisement's unit path of the google ads from the Google Ad Manager panel.
    /// </summary>
    [Required]
    public string AdUnitPath { get; set; } = default!;
    //#endif

    /// <summary>
    /// When the Blazor Hybrid app sends a request to the API server, and the API server and web app are hosted on different URLs,
    /// the origin of the generated links (e.g., email confirmation links) will depend on `WebAppUrl` value.
    /// The config is relevant for Blazor Hybrid apps only.
    /// </summary>
    public Uri? WebAppUrl { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        //#if (captcha == "reCaptcha")
        if (AppEnvironment.IsDevelopment() is false && GoogleRecaptchaSiteKey is "6LdMKr4pAAAAAKMyuEPn3IHNf04EtULXA8uTIVRw")
        {
            validationResults.Add(new ValidationResult("Please set your own GoogleRecaptchaSiteKey in Client.Core's appsettings.json"));
        }
        //#endif

        return validationResults;
    }
}
