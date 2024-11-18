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

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        //#if (captcha == "reCaptcha")
        if (AppEnvironment.IsDev() is false && GoogleRecaptchaSiteKey is "6LdMKr4pAAAAAKMyuEPn3IHNf04EtULXA8uTIVRw")
        {
            validationResults.Add(new ValidationResult("Please set your own GoogleRecaptchaSiteKey in Client.Core's appsettings.json"));
        }
        //#endif

        return validationResults;
    }
}
