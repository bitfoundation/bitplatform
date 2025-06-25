using System.Runtime.Versioning;

namespace Boilerplate.Shared.Services;

public static partial class AppPlatform
{
    public static bool IsBlazorHybrid { get; set; }

    public static bool IsBlazorHybridOrBrowser => IsBlazorHybrid || IsBrowser;

    [SupportedOSPlatformGuard("android")]
    public static bool IsAndroid => IsBlazorHybrid && OperatingSystem.IsAndroid();

    [SupportedOSPlatformGuard("ios")]
    public static bool IsIos => IsBlazorHybrid && OperatingSystem.IsIOS() && !IsIosOnMacOS;

    [SupportedOSPlatformGuard("windows")]
    public static bool IsWindows => IsBlazorHybrid && OperatingSystem.IsWindows();

    /// <summary>
    /// Code executes in the browser via Blazor WebAssembly.
    /// </summary>
    [SupportedOSPlatformGuard("browser")]
    public static bool IsBrowser => OperatingSystem.IsBrowser();

    [SupportedOSPlatformGuard("macOS")]
    public static bool IsMacOS => IsBlazorHybrid && OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || IsIosOnMacOS;

    /// <summary>
    /// This is not supported yet in bit Boilerplate.
    /// </summary>
    public static bool IsLinux => IsBlazorHybrid && OperatingSystem.IsLinux();

    [SupportedOSPlatformGuard("ios")]
    public static bool IsIosOnMacOS { get; set; }

    public static AppPlatformType Type =>
        IsAndroid ? AppPlatformType.Android :
        IsIos ? AppPlatformType.Ios :
        IsWindows ? AppPlatformType.Windows :
        IsMacOS ? AppPlatformType.MacOS :
        IsLinux ? AppPlatformType.Linux : AppPlatformType.Web;
}

public enum AppPlatformType
{
    Web,
    Ios,
    MacOS,
    Linux,
    Android,
    Windows
}
