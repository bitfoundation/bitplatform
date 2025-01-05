//+:cnd:noEmit
using Boilerplate.Client.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Web;

public class ClientWebSettings : ClientCoreSettings
{
    [Required]
    public WebAppRenderOptions WebAppRender { get; set; } = default!;

    //#if (notification == true)
    public AdsPushVapidOptions? AdsPushVapid { get; set; }
    //#endif

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (WebAppRender is null)
            throw new InvalidOperationException("WebAppRender is required. Please set WebAppRender in Client.Core's appsettings.json");

        Validator.TryValidateObject(WebAppRender, new ValidationContext(WebAppRender), validationResults, true);

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

        return validationResults;
    }
}

public partial class WebAppRenderOptions
{
    public bool PrerenderEnabled { get; set; }

    public BlazorWebAppMode BlazorMode { get; set; }

    public IComponentRenderMode? RenderMode
    {
        get
        {
            return BlazorMode switch
            {
                BlazorWebAppMode.BlazorAuto => new InteractiveAutoRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorWebAssembly => new InteractiveWebAssemblyRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorServer => new InteractiveServerRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorSsr => null,
                _ => throw new NotImplementedException(),
            };
        }
    }

    //-:cnd:noEmit
    /// <summary>
    /// To enable/disable pwa support, navigate to Directory.Build.props and modify the PwaEnabled flag.
    /// </summary>
    public bool PwaEnabled =>
#if PwaEnabled
        true;
#else
    false;
#endif
    //+:cnd:noEmit
}

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes#render-modes
/// </summary>
public enum BlazorWebAppMode
{
    BlazorAuto,
    BlazorServer,
    BlazorWebAssembly,
    /// <summary>
    /// Pre-rendering without interactivity
    /// </summary>
    BlazorSsr,
}

//#if (notification == true)
/// <summary>
/// https://github.com/adessoTurkey-dotNET/AdsPush
/// </summary>
public class AdsPushVapidOptions
{
    /// <summary>
    /// Web push's vapid. More info at https://tools.reactpwa.com/vapid
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = default!;
}
