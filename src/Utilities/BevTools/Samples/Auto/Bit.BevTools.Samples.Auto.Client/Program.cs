using Bit.BevTools;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices();

// Once Auto has switched a visit to WebAssembly, BevTools runs in the browser, exactly as in the WebAssembly sample.
builder.Services.AddBitBevToolsServices(o => o.Enabled = builder.HostEnvironment.IsDevelopment());

await builder.Build().RunAsync();
