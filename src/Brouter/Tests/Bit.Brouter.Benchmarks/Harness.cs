using System.Diagnostics;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Brouter.Benchmarks;

/// <summary>One scenario's measured cost at a given route count.</summary>
public readonly record struct Sample(double RenderMs, double AllocMB, double RetainedKB);

/// <summary>
/// Renders each scenario in an isolated bUnit host and measures three things per route count:
///  - render time (instantiation + initial match),
///  - bytes allocated during that render, and
///  - managed heap retained afterwards while the rendered tree is held alive (the "permanently
///    alive" cost the review is about).
///
/// Each measurement uses its own fresh TestContext so nothing carries over between route counts,
/// and every route count is measured over several trials (after warmup) with the median reported,
/// to damp GC/JIT noise. Absolute numbers include a fixed bUnit renderer/host overhead that is
/// identical for both scenarios - the signal is the gap between the two and how it grows with N.
/// </summary>
public static class Harness
{
    public static Sample MeasureBrouter(int routeCount, int warmup, int trials)
        => Measure(routeCount, warmup, trials, brouter: true);

    public static Sample MeasureRouteTable(int routeCount, int warmup, int trials)
        => Measure(routeCount, warmup, trials, brouter: false);

    private static Sample Measure(int routeCount, int warmup, int trials, bool brouter)
    {
        // Navigate to a route roughly in the middle so a match actually happens (and, for the
        // RouteTable baseline, one page is instantiated) - matching the Brouter scenario's work.
        var path = $"/page/{routeCount / 2}";

        for (int i = 0; i < warmup; i++) RenderOnce(routeCount, brouter, path, out _, out _, out _);

        var times = new double[trials];
        var allocs = new double[trials];
        var retained = new double[trials];
        for (int i = 0; i < trials; i++)
        {
            RenderOnce(routeCount, brouter, path, out times[i], out allocs[i], out retained[i]);
        }

        return new Sample(Median(times), Median(allocs), Median(retained));
    }

    private static void RenderOnce(int routeCount, bool brouter, string path,
        out double renderMs, out double allocMB, out double retainedKB)
    {
        // bUnit v2's container may hold IAsyncDisposable-only services, so tear the context down via
        // the async path (blocking) rather than a synchronous `using`, which would throw on dispose.
        var ctx = new Bunit.BunitContext();
        try
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddBitBrouterServices();

            var nav = ctx.Services.GetRequiredService<BunitNavigationManager>();
            nav.NavigateTo("http://localhost" + path);

            Settle();
            var memBefore = GC.GetTotalMemory(forceFullCollection: true);
            var allocBefore = GC.GetTotalAllocatedBytes(precise: true);

            var sw = Stopwatch.StartNew();
            object cut = brouter
                ? ctx.Render<BrouterBenchHost>(p => p.Add(x => x.RouteCount, routeCount))
                : ctx.Render<RouteTableHost>(p => p.Add(x => x.RouteCount, routeCount));
            sw.Stop();

            var allocAfter = GC.GetTotalAllocatedBytes(precise: true);
            // Retained: force a collection with the rendered tree still rooted (via `cut`), so only the
            // memory that survives - the mounted components and renderer bookkeeping - is counted.
            var memAfter = GC.GetTotalMemory(forceFullCollection: true);
            GC.KeepAlive(cut);

            renderMs = sw.Elapsed.TotalMilliseconds;
            allocMB = (allocAfter - allocBefore) / (1024.0 * 1024.0);
            retainedKB = Math.Max(0, memAfter - memBefore) / 1024.0;
        }
        finally
        {
            ctx.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static void Settle()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static double Median(double[] values)
    {
        var copy = (double[])values.Clone();
        Array.Sort(copy);
        var mid = copy.Length / 2;
        return copy.Length % 2 == 1 ? copy[mid] : (copy[mid - 1] + copy[mid]) / 2.0;
    }
}
