using Bit.BevTools;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBevToolsServices here: Bit.BevTools.wasm is not downloaded yet when this runs, and the call would need it.
// The loader installs the hooks a later attach needs instead; outside Development BevTools waits until the app's
// "Open dev tools" button (the layout) asks for it. Logging starts with the app all the same, on its own inside the
// loader: the session that attaches shows what was logged before it in its Logs panel.
builder.Services.AddBitBevToolsLoaderServices(o =>
{
    o.RegisterAtStartup = builder.HostEnvironment.IsDevelopment();
    o.AutoStart = BevToolsAutoStart.Logs;
});

await builder.Build().RunAsync();
