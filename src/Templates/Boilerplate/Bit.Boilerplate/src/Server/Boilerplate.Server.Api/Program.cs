//+:cnd:noEmit
namespace Boilerplate.Server.Api;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        ConfigureGlobalization();

        var builder = WebApplication.CreateBuilder(args);

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddSharedConfigurations();

        //#if (sentry == true)
        builder.WebHost.UseSentry(configureOptions: options => builder.Configuration.DynamicBind("Logging:Sentry", options));
        //#endif

        builder.Services.AddSharedProjectServices(builder.Configuration);
        builder.AddServerApiProjectServices();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync(); // It's recommended to start using ef-core migrations.
        }

        app.ConfigureMiddlewares();

        await app.RunAsync();
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
