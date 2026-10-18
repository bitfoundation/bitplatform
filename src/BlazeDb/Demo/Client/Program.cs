// The WebAssembly half of the demo, and the only half that runs it: every page drives a real database
// held in this browser tab, so nothing is prerendered on the server (see Server/Components/App.razor).

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[assembly: SupportedOSPlatform("browser")]

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoServices();

// No RootComponents are registered on purpose: the host page drives the app through
// blazor.web.js render-mode markers (<Routes @rendermode="..." />).
await builder.Build().RunAsync();
