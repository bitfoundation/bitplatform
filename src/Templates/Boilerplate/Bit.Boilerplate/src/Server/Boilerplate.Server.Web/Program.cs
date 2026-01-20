//+:cnd:noEmit
//#if (api == "Integrated")
//#endif
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Server.Web.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Server.Web;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        ConfigureGlobalization();

        var builder = WebApplication.CreateBuilder(options: new()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        //#if (sentry == true)
        builder.WebHost.UseSentry(configureOptions: options => builder.Configuration.GetRequiredSection("Logging:Sentry").Bind(options));
        //#endif

        builder.AddServerWebProjectServices();

        var app = builder.Build();

        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject, reportedBy: nameof(AppDomain.UnhandledException), app);
        TaskScheduler.UnobservedTaskException += (_, e) => { LogException(e.Exception, reportedBy: nameof(TaskScheduler.UnobservedTaskException), app); e.SetObserved(); };

        //#if (api == "Integrated")
        if (builder.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync(); // It's recommended to start using ef-core migrations.
        }
        //#endif

        app.ConfigureMiddlewares();

        //-:cnd:noEmit
#if Development
        _ = ScssCompilerService.WatchScssFiles(app);
#endif
        //+:cnd:noEmit

        await app.RunAsync();
    }

    private static void LogException(object? error, string reportedBy, WebApplication app)
    {
        if (error is Exception exp)
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IExceptionHandler>().Handle(exp, parameters: new()
            {
                { nameof(reportedBy), reportedBy }
            }, displayKind: AppEnvironment.IsDevelopment() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
        else
        {
            _ = Console.Error.WriteLineAsync(error?.ToString() ?? "Unknown error");
        }
    }

    /// <summary>
    /// You might consider setting `InvariantGlobalization` to `true` when publishing Server.Web and Blazor WebAssembly simultaneously,
    /// as this can reduce the website's size. However, doing so would also make the server project culture-invariant, which offers minimal benefit
    /// and could potentially cause issues.The following environment variable allows you to maintain server culture support
    /// while reducing the client's size through invariant culture.
    /// https://learn.microsoft.com/en-us/dotnet/core/runtime-config/globalization#invariant-mode
    /// </summary>
    private static void ConfigureGlobalization()
    {
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false");
    }
}
