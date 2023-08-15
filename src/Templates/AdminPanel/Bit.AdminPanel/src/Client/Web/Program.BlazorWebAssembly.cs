//-:cnd:noEmit
using AdminPanel.Client.Core.Shared;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
#endif

namespace AdminPanel.Client.Web;

public partial class Program
{
#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault();

        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("AdminPanel.Client.Core.appsettings.json")!);

        var apiServerAddressConfig = builder.Configuration.GetApiServerAddress();

        var apiServerAddress = new Uri($"{builder.HostEnvironment.BaseAddress}{apiServerAddressConfig}");

        builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<AppHttpClientHandler>()) { BaseAddress = apiServerAddress });
        builder.Services.AddScoped<LazyAssemblyLoader>();
        builder.Services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();

        builder.Services.AddSharedServices();
        builder.Services.AddClientSharedServices();
        builder.Services.AddClientWebServices();

        var host = builder.Build();

#if MultilingualEnabled
        var preferredCultureCookie = ((IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>()).Invoke<string?>("window.App.getCookie", ".AspNetCore.Culture");
        CultureInfoManager.SetCurrentCulture(preferredCultureCookie);
#endif

        return host;
    }
#endif
}
