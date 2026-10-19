using System.Collections.Concurrent;

namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// The harness host processes of the run, one per <see cref="HarnessProfile"/>, each started on first use and
/// shared by every fixture that asks for that profile. All of them serve their pages in the run's render mode
/// (<see cref="E2EEnvironment.Host"/>); a page can still ask for another one with <c>?mode=</c>, which is how the
/// prerendering fixtures get static SSR from the same host.
/// </summary>
public static class HarnessTarget
{
    private static readonly ConcurrentDictionary<HarnessProfile, Lazy<Task<WebHarnessHost>>> _hosts = new();

    /// <summary>Starts the default host up front, so a host that cannot start fails the run in one place.</summary>
    public static Task StartAsync() => HostAsync(HarnessProfile.Default);

    public static Task<WebHarnessHost> HostAsync(HarnessProfile profile) =>
        _hosts.GetOrAdd(profile, p => new Lazy<Task<WebHarnessHost>>(() => WebHarnessHost.StartAsync(E2EEnvironment.Host, p))).Value;

    public static async Task StopAllAsync()
    {
        foreach (var host in _hosts.Values.Where(h => h.IsValueCreated))
        {
            try { await (await host.Value).DisposeAsync(); }
            catch (Exception) { /* a host that failed to start already failed the tests that needed it */ }
        }
        _hosts.Clear();
    }
}
