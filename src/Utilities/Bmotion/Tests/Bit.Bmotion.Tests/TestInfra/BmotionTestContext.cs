using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// A bUnit <see cref="BunitContext"/> wired exactly like an app: every Bmotion service comes from
/// the production <see cref="BitBmotion.AddBitBmotionServices(IServiceCollection)"/> registration
/// (same set, same lifetimes), and only the browser bridge is swapped for a
/// <see cref="FakeBmotionInterop"/> so component render/interaction logic can be exercised without
/// a browser. Access the fake through <see cref="Interop"/>.
/// </summary>
internal class BmotionTestContext : Bunit.BunitContext
{
    public FakeBmotionInterop Interop { get; } = new();

    /// <summary>Library options exposed to components; mutate before rendering to adjust policy.</summary>
    public BitBmotionOptions Options { get; } = new();

    public BmotionTestContext()
    {
        Services.AddLogging();
        Services.AddBitBmotionServices();
        // Keep the production lifetime (scoped) and only change what is resolved: the fake bridge.
        Services.Replace(ServiceDescriptor.Scoped<IBmotionInterop>(_ => Interop));
        // AddBitBmotionServices registers its own options instance; swap in the one tests mutate.
        Services.Replace(ServiceDescriptor.Singleton(Options));
    }

    public BmotionAnimationEngine Engine => Services.GetRequiredService<BmotionAnimationEngine>();

    /// <summary>
    /// Ticks the engine like the browser's rAF loop would, one frame per <paramref name="stepMs"/>
    /// from <paramref name="fromMs"/> to <paramref name="toMs"/> inclusive. Each frame runs on the
    /// renderer's dispatcher - the engine is single-threaded by design - and anything a frame posts
    /// back to it (component continuations) gets to run before the next one.
    /// </summary>
    public async Task RunFramesAsync(double fromMs, double toMs, double stepMs = 16)
    {
        for (double t = fromMs; t <= toMs; t += stepMs)
        {
            var timestamp = t;
            await Renderer.Dispatcher.InvokeAsync(() => Engine.ComputeFrame(timestamp));
        }
    }

    /// <summary>
    /// Lets the compositor animations of <paramref name="elementId"/> (all, when null) finish on the
    /// renderer's dispatcher, so the completion continuations they release run where a real
    /// browser callback would land.
    /// </summary>
    public Task<int> FinishWaapiAsync(string? elementId = null)
        => Renderer.Dispatcher.InvokeAsync(() => Interop.FinishWaapi(elementId));

    // bUnit v2's service container holds IAsyncDisposable-only services (e.g. BmotionAnimationEngine),
    // which the synchronous Dispose() path cannot tear down (it throws). Tests use `using` (sync), so
    // route sync disposal through the async path here, blocking on it, to keep call sites unchanged.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
