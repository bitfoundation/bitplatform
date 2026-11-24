//+:cnd:noEmit
using Boilerplate.Client.Web;
using Boilerplate.Client.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;

namespace Boilerplate.Server.Web;

public partial class ServerWebSettings : ClientWebSettings
{
    /// <summary>
    /// Specifies the allowed origins for CORS requests, URLs returned after external sign-in and email confirmation, and permitted origins for Web Auth, as well as forwarded headers middleware in ASP.NET Core.
    /// Each entry may contain the <c>*</c> wildcard (e.g. <c>https://*.myapp.com</c>) so a single entry can trust every tenant subdomain.
    /// </summary>
    public string[] TrustedOrigins { get; set; } = [];
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

    /// <summary>
    /// What <c>/llms.txt</c> (https://llmstxt.org) tells AI agents about the app. The pages it lists are the ones
    /// <c>sitemap.xml</c> lists, with the descriptions their <see cref="PageUrls"/> constants carry.
    /// </summary>
    public LlmsTxtOptions LlmsTxt { get; set; } = new();

    /// <summary>
    /// <c>/.well-known/security.txt</c> (https://www.rfc-editor.org/rfc/rfc9116): whom to tell about a vulnerability.
    /// Served only once <see cref="SecurityTxtOptions.Contact"/> has an address.
    /// </summary>
    public SecurityTxtOptions SecurityTxt { get; set; } = new();
}

public partial class LlmsTxtOptions
{
    /// <summary>The app's name, which the file opens with.</summary>
    public string Name { get; set; } = "Boilerplate";

    /// <summary>One or two sentences on what the app is and who it is for, under the name.</summary>
    public string? Summary { get; set; }
}

public partial class SecurityTxtOptions
{
    /// <summary>Where to report a vulnerability: <c>mailto:</c>, <c>https:</c> or <c>tel:</c> uris, the preferred one first.</summary>
    public string[] Contact { get; set; } = [];

    /// <summary>
    /// When what the file says should be checked again, less than a year ahead. Empty, it is 180 days from each request,
    /// so the file never lapses while the app runs.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>The languages a report may be written in, such as <c>en, nl</c>.</summary>
    public string? PreferredLanguages { get; set; }

    /// <summary>The address of the app's vulnerability disclosure policy, if it has one.</summary>
    public string? Policy { get; set; }
}

[OptionsValidator]
public partial class ServerWebSettingsValidator : IValidateOptions<ServerWebSettings>
{
}

// [mirror] WebAppRenderOptions - keep in sync with:
// - src/LandingPage/Boilerplate.LandingPage.Server/LandingServerSettings.cs
public partial class WebAppRenderOptions
{
    public bool PrerenderEnabled { get; set; }

    public BlazorWebAppMode BlazorMode { get; set; }

    private BlazorWebAppMode EffectiveBlazorMode
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

            return mode;
        }
    }

    public IComponentRenderMode? RenderMode => GetRenderMode(PrerenderEnabled);

    /// <summary>
    /// What <c>?no-prerender</c> renders with: the configured mode, only without pre-rendering. It deliberately does
    /// NOT force WebAssembly - the flag says "skip pre-rendering", not "change how this app runs", and the shipped
    /// service worker requests the app shell with that flag on every published deployment.
    /// </summary>
    public IComponentRenderMode? NoPrerenderRenderMode => GetRenderMode(prerenderEnabled: false);

    private IComponentRenderMode? GetRenderMode(bool prerenderEnabled) => EffectiveBlazorMode switch
    {
        BlazorWebAppMode.BlazorAuto => new InteractiveAutoRenderMode(prerenderEnabled),
        BlazorWebAppMode.BlazorWebAssembly => new InteractiveWebAssemblyRenderMode(prerenderEnabled),
        BlazorWebAppMode.BlazorServer => new InteractiveServerRenderMode(prerenderEnabled),
        BlazorWebAppMode.BlazorSsr => null,
        _ => throw new NotImplementedException(),
    };
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
