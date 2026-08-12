using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoServices();

// No RootComponents are registered on purpose: the app boots through blazor.web.js, and the host
// document (Server/Components/App.razor) declares the root components together with their render
// mode - which is what makes the prerendered pass possible. HeadOutlet is likewise placed by the
// host document rather than attached to "head::after" from here.
await builder.Build().RunAsync();
