//+:cnd:noEmit

namespace Boilerplate.Server.Api;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        AppEnvironment.Name = builder.Environment.EnvironmentName;

        builder.Configuration.AddSharedConfigurations();

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (AppEnvironment.IsDevelopment() && OperatingSystem.IsWindows())
        {
            builder.WebHost.UseUrls("http://localhost:5031", "http://*:5031");
        }

        builder.ConfigureApiServices();
        builder.Services.AddSharedProjectServices();

        var app = builder.Build();

        if (AppEnvironment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.ConfiureMiddlewares();

        await app.RunAsync();
    }
}
