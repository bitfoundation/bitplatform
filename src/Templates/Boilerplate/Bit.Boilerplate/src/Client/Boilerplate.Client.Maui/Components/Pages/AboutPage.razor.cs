using System.Diagnostics;

namespace Boilerplate.Client.Maui.Components.Pages;

public partial class AboutPage
{
    private string appName = default!;
    private string appVersion = default!;
    private string processId = default!;

    protected async override Task OnInitAsync()
    {
        appName = AppInfo.Name;
#if Android
        // You have direct access to the Android, iOS, macOS, and Windows SDK features along with the ability to
        // call third-party Java, Kotlin, Swift, and Objective-C libraries.
        // https://stackoverflow.com/a/2941199/2720104
        appVersion = MauiApplication.Current.PackageManager!.GetPackageInfo(MauiApplication.Current.PackageName!, 0)!.VersionName!;
#else
        appVersion = AppInfo.Version.ToString();
#endif
        processId = Environment.ProcessId.ToString();

        await base.OnInitAsync();
    }
}
