using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration.AddClientConfigurations();

Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

if (apiServerAddress!.IsAbsoluteUri is false)
{
    apiServerAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiServerAddress);
}

builder.Services.AddTransient(sp => new HttpClient(sp.GetRequiredKeyedService<HttpMessageHandler>("DefaultMessageHandler")) { BaseAddress = apiServerAddress });

builder.Services.AddClientWebServices();

var host = builder.Build();

if (AppRenderMode.MultilingualEnabled)
{
    var culture = await host.Services.GetRequiredService<IStorageService>().GetItem("Culture");
    host.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(culture);
}

await host.RunAsync();
