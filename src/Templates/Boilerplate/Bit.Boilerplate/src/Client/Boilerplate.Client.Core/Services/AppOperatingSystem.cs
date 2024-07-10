namespace Boilerplate.Client.Core.Services;

public static class AppOperatingSystem
{
    public static bool IsBlazorHybrid { get; set; }

    public static bool IsBlazorHybridOrBrowser => IsBlazorHybrid || IsBrowser;

    /// <summary>
    /// Instead of checking <see cref="OperatingSystem.IsMacOS"/>, <see cref="OperatingSystem.IsMacCatalyst"/> and Foundation.NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac, you can easily check this property's value.
    /// </summary>
    public static bool IsMacOS { get; set; }
    public static bool IsAndroid => OperatingSystem.IsAndroid();
    public static bool IsIOS => OperatingSystem.IsIOS();
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsBrowser => OperatingSystem.IsBrowser();
}
