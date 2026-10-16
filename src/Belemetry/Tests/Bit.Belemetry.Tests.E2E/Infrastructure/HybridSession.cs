namespace Bit.Belemetry.Tests.E2E.Infrastructure;

/// <summary>
/// The hybrid app every <see cref="HybridModeTests"/> test drives: one window, started on first use, exporting to the
/// receiver of the static SSR web host (any web host would do; that one boots fastest). A failed start is remembered,
/// so the rest of the class fails fast instead of waiting out the startup timeout once per test.
/// </summary>
public static class HybridSession
{
    /// <summary>The web harness host whose receiver collects the hybrid app's exports.</summary>
    public const string CollectorHostMode = "ssr";

    private static Lazy<Task<HybridHarnessHost>> _host = new(StartAsync);

    public static Task<HybridHarnessHost> GetAsync() => _host.Value;

    public static async Task StopAsync()
    {
        if (_host.IsValueCreated is false) return;

        try
        {
            await (await _host.Value).DisposeAsync();
        }
        catch (Exception)
        {
            // A host that failed to start already failed the tests that needed it.
        }

        _host = new(StartAsync);
    }

    private static async Task<HybridHarnessHost> StartAsync()
    {
        var collector = await HarnessHosts.GetAsync(CollectorHostMode);
        return await HybridHarnessHost.StartAsync(collector.BaseUrl);
    }
}
