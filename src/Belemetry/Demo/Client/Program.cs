using Bit.Belemetry;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoServices(builder.HostEnvironment.BaseAddress);

// The .NET half of Belemetry runs here, in the browser: ILogger, Activity and Meter records of the
// WebAssembly app. They go to the demo's own server, whose in-process OTLP receiver feeds the Live
// telemetry page - so the endpoint is this app's origin, absolute, plus "otlp" (the receiver appends
// /v1/traces, /v1/logs and /v1/metrics). JSON keeps the payloads readable in the network tab.
//
// The browser script itself is configured by the server (<BelemetryScript /> in App.razor); this
// callback adds to the shared configuration in AddDemoServices rather than replacing it.
builder.Services.AddBitBelemetryServices(options =>
{
    options.AddOtlpExporter(builder.HostEnvironment.BaseAddress + "otlp", otlp =>
    {
        otlp.Protocol = BelemetryOtlpProtocol.HttpJson;
    });
});

// No RootComponents are registered on purpose: the host page drives the app through
// blazor.web.js render-mode markers (<Routes @rendermode="InteractiveWebAssembly" />),
// so the root component and the HeadOutlet are declared there instead.
await builder.Build().RunAsync();
