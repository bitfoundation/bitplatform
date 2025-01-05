//+:cnd:noEmit
//#if (api == "Integrated")
using Boilerplate.Server.Api.Data;
//#endif
using Boilerplate.Client.Core.Services.Contracts;

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

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        //#if (sentry == true)
        builder.WebHost.UseSentry(configureOptions: options => builder.Configuration.GetRequiredSection("Logging:Sentry").Bind(options));
        //#endif

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (builder.Environment.IsDevelopment() && AppPlatform.IsWindows)
        {
            builder.WebHost.UseUrls("http://localhost:5030", "http://*:5030");
        }

        builder.AddServerWebProjectServices();

        var app = builder.Build();

        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject, reportedBy: nameof(AppDomain.UnhandledException), app);
        TaskScheduler.UnobservedTaskException += (_, e) => { LogException(e.Exception, reportedBy: nameof(TaskScheduler.UnobservedTaskException), app); e.SetObserved(); };

        //#if (api == "Integrated")
        if (builder.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }
        //#endif

        app.ConfigureMiddlewares();

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
            }, displayKind: AppEnvironment.IsDev() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
        else
        {
            _ = Console.Error.WriteLineAsync(error?.ToString() ?? "Unknown error");
        }
    }
}
