// The WebAssembly half: where the sample pages run once the browser has the runtime. WebAssembly has one scope, so the
// tab has one tour engine for as long as it is open, and the library's module and the store are reached in-process.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices();

await builder.Build().RunAsync();
