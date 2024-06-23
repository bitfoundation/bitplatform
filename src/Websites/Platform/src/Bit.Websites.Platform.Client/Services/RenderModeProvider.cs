//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Websites.Platform.Client.Services;

public static class RenderModeProvider
{
    public static IComponentRenderMode PrerenderEnabledAuto { get; } = RenderMode.InteractiveAuto;
    public static IComponentRenderMode PrerenderEnabledBlazorWasm { get; } = RenderMode.InteractiveWebAssembly;
    public static IComponentRenderMode PrerenderEnabledBlazorServer { get; } = RenderMode.InteractiveServer;
    public static IComponentRenderMode Auto { get; } = new InteractiveAutoRenderMode(prerender: false);
    public static IComponentRenderMode BlazorWasm { get; } = new InteractiveWebAssemblyRenderMode(prerender: false);
    public static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(prerender: false);

    // PrerenderOnly: In order to have prerender only mode, simply remove @rendermode usages from App.razor

    public static IComponentRenderMode Current =>
#if DEBUG
    PrerenderEnabledBlazorServer; // Or BlazorServer, for better development experience.
#else
    PrerenderEnabledBlazorWasm;
#endif
}
