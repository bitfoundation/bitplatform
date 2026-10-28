// The WebAssembly half: where the sample pages run once the browser has the runtime. WebAssembly has one scope, so the
// scoped collaboration service is this tab's one replica, connected to the host's hub and stored in IndexedDB - a
// reload, or a document edited offline, starts where it left off.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices(collaboration => collaboration
    .UseSamplesRelay()
    .UseIndexedDbStore());

await builder.Build().RunAsync();
