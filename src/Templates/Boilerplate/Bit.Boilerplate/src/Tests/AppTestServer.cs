using Boilerplate.Server.Api;
using Boilerplate.Server.Web;
using Boilerplate.Server.Api.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Boilerplate.Tests;

public partial class AppTestServer : IAsyncDisposable
{
    private WebApplication? webApp;

    public AppTestServer Build(Action<IServiceCollection>? configureTestServices = null)
    {
        if (webApp != null)
            throw new InvalidOperationException("Server is already built.");

        var builder = WebApplication.CreateBuilder(options: new()
        {
            EnvironmentName = Environments.Development
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddSharedConfigurations();

        builder.WebHost.UseUrls("http://127.0.0.1:0" /* 0 means random port */);

        configureTestServices?.Invoke(builder.Services);

        builder.AddTestProjectServices();

        var app = webApp = builder.Build();

        app.ConfiureMiddlewares();

        return this;
    }

    public IServiceProvider Services => (webApp ?? throw new InvalidOperationException("Web app is null.")).Services;

    public async Task Start()
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

        webApp.Configuration["ServerAddress"] = GetServerAddress().ToString();
    }

    public async ValueTask DisposeAsync()
    {
        if (webApp != null)
        {
            await webApp.DisposeAsync();
        }
    }

    internal Uri GetServerAddress()
    {
        if (webApp == null)
            throw new InvalidOperationException($"web app is null");

        return new Uri(webApp.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First());
    }
}
