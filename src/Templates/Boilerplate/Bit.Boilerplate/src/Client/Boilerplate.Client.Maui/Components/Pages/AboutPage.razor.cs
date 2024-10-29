//-:cnd:noEmit

namespace Boilerplate.Client.Maui.Components.Pages;

public partial class AboutPage
{
    [AutoInject] private ITelemetryContext telemetryContext = default!;

    protected override string? Title => Localizer[nameof(AppStrings.AboutTitle)];
    protected override string? Subtitle => string.Empty;


    private string appName = default!;
    private string appVersion = default!;
    private string processId = default!;
    private string os = default!;
    private string oem = default!;

    protected async override Task OnInitAsync()
    {
        // You have direct access to the Android, iOS, macOS, and Windows SDK features along with the ability to
        // call third-party Java, Kotlin, Swift, and Objective-C libraries.
        // https://stackoverflow.com/a/2941199/2720104
        appName = AppInfo.Name;
        appVersion = telemetryContext.AppVersion!;
        os = $"{telemetryContext.OS} {telemetryContext.WebView}";
        processId = Environment.ProcessId.ToString();
        oem = DeviceInfo.Current.Manufacturer;

        await base.OnInitAsync();
    }
}
