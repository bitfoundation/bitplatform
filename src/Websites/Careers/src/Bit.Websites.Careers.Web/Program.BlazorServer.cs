#if BlazorServer
#endif

namespace Bit.Websites.Careers.Web;

public partial class Program
{
#if BlazorServer
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonStream(typeof(MainLayout).Assembly.GetManifestResourceStream("Bit.Websites.Careers.Web.appsettings.json")!);

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

        return app;
    }
#endif
}
