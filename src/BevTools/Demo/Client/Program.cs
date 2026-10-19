using Bit.BevTools;
using Bit.BevTools.Demo.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Match the server host: the demo deliberately registers a captive dependency and a cycle so the Services panel can
// diagnose them. WebAssembly validates registrations during Build by default, which would otherwise prevent the
// Interactive Auto client from starting before BevTools can show those registrations.
builder.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);

builder.Services.AddDemoServices();

// An application passes the WebAssembly host environment explicitly
// (options.Enabled = builder.HostEnvironment.IsDevelopment()), so that Development-only activation does not depend on
// server process environment variables. The demo exists to show BevTools wherever it is deployed, so it is always on.
builder.Services.AddBitBevToolsServices(options =>
{
    options.Enabled = true;
    options.Ui.OpenByDefault = false;
});

await builder.Build().RunAsync();
