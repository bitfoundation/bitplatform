using Bit.BevTools;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBevToolsServices here: Bit.BevTools.wasm is not downloaded yet when this runs, and the call would need it.
// The loader installs the hooks a later attach needs instead; outside Development BevTools waits until the app's
// "Open dev tools" button (the layout) asks for it.
builder.Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = builder.HostEnvironment.IsDevelopment());

await builder.Build().RunAsync();
