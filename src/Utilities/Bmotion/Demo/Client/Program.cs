using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoServices(builder.HostEnvironment.BaseAddress);

// No RootComponents are registered on purpose: the host page drives the app through
// blazor.web.js render-mode markers (<Routes @rendermode="InteractiveWebAssembly" />),
// so the root component and the HeadOutlet are declared there instead.
await builder.Build().RunAsync();
