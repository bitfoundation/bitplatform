using Bit.BevTools;
using Bit.BevTools.Samples.Lazy.Client.DevTools;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBevToolsServices here: Bit.BevTools.wasm is not downloaded yet when this runs, and the call would need it.
// The loader installs the hooks a later attach needs instead; outside Development BevTools waits until the app's
// "Open dev tools" button (the layout) asks for it. Logging starts with the app all the same, on its own inside the
// loader: the session that attaches shows what was logged before it in its Logs panel. The app's own panels, extension
// and options go through ConfigureBevTools, which serves both the startup registration and the attach: the lambda's body
// is compiled when it first runs, once Bit.BevTools is loaded, so nothing here names a Bit.BevTools type.
builder.Services.AddBitBevToolsLoaderServices(o =>
{
    o.RegisterAtStartup = builder.HostEnvironment.IsDevelopment();
    o.AutoStart = BevToolsAutoStart.Logs;
    o.ConfigureBevTools(static options => SampleDevTools.Configure((BevToolsOptions)options));
});

await builder.Build().RunAsync();
