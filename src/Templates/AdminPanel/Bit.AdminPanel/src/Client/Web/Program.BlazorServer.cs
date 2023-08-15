//-:cnd:noEmit
using AdminPanel.Client.Core.Shared;
#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#endif

namespace AdminPanel.Client.Web;

public partial class Program
{
#if BlazorServer && !BlazorElectron
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("AdminPanel.Client.Core.appsettings.json")!);

#if DEBUG
        if (OperatingSystem.IsWindows())
        {
            // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
            builder.WebHost.UseUrls("https://localhost:4031", "http://localhost:4030", "https://*:4031", "http://*:4030");
        }
#endif

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

        return app;
    }
#endif
}
