using System.Collections.Concurrent;

namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>
/// One running harness host per render mode, started on first use and shared by every test that drives
/// that mode. The host keeps all per-test state under the test's own host name (see
/// <see cref="HarnessSession"/>), and the browser keeps service workers and caches per origin, so sharing a
/// process never shares state between tests.
/// </summary>
public static class HarnessHosts
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<WebHarnessHost>>> _hosts = new();

    public static Task<WebHarnessHost> GetAsync(string mode) =>
        _hosts.GetOrAdd(mode, m => new Lazy<Task<WebHarnessHost>>(() => WebHarnessHost.StartAsync(m))).Value;

    public static async Task StopAllAsync()
    {
        foreach (var host in _hosts.Values.Where(h => h.IsValueCreated))
        {
            try
            {
                await (await host.Value).DisposeAsync();
            }
            catch (Exception)
            {
                // A host that failed to start already failed the tests that needed it.
            }
        }

        _hosts.Clear();
    }
}

public sealed class WebHarnessHost : IAsyncDisposable
{
    private readonly ChildProcess _process;

    private WebHarnessHost(string mode, int port, ChildProcess process)
    {
        Mode = mode;
        Port = port;
        _process = process;
    }

    public string Mode { get; }

    public int Port { get; }

    public string RecentOutput => _process.RecentOutput;

    public static async Task<WebHarnessHost> StartAsync(string mode)
    {
        var port = ChildProcess.FreePort();
        string[] hostArguments = ["--urls", $"http://127.0.0.1:{port}", $"--BswupHarness:Mode={mode}", "--Logging:LogLevel:Default=Warning"];

        ChildProcess process;
        if (E2EEnvironment.PublishedHost is { } published)
        {
            // A publish output runs as it would in production: static web assets come from its wwwroot.
            process = ChildProcess.Start("dotnet", [Path.Combine(published, $"{RepoLayout.WebHostName}.dll"), .. hostArguments], published,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });
        }
        else
        {
            // A build output only finds the static web assets of its references (the Bswup scripts, both
            // clients) through the development manifest, which Development switches on.
            var assembly = RepoLayout.WebHostAssembly(E2EEnvironment.Framework, E2EEnvironment.Configuration);
            if (File.Exists(assembly) is false)
                throw new FileNotFoundException($"The harness host has not been built for {E2EEnvironment.Framework}/{E2EEnvironment.Configuration}.", assembly);

            process = ChildProcess.Start("dotnet", [assembly, .. hostArguments], Path.GetDirectoryName(RepoLayout.WebHostProject())!,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" });
        }

        try
        {
            await process.WaitForHttpAsync($"http://127.0.0.1:{port}/_harness/options", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }

        return new WebHarnessHost(mode, port, process);
    }

    public ValueTask DisposeAsync() => _process.DisposeAsync();
}
