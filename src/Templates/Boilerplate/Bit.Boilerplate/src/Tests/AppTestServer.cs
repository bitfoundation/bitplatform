using Boilerplate.Server.Api;
using Boilerplate.Server.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Tests;

public partial class AppTestServer : IAsyncDisposable
{
    private WebApplication? webApp;

    public WebApplication WebApp => webApp ?? throw new InvalidOperationException($"{nameof(WebApp)} is null. Call {nameof(Build)} method first.");
    public Uri WebAppServerAddress => new(WebApp.Urls.First());

    public AppTestServer Build(Action<IServiceCollection>? configureTestServices = null,
        Action<ConfigurationManager>? configureTestConfigurations = null)
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

        configureTestConfigurations?.Invoke(builder.Configuration);

        builder.WebHost.UseUrls("http://127.0.0.1:0" /* 0 means random port */);

        configureTestServices?.Invoke(builder.Services);

        builder.AddTestProjectServices();

        configureTestServices?.Invoke(builder.Services);

        var app = webApp = builder.Build();

        app.ConfiureMiddlewares();

        return this;
    }

    public async Task Start()
    {
        await WebApp.StartAsync();
        WebApp.Configuration["ServerAddress"] = WebAppServerAddress.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        if (webApp != null)
        {
            await webApp.StopAsync();
            await webApp.DisposeAsync();
        }
    }
}
