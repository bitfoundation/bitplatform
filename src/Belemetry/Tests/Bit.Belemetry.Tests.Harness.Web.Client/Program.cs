using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The host serves the capturing receiver, so the page's own origin is the collector origin. The server-pipeline switch
// means nothing here: in WebAssembly the .NET pipeline always runs, in the page, under the user's consent.
builder.Services.AddBelemetryHarness(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
