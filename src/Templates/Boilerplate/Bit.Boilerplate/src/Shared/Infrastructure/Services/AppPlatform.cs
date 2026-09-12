using System.Runtime.Versioning;

namespace Boilerplate.Shared.Infrastructure.Services;

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

    /// <summary>
    /// Blazor WebAssembly Standalone: Client.Web on its own (a Static Web App, for instance), with no Server.Web in
    /// front of it, so nothing but the app itself renders the document's head. Set in Client.Web's Program.
    /// <see cref="IsBrowser"/> is true for both Blazor WebAssembly Standalone and Blazor WebAssembly Hosted, so this property is needed to distinguish between the two.
    /// </summary>
    public static bool IsWasmStandalone { get; set; }

    [SupportedOSPlatformGuard("macOS")]
    public static bool IsMacOS => IsBlazorHybrid && OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || IsIosOnMacOS;

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
