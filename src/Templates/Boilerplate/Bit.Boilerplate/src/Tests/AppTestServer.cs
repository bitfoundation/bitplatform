using Boilerplate.Server.Api;
using Boilerplate.Server.Web;
using Boilerplate.Server.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Tests;

public partial class AppTestServer : IAsyncDisposable
{
    private WebApplication? _webApp;
    public WebApplication WebApp => _webApp ?? throw new InvalidOperationException($"WebApp is null. Call {nameof(Build)} method first.");
    public Uri ServerAddress => new(WebApp.Urls.First());
    public IServiceProvider Services => WebApp.Services;
    public IConfiguration Configuration => WebApp.Configuration;

    public AppTestServer Build(Action<IServiceCollection>? configureTestServices = null)
    {
        if (_webApp != null)
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

        var app = _webApp = builder.Build();

        app.ConfiureMiddlewares();

        return this;
    }

    public async Task StartAsync()
    {
        if (AppEnvironment.IsDev())
        {
            await using var scope = WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        await WebApp.StartAsync();

        WebApp.Configuration["ServerAddress"] = ServerAddress.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        if (_webApp != null)
        {
            await _webApp.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
