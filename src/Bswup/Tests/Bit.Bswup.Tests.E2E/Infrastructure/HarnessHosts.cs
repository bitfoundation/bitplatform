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

    public static async Task<WebHarnessHost> GetAsync(string mode)
    {
        var host = _hosts.GetOrAdd(mode, m => new Lazy<Task<WebHarnessHost>>(() => WebHarnessHost.StartAsync(m)));

        try
        {
            return await host.Value;
        }
        catch
        {
            // A failed start is not cached: the next test that needs this mode starts the host again.
            _hosts.TryRemove(new KeyValuePair<string, Lazy<Task<WebHarnessHost>>>(mode, host));
            throw;
        }
    }

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

    private const int StartAttempts = 3;

    public static async Task<WebHarnessHost> StartAsync(string mode)
    {
        for (var attempt = 1; ; attempt++)
        {
            // FreePort releases the port before the host binds it, so another process can take it in between:
            // when the host then fails to bind, start it again on a fresh port.
            var port = ChildProcess.FreePort();
            var process = StartProcess(mode, port);

            try
            {
                await process.WaitForHttpAsync($"http://127.0.0.1:{port}/_harness/options", TimeSpan.FromMinutes(2));
                return new WebHarnessHost(mode, port, process);
            }
            catch (InvalidOperationException) when (attempt < StartAttempts && process.HasExited
                                                    && process.RecentOutput.Contains("address already in use", StringComparison.OrdinalIgnoreCase))
            {
                await process.DisposeAsync();
            }
            catch
            {
                await process.DisposeAsync();
                throw;
            }
        }
    }

    private static ChildProcess StartProcess(string mode, int port)
    {
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

        return process;
    }

    public ValueTask DisposeAsync() => _process.DisposeAsync();
}
