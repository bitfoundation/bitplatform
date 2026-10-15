using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The frame around BitFullCalendar (/e2e/full-calendar/toolbar): the Views it offers and the bound View clamped into
/// them, HideHeader / HideFilters / HideSettings, the root's HtmlAttributes and Visibility, IsLoading driven by
/// OnDateChange, mounting and unmounting without an error, and a calendar inside a BitModal whose Escape stays its own.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string ToolbarPage = "/e2e/full-calendar/toolbar";

    [TestMethod]
    public async Task Views_TheOfferedViews_AreTheTabsInTheirOrder_AndTheOpeningViewIsClampedIntoThem()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#wd-cal");

        await Expect(cal.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["Week", "Day"]);
        await Expect(cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("Week");
        await Expect(cal.Locator(".bit-bfc-week-layout")).ToBeVisibleAsync();
        await SwitchViewAsync(cal, "Day");
        await Expect(cal.Locator(".bit-bfc-day-layout")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Views_ASingleView_HidesTheTabStrip_AndTheBodyIsAPlainRegion()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#single-cal");

        await Expect(cal.Locator(".bit-bfc-view-tabs")).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-mode-tabs")).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-month")).ToBeVisibleAsync();
        var body = Page.Locator("#single-cal-body");
        await Expect(body).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(body).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
    }

    [TestMethod]
    public async Task Views_ABoundViewOutsideTheOfferedOnes_IsClamped_AndTheBindingCorrected()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#clamped-cal");

        await Expect(cal.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["Month", "Agenda"]);
        await Expect(cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("Month");
        await Expect(Page.Locator("#clamped-view")).ToHaveTextAsync("Month");

        await SwitchViewAsync(cal, "Agenda");
        await Expect(Page.Locator("#clamped-view")).ToHaveTextAsync("Agenda");
        await Page.Locator("#btn-clamped-year").ClickAsync();
        await Expect(Page.Locator("#clamped-view")).Not.ToHaveTextAsync("Year");
        await Expect(cal.Locator(".bit-bfc-year")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Toolbar_HideHeaderRemovesTheToolbar_HideFiltersAndHideSettingsTrimIt()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#hide-cal");
        await Expect(cal.Locator(".bit-bfc-header")).ToBeVisibleAsync();

        await Page.Locator("#btn-hide-header").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-header")).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-month")).ToBeVisibleAsync();
        // with no tab to label it, the body is no tab panel
        await Expect(Page.Locator("#hide-cal-body")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Page.Locator("#btn-hide-header").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-header")).ToBeVisibleAsync();
        await Expect(Page.Locator("#hide-cal-body")).ToHaveAttributeAsync("role", "tabpanel");

        var trimmed = Page.Locator("#trim-cal");
        await Expect(trimmed.Locator(".bit-bfc-header")).ToBeVisibleAsync();
        await Expect(trimmed.Locator(".bit-bfc-filter-row")).ToHaveCountAsync(0);
        await Expect(trimmed.Locator("button[aria-label='Settings']")).ToHaveCountAsync(0);
        await Expect(trimmed.Locator(".bit-bfc-header-right .bit-bfc-btn-primary")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Attributes_HtmlAttributesReachTheRoot_AndANameMakesItARegion()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#attrs-cal");

        await Expect(cal).ToHaveAttributeAsync("data-testid", "calendar-under-test");
        await Expect(cal).ToHaveAttributeAsync("title", "A calendar");
        await Expect(cal).ToHaveAttributeAsync("aria-label", "Named by an attribute");
        await Expect(cal).ToHaveAttributeAsync("role", "region");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Named by an attribute" })).ToHaveCountAsync(1);

        // an unnamed calendar is a plain container
        await Expect(Page.Locator("#unnamed-cal")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheCalendar()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#attrs-cal");

        await Page.Locator("#btn-visibility-hidden").ClickAsync();
        await Expect(cal).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await cal.BoundingBoxAsync())!.Height > 100, "hidden keeps its room");

        await Page.Locator("#btn-visibility-collapsed").ClickAsync();
        await Expect(cal).ToHaveCSSAsync("display", "none");

        await Page.Locator("#btn-visibility-visible").ClickAsync();
        await Expect(cal).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Loading_IsLoading_RunsTheBarAndMarksTheBodyBusy_WhileTheRangeIsFetched()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#loading-cal");
        var body = Page.Locator("#loading-cal-body");

        // the first range is reported on the first render and fetched
        await Expect(Page.Locator("#loaded")).ToHaveTextAsync("2025-07-01|2025-07-31|5");
        await Expect(cal.Locator(".bit-bfc-loading")).ToHaveCountAsync(0);
        await Expect(body).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(cal.Locator(".bit-bfc-event-badge-title", new() { HasTextRegex = new Regex("^Loaded Jul 1$") })).ToBeVisibleAsync();

        await cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-loading")).ToBeVisibleAsync();
        await Expect(cal.Locator(".bit-bfc-loading")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(body).ToHaveAttributeAsync("aria-busy", "true");
        // the calendar stays usable meanwhile
        await Expect(cal.Locator(".bit-bfc-btn-nav[aria-label='Next']")).ToBeEnabledAsync();
        await Badge(cal, "load-20250729").ClickAsync();
        await Expect(Dialog(cal).Locator("h3")).ToHaveTextAsync("Loaded Jul 29");
        await PressEscapeAsync();
        await Expect(Page.Locator("#load-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-finish-load").ClickAsync();

        await Expect(Page.Locator("#loaded")).ToHaveTextAsync("2025-08-01|2025-08-31|5");
        await Expect(cal.Locator(".bit-bfc-loading")).ToHaveCountAsync(0);
        await Expect(body).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(cal.Locator(".bit-bfc-event-badge-title", new() { HasTextRegex = new Regex("^Loaded Aug 1$") })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAndMountingAgain_LeavesNoErrorBehind()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#mounted-cal");
        await Expect(cal.Locator(".bit-bfc-week-layout")).ToBeVisibleAsync();
        await Block(cal, "l1").FocusAsync();

        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(cal).ToHaveCountAsync(0);
        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-layout")).ToBeVisibleAsync();
        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(cal).ToHaveCountAsync(0);

        // a press anywhere after the unmount finds no stale listener of the disposed calendar
        await Page.Locator("#btn-open-modal").HoverAsync();
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();
        await AssertNoServerErrorsAsync();
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Modal_EscapeInsideTheCalendarsDialog_ClosesThatDialogAlone_TheNextOneTheModal()
    {
        await OpenAsync(ToolbarPage);
        await Page.Locator("#btn-open-modal").ClickAsync();
        var cal = Page.Locator("#modal-cal");
        await Expect(cal).ToBeVisibleAsync();

        await Badge(cal, "l1").ClickAsync();
        await Expect(Dialog(cal)).ToBeVisibleAsync();
        await PressEscapeAsync();
        await Expect(Dialog(cal)).ToHaveCountAsync(0);
        await Expect(cal).ToBeVisibleAsync();
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("True");

        // the settings panel claims its Escape too
        var panel = await OpenSettingsAsync(cal);
        await PressEscapeAsync();
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("True");

        // with nothing of the calendar's open, Escape is the modal's
        await cal.Locator(".bit-bfc-view-tab.active").FocusAsync();
        await PressEscapeAsync();
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("False");
    }
}
