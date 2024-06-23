using Microsoft.AspNetCore.Components.Web;
using OS = System.OperatingSystem;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public static class AppRenderMode
{
    public static readonly bool PrerenderEnabled = false;

    private static IComponentRenderMode Auto { get; } = new InteractiveAutoRenderMode(PrerenderEnabled);
    private static IComponentRenderMode BlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    private static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);
    public static IComponentRenderMode NoPrerenderBlazorWebAssembly => new InteractiveWebAssemblyRenderMode(prerender: false);

    public static IComponentRenderMode Current =>
        BuildConfiguration.IsDebug() ? BlazorServer /*For better development experience*/ : Auto;

    public static bool PwaEnabled { get; } =
#if PwaEnabled
        true;
#else
    false;
#endif

    /// <summary>
    /// Is running under .NET MAUI?
    /// </summary>
    public static bool IsBlazorHybrid { get; set; }
}
