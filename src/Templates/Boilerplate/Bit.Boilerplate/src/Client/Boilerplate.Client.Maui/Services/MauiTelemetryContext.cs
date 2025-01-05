namespace Boilerplate.Client.Maui.Services;

public class MauiTelemetryContext : AppTelemetryContext
{
    public override string? Platform { get; set; } = $"{DeviceInfo.Current.Manufacturer} {(AppPlatform.IsIosOnMacOS ? DevicePlatform.macOS : DeviceInfo.Current.Platform)} {DeviceInfo.Current.Version}";

    public override string? AppVersion { get; set; } = VersionTracking.CurrentVersion;

    //-:cnd:noEmit
    public override string? WebView { get; set; } =
#if Android
        $"{Android.Webkit.WebView.CurrentWebViewPackage?.PackageName} {Android.Webkit.WebView.CurrentWebViewPackage?.VersionName}";
#elif Windows
        $"EdgeWebView2 {Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()}";
#else
        null;
#endif
}
