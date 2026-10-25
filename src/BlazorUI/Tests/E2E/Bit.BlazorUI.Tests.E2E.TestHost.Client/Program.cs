using Bit.BlazorUI;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// The browser side of the E2E test host: the pages rendered in InteractiveWebAssembly mode get the same services the
// server registers for them in InteractiveServer mode, so every page runs unchanged in either mode.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBitBlazorUIServices();
builder.Services.AddBitBlazorUIExtrasServices();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
