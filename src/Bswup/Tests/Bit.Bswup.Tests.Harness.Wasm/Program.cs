using Bit.Bswup.Tests.Harness;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<HarnessApp>("#app");

await builder.Build().RunAsync();
