using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Data;

namespace Boilerplate.Tests;

public class AppTestServer : IAsyncDisposable
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
        builder.ConfigureApiServices();
        builder.Services.AddSharedProjectServices();

        var app = webApp = builder.Build();

        app.ConfiureMiddlewares();

        return this;
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

    public HttpClient CreateClient()
    {
        return (testServer ?? throw new InvalidOperationException()).CreateClient();
    }

    public HttpMessageHandler CreateHandler()
    {
        return (testServer ?? throw new InvalidOperationException()).CreateHandler();
    }

    public RequestBuilder CreateRequest(string path)
    {
        return (testServer ?? throw new InvalidOperationException()).CreateRequest(path);
    }

    public WebSocketClient CreateWebSocketClient()
    {
        return (testServer ?? throw new InvalidOperationException()).CreateWebSocketClient();
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
