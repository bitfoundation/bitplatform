using System;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Performance tests of BitDataGrid over client-side rows (/perf/data-grid): 100,000 rows virtualized - rendered,
/// sorted, filtered and scrolled to the end - and 10,000 rows grouped, which the grid renders without virtualization.
/// What each interaction cost goes to the run's results file; the thresholds only catch what is catastrophic.
/// </summary>
[TestClass]
[TestCategory("Performance")]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitDataGridBrowserTests
public class BitDataGridBrowserTests : PerformanceTestBase
{
    private const int LargeCount = 100_000;
    private const int GroupedCount = 10_000;

    // The page's rows fall into 20 categories in turn.
    private const int CategoryCount = 20;

    // Generous on purpose (the README's Performance Thresholds): ten times or more what a Debug run takes on the server
    // (CI's mode), and three times or more what it takes in WebAssembly, where sorting and grouping are slowest.
    private const double RenderThreshold = 15_000;
    private const double ChangeThreshold = 15_000;
    private const double ScrollThreshold = 10_000;
    private const double GroupThreshold = 60_000;

    // The rows the grid has in the DOM: a virtualized grid keeps a window of them, whatever the count.
    private const string RenderedRows = "#perf-grid .bit-dtg-body .bit-dtg-row[aria-rowindex]:not(.bit-dtg-group-row)";

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitDataGrid_InitialRender_100kRowsVirtualized()
    {
        await OpenPerformancePageAsync($"/perf/data-grid/{LargeCount}");

        var browser = await MeasureInBrowserAsync(Page.Locator("#btn-render"), "Rendered");
        var renderTime = await GetRenderTime();

        Console.WriteLine($"Render - {LargeCount} rows: {renderTime:F2} ms (browser {browser.Duration:F2} ms, {browser.DomNodes} elements)");
        RecordInteraction("render", LargeCount, renderTime, browser);

        var rendered = await Page.Locator(RenderedRows).CountAsync();
        Assert.IsTrue(rendered is > 0 and < 500, $"The virtualized grid has {rendered} of its {LargeCount} rows in the DOM.");
        AssertWithinThreshold(renderTime, RenderThreshold, $"Render time for {LargeCount} rows");
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitDataGrid_Sort_100kRows()
    {
        await RenderAsync(LargeCount);

        var browser = await MeasureInBrowserAsync(Page.Locator("#btn-sort"), "Sorted");
        var sortTime = await GetReRenderTime();

        Console.WriteLine($"Sort - {LargeCount} rows: {sortTime:F2} ms (browser {browser.Duration:F2} ms)");
        RecordInteraction("sort", LargeCount, sortTime, browser);

        // The prices are every multiple of 0.01 below 1,000 once each, so the dearest row comes first.
        var first = await Page.Locator(RenderedRows).First.TextContentAsync();
        Assert.Contains("999.99", first!, $"The first row after the descending sort by price is '{first}'.");
        AssertWithinThreshold(sortTime, ChangeThreshold, $"Sort time for {LargeCount} rows");
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitDataGrid_Filter_100kRows()
    {
        await RenderAsync(LargeCount);

        var browser = await MeasureInBrowserAsync(Page.Locator("#btn-filter"), "Filtered");
        var filterTime = await GetReRenderTime();

        Console.WriteLine($"Filter - {LargeCount} rows to {LargeCount / CategoryCount}: {filterTime:F2} ms (browser {browser.Duration:F2} ms)");
        RecordInteraction("filter", LargeCount, filterTime, browser);

        var first = await Page.Locator(RenderedRows).First.TextContentAsync();
        Assert.Contains("Category 07", first!, $"The first row after the filter is '{first}'.");
        AssertWithinThreshold(filterTime, ChangeThreshold, $"Filter time for {LargeCount} rows");
    }

    [TestMethod]
    [TestCategory("Scroll")]
    public async Task BitDataGrid_ScrollToEnd_100kRows()
    {
        await RenderAsync(LargeCount);

        // The page's own stopwatch ends with the render that asks the grid to scroll; the rows at the end come after it,
        // once the browser has reported the scroll, so only the browser's time is recorded.
        var browser = await MeasureInBrowserUntilAsync(Page.Locator("#btn-reveal-last"), "#perf-grid .perf-last-row");

        Console.WriteLine($"Scroll to the end - {LargeCount} rows: browser {browser.Duration:F2} ms");
        RecordBrowserTiming("scrollToEnd", LargeCount, browser);

        var rendered = await Page.Locator(RenderedRows).CountAsync();
        Assert.IsLessThan(500, rendered, $"The virtualized grid has {rendered} rows in the DOM after the scroll.");
        AssertWithinThreshold(browser.Duration, ScrollThreshold, $"Scroll to the end of {LargeCount} rows");
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitDataGrid_Group_10kRows()
    {
        await RenderAsync(GroupedCount);

        // Longer than a measurement's 30 seconds: a Debug WebAssembly build on the interpreter takes more than that.
        var browser = await MeasureInBrowserAsync(Page.Locator("#btn-group"), "Grouped", TimeSpan.FromMinutes(5));
        var groupTime = await GetReRenderTime();

        Console.WriteLine($"Group - {GroupedCount} rows in {CategoryCount} groups: {groupTime:F2} ms (browser {browser.Duration:F2} ms, {browser.DomNodes} elements)");
        RecordInteraction("group", GroupedCount, groupTime, browser);

        // The grid does not virtualize a grouped view: every row of the open groups is in the DOM.
        Assert.AreEqual(CategoryCount, await Page.Locator("#perf-grid .bit-dtg-group-row").CountAsync(), "group rows");
        Assert.AreEqual(GroupedCount, await Page.Locator(RenderedRows).CountAsync(), "rows of the grouped view");
        AssertWithinThreshold(groupTime, GroupThreshold, $"Group time for {GroupedCount} rows");
    }

    // Opens the page and renders its grid, untimed: the start of every test that times what comes after.
    private async Task RenderAsync(int count)
    {
        await OpenPerformancePageAsync($"/perf/data-grid/{count}");
        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();
    }
}
