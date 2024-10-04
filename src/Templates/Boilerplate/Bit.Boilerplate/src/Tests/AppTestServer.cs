using Boilerplate.Server.Api;
using Boilerplate.Server.Web;
using Boilerplate.Server.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Tests;

public class AppTestServer : IAsyncDisposable
{
    private WebApplication? webApp;

    public Uri ServerAddress => new((webApp ?? throw new InvalidOperationException($"web app is null")).Urls.First());
    public IServiceProvider Services => (webApp ?? throw new InvalidOperationException("Web app is null.")).Services;
    public IConfiguration Configuration => (webApp ?? throw new InvalidOperationException("Web app is null.")).Configuration;

    public AppTestServer Build(Action<IServiceCollection>? configureTestServices = null)
    {
        if (webApp != null)
            throw new InvalidOperationException("Server is already built.");

        var builder = WebApplication.CreateBuilder(options: new()
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(Server.Web.Program).Assembly.GetName().Name
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddClientConfigurations();

        builder.WebHost.UseUrls("http://127.0.0.1:0" /* 0 means random port */);

        builder.AddTestProjectServices();

        configureTestServices?.Invoke(builder.Services);

        webApp = builder.Build();

        webApp.ConfiureMiddlewares();

        return this;
    }

    public async Task StartAsync()
    {
        if (webApp == null)
            throw new InvalidOperationException($"Call {nameof(Build)} first.");

        if (AppEnvironment.IsDev())
        {
            await using var scope = webApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        await webApp.StartAsync();

        webApp.Configuration["ServerAddress"] = webApp.Urls.First();
    }

    public async ValueTask DisposeAsync()
    {
        if (webApp != null)
        {
            await webApp.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
