using System.Runtime.Versioning;

namespace Boilerplate.Shared.Services;

public static partial class AppPlatform
{
    public static bool IsBlazorHybrid { get; set; }

    public static bool IsBlazorHybridOrBrowser => IsBlazorHybrid || IsBrowser;

    [SupportedOSPlatformGuard("android")]
    public static bool IsAndroid => OperatingSystem.IsAndroid();

    [SupportedOSPlatformGuard("ios")]
    public static bool IsIOS => OperatingSystem.IsIOS() && !IsIosOnMacOS;

    [SupportedOSPlatformGuard("windows")]
    public static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>
    /// Blazor WebAssembly
    /// </summary>
    [SupportedOSPlatformGuard("browser")]
    public static bool IsBrowser => OperatingSystem.IsBrowser();

    [SupportedOSPlatformGuard("macOS")]
    public static bool IsMacOS => OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || IsIosOnMacOS;

    [SupportedOSPlatformGuard("ios")]
    public static bool IsIosOnMacOS { get; set; }
}
