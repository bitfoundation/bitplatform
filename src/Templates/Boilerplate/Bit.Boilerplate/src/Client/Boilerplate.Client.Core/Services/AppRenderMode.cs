using Microsoft.AspNetCore.Components.Web;
using OS = System.OperatingSystem;

namespace Boilerplate.Client.Core.Services;

public static class AppRenderMode
{
    public const bool PrerenderEnabled = false;

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

    public static bool IsHybrid() => OS.IsAndroid() || OS.IsIOS() || OS.IsMacCatalyst() || OS.IsMacOS() || OS.IsWindows();
}
