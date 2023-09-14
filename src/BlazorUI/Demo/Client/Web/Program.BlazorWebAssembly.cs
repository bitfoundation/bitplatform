using Bit.BlazorUI.Demo.Client.Core.Shared;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#endif

namespace Bit.BlazorUI.Demo.Client.Web;

public partial class Program
{
#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault();
        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("Bit.BlazorUI.Demo.Client.Core.appsettings.json")!);

        var apiServerAddressConfig = builder.Configuration.GetApiServerAddress();

        var apiServerAddress = new Uri($"{builder.HostEnvironment.BaseAddress}{apiServerAddressConfig}");

        builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<AppHttpClientHandler>()) { BaseAddress = apiServerAddress });
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader>();

        builder.Services.AddSharedServices();
        builder.Services.AddClientSharedServices();
        builder.Services.AddClientWebServices();

        var host = builder.Build();

        return host;
    }
#endif
}
