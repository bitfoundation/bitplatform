using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public static class AppRenderMode
{
    public static readonly bool PrerenderEnabled = false;

    private static IComponentRenderMode Auto => new InteractiveAutoRenderMode(PrerenderEnabled);
    private static IComponentRenderMode BlazorWebAssembly => new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    private static IComponentRenderMode BlazorServer => new InteractiveServerRenderMode(PrerenderEnabled);
    public static IComponentRenderMode NoPrerenderBlazorWebAssembly => new InteractiveWebAssemblyRenderMode(prerender: false);

    public static IComponentRenderMode Current => BuildConfiguration.IsDebug() 
                                                    ? BlazorServer /*For better development experience*/ 
                                                    : BlazorWebAssembly;

    public static bool PwaEnabled =>
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
