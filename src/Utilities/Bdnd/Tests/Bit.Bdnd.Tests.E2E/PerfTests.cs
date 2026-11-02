using System.Globalization;
using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Measurements, not assertions: a scripted drag across a stress page, with the page recording its animation frames
/// (the longest gap between two, and how many), its long tasks and its DOM mutations while the drag runs. Run them on
/// their own (<c>--filter "TestCategory=Perf"</c>), against a demo of each version to compare
/// (<c>BDND_E2E_BASE_URL</c>); the numbers go to the test output. They only fail when a page breaks.
/// </summary>
[TestClass]
public sealed class PerfTests : BdndTest
{
    private const string Recorder =
        """
        () => {
          const r = { frames: 0, maxGap: 0, last: performance.now(), longTasks: 0, longTaskMs: 0, mutations: 0, running: true };
          const tick = (t) => { r.frames++; r.maxGap = Math.max(r.maxGap, t - r.last); r.last = t; if (r.running) requestAnimationFrame(tick); };
          requestAnimationFrame(tick);
          try {
            new PerformanceObserver((list) => { for (const e of list.getEntries()) { r.longTasks++; r.longTaskMs += e.duration; } })
              .observe({ type: 'longtask', buffered: false });
          } catch { }
          new MutationObserver((records) => { r.mutations += records.length; })
            .observe(document.body, { subtree: true, childList: true, attributes: true, characterData: true });
          r.start = performance.now();
          window.__bdndPerf = r;
        }
        """;

    private const string Collect =
        """
        () => { const r = window.__bdndPerf; r.running = false; return {
          ms: performance.now() - r.start, frames: r.frames, maxGap: r.maxGap,
          longTasks: r.longTasks, longTaskMs: r.longTaskMs, mutations: r.mutations }; }
        """;

    public sealed class Sample
    {
        public double Ms { get; set; }
        public int Frames { get; set; }
        public double MaxGap { get; set; }
        public int LongTasks { get; set; }
        public double LongTaskMs { get; set; }
        public int Mutations { get; set; }

        public override string ToString() => string.Create(CultureInfo.InvariantCulture,
            $"{Ms:0} ms, {Frames} frames ({Frames * 1000 / Math.Max(1, Ms):0} fps), longest frame gap {MaxGap:0.0} ms, {LongTasks} long tasks ({LongTaskMs:0} ms), {Mutations} DOM mutation records");
    }

    private async Task<Sample> MeasureAsync(IPage page, Func<Task> drag)
    {
        await page.EvaluateAsync(Recorder);
        await drag();
        await WaitForQuietAsync(page, 300);
        return await page.EvaluateAsync<Sample>(Collect);
    }

    // a slow drag along a path: 120 moves, a few pixels each, as a hand would
    private static async Task SweepAsync(IPage page, Point from, Point to, int moves = 120)
    {
        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)from.X + 6, (float)from.Y + 6, new MouseMoveOptions { Steps = 3 });
        for (var i = 1; i <= moves; i++)
        {
            var x = from.X + (to.X - from.X) * i / moves;
            var y = from.Y + (to.Y - from.Y) * i / moves;
            await page.Mouse.MoveAsync((float)x, (float)y);
        }
        await page.Mouse.UpAsync();
    }

    [TestMethod, TestCategory("Perf")]
    public async Task A_drag_down_the_hundred_card_sortable_list()
    {
        var page = await OpenAsync("/sortable/stress-test");
        var cards = page.Locator(".sortable-card");
        var first = await BoxAsync(cards.Nth(0));
        var tray = await BoxAsync(page.Locator(".sortable-scroll"));

        var sample = await MeasureAsync(page, () => SweepAsync(page, first.Centre, new Point(first.Centre.X, tray.Bottom - 20)));

        TestContext.WriteLine($"sortable stress (native mouse drag over 100 hover-reordering cards): {sample}");
    }

    [TestMethod, TestCategory("Perf")]
    public async Task A_drag_across_the_dustbin_stress_test()
    {
        var page = await OpenAsync("/dustbin/stress-test");
        var box = page.Locator(".box").First;
        var from = (await BoxAsync(box)).Centre;
        var bins = page.Locator(".dustbin");
        var last = await BoxAsync(bins.Last);

        var sample = await MeasureAsync(page, () => SweepAsync(page, from, last.Centre));

        TestContext.WriteLine($"dustbin stress (native mouse drag across 6 targets, 30 sources re-rendering): {sample}");
    }

    [TestMethod, TestCategory("Perf")]
    public async Task A_drag_with_a_custom_drag_layer()
    {
        var page = await OpenAsync("/drag-around/custom-layer");
        var box = await BoxAsync(page.Locator(".drag-around-box").First);
        var area = await BoxAsync(page.Locator(".drag-around-container"));

        var sample = await MeasureAsync(page, () => SweepAsync(page, box.Centre, new Point(area.Right - 40, area.Bottom - 40)));

        TestContext.WriteLine($"custom drag layer (a preview rendered by .NET on every pointer move): {sample}");
    }
}
