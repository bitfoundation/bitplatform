using Bit.Belemetry;
using Bit.Belemetry.Samples.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Samples.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
        services.AddLogging(logging => logging.AddDebug());
#else
        services.AddLogging();
#endif

        // There is no server to fetch from: the "fetch" button requests a file of this app's wwwroot.
        services.AddCoreServices(settings => settings.FetchUrl = "sample-data.json");

        services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "belemetry-samples-winforms";
#if DEBUG
            o.Environment = "Development";
#else
            o.Environment = "Production";
#endif

            // No web host is registered, so Belemetry detects Hybrid and defaults to the Relay transport: the WebView's
            // records reach .NET over JS interop and this process exports them, persisting them on disk while offline.
            o.AddOtlpExporter("http://localhost:4318");

            // o.AddAzureMonitorExporter("InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/");

#if DEBUG
            o.AddConsoleExporter();
#endif

            o.Traces.Sources.Add(SampleTelemetry.Name);
            o.Metrics.Meters.Add(SampleTelemetry.Name);
        });

        using var provider = services.BuildServiceProvider();
        Application.Run(new MainForm(provider));
    }
}
