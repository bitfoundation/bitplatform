//-:cnd:noEmit

namespace Boilerplate.Client.Maui.Components.Pages;

public partial class AboutPage
{
    protected override string? Title => Localizer[nameof(AppStrings.AboutTitle)];
    protected override string? Subtitle => string.Empty;


    private string appName = default!;
    private string appVersion = default!;
    private string processId = default!;
    private string os = default!;
    private string oem = default!;

    protected async override Task OnInitAsync()
    {
        appName = AppInfo.Name;
        appVersion = AppInfo.Version.ToString();
        processId = Environment.ProcessId.ToString();
        os = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";
#if Android
        // You have direct access to the Android, iOS, macOS, and Windows SDK features along with the ability to
        // call third-party Java, Kotlin, Swift, and Objective-C libraries.
        // https://stackoverflow.com/a/2941199/2720104
        os += $" {Android.Webkit.WebView.CurrentWebViewPackage?.PackageName}: {Android.Webkit.WebView.CurrentWebViewPackage?.VersionName}";
#elif Windows
        os += $" EdgeWebView2: {Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()}";
#endif
        oem = DeviceInfo.Current.Manufacturer;

        await base.OnInitAsync();
    }
}
