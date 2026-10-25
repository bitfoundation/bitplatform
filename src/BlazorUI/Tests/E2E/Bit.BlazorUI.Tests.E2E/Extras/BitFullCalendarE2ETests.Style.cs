using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// How BitFullCalendar is re-skinned (/e2e/full-calendar/style): Style/Class and the Styles/Classes of every part, every
/// public --bit-FullCalendar-* variable set on an ancestor and read back off the computed styles, the dark theme of a
/// [bit-theme] scope, and the BitParams cascade of a BitFullCalendarParams.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string StylePage = "/e2e/full-calendar/style";

    [TestMethod]
    public async Task Style_StyleClassStylesAndClasses_ReachEveryPart()
    {
        await OpenAsync(StylePage);
        var cal = Page.Locator("#styled-cal");

        await Expect(cal).ToHaveClassAsync(HasClass("my-calendar"));
        await Expect(cal).ToHaveClassAsync(HasClass("root-class"));
        await Expect(cal).ToHaveCSSAsync("border-top-style", "dashed");
        await Expect(cal).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(cal).ToHaveCSSAsync("outline-color", "rgb(0, 0, 255)");

        var header = cal.Locator(".bit-bfc-header");
        await Expect(header).ToHaveClassAsync(HasClass("header-class"));
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        var body = cal.Locator(".bit-bfc-body");
        await Expect(body).ToHaveClassAsync(HasClass("body-class"));
        await Expect(body).ToHaveCSSAsync("padding-top", "7px");

        // every event surface takes Event beside the event's own CssClass
        var badge = Badge(cal, "s1");
        await Expect(badge).ToHaveClassAsync(HasClass("event-class"));
        await Expect(badge).ToHaveClassAsync(HasClass("own-class"));
        await Expect(badge).ToHaveCSSAsync("font-weight", "700");
        await SwitchViewAsync(cal, "Week");
        await Expect(Block(cal, "s1")).ToHaveClassAsync(HasClass("event-class"));
        await Expect(Block(cal, "s1")).ToHaveClassAsync(HasClass("own-class"));
        await SwitchViewAsync(cal, "Agenda");
        await Expect(cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='s1']")).ToHaveClassAsync(HasClass("event-class"));
        await Expect(cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='s1']")).ToHaveCSSAsync("font-weight", "700");

        await cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='s1']").ClickAsync();
        var dialog = Dialog(cal);
        await Expect(dialog).ToHaveClassAsync(HasClass("dialog-class"));
        await Expect(dialog).ToHaveCSSAsync("border-top-color", "rgb(9, 8, 7)");
        await Expect(dialog).ToHaveCSSAsync("border-top-width", "4px");
    }

    [TestMethod]
    [DataRow("--bit-FullCalendar-background", "#vars-cal", "background-color", "rgb(10, 20, 30)")]
    [DataRow("--bit-FullCalendar-secondary-background", "#vars-cal .bit-bfc-view-tabs", "background-color", "rgb(11, 21, 31)")]
    [DataRow("--bit-FullCalendar-color", "#vars-cal", "color", "rgb(13, 23, 33)")]
    [DataRow("--bit-FullCalendar-muted-color", "#vars-cal .bit-bfc-time-slot-label", "color", "rgb(15, 25, 35)")]
    [DataRow("--bit-FullCalendar-border-color", "#vars-cal", "border-top-color", "rgb(16, 26, 36)")]
    [DataRow("--bit-FullCalendar-accent-color", "#vars-cal .bit-bfc-header-right .bit-bfc-btn-primary", "border-top-color", "rgb(18, 28, 38)")]
    [DataRow("--bit-FullCalendar-accent-text-color", "#vars-cal .bit-bfc-header-right .bit-bfc-btn-primary", "color", "rgb(20, 30, 40)")]
    [DataRow("--bit-FullCalendar-event-border-radius", "#vars-cal .bit-bfc-event-block", "border-top-left-radius", "9px")]
    [DataRow("--bit-FullCalendar-today-background", "#vars-cal .bit-bfc-today-col", "background-color", "rgb(22, 32, 42)")]
    [DataRow("--bit-FullCalendar-off-hours-background", "#vars-cal .bit-bfc-slot-off", "background-color", "rgb(24, 34, 44)")]
    [DataRow("--bit-FullCalendar-now-indicator-color", "#vars-cal .bit-bfc-timeline-line", "background-color", "rgb(25, 35, 45)")]
    [DataRow("--bit-FullCalendar-border-radius", "#vars-cal", "border-top-left-radius", "13px")]
    [DataRow("--bit-FullCalendar-height", "#vars-cal", "height", "450px")]
    [DataRow("--bit-FullCalendar-hour-height", "#vars-cal .bit-bfc-hour-row", "height", "48px")]
    [DataRow("--bit-FullCalendar-time-gutter-width", "#vars-cal .bit-bfc-time-column", "width", "90px")]
    public async Task CssVariables_SetOnAnAncestor_ReskinTheCalendar(string variable, string selector, string property, string expected)
    {
        await OpenAsync(StylePage);

        Assert.IsTrue((await Page.Locator("#vars-host").GetAttributeAsync("style"))!.Contains(variable, StringComparison.Ordinal));
        await Expect(Page.Locator(selector).First).ToHaveCSSAsync(property, expected);

        // and the same part of a calendar outside the ancestor keeps its default
        var plain = selector.Replace("#vars-cal", "#styled-cal", StringComparison.Ordinal);
        if (await Page.Locator(plain).CountAsync() > 0)
        {
            Assert.AreNotEqual(expected, await ComputedStyleAsync(plain, property));
        }
    }

    [TestMethod]
    public async Task CssVariables_TheStateAndDialogVariables_ReskinWhatOnlyAppearsOnDemand()
    {
        await OpenAsync(StylePage);
        var cal = Page.Locator("#vars-cal");

        // the hover of a tab and of the primary button
        var tab = cal.Locator(".bit-bfc-view-tab:not(.active)").First;
        await tab.HoverAsync();
        await Expect(tab).ToHaveCSSAsync("background-color", "rgb(12, 22, 32)");
        var primary = cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary");
        await primary.HoverAsync();
        await Expect(primary).ToHaveCSSAsync("background-color", "rgb(19, 29, 39)");

        // the slots a range selection covers
        var from = Slot(cal, "Thu 13:00");
        await from.ScrollIntoViewIfNeededAsync();
        await Slot(cal, "Thu 14:00").ScrollIntoViewIfNeededAsync();
        var a = (await from.BoundingBoxAsync())!;
        var b = (await Slot(cal, "Thu 14:00").BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(a.X + a.Width / 2, a.Y + a.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(b.X + b.Width / 2, b.Y + b.Height / 2, new() { Steps = 5 });
        await Expect(cal.Locator(".bit-bfc-slot-selected").First).ToHaveCSSAsync("background-color", "rgb(23, 33, 43)");
        await Page.Mouse.UpAsync();

        // the dialog: its width, the edge of its fields, the secondary text
        var dialog = Dialog(cal);
        await Expect(dialog).ToHaveCSSAsync("max-width", "300px");
        await Expect(dialog.GetByPlaceholder("Event title")).ToHaveCSSAsync("border-top-color", "rgb(17, 27, 37)");
        await Expect(dialog.Locator(".bit-bfc-dialog-header p")).ToHaveCSSAsync("color", "rgb(14, 24, 34)");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        // an event whose color nothing knows is drawn in the event color
        await Block(cal, "v1").ClickAsync();
        await Expect(Dialog(cal).Locator(".bit-bfc-event-detail-swatch")).ToHaveCSSAsync("background-color", "rgb(21, 31, 41)");
        await PressEscapeAsync();

        // the tint of a background band
        var band = await cal.Locator(".bit-bfc-bg-event").First.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        StringAssert.Matches(band, new Regex(@"(/ 0\.5\)|, 0\.5\))$"), $"the band takes half of its color: {band}");

        // the month grid: its cells and its week-number rail
        await SwitchViewAsync(cal, "Month");
        await Expect(cal.Locator(".bit-bfc-month-cell").First).ToHaveCSSAsync("min-height", "140px");
        await Expect(cal.Locator(".bit-bfc-weeknum-cell").First).ToHaveCSSAsync("width", "50px");
        await Expect(cal.Locator(".bit-bfc-month-header-cell").Last).ToHaveCSSAsync("color", "rgb(14, 24, 34)");
    }

    [TestMethod]
    public async Task Theme_ADarkThemeScope_RedrawsTheCalendarInTheDarkScheme()
    {
        await OpenAsync(StylePage);

        var light = await ComputedStyleAsync("#light-cal", "background-color");
        var dark = await ComputedStyleAsync("#dark-cal", "background-color");
        Assert.AreNotEqual(light, dark);

        static double Luminance(string rgb)
        {
            var parts = Regex.Matches(rgb, @"\d+(\.\d+)?").Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return 0.2126 * parts[0] + 0.7152 * parts[1] + 0.0722 * parts[2];
        }

        Assert.IsTrue(Luminance(dark) < Luminance(light), $"the dark surface ({dark}) is darker than the light one ({light})");
        Assert.IsTrue(Luminance(await ComputedStyleAsync("#dark-cal", "color")) > Luminance(dark), "light text on the dark surface");
        // the native controls inherit the calendar's colors rather than the system's
        Assert.AreEqual(await ComputedStyleAsync("#dark-cal", "color"), await ComputedStyleAsync("#dark-cal .bit-bfc-header-left .bit-bfc-btn", "color"));
        Assert.AreEqual(await ComputedStyleAsync("#light-cal", "color"), await ComputedStyleAsync("#light-cal .bit-bfc-header-left .bit-bfc-btn", "color"));
    }

    [TestMethod]
    public async Task Params_BitParams_SetsTheDefaultsOfEveryCalendarUnderIt_AndOwnValuesWin()
    {
        await OpenAsync(StylePage);
        var first = Page.Locator("#cascade-1");
        var second = Page.Locator("#cascade-2");

        // the cascaded views, hidden filters, culture, clock and shared settings
        await Expect(first.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["Week", "Month"]);
        await Expect(first.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("Month");
        await Expect(first.Locator(".bit-bfc-filter-row")).ToHaveCountAsync(0);
        await Expect(Title(first)).ToHaveTextAsync("July 2025");
        await Expect(Badge(first, "s1")).ToHaveClassAsync(HasClass("dot-variant"));
        await Expect(Badge(first, "s1").Locator(".bit-bfc-event-badge-time")).ToHaveTextAsync("9:00 AM");

        // the second one's own Views and HideFilters win
        await Expect(second.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["Day", "Week", "Month", "Year", "Agenda"]);
        await Expect(second.Locator(".bit-bfc-filter-row")).ToBeVisibleAsync();
        await Expect(Badge(second, "s1")).ToHaveClassAsync(HasClass("dot-variant"));

        // one Settings object: a preference changed in one calendar's gear reaches the other
        var panel = await OpenSettingsAsync(first);
        await panel.GetByRole(AriaRole.Switch, new() { Name = "Dot badge" }).ClickAsync();
        await Expect(Page.Locator("#cascade-changes")).ToHaveTextAsync("1");
        await Expect(Badge(first, "s1")).ToHaveClassAsync(HasClass("bit-bfc-color"));
        await Expect(Badge(second, "s1")).ToHaveClassAsync(HasClass("bit-bfc-color"));
    }
}
