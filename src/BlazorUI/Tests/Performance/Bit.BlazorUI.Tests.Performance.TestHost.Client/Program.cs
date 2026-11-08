using Bit.BlazorUI;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// The browser side of the performance test host: the performance pages, where they run in InteractiveWebAssembly mode,
// get the services the server registers for them in InteractiveServer mode, so every page runs unchanged in either mode.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBitBlazorUIServices();

await builder.Build().RunAsync();
