﻿//+:cnd:noEmit

namespace Boilerplate.Client.Maui.Components.Pages;

public partial class AboutPage
{
    [AutoInject] private ITelemetryContext telemetryContext = default!;


    private string oem = default!;
    private string appName = default!;
    private string webView = default!;
    private string platform = default!;
    private string processId = default!;
    private string appVersion = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // You have direct access to the Android, iOS, macOS, and Windows SDK features along with the ability to
        // call third-party Java, Kotlin, Swift, and Objective-C libraries.
        // https://stackoverflow.com/a/2941199/2720104
        appName = AppInfo.Name;
        webView = telemetryContext.WebView!;
        platform = telemetryContext.Platform!;
        oem = DeviceInfo.Current.Manufacturer;
        appVersion = telemetryContext.AppVersion!;
        processId = Environment.ProcessId.ToString();
    }
}
