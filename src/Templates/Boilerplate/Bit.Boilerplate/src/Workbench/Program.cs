//-:cnd:noEmit
#if Development
using Boilerplate.Server.Web.Infrastructure.Services;
#endif
//+:cnd:noEmit

namespace Boilerplate.Workbench;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false");

        var builder = WebApplication.CreateBuilder(options: new()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Workbench");

        builder.AddWorkbenchProjectServices();

        var app = builder.Build();

        app.ConfigureMiddlewares();

        //-:cnd:noEmit
#if Development
        await FileWatcherService.StartAsync(app);
#endif
        //+:cnd:noEmit

        await app.RunAsync();
    }
}
