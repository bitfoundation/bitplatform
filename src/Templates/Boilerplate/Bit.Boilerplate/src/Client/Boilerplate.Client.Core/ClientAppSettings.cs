
namespace Boilerplate.Client.Core;

public partial class ClientAppSettings : SharedAppSettings
{
    /// <summary>
    /// If you're running Boilerplate.Server.Web project, then you can also use relative urls such as / for Blazor Server and WebAssembly
    /// </summary>
    public string? ServerAddress { get; set; }

    //#if (captcha == "reCaptcha")
    public string? GoogleRecaptchaSiteKey { get; set; }
    //#endif

    public WindowsUpdateOptions WindowsUpdate { get; set; } = default!;

    //#if (notification == true)
    public AdsPushVapidOptions AdsPushVapid { get; set; } = default!;
    //#endif

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        Validator.TryValidateObject(WindowsUpdate, new ValidationContext(WindowsUpdate), validationResults, true);

        //#if (notification == true)
        if (AdsPushVapid is not null)
        {
            Validator.TryValidateObject(AdsPushVapid, new ValidationContext(AdsPushVapid), validationResults, true);

            if (AppEnvironment.IsDev() is false && AdsPushVapid.PublicKey is "BDSNUvuIISD8NQVByQANEtZ2foKaENIcIGUxsiQs9kDz11fQik8c9WeiMwUHs3iTgNNH4nvXioNQIEsn4OAjTKc")
            {
                validationResults.Add(new ValidationResult("Please set your own AdsPushVapid.PublicKey in Client.Core's appsettings.json"));
            }
        }
        //#endif

        //#if (captcha == "reCaptcha")
        if (AppEnvironment.IsDev() is false && GoogleRecaptchaSiteKey is "6LdMKr4pAAAAAKMyuEPn3IHNf04EtULXA8uTIVRw")
        {
            validationResults.Add(new ValidationResult("Please set your own GoogleRecaptchaSiteKey in Client.Core's appsettings.json"));
        }
        //#endif

        return validationResults;
    }
}

public partial class WindowsUpdateOptions
{
    public bool AutoReload { get; set; }

    public string? FilesUrl { get; set; }
}

//#if (notification == true)
/// <summary>
/// https://github.com/adessoTurkey-dotNET/AdsPush
/// </summary>
public class AdsPushVapidOptions
{
    /// <summary>
    /// Web push's vapid. More info at https://vapidkeys.com/
    /// </summary>
    [Required] public string? PublicKey { get; set; }
}
//#endif
