using System.Net.Sockets;
using Bit.Bcrdt.Server;
using Boilerplate.Workbench;
using Microsoft.AspNetCore.Hosting;

namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>
/// Runs the Workbench in this process on a free port, built the way its Program.Main builds it.
/// </summary>
public sealed class WorkbenchTestServer : IAsyncDisposable
{
    private WebApplication? app;

    public Uri Address { get; } = new($"http://localhost:{FreePort()}/");

    public WebApplication App => app ?? throw new InvalidOperationException($"Call {nameof(Start)} first.");

    public IServiceProvider Services => App.Services;

    public WorkbenchReviewAgent Agent => Services.GetRequiredService<WorkbenchReviewAgent>();

    public BcrdtDocumentHost ReviewHost => Services.GetRequiredService<BcrdtDocumentHost>();

    public async Task<WorkbenchTestServer> Start(Action<IServiceCollection>? configureServices = null, CancellationToken cancellationToken = default)
    {
        var builder = CreateBuilder();

        builder.WebHost.UseUrls(Address.ToString());

        configureServices?.Invoke(builder.Services);

        app = builder.Build();

        app.ConfigureMiddlewares();

        await app.StartAsync(cancellationToken);

        return this;
    }

    /// <summary>The Workbench's builder with all of its services, not started; bUnit tests copy its services.</summary>
    public static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(Workbench.Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory
        });

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Workbench");

        builder.AddWorkbenchProjectServices();

        return builder;
    }

    /// <summary>A client of the server, which talks to it from this machine.</summary>
    public HttpClient CreateClient() => new() { BaseAddress = Address };

    /// <summary>A request that reaches the server the way one through a tunnel does: from loopback, with a forwarding header.</summary>
    public static HttpRequestMessage Remote(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add("X-Forwarded-For", "203.0.113.7");

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    public async ValueTask DisposeAsync()
    {
        if (app is null)
            return;

        try
        {
            await app.StopAsync();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    private static int FreePort()
    {
        while (true)
        {
            var port = Random.Shared.Next(20_000, 32_768);

            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return port;
            }
            catch (SocketException)
            {
            }
        }
    }
}
