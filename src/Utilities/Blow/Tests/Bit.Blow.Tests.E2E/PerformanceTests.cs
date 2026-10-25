using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// Interaction latency in a real browser, end to end: from the input event being dispatched to the change being on
/// the page - the C# render, the batch crossing to the browser, and the DOM update. Run on the stress page with 900
/// nodes and culling off, so every node is on the page. The numbers are written to the test output; the assertions
/// only catch a catastrophic regression, since absolute times depend on the machine and on the build (a Debug
/// WebAssembly build runs several times slower than the Release one the CI publishes).
/// </summary>
[TestClass]
[TestCategory("Performance")]
public sealed class PerformanceTests : BlowTest
{
    private async Task<IPage> OpenStressAsync()
    {
        var page = await OpenAsync("/examples/stress");
        await page.SelectOptionAsync(".example-toolbar select", "30");
        await page.Locator(".example-toolbar input[type=checkbox]").UncheckAsync();
        await EventuallyAsync(async () => await page.Locator(".bit-blow-node").CountAsync() == 900, "900 nodes were rendered");
        await WaitForQuietAsync(page, 500);
        return page;
    }

    /// <summary>Median and 90th percentile of one interaction, repeated, in milliseconds.</summary>
    private static async Task<(double Median, double P90)> MeasureAsync(IPage page, string kind, int count)
        => await page.EvaluateAsync<double[]>(
            """
            async ([kind, count]) => {
              const pane = document.querySelector('.bit-blow');
              const viewport = pane.querySelector('.bit-blow-viewport');
              const node = pane.querySelector('.bit-blow-node');
              const r = pane.getBoundingClientRect();
              const cx = r.left + r.width / 2, cy = r.top + r.height / 2;
              const watched = kind === 'drag' ? node : viewport;
              const changed = before => new Promise(resolve => {
                const check = () => watched.style.transform !== before ? resolve() : requestAnimationFrame(check);
                check();
              });

              if (kind === 'drag') {
                const nr = node.getBoundingClientRect();
                node.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, pointerId: 7, clientX: nr.left + 5, clientY: nr.top + 5 }));
                await new Promise(resolve => setTimeout(resolve, 100));
              }

              const times = [];
              for (let i = 0; i < count; i++) {
                const before = watched.style.transform;
                const t0 = performance.now();
                if (kind === 'zoom') {
                  pane.dispatchEvent(new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: i % 2 ? 40 : -40, clientX: cx, clientY: cy }));
                } else {
                  pane.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, pointerId: 7, clientX: cx + (i % 2 ? 40 : 0), clientY: cy + i }));
                }
                await changed(before);
                times.push(performance.now() - t0);
              }

              if (kind === 'drag') pane.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, pointerId: 7, clientX: cx, clientY: cy }));
              times.sort((a, b) => a - b);
              return [times[Math.floor(count / 2)], times[Math.floor(count * 0.9)]];
            }
            """, new object[] { kind, count }) is [var median, var p90] ? (median, p90) : default;

    [TestMethod]
    public async Task Zooming_a_900_node_graph()
    {
        var page = await OpenStressAsync();

        var (median, p90) = await MeasureAsync(page, "zoom", 30);

        TestContext.WriteLine($"zoom, 900 nodes, no culling: median {median:0.0} ms, p90 {p90:0.0} ms");
        Assert.IsLessThan(2000, p90, "a single zoom step took seconds");
    }

    [TestMethod]
    public async Task Dragging_a_node_of_a_900_node_graph()
    {
        var page = await OpenStressAsync();

        var (median, p90) = await MeasureAsync(page, "drag", 30);

        TestContext.WriteLine($"drag, 900 nodes, no culling: median {median:0.0} ms, p90 {p90:0.0} ms");
        Assert.IsLessThan(2000, p90, "a single drag frame took seconds");
    }
}
