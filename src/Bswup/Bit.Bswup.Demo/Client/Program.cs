using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// No RootComponents are registered here any more: this app boots through blazor.web.js, and the
// host document (Server/Components/App.razor) declares the root components together with their
// render mode - which is what makes the prerendered pass possible. HeadOutlet is likewise placed
// by the host document rather than attached to "head::after" from here.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Pages/McpPage.razor calls this site's own MCP endpoint (/mcp) and its HTTP mirror from the
// browser, so the client needs a client pointed at the origin it was served from.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
