using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Three hundred BitSortable rows in a scrolling panel, on /e2e/sortable/large-list: a pointer drag held near the
/// panel's edge scrolls it and carries the row along, and the script's own counters
/// (<c>BitBlazorUI.Sortable.diagnostics</c>) show what a drag costs - how many positions reached .NET for how many
/// pointer moves, and how long each took - with and without IsolateItems. The numbers are written to the test output;
/// the assertions only hold them to what the design promises.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableLargeListE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/large-list";

    // A row of the list by its name (a row also shows a note, so the item text is more than the name).
    private ILocator Row(string name) =>
        Page.Locator("#large-list > [data-bit-srt-item]").Filter(new()
        {
            Has = Page.Locator(".large-name", new() { HasTextRegex = new System.Text.RegularExpressions.Regex("^" + System.Text.RegularExpressions.Regex.Escape(name) + "$") })
        });

    private Task<double> PanelScrollTopAsync() => Page.EvaluateAsync<double>("() => document.getElementById('large-panel').scrollTop");

    [TestMethod]
    public async Task Scroll_APointerDragHeldAtTheEdgeOfThePanel_ScrollsItAndCarriesTheRowAlong()
    {
        await OpenAsync(Url);
        await Page.Locator("#large-fallback").CheckAsync();
        await WaitForQuietAsync();

        var panel = await BoxAsync(Page.Locator("#large-panel"));
        // The pointer stays over the list: the panel is as wide as the page and the list (520px at most) sits at its start,
        // so the middle of the panel is beside the list, where the panel scrolls but no list is under the pointer to sort.
        var x = (float)(await ListBoxAsync("large-list")).Centre.X;
        var inView = await Page.EvaluateAsync<int>(
            "() => { const p = document.getElementById('large-panel').getBoundingClientRect(); return [...document.querySelectorAll('#large-list > [data-bit-srt-item]')].filter(e => e.getBoundingClientRect().top < p.bottom).length; }");
        await PressAsync((await BoxAsync(Row("Row 1"))).Centre);
        // Into the bottom edge band of the panel, and held there.
        await Page.Mouse.MoveAsync(x, (float)(panel.Bottom - 8), new() { Steps = 20 });
        await EventuallyAsync(async () => await PanelScrollTopAsync() > 400, "the panel scrolls while the row is held at its edge");
        await Page.Mouse.MoveAsync(x, (float)(panel.Bottom - 120), new() { Steps = 4 });
        await WaitForQuietAsync();
        await ReleaseAsync();

        // Row 1 went down with the scroll, past the rows that were in view when it was picked up. How far past depends on
        // how long the panel scrolled before the poll above saw it, so only that bound is held.
        var index = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('#large-list > [data-bit-srt-item]')].findIndex(e => e.querySelector('.large-name').textContent === 'Row 1')");
        Assert.IsGreaterThan(inView, index, $"Row 1 is at {index}, and {inView} rows were in view when it was picked up.");
        await Expect(Page.Locator("#large-order")).Not.ToContainTextAsync("Row 1,");
    }

    [TestMethod]
    public async Task Diagnostics_OnlyNewPositionsReachDotNet_WithAndWithoutIsolateItems()
    {
        await OpenAsync(Url);
        var results = new List<string>();
        // Every dragover the browser fires: each of them would be a call into .NET if the script did not filter them.
        await Page.EvaluateAsync("() => { window.__dragovers = 0; document.addEventListener('dragover', () => window.__dragovers++, true); }");

        foreach (var isolate in new[] { false, true })
        {
            await Page.Locator("#large-isolate").SetCheckedAsync(isolate);
            await WaitForQuietAsync();
            await Page.EvaluateAsync("() => { document.getElementById('large-panel').scrollTop = 0; window.__dragovers = 0; }");
            await DiagnosticsAsync(reset: true);

            var from = await BoxAsync(Row(isolate ? "Row 2" : "Row 1"));
            await PressAsync(from.Centre);
            // Down ten rows in forty pointer moves, then back up five.
            await Page.Mouse.MoveAsync((float)from.Centre.X, (float)(from.Centre.Y + from.Height * 10), new() { Steps = 40 });
            await Page.Mouse.MoveAsync((float)from.Centre.X, (float)(from.Centre.Y + from.Height * 5), new() { Steps = 20 });
            await WaitForQuietAsync();
            await ReleaseAsync();

            var stats = await DiagnosticsAsync();
            int positions = stats.GetProperty("positions").GetInt32();
            double average = stats.GetProperty("averageMs").GetDouble();
            double slowest = stats.GetProperty("slowestMs").GetDouble();
            int dragovers = await Page.EvaluateAsync<int>("() => window.__dragovers");
            results.Add(string.Create(CultureInfo.InvariantCulture,
                $"IsolateItems={isolate}: 60 pointer moves, {dragovers} dragover events, {positions} positions sent to .NET ({stats.GetProperty("changes").GetInt32()} changed a list), average {average:0.0} ms, slowest {slowest:0.0} ms"));

            Assert.IsGreaterThan(0, positions);
            // A position per row crossed, not per pointer move (40 + 20 of them, and dragover repeats more).
            Assert.IsLessThan(40, positions, results[^1]);
        }

        foreach (var line in results) TestContext.WriteLine(line);
    }

    [TestMethod]
    public async Task Axe_TheList_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#large");
    }
}
