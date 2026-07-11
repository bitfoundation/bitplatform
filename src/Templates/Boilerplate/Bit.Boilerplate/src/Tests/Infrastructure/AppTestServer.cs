using Hangfire;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Boilerplate.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

namespace Boilerplate.Tests.Infrastructure;

/// <summary>
/// Test server, capable of running backend API and Blazor Server UI for integration and UI tests using Playwright.
/// </summary>
public partial class AppTestServer : IAsyncDisposable
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
    }

    /// <summary>
    /// Waits until Hangfire reports no more background jobs waiting or running (enqueued + processing == 0).
    /// Actions like sending an e-mail are handled by Hangfire background jobs, so tests call this to deterministically
    /// wait for that work to finish instead of polling for its side effects (e.g. an e-mail landing in the log).
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
    /// Waits for pending background jobs to complete, then reads the elevated access token the dev environment logged
    /// for <paramref name="email"/>. The token is embedded in the e-mail subject: "Boilerplate {token} - Elevated access
    /// token" (See EmailStrings.ElevatedAccessTokenEmailSubject). Callers should first wait for the elevated-access OTP
    /// prompt (e.g. <c>.bit-otp-inp</c>) to be sure the token e-mail has already been requested.
    /// </summary>
    public async Task<string> ReadElevatedAccessTokenFromDiagnosticLog(string email, CancellationToken cancellationToken)
    {
        await WaitForBackgroundJobsToComplete(cancellationToken);

        var tokenRegex = new Regex(@"Boilerplate\s+(?<code>\d{6})\s+-\s+Elevated access token", RegexOptions.IgnoreCase);

        (DateTimeOffset CreatedOn, string Code)? latest = null;

        foreach (var log in DiagnosticLogger.Store)
        {
            if (log.Message is null || log.Message.Contains(email, StringComparison.OrdinalIgnoreCase) is false)
                continue;

            var match = tokenRegex.Match(log.Message);
            if (match.Success is false)
                continue;

            // Take the most recent one in case an earlier (expired) token is still sitting in the store.
            if (latest is null || log.CreatedOn >= latest.Value.CreatedOn)
                latest = (log.CreatedOn, match.Groups["code"].Value);
        }

        if (latest is not null)
            return latest.Value.Code;

        throw new InvalidOperationException($"No elevated access token was found in the diagnostic log for '{email}'.");
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
