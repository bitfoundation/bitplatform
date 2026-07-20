using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public static class AppRenderMode
{
    // Prerendering is required for SEO: without it, Blazor WASM returns an empty shell to crawlers and
    // social scrapers, so per-page titles/descriptions/canonical/OG tags and page content never reach them.
    // .NET 10 resolves the earlier prerendering issues, so this is enabled to serve fully-formed HTML.
    public static readonly bool PrerenderEnabled = true;

    public static IComponentRenderMode Auto { get; } = new InteractiveAutoRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);
    public static IComponentRenderMode NoPrerenderBlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(prerender: false);
    public static IComponentRenderMode NoPrerenderBlazorServer { get; } = new InteractiveServerRenderMode(prerender: false);

    public static IComponentRenderMode Current => BuildConfiguration.IsDebug() 
                                                    ? BlazorServer /*For better development experience*/ 
                                                    : BlazorWebAssembly;

    /// <summary>
    /// Is running under .NET MAUI?
    /// </summary>
    public static bool IsBlazorHybrid { get; set; }
}
