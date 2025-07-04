//+:cnd:noEmit
using Boilerplate.Client.Web;
using Boilerplate.Client.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Server.Web;

public partial class ServerWebSettings : ClientWebSettings
{
    public ForwardedHeadersOptions? ForwardedHeaders { get; set; } = default!;

    /// <summary>
    /// Specifies the allowed origins for CORS requests, URLs returned after social sign-in and email confirmation, and permitted origins for Web Auth, as well as forwarded headers middleware in ASP.NET Core.
    /// </summary>
    public Uri[] TrustedOrigins { get; set; } = [];
    [Required]
    public WebAppRenderOptions WebAppRender { get; set; } = default!;

    /// <summary>
    /// In a production environment, <see cref="ClientCoreSettings.ServerAddress"/> is usually set to  
    /// a URL like <c>https://api.myproject.com</c>, often secured behind a CDN or firewall.  
    /// However, during pre-rendering or in Blazor Server/Auto mode, using a local address  
    /// such as <c>http://localhost:8080</c> is much more efficient.  
    /// This optional setting allows overriding HttpClient's BaseAddress specifically for the server project.
    /// </summary>
    public string? ServerSideHttpClientBaseAddress { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (WebAppRender is null)
            throw new InvalidOperationException("WebAppRender is required. Please set WebAppRender in appsettings.json");

        Validator.TryValidateObject(WebAppRender, new ValidationContext(WebAppRender), validationResults, true);

        if (ForwardedHeaders is not null)
        {
            Validator.TryValidateObject(ForwardedHeaders, new ValidationContext(ForwardedHeaders), validationResults, true);
        }

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
            var mode = BlazorMode;

            // When opening an .slnx/.slnf solutions in Visual Studio instead of .sln,  
            // you can switch between to `DebugBlazorServer` configuration to have optimized build times during development.  
            // If `DebugBlazorServer` is selected, `BlazorMode` will be set to `BlazorServer`  
            // regardless of its value in appsettings.json
            //-:cnd:noEmit
#if DebugBlazorServer
            mode = BlazorWebAppMode.BlazorServer;
#endif
            //+:cnd:noEmit

            return mode switch
            {
                BlazorWebAppMode.BlazorAuto => new InteractiveAutoRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorWebAssembly => new InteractiveWebAssemblyRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorServer => new InteractiveServerRenderMode(PrerenderEnabled),
                BlazorWebAppMode.BlazorSsr => null,
                _ => throw new NotImplementedException(),
            };
        }
    }
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
