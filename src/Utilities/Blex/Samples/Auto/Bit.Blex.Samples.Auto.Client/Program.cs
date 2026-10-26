using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The same registrations as the server's: under InteractiveAuto the pages render in both containers.
builder.Services.AddWebServices();

// No RootComponents on purpose: the host page drives the app through blazor.web.js render-mode
// markers (<Routes @rendermode="InteractiveAuto" />).
await builder.Build().RunAsync();
