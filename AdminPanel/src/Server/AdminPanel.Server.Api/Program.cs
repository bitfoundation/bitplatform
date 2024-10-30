namespace AdminPanel.Server.Api;

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
            builder.WebHost.UseUrls("http://localhost:5246", "http://*:5246");
        }

        builder.Services.AddSharedProjectServices(builder.Configuration);
        builder.AddServerApiProjectServices();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        app.ConfigureMiddlewares();

        await app.RunAsync();
    }
}
