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

        app.ConfigureLandingPageMiddlewares();

        //-:cnd:noEmit
#if Development
        await FileWatcherService.StartAsync(app);
#endif
        //+:cnd:noEmit

        await app.RunAsync();
    }
}
