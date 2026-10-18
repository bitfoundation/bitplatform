using Bit.Butil;
using ButilTests.PublishFixture.Hosted.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<Main>("#app");
builder.Services.AddBitButilServices();

await builder.Build().RunAsync();
