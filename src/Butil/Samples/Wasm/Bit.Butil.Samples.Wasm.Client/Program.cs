using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The browser's container: the same registration the server's Program.cs makes for the prerender pass.
builder.Services.AddCoreServices();

await builder.Build().RunAsync();
