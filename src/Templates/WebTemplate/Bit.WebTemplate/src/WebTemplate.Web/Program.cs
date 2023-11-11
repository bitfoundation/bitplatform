﻿using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration.AddClientAppConfigurations();

Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

if (apiServerAddress!.IsAbsoluteUri is false)
{
    apiServerAddress = new Uri($"{builder.HostEnvironment.BaseAddress}{apiServerAddress}");
}

builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<AppHttpClientHandler>()) { BaseAddress = apiServerAddress });
builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader>();
builder.Services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();

builder.Services.AddClientSharedServices();

var host = builder.Build();

#if MultilingualEnabled
var preferredCultureCookie = ((IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>()).Invoke<string?>("window.App.getCookie", ".AspNetCore.Culture");
CultureInfoManager.SetCurrentCulture(preferredCultureCookie);
#endif

await host.RunAsync();
