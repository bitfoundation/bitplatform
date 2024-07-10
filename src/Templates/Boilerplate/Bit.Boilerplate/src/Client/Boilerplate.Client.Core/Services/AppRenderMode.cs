//-:cnd:noEmit
﻿using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Services;

public static class AppRenderMode
{
    public static readonly bool PrerenderEnabled = false;

    private static IComponentRenderMode Auto { get; } = new InteractiveAutoRenderMode(PrerenderEnabled);
    private static IComponentRenderMode BlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    private static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);
    public static IComponentRenderMode NoPrerenderBlazorWebAssembly => new InteractiveWebAssemblyRenderMode(prerender: false);

    public static IComponentRenderMode Current =>
        AppEnvironment.IsDev() 
        ? BlazorServer // For better development experience.
        : Auto; // For better production experience.

    /// <summary>
    /// To enable/disable pwa support, navigate to Directory.Build.props and modify the PwaEnabled flag.
    /// </summary>
    public static bool PwaEnabled { get; } =
#if PwaEnabled
        true;
#else
    false;
#endif

    public static bool IsBlazorHybrid { get; set; }

    /// <summary>
    /// Instead of checking <see cref="OperatingSystem.IsMacOS"/>, <see cref="OperatingSystem.IsMacCatalyst"/> and Foundation.NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac, you can easily check this property's value.
    /// </summary>
    public static bool IsRunningOnMacOS { get; set; }
}
