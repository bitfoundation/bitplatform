//-:cnd:noEmit
#if BlazorElectron
using ElectronNET.API;
using ElectronNET.API.Entities;
#endif

namespace Boilerplate.Client.Web;

public partial class Program
{
#if BlazorElectron
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddClientConfigurations();

        builder.WebHost.UseElectron(args);
        builder.Services.AddElectron();

        builder.WebHost.UseUrls("http://localhost:8001");

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

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

        return app;
    }
#endif
}
