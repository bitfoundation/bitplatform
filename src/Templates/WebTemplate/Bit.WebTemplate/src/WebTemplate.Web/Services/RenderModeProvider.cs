using Microsoft.AspNetCore.Components.Web;

namespace WebTemplate.Web.Services;

public static class RenderModeProvider
{
    static IComponentRenderMode PrerenderEnabledAuto = RenderMode.InteractiveAuto;
    static IComponentRenderMode PrerenderEnabledBlazorWebAssembly = RenderMode.InteractiveWebAssembly;
    static IComponentRenderMode PrerenderEnabledBlazorServer = RenderMode.InteractiveServer;

    static IComponentRenderMode Auto = new InteractiveAutoRenderMode(prerender: false);
    static IComponentRenderMode BlazorWebAssembly = new InteractiveWebAssemblyRenderMode(prerender: false);
    static IComponentRenderMode BlazorServer = new InteractiveServerRenderMode(prerender: false);

    // PrerenderOnly: In order to have pre render only mode, simply remove @rendermode usages from App.razor

    public static IComponentRenderMode CurrentRenderMode = PrerenderEnabledAuto;
}

public class PrerenderOnlyRenderMode : IComponentRenderMode
{

}
