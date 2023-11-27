//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Web;

namespace BlazorWeb.Client.Services;

public static class RenderModeProvider
{
    public static bool PrerenderEnabled { get; set; } = false;
    public static IComponentRenderMode Auto { get; } = new InteractiveAutoRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorWasm { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);

    // PrerenderOnly: In order to have prerender only mode, simply remove @rendermode usages from App.razor

    public static IComponentRenderMode Current =>
#if DEBUG
    BlazorServer; // Or BlazorServer, for better development experience.
#else
    Auto;
#endif
}
