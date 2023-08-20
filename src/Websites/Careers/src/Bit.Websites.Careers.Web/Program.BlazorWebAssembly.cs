using Bit.Websites.Careers.Web;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#endif

namespace Bit.Websites.Careers.Web;

public partial class Program
{
#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault();
        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("Bit.Websites.Careers.Web.appsettings.json"));

        if (Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress) is false)
        {
            throw new InvalidOperationException("Api server address is invalid");
        }

        if (apiServerAddress.IsAbsoluteUri is false)
        {
            apiServerAddress = new Uri($"{builder.HostEnvironment.BaseAddress}{apiServerAddress}");
        }

        builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<AppHttpClientHandler>()) { BaseAddress = apiServerAddress });
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader>();
        builder.Services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();

        builder.Services.AddSharedServices();
        builder.Services.AddClientSharedServices();

        var host = builder.Build();

        return host;
    }
#endif
}
