using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;
using Boilerplate.Server.Api;
using Boilerplate.Server.Web;
using Boilerplate.Server.Api.Data;

namespace Boilerplate.Tests;

public partial class AppTestServer : IAsyncDisposable
{
    private TestServer? testServer;
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

        builder.WebHost.UseTestServer();

        configureTestServices?.Invoke(builder.Services);
        AddTestProjectServices(builder.Services);
        builder.AddServerWebProjectServices();

        var app = webApp = builder.Build();

        app.ConfiureMiddlewares();

        return this;
    }

    public IServiceProvider Services => (webApp ?? throw new InvalidOperationException("Web app is null.")).Services;

    private void AddTestProjectServices(IServiceCollection services)
    {
        services.TryAddTransient(_ => testServer!.CreateWebSocketClient());

        services.TryAddTransient(sp =>
        {
            var underlyingHttpMessageHandler = testServer!.CreateHandler();

            var constructedHttpMessageHander = testServer.Services.GetRequiredService<Func<HttpMessageHandler, HttpMessageHandler>>()
                .Invoke(underlyingHttpMessageHandler);

            return new HttpClient(constructedHttpMessageHander)
            {
                BaseAddress = testServer.BaseAddress
            };
        });
    }

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

        testServer = webApp.GetTestServer();
    }

    public async ValueTask DisposeAsync()
    {
        if (webApp != null)
        {
            await webApp.DisposeAsync();
        }
        testServer?.Dispose();
    }
}
