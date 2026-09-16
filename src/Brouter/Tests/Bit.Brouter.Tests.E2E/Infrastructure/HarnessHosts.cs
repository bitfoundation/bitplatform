using System.Collections.Concurrent;

namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// One running web harness host per render mode, started on first use and shared by every test that
/// drives that mode. The hosts only hold per-scope state, and every test opens its own browser
/// context (its own circuit / WebAssembly instance), so sharing a process never shares state.
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

    private WebHarnessHost(string mode, string baseUrl, ChildProcess process)
    {
        Mode = mode;
        BaseUrl = baseUrl;
        _process = process;
    }

    public string Mode { get; }

    /// <summary>The URL the host answers at, without a trailing slash.</summary>
    public string BaseUrl { get; }

    public string RecentOutput => _process.RecentOutput;

    public static async Task<WebHarnessHost> StartAsync(string mode)
    {
        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";
        string[] hostArguments = ["--urls", baseUrl, $"--BrouterHarness:Mode={mode}"];

        ChildProcess process;
        if (E2EEnvironment.PublishedHost is { } published)
        {
            // A publish output runs as it would in production: static web assets come from its wwwroot.
            process = ChildProcess.Start("dotnet", [Path.Combine(published, $"{RepoLayout.WebHostName}.dll"), .. hostArguments], published,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });
        }
        else
        {
            // A build output only finds the static web assets of its references (bit-brouter.js among
            // them) through the development manifest, which Development switches on.
            var assembly = RepoLayout.WebHostAssembly(E2EEnvironment.Framework, E2EEnvironment.Configuration);
            if (File.Exists(assembly) is false)
                throw new FileNotFoundException($"The web harness host has not been built for {E2EEnvironment.Framework}/{E2EEnvironment.Configuration}.", assembly);

            process = ChildProcess.Start("dotnet", [assembly, .. hostArguments], Path.GetDirectoryName(RepoLayout.WebHostProject())!,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" });
        }

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }

        return new WebHarnessHost(mode, baseUrl, process);
    }

    public ValueTask DisposeAsync() => _process.DisposeAsync();
}
