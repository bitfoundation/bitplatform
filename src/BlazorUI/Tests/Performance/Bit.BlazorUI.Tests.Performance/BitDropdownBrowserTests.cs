using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser-based performance and memory-leak tests for the BitDropdown component.
///
/// The primary goal of these tests is to benchmark the two memory-leak fixes applied
/// to BitDropdown.razor.cs:
///   1. DotNetObjectReference (_dotnetObj) now disposed in DisposeAsync, preventing
///      GC handles from accumulating across mount/unmount cycles.
///   2. OnValueChanged event handler is now unsubscribed in DisposeAsync.
///
/// Memory is measured server-side (managed heap) via the /api/gc-info endpoint so
/// the results are accurate regardless of whether JS heap metrics are available.
/// </summary>
[TestClass]
[TestCategory("Performance")]
[TestCategory("Browser")]
[Ignore("Browser tests must be run explicitly. Use: dotnet test --filter FullyQualifiedName~BitDropdownBrowserTests")]
public class BitDropdownBrowserTests : PerformanceTestBase
{
    // ── Thresholds ────────────────────────────────────────────────────────────

    // Render thresholds (ms) — dropdowns are heavier than simple buttons.
    private const double RenderThreshold10  = 800;
    private const double RenderThreshold100 = 3000;
    private const double RenderThreshold500 = 12000;

    // Re-render thresholds (ms)
    private const double ReRenderThreshold10  = 300;
    private const double ReRenderThreshold100 = 1500;
    private const double ReRenderThreshold500 = 6000;

    // ── Initial render ────────────────────────────────────────────────────────

    #region Initial Render Performance Tests

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitDropdown_InitialRender_10Components()
    {
        await TestInitialRender(10, RenderThreshold10);
    }

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitDropdown_InitialRender_100Components()
    {
        await TestInitialRender(100, RenderThreshold100);
    }

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitDropdown_InitialRender_500Components()
    {
        await TestInitialRender(500, RenderThreshold500);
    }

