using Microsoft.AspNetCore.Components.Web;
using OS = System.OperatingSystem;

namespace Boilerplate.Client.Core.Services;
public static class RenderModeProvider
{
    public static bool PrerenderEnabled { get; set; } = false;
    public static IComponentRenderMode Auto { get; } = new InteractiveAutoRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorWasm { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);

    // PrerenderOnly: In order to have prerender only mode, simply remove @rendermode usages from App.razor

    public static IComponentRenderMode Current =>
        BuildConfigurationModeDetector.Current.IsDebug() ? BlazorServer /*for better development experience*/ : Auto;

    public static bool IsHybridRender() => OS.IsAndroid()
        || OS.IsIOS() || OS.IsMacCatalyst() || OS.IsMacOS() || OS.IsWindows();
}
