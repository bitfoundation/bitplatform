using System.Globalization;
using System.Text.Json;
using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// Three hundred rows in a scrolling panel: a pointer drag held near the panel's edge scrolls it and carries the row
/// along, and the browser layer's own counters show what a drag costs - how many positions reached .NET for how many
/// pointer moves, and how long each took - with and without <c>IsolateItems</c>. The numbers are written to the test
/// output; the assertions only hold them to what the design promises.
/// </summary>
[TestClass]
public sealed class LargeListTests : BortableTest
{
    private static Task<JsonElement> DiagnosticsAsync(IPage page, bool reset = false) =>
        page.EvaluateAsync<JsonElement>(
            """
            async reset => {
              const module = await import('./_content/Bit.Bortable/bit-bortable.js');
              const now = module.diagnostics();
              if (reset) module.resetDiagnostics();
              return now;
            }
            """, reset);

    /// <summary>A row of the list by its name (a row also shows a note, so the item text is more than the name).</summary>
    private static ILocator Row(IPage page, string name) =>
        page.Locator("#large-list > [data-bortable-item]").Filter(new LocatorFilterOptions
        {
            Has = page.Locator(".large-name", new PageLocatorOptions { HasTextRegex = new System.Text.RegularExpressions.Regex("^" + System.Text.RegularExpressions.Regex.Escape(name) + "$") })
        });

    private static Task<double> PanelScrollTopAsync(IPage page) =>
        page.EvaluateAsync<double>("() => document.getElementById('large-panel').scrollTop");

    [TestMethod]
    public async Task A_pointer_drag_held_at_the_edge_of_the_panel_scrolls_it_and_carries_the_row_along()
    {
        var page = await OpenAsync("/large-list");
        await page.Locator("#large-fallback").CheckAsync();
        await WaitForQuietAsync(page);

        var panel = await BoxAsync(page.Locator("#large-panel"));
        await PressAsync(page, (await BoxAsync(Row(page, "Row 1"))).Centre);
        // Into the bottom edge band of the panel, and held there.
        await page.Mouse.MoveAsync((float)(panel.X + panel.Width / 2), (float)(panel.Bottom - 8), new MouseMoveOptions { Steps = 20 });
        await EventuallyAsync(async () => await PanelScrollTopAsync(page) > 400, "the panel scrolls while the row is held at its edge");
        await page.Mouse.MoveAsync((float)(panel.X + panel.Width / 2), (float)(panel.Bottom - 120), new MouseMoveOptions { Steps = 4 });
        await WaitForQuietAsync(page);
        await ReleaseAsync(page);

        // Row 1 went down with the scroll, far past the rows that were in view when it was picked up.
        var index = await page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('#large-list > [data-bortable-item]')].findIndex(e => e.querySelector('.large-name').textContent === 'Row 1')");
        Assert.IsGreaterThan(15, index, $"Row 1 is at {index}.");
        await Expect(page.Locator("#large-order")).Not.ToContainTextAsync("Row 1,");
    }

    [TestMethod]
    public async Task Only_new_positions_reach_dotnet_and_isolated_items_cost_less_per_step()
    {
        var page = await OpenAsync("/large-list");
        var results = new List<string>();
        // Every dragover the browser fires: each of them used to be a call into .NET.
        await page.EvaluateAsync("() => { window.__dragovers = 0; document.addEventListener('dragover', () => window.__dragovers++, true); }");

        foreach (var isolate in new[] { false, true })
        {
            await page.Locator("#large-isolate").SetCheckedAsync(isolate);
            await WaitForQuietAsync(page);
            await page.EvaluateAsync("() => { document.getElementById('large-panel').scrollTop = 0; window.__dragovers = 0; }");
            await DiagnosticsAsync(page, reset: true);

            var from = await BoxAsync(Row(page, isolate ? "Row 2" : "Row 1"));
            await PressAsync(page, from.Centre);
            // Down ten rows in forty pointer moves, then back up five.
            await page.Mouse.MoveAsync((float)from.Centre.X, (float)(from.Centre.Y + from.Height * 10), new MouseMoveOptions { Steps = 40 });
            await page.Mouse.MoveAsync((float)from.Centre.X, (float)(from.Centre.Y + from.Height * 5), new MouseMoveOptions { Steps = 20 });
            await WaitForQuietAsync(page);
            await ReleaseAsync(page);

            var stats = await DiagnosticsAsync(page);
            int positions = stats.GetProperty("positions").GetInt32();
            double average = stats.GetProperty("averageMs").GetDouble();
            double slowest = stats.GetProperty("slowestMs").GetDouble();
            int dragovers = await page.EvaluateAsync<int>("() => window.__dragovers");
            results.Add(string.Create(CultureInfo.InvariantCulture,
                $"IsolateItems={isolate}: 60 pointer moves, {dragovers} dragover events, {positions} positions sent to .NET ({stats.GetProperty("changes").GetInt32()} changed a list), average {average:0.0} ms, slowest {slowest:0.0} ms"));

            Assert.IsGreaterThan(0, positions);
            // A position per row crossed, not per pointer move (40 + 20 of them, and dragover repeats more).
            Assert.IsLessThan(40, positions, results[^1]);
        }

        foreach (var line in results) TestContext.WriteLine(line);
    }
}
