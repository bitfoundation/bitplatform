using Bit.Belemetry;
using Bit.Belemetry.Samples.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices();

// In WebAssembly the .NET pipeline runs in the browser: ILogger, Activity and Meter data, and IBelemetry calls, are
// exported from the page next to the browser script's records, under the same user consent. Keep the service name
// and exporters in step with the server's Program.cs, which renders the browser script's configuration.
builder.Services.AddBitBelemetryServices(o =>
{
    o.ServiceName = "belemetry-samples-wasm";
    o.Environment = builder.HostEnvironment.Environment;

    // Both the script and .NET post from the page, so the endpoint has to allow this site's origin (CORS).
    o.AddOtlpExporter("http://localhost:4318");

    // o.AddAzureMonitorExporter("InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/");

    if (builder.HostEnvironment.IsDevelopment())
    {
        o.AddConsoleExporter();
    }

    o.Traces.Sources.Add(SampleTelemetry.Name);
    o.Metrics.Meters.Add(SampleTelemetry.Name);
});

await builder.Build().RunAsync();
