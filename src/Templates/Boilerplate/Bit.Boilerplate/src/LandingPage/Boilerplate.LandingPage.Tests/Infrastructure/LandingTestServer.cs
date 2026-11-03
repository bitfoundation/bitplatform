using System.Net.Sockets;
using Boilerplate.LandingPage.Server;

namespace Boilerplate.LandingPage.Tests.Infrastructure;

public sealed class LandingTestServer : IAsyncDisposable
{
    private WebApplication? app;

    public Uri Address { get; } = new($"http://localhost:{FreePort()}/");

    public IServiceProvider Services => (app ?? throw new InvalidOperationException($"Call {nameof(Start)} first.")).Services;

    public async Task<LandingTestServer> Start(Action<WebApplicationBuilder>? before = null, Action<IServiceCollection>? after = null, string environment = "Development")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.Sources.Insert(0, new ChainedConfigurationSource
        {
            Configuration = new ConfigurationBuilder().AddLandingPageClientConfigurations(builder.Environment.EnvironmentName).Build()
        });

        builder.WebHost.UseUrls(Address.ToString());

        before?.Invoke(builder);

        builder.AddLandingPageServices();

        after?.Invoke(builder.Services);

        app = builder.Build();

        app.ConfigureLandingPageMiddlewares();

        await app.StartAsync();

        return this;
    }

    public HttpClient CreateClient() => new(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = Address };

    public static HttpRequestMessage From(string visitorIp, HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };

        // Loopback is a trusted proxy, so the rate limits count each test's visitor on its own
        request.Headers.Add("X-Forwarded-For", visitorIp);

        return request;
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
