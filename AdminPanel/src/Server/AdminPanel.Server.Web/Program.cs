using AdminPanel.Server.Api.Data;

namespace AdminPanel.Server.Web;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(options: new()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "AdminPanel.Client.Web");

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
        {
            builder.WebHost.UseUrls("http://localhost:5000", "http://*:5000");
        }

        builder.AddServerWebProjectServices();

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
