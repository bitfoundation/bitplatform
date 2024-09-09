//-:cnd:noEmit
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Services;

public static class AppRenderMode
{
    public static readonly bool PrerenderEnabled = false;

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes#render-modes
    public static IComponentRenderMode BlazorAuto { get; } = new InteractiveAutoRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);
    public static IComponentRenderMode? StaticSsr { get; } = null /*Pre-rendering without interactivity*/;
    public static IComponentRenderMode NoPrerenderBlazorWebAssembly => new InteractiveWebAssemblyRenderMode(prerender: false);

    public static IComponentRenderMode? Current =>
        AppEnvironment.IsDev()
        ? BlazorServer // For better development experience.
        : BlazorAuto; // For better production experience.

    /// <summary>
    /// To enable/disable pwa support, navigate to Directory.Build.props and modify the PwaEnabled flag.
    /// </summary>
    public static bool PwaEnabled { get; } =
#if PwaEnabled
        true;
#else
    false;
#endif
}
