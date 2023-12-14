using Bit.BlazorUI.Demo.Client.Core.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration.AddClientConfigurations();

Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

if (apiServerAddress!.IsAbsoluteUri is false)
{
    apiServerAddress = new Uri($"{builder.HostEnvironment.BaseAddress}{apiServerAddress}");
}

builder.Services.AddTransient(sp => new HttpClient(sp.GetRequiredService<RequestHeadersDelegationHandler>()) { BaseAddress = apiServerAddress });

builder.Services.AddClientWebServices();

var host = builder.Build();

await host.RunAsync();
