//-:cnd:noEmit
using TodoTemplate.Client.Core.Shared;
#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#endif

namespace TodoTemplate.Client.Web;

public partial class Program
{
#if BlazorServer && !BlazorElectron
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("TodoTemplate.Client.Core.appsettings.json")!);

#if DEBUG
        if (OperatingSystem.IsWindows())
        {
            // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
            builder.WebHost.UseUrls("https://localhost:4041", "http://localhost:4040", "https://*:4041", "http://*:4040");
        }
        else
        {
            builder.WebHost.UseUrls("https://localhost:4041", "http://localhost:4040");
        }
#endif

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

        return app;
    }
#endif
}
