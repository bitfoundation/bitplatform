//-:cnd:noEmit
#if Development
using Boilerplate.Server.Web.Infrastructure.Services;
#endif
//+:cnd:noEmit

namespace Boilerplate.LandingPage.Server;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        //-:cnd:noEmit
#if Development
        Console.OutputEncoding = System.Text.Encoding.UTF8;
#endif
        //+:cnd:noEmit

        // [mirror] the server keeps its cultures under -p:InvariantGlobalization=true - keep in sync with:
        // - src/Server/Boilerplate.Server.Web/Program.cs (ConfigureGlobalization)
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false");

        var builder = WebApplication.CreateBuilder(options: new()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        // The client's settings go in first, so the server's own files and environment override them
        builder.Configuration.Sources.Insert(0, new ChainedConfigurationSource
        {
            Configuration = new ConfigurationBuilder().AddLandingPageClientConfigurations(builder.Environment.EnvironmentName).Build()
        });

        builder.AddLandingPageServices();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Boilerplate.LandingPage.Server");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => logger.LogError(e.ExceptionObject as Exception, "An unhandled exception.");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.LogError(e.Exception, "An unobserved task exception.");
            e.SetObserved();
        };

        app.ConfigureLandingPageMiddlewares();

        //-:cnd:noEmit
#if Development
        await FileWatcherService.StartAsync(app);
#endif
        //+:cnd:noEmit

        await app.RunAsync();
    }
}
