namespace Boilerplate.Client.Core.Services;

public static class AppOperatingSystem
{
    public static bool IsBlazorHybrid { get; set; }

    public static bool IsBlazorHybridOrBrowser => IsBlazorHybrid || IsRunningOnBrowser;

    /// <summary>
    /// Instead of checking <see cref="OperatingSystem.IsMacOS"/>, <see cref="OperatingSystem.IsMacCatalyst"/> and Foundation.NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac, you can easily check this property's value.
    /// </summary>
    public static bool IsRunningOnMacOS { get; set; }

    public static bool IsRunningOnAndroid => OperatingSystem.IsAndroid();
    public static bool IsRunningOnIOS => OperatingSystem.IsIOS();
    public static bool IsRunningOnWindows => OperatingSystem.IsWindows();
    public static bool IsRunningOnBrowser => OperatingSystem.IsBrowser();
}
