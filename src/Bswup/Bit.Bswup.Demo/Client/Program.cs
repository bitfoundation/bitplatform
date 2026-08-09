using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// No RootComponents are registered here any more: this app boots through blazor.web.js, and the
// host document (Server/Components/App.razor) declares the root components together with their
// render mode - which is what makes the prerendered pass possible. HeadOutlet is likewise placed
// by the host document rather than attached to "head::after" from here.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

await builder.Build().RunAsync();
