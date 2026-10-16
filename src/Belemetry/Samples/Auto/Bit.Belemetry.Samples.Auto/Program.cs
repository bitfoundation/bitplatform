using Bit.Belemetry;
using Bit.Belemetry.Samples.Core;
using Bit.Belemetry.Samples.Auto.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCoreServices();

// Renders <BelemetryScript />'s configuration and serves the interactive Server phase of Auto. Keep the service
// name and exporters in step with the client's Program.cs.
builder.Services.AddBitBelemetryServices(o =>
{
    o.ServiceName = "belemetry-samples-auto";
    o.Environment = builder.Environment.EnvironmentName;

    o.AddOtlpExporter("http://localhost:4318");

    // o.AddAzureMonitorExporter("InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/");

    if (builder.Environment.IsDevelopment())
    {
        o.AddConsoleExporter();
    }

    // Until Auto switches a visit to WebAssembly, the pages run on a circuit here, so the server pipeline collects
    // their ILogger, Activity and Meter data. Leave it off when the host has an OpenTelemetry setup of its own.
    o.Server.CollectDotnetTelemetry = true;
    o.Traces.Sources.Add(SampleTelemetry.Name);
    o.Metrics.Meters.Add(SampleTelemetry.Name);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
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

// The target of the "fetch" button.
app.MapGet("/api/weather", () => new { summary = "Sunny", temperatureC = 23 });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
