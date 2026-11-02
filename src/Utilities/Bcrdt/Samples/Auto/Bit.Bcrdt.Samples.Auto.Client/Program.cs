// The WebAssembly half of Auto: where a page runs once the browser has the runtime. The registration matches the
// server's, so a page finds the same services on whichever side it renders - but for the hub's address, which in the
// browser is the page's own.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices(collaboration => collaboration
    .UseSamplesRelay()
    .UseIndexedDbStore());

await builder.Build().RunAsync();
