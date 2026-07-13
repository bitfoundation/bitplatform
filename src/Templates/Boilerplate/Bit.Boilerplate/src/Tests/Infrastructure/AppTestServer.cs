using Hangfire;
using System.Net;
using System.Net.Sockets;
using Boilerplate.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Boilerplate.Tests.Infrastructure.Services;

namespace Boilerplate.Tests.Infrastructure;

/// <summary>
/// Test server, capable of running backend API and Blazor Server UI for integration and UI tests using Playwright.
/// </summary>
public partial class AppTestServer(IBrowserContext? ClientBrowserContext = null) : IAsyncDisposable
{
    private WebApplication? webApp;

    public WebApplication WebApp => webApp ?? throw new InvalidOperationException($"{nameof(WebApp)} is null. Call {nameof(Build)} method first.");
    public readonly Uri WebAppServerAddress = new(GenerateServerUrl());

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

        builder.Configuration["ServerAddress"] = WebAppServerAddress.ToString();
        builder.WebHost.UseUrls(WebAppServerAddress.ToString());

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        configureTestConfigurations?.Invoke(builder.Configuration);

        builder.AddTestProjectServices();

        configureTestServices?.Invoke(builder.Services);

        var app = webApp = builder.Build();

        app.ConfigureMiddlewares();

        return this;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        await WebApp.StartAsync(cancellationToken);
        if (ClientBrowserContext is not null)
        {
            await ClientBrowserContext.AddInitScriptAsync($"window.startupParams = function() {{ return [ 'ServerAddress={WebAppServerAddress}' ]; }};");
        }
    }

    /// <summary>
    /// Waits until Hangfire reports no more background jobs waiting or running (enqueued + processing == 0).
    /// Actions like sending an e-mail are handled by Hangfire background jobs, so tests call this to deterministically
    /// wait for that work to finish instead of polling for its side effects (e.g. an e-mail being captured by CapturingBackgroundJobClient).
    /// </summary>
    public async Task WaitForBackgroundJobsToComplete(CancellationToken cancellationToken)
    {
        var monitoringApi = WebApp.Services.GetRequiredService<JobStorage>().GetMonitoringApi();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            var statistics = monitoringApi.GetStatistics();
            if (statistics.Enqueued is 0 && statistics.Processing is 0)
                return;

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("Hangfire background jobs did not complete within 30 seconds.");

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    /// <summary>
    /// Returns the newest captured e-mail addressed to <paramref name="email"/> that satisfies <paramref name="predicate"/>,
    /// or throws after a timeout. E-mails are captured synchronously as they are requested (See
    /// <see cref="TestIdentityEmailService"/>), so the message is normally already present; the short poll only guards
    /// against a caller reading a hair before the triggering request finished.
    /// </summary>
    public async Task<CapturedEmail> WaitForCapturedEmail(string email, Func<CapturedEmail, bool> predicate, CancellationToken cancellationToken)
    {
        var store = WebApp.Services.GetRequiredService<EmailCaptureStore>();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            // Newest-first so a freshly requested code wins over an earlier (now expired) one still in the capture.
            var match = store.Captured.Reverse().FirstOrDefault(capturedEmail => capturedEmail.IsTo(email) && predicate(capturedEmail));
            if (match is not null)
                return match;

            if (DateTimeOffset.UtcNow >= deadline)
            {
                var recipients = store.Captured.Select(capturedEmail => capturedEmail.ToEmailAddress).Distinct().ToArray();
                throw new InvalidOperationException(
                    $"No captured e-mail addressed to '{email}' matched within the timeout. " +
                    $"Captured {store.Captured.Count} e-mail(s) to: [{string.Join(", ", recipients)}].");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (webApp != null)
        {
            try
            {
                await webApp.StopAsync();
            }
            catch (OperationCanceledException) { }
            await webApp.DisposeAsync();
        }
    }

    private static string GenerateServerUrl()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}/";
    }
}
