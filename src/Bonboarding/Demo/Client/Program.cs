// The WebAssembly half of the demo: the same Bonboarding registration as the server, through AddDemoServices.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoServices();

// No RootComponents are registered on purpose: the host page drives the app through
// blazor.web.js render-mode markers (<Routes @rendermode="InteractiveAuto" />).
await builder.Build().RunAsync();
