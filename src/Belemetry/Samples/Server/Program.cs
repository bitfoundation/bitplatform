using Bit.Belemetry;
using Bit.Belemetry.Samples.Core;
using Bit.Belemetry.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCoreServices();

builder.Services.AddBitBelemetryServices(o =>
{
    o.ServiceName = "belemetry-samples-server";
    o.Environment = builder.Environment.EnvironmentName;

    // OTLP/HTTP to an OpenTelemetry Collector or the Aspire dashboard. On Blazor Server the browser script exports
    // straight to it as well, so the endpoint has to allow this site's origin (CORS). For a standalone dashboard:
    //   docker run -p 18888:18888 -p 4318:18890 -e DASHBOARD__OTLP__CORS__ALLOWEDORIGINS=* mcr.microsoft.com/dotnet/aspire-dashboard
    o.AddOtlpExporter("http://localhost:4318");

    // Azure Monitor (Application Insights), straight to its ingestion endpoint - no collector needed:
    // o.AddAzureMonitorExporter("InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/");

    if (builder.Environment.IsDevelopment())
    {
        // Prints every record: to the server console, and to the browser console for the browser's records.
        o.AddConsoleExporter();
    }

    // By default a server only serves the browser script and leaves .NET telemetry to the host's own OpenTelemetry
    // setup. This sample has none, so the server pipeline collects ILogger, Activity and Meter data itself. It is
    // process-wide - it serves every user at once - so it follows Server.Features, not any one user's consent.
    o.Server.CollectDotnetTelemetry = true;
    o.Traces.Sources.Add(SampleTelemetry.Name);
    o.Metrics.Meters.Add(SampleTelemetry.Name);
});

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// A deep link to an unknown address matches no endpoint, which would end in an empty 404: re-execute it to render the
// shared Not found page instead, keeping the status code.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// The target of the "fetch" button. The browser's request carries a traceparent header (same origin), so this
// request's server span and the browser's client span share one trace.
app.MapGet("/api/weather", () => new { summary = "Sunny", temperatureC = 23 });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
