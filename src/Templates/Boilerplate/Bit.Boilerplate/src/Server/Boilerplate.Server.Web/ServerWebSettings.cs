//+:cnd:noEmit
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Boilerplate.Client.Web;
using Boilerplate.Client.Core;

namespace Boilerplate.Server.Web;

public partial class ServerWebSettings : ClientWebSettings
{
    public ForwardedHeadersOptions ForwardedHeaders { get; set; } = default!;

    public ResponseCachingOptions ResponseCaching { get; set; } = default!;

    [Required]
    public WebAppRenderOptions WebAppRender { get; set; } = default!;

    /// <summary>
    /// Handles API data fetching during pre-rendering using an HTTP client.
    /// </summary>
    /// <remarks>
    /// In a production environment, the <see cref="ClientCoreSettings.ServerAddress"/> typically points to
    /// a URL like <c>https://api.myproject.com</c>, which is often behind a CDN, firewall, etc.
    /// However, during pre-rendering, using a local address like <c>http://localhost:8080</c>
    /// is significantly more efficient.
    /// </remarks>
    public string? ServerSideHttpClientBaseAddress { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (WebAppRender is null)
            throw new InvalidOperationException("WebAppRender is required. Please set WebAppRender in appsettings.json");

        Validator.TryValidateObject(WebAppRender, new ValidationContext(WebAppRender), validationResults, true);

        Validator.TryValidateObject(ForwardedHeaders, new ValidationContext(ForwardedHeaders), validationResults, true);

        Validator.TryValidateObject(ResponseCaching, new ValidationContext(ResponseCaching), validationResults, true);

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

            // When opening an .slnx solution in Visual Studio instead of .sln or .slnf,  
            // you can switch between configurations like `DebugBlazorServer` and `DebugBlazorWasm`.  
            // If `DebugBlazorServer` is selected, `BlazorMode` will be set to `BlazorServer`  
            // regardless of its value in appsettings.json
#if DebugBlazorServer
            mode = BlazorWebAppMode.BlazorServer;
#elif DebugBlazorWasm
            mode = BlazorWebAppMode.BlazorWebAssembly;
#endif

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
