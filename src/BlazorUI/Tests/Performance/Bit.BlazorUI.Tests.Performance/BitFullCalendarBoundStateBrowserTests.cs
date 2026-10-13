using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for a BitFullCalendar that opens on a bound View, Mode or Date while the same parameter
/// pass also syncs the views, the resources or the read-only flag: the calendar shows the bound value, the binding is
/// never handed another one, no change callback is raised for it - and what the user changes afterwards still reaches
/// the binding and the callbacks. The bUnit tests pin the component; these drive the page each engine renders, the
/// prerendered HTML and the interactive circuit that replaces it.
///
/// Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitFullCalendarBoundStateBrowserTests
public class BitFullCalendarBoundStateBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/fullcalendar-bound-state";

    private static readonly HttpClient _http = new();
    private readonly List<string> _pageErrors = [];

    [TestMethod]
    public async Task BitFullCalendar_PrerendersTheBoundValues()
    {
        // The HTML sent before the page is interactive is the first render of the same parameter pass.
        var html = await _http.GetStringAsync($"{BaseUrl}{PageUrl}");

        var views = Section(html, "views");
        Assert.AreEqual("Week", SelectedTab(views, "bit-bfc-view-tab"), "the prerender opens on another view");

        Assert.AreEqual("Timeline", SelectedTab(Section(html, "timeline"), "bit-bfc-mode-tab"), "the prerender opens on another mode");
        Assert.AreEqual("Week", SelectedTab(Section(html, "readonly"), "bit-bfc-view-tab"), "the read-only prerender opens on another view");
    }

    [TestMethod]
    public async Task BitFullCalendar_OpensOnABoundViewTheViewsAllow()
    {
        await Open();

        await AssertSelectedTab("#views .bit-bfc-view-tab", "Week");
        await AssertBinding("#views", "Week", pushed: "", changes: "0");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_OpensOnABoundTimelineMode()
    {
        await Open();

        await AssertSelectedTab("#timeline .bit-bfc-mode-tab", "Timeline");
        await AssertBinding("#timeline", "Timeline", pushed: "", changes: "0");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_OpensOnABoundDateTheViewsAllow()
    {
        await Open();

        var expected = await Page.Locator("#date .expected").InnerTextAsync();
        await Expect(Page.Locator("#date .bound")).ToHaveTextAsync(expected);
        await Expect(Page.Locator("#date .pushed")).ToHaveTextAsync("0");

        await Page.ClickAsync("#date-read");
        await Expect(Page.Locator("#date .range")).Not.ToBeEmptyAsync();
        var range = (await Page.Locator("#date .range").InnerTextAsync()).Split("..");
        var start = DateTime.ParseExact(range[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = DateTime.ParseExact(range[1], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var date = DateTime.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        Assert.IsTrue(start <= date && date <= end, $"the calendar shows {start:d}..{end:d}, not the bound {date:d}");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_OpensAReadOnlyCalendarOnItsBoundView()
    {
        await Open();

        await AssertSelectedTab("#readonly .bit-bfc-view-tab", "Week");
        await Expect(Page.Locator("#readonly .bound")).ToHaveTextAsync("Week");
        await Expect(Page.Locator("#readonly .pushed")).ToHaveTextAsync("");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_AppliesABoundViewThatChangesWithTheEvents()
    {
        await Open();

        await AssertSelectedTab("#change .bit-bfc-view-tab", "Month");

        await Page.ClickAsync("#change-go");

        await AssertSelectedTab("#change .bit-bfc-view-tab", "Week");
        await AssertBinding("#change", "Week", pushed: "", changes: "0");
        await Expect(Page.Locator("#change .bit-bfc-body")).ToContainTextAsync("Changed");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_StillReportsTheViewTheUserPicks()
    {
        await Open();

        await Page.Locator("#views .bit-bfc-view-tab", new() { HasText = "Day" }).ClickAsync();

        await AssertSelectedTab("#views .bit-bfc-view-tab", "Day");
        await AssertBinding("#views", "Day", pushed: "Day", changes: "1");

        // Keyboard too: the arrow keys walk the tab strip and pick the view they land on.
        await Page.Locator("#views .bit-bfc-view-tab[aria-selected='true']").FocusAsync();
        await Page.Keyboard.PressAsync(await IsRtl("#views") ? "ArrowLeft" : "ArrowRight");

        await AssertSelectedTab("#views .bit-bfc-view-tab", "Week");
        await AssertBinding("#views", "Week", pushed: "Day,Week", changes: "2");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_StillReportsTheModeTheUserPicks()
    {
        await Open();

        await Page.Locator("#timeline .bit-bfc-mode-tab", new() { HasText = "Events" }).ClickAsync();

        await AssertSelectedTab("#timeline .bit-bfc-mode-tab", "Events");
        await AssertBinding("#timeline", "Event", pushed: "Event", changes: "1");
        await AssertNoErrors();
    }

    [TestMethod]
    public async Task BitFullCalendar_StillReportsTheDateTheUserNavigatesTo()
    {
        await Open();

        var expected = await Page.Locator("#date .expected").InnerTextAsync();

        await Page.Locator("#date .bit-bfc-date-nav .bit-bfc-btn-nav").Last.ClickAsync();

        await Expect(Page.Locator("#date .pushed")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#date .bound")).Not.ToHaveTextAsync(expected);

        var bound = DateTime.ParseExact(await Page.Locator("#date .bound").InnerTextAsync(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.IsTrue(bound > DateTime.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture), "next moved the date backwards");
        await AssertNoErrors();
    }

    private async Task AssertSelectedTab(string tabs, string label)
    {
        var selected = Page.Locator($"{tabs}[aria-selected='true']");

        await Expect(selected).ToHaveCountAsync(1);
        await Expect(selected).ToHaveTextAsync(label);
        await Expect(selected).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bactive\b"));
    }

    private async Task AssertBinding(string section, string bound, string pushed, string changes)
    {
        await Expect(Page.Locator($"{section} .bound")).ToHaveTextAsync(bound);
        await Expect(Page.Locator($"{section} .pushed")).ToHaveTextAsync(pushed);
        await Expect(Page.Locator($"{section} .changes")).ToHaveTextAsync(changes);
    }

    private Task<bool> IsRtl(string selector) => Page.EvalOnSelectorAsync<bool>($"{selector} .bit-bfc", "e => getComputedStyle(e).direction === 'rtl'");

    private async Task Open()
    {
        await _http.DeleteAsync($"{BaseUrl}/api/server-errors");

        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Ready'");

        Page.Console += (_, message) =>
        {
            if (message.Type == "error") _pageErrors.Add(message.Text);
        };
        Page.PageError += (_, error) => _pageErrors.Add(error);
    }

    private async Task AssertNoErrors()
    {
        var errors = _pageErrors.Where(e => e.Contains("favicon", StringComparison.OrdinalIgnoreCase) is false).ToList();

        Assert.AreEqual(0, errors.Count, $"the page reported: {string.Join(" || ", errors)}");

        var json = await _http.GetStringAsync($"{BaseUrl}/api/server-errors");
        var serverErrors = JsonSerializer.Deserialize<string[]>(json) ?? [];

        Assert.AreEqual(0, serverErrors.Length, $"the server logged: {string.Join(" || ", serverErrors)}");
    }

    /// <summary>The prerendered markup of one section of the page.</summary>
    private static string Section(string html, string id)
    {
        var start = html.IndexOf($"<section id=\"{id}\"", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"the page has no section {id}");

        var end = html.IndexOf("</section>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    /// <summary>The label of the selected tab of a strip in prerendered markup.</summary>
    private static string SelectedTab(string html, string tabClass)
    {
        var match = System.Text.RegularExpressions.Regex.Match(html,
            $"<button class=\"{tabClass} active\"[^>]*aria-selected=\"true\"[^>]*>\\s*([^<]+?)\\s*</button>");

        Assert.IsTrue(match.Success, $"no selected {tabClass} in the prerendered markup");
        return match.Groups[1].Value;
    }
}
