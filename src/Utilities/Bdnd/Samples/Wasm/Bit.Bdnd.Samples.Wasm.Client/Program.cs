using Bit.Bdnd;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBitBdndServices();

await builder.Build().RunAsync();
