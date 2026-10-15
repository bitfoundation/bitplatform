using Bit.Belemetry;
using Bit.Belemetry.Samples.Core;
using Bit.Belemetry.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCoreServices();

// The server prerenders the pages and renders <BelemetryScript />, whose configuration comes from these options -
// keep them in step with the client's Program.cs, where the .NET pipeline actually runs.
builder.Services.AddBitBelemetryServices(o =>
{
    o.ServiceName = "belemetry-samples-wasm";
    o.Environment = builder.Environment.EnvironmentName;

    o.AddOtlpExporter("http://localhost:4318");

    // o.AddAzureMonitorExporter("InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/");

    if (builder.Environment.IsDevelopment())
    {
        o.AddConsoleExporter();
    }

    // The server pipeline stays off (the default): this host only prerenders. Set o.Server.CollectDotnetTelemetry
    // to also export the server's own ILogger, Activity and Meter data when it has no OpenTelemetry setup.
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// The target of the "fetch" button.
app.MapGet("/api/weather", () => new { summary = "Sunny", temperatureC = 23 });

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
