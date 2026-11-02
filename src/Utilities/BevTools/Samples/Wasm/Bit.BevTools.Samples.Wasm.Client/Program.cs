using Bit.BevTools;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices();

// The WebAssembly host environment is passed explicitly, so Development-only activation does not depend on the
// server process's environment variables.
builder.Services.AddBitBevToolsServices(o => o.Enabled = builder.HostEnvironment.IsDevelopment());

await builder.Build().RunAsync();
