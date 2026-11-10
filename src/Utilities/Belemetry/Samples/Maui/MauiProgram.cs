using Bit.Belemetry.Samples.Core;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // There is no server to fetch from: the "fetch" button requests a file of this app's wwwroot.
        builder.Services.AddCoreServices(settings =>
        {
            settings.FetchUrl = "sample-data.json";
            settings.FailUrl = "missing-data.json";

            // Android's WebView and WKWebView ignore a download: the support bundle is written to the app's data folder
            // (share it from there with Share.RequestAsync). WebView2 on Windows saves downloads itself.
            if (OperatingSystem.IsWindows() is false)
            {
                settings.SaveBundle = async capture =>
                {
                    var path = Path.Combine(FileSystem.AppDataDirectory, capture.SuggestedFileName);
                    await using var file = File.Create(path);
                    await capture.ExportAsync(file);
                    return path;
                };
            }
        });

        builder.Services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "belemetry-samples-maui";
#if DEBUG
            o.Environment = "Development";
#else
            o.Environment = "Production";
#endif

            // In Blazor Hybrid the transport defaults to Relay: the browser script hands its records to .NET over JS
            // interop and the app process exports them with everything else. So there is no CORS to configure, and
            // exporter credentials never reach the WebView.
            o.AddOtlpExporter(OtlpEndpoint);

            // o.AddAzureMonitorExporter("InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/");

#if DEBUG
            o.AddConsoleExporter();
#endif

            o.Traces.Sources.Add(SampleTelemetry.Name);
            o.Metrics.Meters.Add(SampleTelemetry.Name);
        });

        var app = builder.Build();

        // Belemetry records what reaches AppDomain.UnhandledException itself (iOS and Mac Catalyst raise it for a crash),
        // and saves it for the next launch to send. These platforms may report a crash only through an event of their own:
        // forwarded, it is recorded and saved the same way, and one both events report is recorded once.
#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => app.Services.TrackBitBelemetryUnhandledException(e.Exception);
#elif WINDOWS
        // WinUI reports an exception on its UI thread here; it ends the app unless a handler sets e.Handled.
        Microsoft.UI.Xaml.Application.Current.UnhandledException += (_, e) => app.Services.TrackBitBelemetryUnhandledException(e.Exception);
#endif

        return app;
    }

    // The Android emulator reaches the development machine's localhost at 10.0.2.2.
    private static string OtlpEndpoint => OperatingSystem.IsAndroid() ? "http://10.0.2.2:4318" : "http://localhost:4318";
}
