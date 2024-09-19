using System.Runtime.InteropServices;

namespace Boilerplate.Client.Core.Services;

public static partial class AppPlatform
{
    public static bool IsBlazorHybrid { get; set; }

    public static bool IsBlazorHybridOrBrowser => IsBlazorHybrid || IsBrowser;

    public static bool IsAndroid => OperatingSystem.IsAndroid();
    public static bool IsIOS => OperatingSystem.IsIOS();
    public static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>
    /// Blazor WebAssembly
    /// </summary>
    public static bool IsBrowser => OperatingSystem.IsBrowser();
    public static bool IsMacOS => OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || IsIosOnMacOS;
    public static bool IsIosOnMacOS { get; set; }

    public static string OSDescription { get; set; } = RuntimeInformation.OSDescription;
}
