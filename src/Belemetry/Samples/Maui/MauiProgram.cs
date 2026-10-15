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
        builder.Services.AddCoreServices(settings => settings.FetchUrl = "sample-data.json");

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

        return builder.Build();
    }

    // The Android emulator reaches the development machine's localhost at 10.0.2.2.
    private static string OtlpEndpoint => OperatingSystem.IsAndroid() ? "http://10.0.2.2:4318" : "http://localhost:4318";
}
