using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoClientServices();

// No RootComponents are registered on purpose: the server's App.razor drives the app through
// blazor.web.js render-mode markers (<Routes @rendermode=... />, <HeadOutlet @rendermode=... />),
// so the root components are declared there instead.
await builder.Build().RunAsync();
