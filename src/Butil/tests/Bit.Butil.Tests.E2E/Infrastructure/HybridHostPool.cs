using System.Collections.Concurrent;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// The hybrid app windows of one script-loading mode, rented to one test at a time.
/// </summary>
/// <remarks>
/// A BlazorWebView has exactly one page, so two tests can never share a window the way two tests share a web
/// host through contexts of their own. Rather than serialising the whole run, the pool starts up to
/// <see cref="E2EEnvironment.HybridInstances"/> windows and lends each to one test; the rest wait for one to be
/// returned. A window whose process died or whose page was closed is replaced instead of lent again. A start
/// that failed is remembered, so the tests after it fail at once with the reason instead of each waiting out
/// the startup timeout.
/// </remarks>
public sealed class HybridHostPool
{
    private static readonly ConcurrentDictionary<bool, HybridHostPool> _pools = new();

    private readonly bool _lazyScripts;
    private readonly SemaphoreSlim _slots = new(E2EEnvironment.HybridInstances);
    private readonly ConcurrentBag<HybridHarnessHost> _idle = [];
    private readonly ConcurrentBag<HybridHarnessHost> _started = [];
    private Exception? _startFailure;

    private HybridHostPool(bool lazyScripts) => _lazyScripts = lazyScripts;

    public static HybridHostPool For(bool lazyScripts) => _pools.GetOrAdd(lazyScripts, lazy => new HybridHostPool(lazy));

    public async Task<HybridHarnessHost> RentAsync()
    {
        await _slots.WaitAsync();

        try
        {
            while (_idle.TryTake(out var idle))
            {
                if (idle.IsUsable) return idle;
                await idle.DisposeAsync();
            }

            if (_startFailure is not null)
                throw new InvalidOperationException("An earlier hybrid harness host failed to start; see the inner exception.", _startFailure);

            try
            {
                var host = await HybridHarnessHost.StartAsync(_lazyScripts);
                _started.Add(host);
                return host;
            }
            catch (Exception exception)
            {
                _startFailure ??= exception;
                throw;
            }
        }
        catch
        {
            _slots.Release();
            throw;
        }
    }

    public void Return(HybridHarnessHost host)
    {
        _idle.Add(host);
        _slots.Release();
    }

    public static async Task StopAllAsync()
    {
        foreach (var pool in _pools.Values)
        {
            foreach (var host in pool._started)
            {
                try { await host.DisposeAsync(); }
                catch (Exception) { /* best-effort: the run is over */ }
            }
        }

        _pools.Clear();
    }
}
