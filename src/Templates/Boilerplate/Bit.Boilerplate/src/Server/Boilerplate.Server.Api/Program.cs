//+:cnd:noEmit
namespace Boilerplate.Server.Api;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddSharedConfigurations();

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
        {
            builder.WebHost.UseUrls("http://localhost:5031", "http://*:5031");
        }

        builder.Services.AddSharedProjectServices();
        builder.AddServerApiProjectServices();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        app.ConfiureMiddlewares();

        await app.RunAsync();
    }
}
