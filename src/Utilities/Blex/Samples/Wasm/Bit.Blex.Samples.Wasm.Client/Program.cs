using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The same registrations as the server's: every page is instantiated once there for the prerender
// pass and again here, so anything a page injects must resolve in both containers.
builder.Services.AddWebServices();

// No RootComponents on purpose: the host page drives the app through blazor.web.js render-mode
// markers (<Routes @rendermode="InteractiveWebAssembly" />).
await builder.Build().RunAsync();
