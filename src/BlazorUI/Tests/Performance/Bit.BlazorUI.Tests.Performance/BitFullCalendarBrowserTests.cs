using System;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Performance tests of BitFullCalendar over 5,000 events of a year, 200 of them weekly series (/perf/full-calendar):
/// the month view and the timeline's month, each rendered and moved on to the next month. Every month view is handed
/// all the events, and each of its day cells looks through them for its own (BitFullCalendarHelpers.GetMonthCellEvents).
/// What each interaction cost goes to the run's results file; the thresholds only catch what is catastrophic.
/// </summary>
[TestClass]
[TestCategory("Performance")]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitFullCalendarBrowserTests
public class BitFullCalendarBrowserTests : PerformanceTestBase
{
    private const int EventCount = 5_000;
    private const int ResourceCount = 50;

    // The ends the tests wait for, as the calendar labels its days (en-US, see the page): the last day of January, the
    // month it opens on, and a day of February that January's month view does not show (its grid ends on February 7).
    private const string January = "#perf-calendar [aria-label*='January 31, 2026']";
    private const string February = "#perf-calendar [aria-label*='February 16, 2026']";

    // Generous on purpose (the README's Performance Thresholds): ten times or more what a Debug run takes on the server
    // (CI's mode), and four times or more what it takes in WebAssembly.
    private const double RenderThreshold = 30_000;
    private const double NavigateThreshold = 30_000;

    // A measurement waits longer than its usual 30 seconds: a Debug WebAssembly build on the interpreter can take more.
    private static readonly TimeSpan Wait = TimeSpan.FromMinutes(2);

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitFullCalendar_InitialRender_MonthView_5kEvents()
    {
        await OpenPerformancePageAsync($"/perf/full-calendar/{EventCount}");

        var browser = await MeasureInBrowserUntilAsync(Page.Locator("#btn-render-month"), January, Wait);
        await WaitForRenderComplete();
        var renderTime = await GetRenderTime();

        Console.WriteLine($"Month view - {EventCount} events: {renderTime:F2} ms (browser {browser.Duration:F2} ms, {browser.DomNodes} elements)");
        RecordInteraction("renderMonth", EventCount, renderTime, browser);

        var cells = await Page.Locator("#perf-calendar .bit-bfc-month-cell").CountAsync();
        Assert.IsGreaterThanOrEqualTo(35, cells, $"The month view has {cells} day cells.");
        AssertWithinThreshold(browser.Duration, RenderThreshold, $"Month view render of {EventCount} events");
    }

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitFullCalendar_InitialRender_TimelineMonth_5kEvents()
    {
        await OpenPerformancePageAsync($"/perf/full-calendar/{EventCount}");

        var browser = await MeasureInBrowserUntilAsync(Page.Locator("#btn-render-timeline"), January, Wait);
        await WaitForRenderComplete();
        var renderTime = await GetRenderTime();

        Console.WriteLine($"Timeline month - {EventCount} events, {ResourceCount} resources: {renderTime:F2} ms (browser {browser.Duration:F2} ms, {browser.DomNodes} elements)");
        RecordInteraction("renderTimeline", EventCount, renderTime, browser);

        // A slot of the day on every resource's row.
        var slots = await Page.Locator(January).CountAsync();
        Assert.IsGreaterThanOrEqualTo(ResourceCount, slots, $"The timeline has {slots} slots of January 31, for {ResourceCount} resources.");
        AssertWithinThreshold(browser.Duration, RenderThreshold, $"Timeline month render of {EventCount} events");
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitFullCalendar_NextMonth_MonthView_5kEvents()
    {
        await RenderAsync("#btn-render-month");

        var browser = await MeasureInBrowserUntilAsync(Page.Locator("#btn-next"), February, Wait);
        await WaitForStatus("Navigated");
        var navigateTime = await GetReRenderTime();

        Console.WriteLine($"Month view, next month - {EventCount} events: {navigateTime:F2} ms (browser {browser.Duration:F2} ms)");
        RecordInteraction("nextMonth", EventCount, navigateTime, browser);

        AssertWithinThreshold(browser.Duration, NavigateThreshold, $"Month view navigation over {EventCount} events");
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitFullCalendar_NextMonth_TimelineMonth_5kEvents()
    {
        await RenderAsync("#btn-render-timeline");

        var browser = await MeasureInBrowserUntilAsync(Page.Locator("#btn-next"), February, Wait);
        await WaitForStatus("Navigated");
        var navigateTime = await GetReRenderTime();

        Console.WriteLine($"Timeline month, next month - {EventCount} events: {navigateTime:F2} ms (browser {browser.Duration:F2} ms)");
        RecordInteraction("nextTimelineMonth", EventCount, navigateTime, browser);

        AssertWithinThreshold(browser.Duration, NavigateThreshold, $"Timeline month navigation over {EventCount} events");
    }

    // Opens the page and renders its calendar in a view, untimed: the start of the tests that time what comes after.
    private async Task RenderAsync(string trigger)
    {
        await OpenPerformancePageAsync($"/perf/full-calendar/{EventCount}");
        await Page.Locator(trigger).ClickAsync(new() { Timeout = (float)Wait.TotalMilliseconds });
        await Page.Locator(January).First.WaitForAsync(new() { Timeout = (float)Wait.TotalMilliseconds });
        await WaitForRenderComplete();
    }
}
