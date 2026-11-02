//-:cnd:noEmit
#if Development
using Boilerplate.Server.Web.Infrastructure.Services;
#endif
//+:cnd:noEmit

namespace Boilerplate.Workbench;

/// <summary>
/// Starts the Workbench: a Blazor Server app that runs the app's pages on sample data inside device frames.
/// </summary>
public static partial class Program
{
    public static async Task Main(string[] args)
    {
        // Keeps culture support on, as Server.Web does, so the views can switch between every supported culture.
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false");

        var builder = WebApplication.CreateBuilder(options: new()
        {
            Args = args,
            // The build output, not the working directory, which changes with how the app is launched.
            ContentRootPath = AppContext.BaseDirectory
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        // The appsettings embedded in Shared, Client.Core and this project, read the way the app's clients read them.
        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Workbench");

        builder.AddWorkbenchProjectServices();

        var app = builder.Build();

        app.ConfigureMiddlewares();

        //-:cnd:noEmit
#if Development
        // Rebuilds the outputs of scss, ts and resx files as they change.
        await FileWatcherService.StartAsync(app);
#endif
        //+:cnd:noEmit

        await app.RunAsync();
    }
}
