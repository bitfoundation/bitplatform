using Bit.BlazorUI;
using Bit.BlazorUI.Tests.Trimming.Hosted.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<Main>("#app");
builder.Services.AddBitBlazorUIServices();

await builder.Build().RunAsync();
