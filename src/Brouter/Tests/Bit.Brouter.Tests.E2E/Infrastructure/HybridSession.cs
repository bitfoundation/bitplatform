namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// The hybrid app every <see cref="HybridModeTests"/> test drives: one window, started on first use.
/// A failed start is remembered, so the rest of the class fails fast instead of waiting out the
/// startup timeout once per test.
/// </summary>
public static class HybridSession
{
    private static Lazy<Task<HybridHarnessHost>> _host = new(() => HybridHarnessHost.StartAsync());

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

        _host = new(() => HybridHarnessHost.StartAsync());
    }
}
