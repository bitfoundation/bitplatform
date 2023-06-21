//-:cnd:noEmit
using AdminPanel.Client.Core;
#if BlazorElectron
using ElectronNET.API;
using ElectronNET.API.Entities;
#endif
#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#elif BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Configuration;
#endif

namespace AdminPanel.Client.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
#if !BlazorWebAssembly && !BlazorServer
        throw new InvalidOperationException("Please switch to either blazor web assembly or server as described in readme.md");
#else
        await CreateHostBuilder(args)
            .RunAsync();
#endif
    }

#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault();

        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("AdminPanel.Client.Core.appsettings.json"));

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
#elif BlazorServer
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("AdminPanel.Client.Core.appsettings.json")!);
#if BlazorElectron
        builder.WebHost.UseElectron(args);
        builder.Services.AddElectron();
#endif

#if BlazorElectron
        builder.WebHost.UseUrls("http://localhost:8001");
#elif DEBUG
        if (OperatingSystem.IsWindows())
        {
            // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
            builder.WebHost.UseUrls("https://localhost:4031", "http://localhost:4030", "https://*:4031", "http://*:4030");
        }
#endif

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

#if BlazorElectron
        Task.Run(async () =>
        {
            var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                AutoHideMenuBar = true,
                BackgroundColor = "#0D2960",
                WebPreferences = new WebPreferences
                {
                    NodeIntegration = false
                }
            }, "http://localhost:8001");

            window.OnClosed += delegate
            {
                app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
                Electron.App.Quit();
            };
        });
#endif

        return app;
    }
#endif
}
