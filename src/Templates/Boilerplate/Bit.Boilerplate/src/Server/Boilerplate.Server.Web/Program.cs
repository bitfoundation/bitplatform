//+:cnd:noEmit
//#if (api == "Integrated")
using Boilerplate.Server.Api.Data;
//#endif

namespace Boilerplate.Server.Web;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(options: new()
        {
            Args = args,
            //#if (api == "Integrated")
            ContentRootPath = AppContext.BaseDirectory
            //#endif
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        //#if IsInsideProjectTemplate
        builder.Configuration.AddJsonFile("_appsettings.json");
        builder.Configuration.AddJsonFile($"_appsettings.{AppEnvironment.Current}.json");
        //#endif

        builder.Configuration.AddClientConfigurations();

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (AppEnvironment.IsDev() && OperatingSystem.IsWindows())
        {
            builder.WebHost.UseUrls("http://localhost:5030", "http://*:5030");
        }

        builder.ConfigureServices();

        var app = builder.Build();

        //#if (api == "Integrated")
        if (AppEnvironment.IsDev())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }
        //#endif

        app.ConfiureMiddlewares();

        await app.RunAsync();
    }
}
