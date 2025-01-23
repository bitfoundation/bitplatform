using Bit.Besql.Demo.Client.Data;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAppServices();

var app = builder.Build();

await app.RunAsync();
