//-:cnd:noEmit
#if BlazorWebAssemblyStandalone
using Microsoft.AspNetCore.Components.Web;
#endif
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

#if BlazorWebAssemblyStandalone
builder.RootComponents.Add<Routes>("#app-container");
builder.RootComponents.Add<HeadOutlet>("head::after");
#endif

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

try
{
    await host.RunAsync();
}
catch (JSException exp) when (exp.Message is "Error: Could not find any element matching selector '#app-container'.")
{
#if BlazorWebAssemblyStandalone
    await Console.Error.WriteLineAsync("Either run/publish Client.Web project or set BlazorWebAssemblyStandalone to false.");
#endif
}
