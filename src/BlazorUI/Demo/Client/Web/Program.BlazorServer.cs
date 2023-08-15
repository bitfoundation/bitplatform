using Bit.BlazorUI.Demo.Client.Core.Shared;
#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#endif

namespace Bit.BlazorUI.Demo.Client.Web;

public partial class Program
{
#if BlazorServer && !BlazorElectron
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("Bit.BlazorUI.Demo.Client.Core.appsettings.json")!);

#if DEBUG
    if (OperatingSystem.IsWindows())
    {
        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        builder.WebHost.UseUrls("https://localhost:4001", "http://localhost:4000", "https://*:4001", "http://*:4000");
    }
#endif

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

        return app;
    }
#endif
}