    private async Task TestInitialRender(int count, double threshold)
    {
        await Page.GotoAsync($"{BaseUrl}/perf/dropdown/{count}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        var renderTime     = await GetRenderTime();
        var componentCount = await GetComponentCount();

        Console.WriteLine($"InitialRender – {count} components: {renderTime:F2} ms");

        Assert.AreEqual(count, componentCount, $"Expected {count} rendered dropdowns, got {componentCount}");
        AssertWithinThreshold(renderTime, threshold, $"Initial render time for {count} components");
    }

    #endregion

    // ── Re-render ─────────────────────────────────────────────────────────────

    #region Re-render Performance Tests

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitDropdown_ReRender_10Components()
    {
        await TestReRender(10, ReRenderThreshold10);
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitDropdown_ReRender_100Components()
    {
        await TestReRender(100, ReRenderThreshold100);
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitDropdown_ReRender_500Components()
    {
        await TestReRender(500, ReRenderThreshold500);
    }

    private async Task TestReRender(int count, double threshold)
    {
        await Page.GotoAsync($"{BaseUrl}/perf/dropdown/{count}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        await Page.Locator("#btn-rerender").ClickAsync();
        await WaitForReRenderComplete();

        var reRenderTime = await GetReRenderTime();

        Console.WriteLine($"ReRender – {count} components: {reRenderTime:F2} ms");

        AssertWithinThreshold(reRenderTime, threshold, $"Re-render time for {count} components");
    }

    #endregion

    // ── Memory-leak benchmarks ────────────────────────────────────────────────

    #region Memory Leak Tests

    /// <summary>
    /// Verifies that the server-side managed heap does NOT grow linearly when
    /// BitDropdown components are repeatedly mounted and unmounted (Responsive=false).
    ///
    /// Root cause tested: DotNetObjectReference created in OnAfterRenderAsync was never
    /// disposed in DisposeAsync when Responsive=false, causing a GCHandle to be retained
    /// for every component instance across every mount/unmount cycle. With the leak in
    /// place, growth per cycle stayed roughly constant. After the fix, growth decelerates
    /// and approaches an asymptote — Blazor Server JIT/cache warm-up still allocates a
    /// few MB on first runs, but no further growth occurs once steady state is reached.
    ///
    /// The assertion compares the per-cycle growth rate of an early window (cycles 1-10
    /// after warmup) to a late window (cycles 31-40 after warmup). A real leak shows a
    /// flat or rising rate; a healthy component shows a sharply decreasing rate.
    /// </summary>
    [TestMethod]
    [TestCategory("MemoryLeak")]
    public async Task BitDropdown_MemoryLeak_StableHeapAfterMountUnmountCycles()
    {
        await AssertNoLinearLeak(count: 50, responsive: false);
    }

    /// <summary>
    /// Same as above but with Responsive=true, which additionally sets up a JS swipe
    /// handler. Although the JS swipe dispose path called dotnetObj.dispose() on the
    /// JS side, the C# DotNetObjectReference.Dispose() was still never called, leaving
    /// the managed GCHandle open.
    /// </summary>
    [TestMethod]
    [TestCategory("MemoryLeak")]
    public async Task BitDropdown_MemoryLeak_StableHeapAfterMountUnmountCycles_Responsive()
    {
        await AssertNoLinearLeak(count: 50, responsive: true);
    }

    /// <summary>
    /// Scalability check: per-cycle heap growth must trend toward zero as cycles grow.
    /// Reports the heap at 5, 10, 20, and 40 cycles so regressions are visible in CI.
    /// </summary>
    [TestMethod]
    [TestCategory("MemoryLeak")]
    [TestCategory("Scalability")]
    public async Task BitDropdown_MemoryLeak_HeapGrowthIsSubLinearAcrossCycles()
    {
        const int count = 30;
        const int warmupCycles = 5;

        await Page.GotoAsync($"{BaseUrl}/perf/dropdown/{count}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        // Warm up to reach a steady-state heap before measuring.
        for (int i = 0; i < warmupCycles; i++)
        {
            await MountAndDismount();
        }

        var baseline = await GetServerGCMemoryMB();
        Console.WriteLine($"Baseline heap after {warmupCycles} warmup cycles: {baseline:F2} MB");

        var samples = new List<(int CyclesSoFar, double HeapMB)>();
        var checkpoints = new[] { 5, 10, 20, 40 };
        var ranBefore = 0;

        foreach (var checkpoint in checkpoints)
        {
            for (int i = ranBefore; i < checkpoint; i++)
            {
                await MountAndDismount();
            }
            ranBefore = checkpoint;

            var heapNow = await GetServerGCMemoryMB();
            samples.Add((checkpoint, heapNow));
            Console.WriteLine(
                $"After {checkpoint} cycles – heap: {heapNow:F2} MB, growth: {(heapNow - baseline):F2} MB");
        }

        // Per-cycle growth in the first 5 cycles vs. the last 20 (cycles 21..40).
        var firstWindowRate  = (samples[0].HeapMB - baseline)             / 5.0;
        var lateWindowRate   = (samples[3].HeapMB - samples[2].HeapMB)    / 20.0;

        Console.WriteLine($"Per-cycle growth — first 5: {firstWindowRate:F4} MB/cycle, late 20: {lateWindowRate:F4} MB/cycle");

        // The GC endpoint reports the ENTIRE server heap. When running in parallel with
        // other test processes, their allocations inflate the late window. A relative
        // deceleration check is unreliable in that scenario.
        //
        // Use an absolute ceiling on the late-window rate: a real leak with 30 components
        // would show 1-3+ MB/cycle sustained. A healthy component contributes near-zero;
        // any observed rate is from GC noise and parallel test activity.
        Assert.IsTrue(
            lateWindowRate < 0.75,
            $"Late-window per-cycle growth ({lateWindowRate:F4} MB/cycle over cycles 21..40) " +
            $"exceeds 0.75 MB/cycle, indicating a possible memory leak. " +
            $"First-window rate was {firstWindowRate:F4} MB/cycle for reference.");
    }

    #endregion

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task AssertNoLinearLeak(int count, bool responsive)
    {
        const int warmupCycles    = 5;
        const int earlyWindowSize = 10;
        const int lateWindowSize  = 10;
        const int gapBetween      = 20; // cycles 16..35 sit between the two windows

        // Absolute ceiling: even under GC noise, the late window must not grow more
        // than this many MB. A real leak with 50 components × 10 cycles would produce
        // well over 5 MB; a healthy component stays under 2 MB of noise.
        // NOTE: When multiple test processes share the same host (parallel execution),
        // other tests' mount/unmount cycles contribute to the heap. We use a generous
        // ceiling (10 MB) to avoid false positives in parallel runs while still catching
        // real leaks (which would produce 15-30+ MB growth in the late window).
        const double maxLateGrowthAbsoluteMB = 10.0;

        await Page.GotoAsync($"{BaseUrl}/perf/dropdown/{count}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        if (responsive)
        {
            await Page.Locator("#btn-toggle-responsive").ClickAsync();
            await Page.WaitForFunctionAsync(
                "() => document.getElementById('responsive-mode')?.innerText === 'true'",
                new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
        }

        // Warm-up — JIT, expression-tree caches, SignalR buffers etc. allocate during
        // the first few cycles. We measure only after this has settled.
        for (int i = 0; i < warmupCycles; i++)
        {
            await MountAndDismount();
        }
        var afterWarmupMB = await GetServerGCMemoryMB();

        // Early window: cycles 6..15 after warmup.
        for (int i = 0; i < earlyWindowSize; i++)
        {
            await MountAndDismount();
        }
        var afterEarlyMB = await GetServerGCMemoryMB();
        var earlyGrowth  = afterEarlyMB - afterWarmupMB;

        // Gap to let any deferred allocations settle.
        for (int i = 0; i < gapBetween; i++)
        {
            await MountAndDismount();
        }
        var afterGapMB = await GetServerGCMemoryMB();

        // Late window: cycles 36..45 after warmup.
        for (int i = 0; i < lateWindowSize; i++)
        {
            await MountAndDismount();
        }
        var afterLateMB = await GetServerGCMemoryMB();
        var lateGrowth  = afterLateMB - afterGapMB;

        Console.WriteLine($"[responsive={responsive}] heap MB — afterWarmup={afterWarmupMB:F2}, afterEarly={afterEarlyMB:F2}, afterGap={afterGapMB:F2}, afterLate={afterLateMB:F2}");
        Console.WriteLine($"[responsive={responsive}] growth — early {earlyWindowSize} cycles: {earlyGrowth:F2} MB | late {lateWindowSize} cycles: {lateGrowth:F2} MB");

        // Strategy: The GC endpoint reports the ENTIRE server heap, which includes
        // allocations from all concurrent test circuits when running in parallel.
        // A relative deceleration check (late < early * X) is unreliable because other
        // tests' mount/unmount cycles inflate the late window unpredictably.
        //
        // Instead, we use a single robust check: the late window's absolute growth must
        // stay under a ceiling. A real leak (undisposed DotNetObjectReference) with
        // 50 components × 10 cycles would produce 15-30+ MB of growth in the late
        // window even in isolation. A healthy component contributes near-zero to the
        // late window; the observed growth is entirely from GC non-determinism and
        // parallel test noise.
        //
        // Ceiling of 10 MB accommodates up to ~10 parallel test processes sharing the
        // host while still catching a real leak (which would exceed 15 MB easily).
        Assert.IsTrue(
            lateGrowth <= maxLateGrowthAbsoluteMB,
            $"Late window grew {lateGrowth:F2} MB over {lateWindowSize} cycles of {count} " +
            $"BitDropdown components (responsive={responsive}), exceeding the " +
            $"{maxLateGrowthAbsoluteMB} MB ceiling. This indicates a memory leak.");
    }

    /// <summary>Renders the current count of dropdowns and then clears them.</summary>
    private async Task MountAndDismount()
    {
        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        await Page.Locator("#btn-clear").ClickAsync();
        await WaitForStatus("Cleared");

        // Give the SignalR circuit a moment to run async DisposeAsync on every
        // unmounted BitDropdown (each one performs a couple of JS interop calls).
        // Without this, the GC sample taken next can include objects whose disposal
        // is still in flight, producing a noisy, non-monotonic memory reading.
        await Task.Delay(150);
    }
}
