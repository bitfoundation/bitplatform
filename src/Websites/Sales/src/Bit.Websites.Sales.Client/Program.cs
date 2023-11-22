using Bit.Websites.Sales.Client.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration.AddClientConfigurations();

Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

if (apiServerAddress!.IsAbsoluteUri is false)
{
    apiServerAddress = new Uri($"{builder.HostEnvironment.BaseAddress}{apiServerAddress}");
}

builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<RetryDelegatingHandler>()) { BaseAddress = apiServerAddress });

builder.Services.AddClientSharedServices();

var host = builder.Build();

await host.RunAsync();
