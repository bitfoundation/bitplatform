using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

#if (UseWebAssembly)
builder.Services.AddBitBlazorUIServices();
#endif

await builder.Build().RunAsync();
