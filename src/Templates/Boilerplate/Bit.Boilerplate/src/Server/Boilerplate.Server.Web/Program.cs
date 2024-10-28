//+:cnd:noEmit
//#if (api == "Integrated")
using Boilerplate.Server.Api.Data;
//#endif

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

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
        {
            builder.WebHost.UseUrls("http://localhost:5030", "http://*:5030");
        }

        builder.AddServerWebProjectServices();

        var app = builder.Build();

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
}
