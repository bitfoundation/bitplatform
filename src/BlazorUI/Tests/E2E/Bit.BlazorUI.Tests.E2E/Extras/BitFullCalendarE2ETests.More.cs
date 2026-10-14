using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The remaining parameters and behaviours of BitFullCalendar: reduced motion and ForceAnimation, DefaultDate, Views with
/// repeated and unknown entries, an inverted MinDate/MaxDate window, RequireEventDescription, the Culture parameter and
/// CultureName taking precedence over it, the events an OnDateChange fetches already in the prerendered HTML, the
/// tooltips and hover hints, and the refusal notice going away on its own.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private static double Seconds(string duration)
        => duration.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(duration[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(duration.TrimEnd('s'), CultureInfo.InvariantCulture);

    [TestMethod]
    public async Task Motion_ReducedMotionSlowsTheLoadingBarAndCollapsesTheDialogMotion_UnlessForceAnimation()
    {
        await OpenAsync(ToolbarPage);
        const string loopDuration = "e => getComputedStyle(e, '::after').animationDuration";
        Assert.AreEqual(1.2, Seconds(await Page.Locator("#motion-cal .bit-bfc-loading").EvaluateAsync<string>(loopDuration)), 0.01);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        // a loader never stops: it slows down
        Assert.AreEqual(3.6, Seconds(await Page.Locator("#motion-cal .bit-bfc-loading").EvaluateAsync<string>(loopDuration)), 0.01);
        Assert.AreEqual(1.2, Seconds(await Page.Locator("#motion-fam-cal .bit-bfc-loading").EvaluateAsync<string>(loopDuration)), 0.01);
        await Expect(Page.Locator("#motion-fam-cal")).ToHaveClassAsync(HasClass("bit-fam"));

        await Badge(Page.Locator("#motion-cal"), "l1").ClickAsync();
        var reduced = Seconds(await Dialog(Page.Locator("#motion-cal")).EvaluateAsync<string>("e => getComputedStyle(e).animationDuration"));
        Assert.IsTrue(reduced < 0.02, $"the dialog's entrance collapses under reduced motion ({reduced}s)");
        await PressEscapeAsync();

        await Badge(Page.Locator("#motion-fam-cal"), "l1").ClickAsync();
        var forced = Seconds(await Dialog(Page.Locator("#motion-fam-cal")).EvaluateAsync<string>("e => getComputedStyle(e).animationDuration"));
        Assert.IsTrue(forced > 0.05, $"ForceAnimation keeps the dialog's entrance ({forced}s)");
    }

    [TestMethod]
    public async Task Defaults_DefaultDateOpensTheCalendarThere_WithoutTakingControl()
    {
        await OpenAsync(ToolbarPage);
        var cal = Page.Locator("#default-date-cal");

        await Expect(Title(cal)).ToHaveTextAsync("December 2025");
        await Expect(Page.Locator("#default-date-range")).ToHaveTextAsync("2025-12-01");
        await cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("January 2026");
        await cal.Locator(".bit-bfc-header-left > .bit-bfc-btn", new() { HasText = "Today" }).ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("July 2025");
    }

    [TestMethod]
    public async Task Views_RepeatedAndUnknownEntries_AreIgnored()
    {
        await OpenAsync(ToolbarPage);

        await Expect(Page.Locator("#dupes-cal .bit-bfc-view-tab")).ToHaveTextAsync(["Day", "Week"]);
    }

    [TestMethod]
    public async Task Bounds_AnInvertedWindow_IsIgnoredAltogether()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#inverted-cal");

        await Expect(cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']")).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(cal.Locator(".bit-bfc-btn-nav[aria-label='Next']")).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(cal.Locator(".bit-bfc-out-of-range")).ToHaveCountAsync(0);
        await cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("June 2025");
    }

    [TestMethod]
    public async Task Required_RequireEventDescription_MakesTheDescriptionMandatory()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#required-cal");
        await cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(cal);
        var description = dialog.GetByPlaceholder("Event description");
        await Expect(description).ToHaveAttributeAsync("aria-required", "true");
        await Expect(dialog.Locator("label.bit-bfc-field-required", new() { HasText = "Description" })).ToBeVisibleAsync();

        await dialog.GetByPlaceholder("Event title").FillAsync("No details");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        await Expect(dialog.Locator(".bit-bfc-field-error")).ToHaveTextAsync("Description is required");
        await Expect(description).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(description).ToBeFocusedAsync();

        await description.FillAsync("Now it has some.");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-event-badge-title", new() { HasText = "No details" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Culture_TheCultureParameter_AndCultureNameWinningOverIt()
    {
        await OpenAsync(CulturePage);

        var german = Page.Locator("#de-cal");
        await Expect(Title(german)).ToHaveTextAsync("Juli 2025");
        await Expect(german.Locator(".bit-bfc-month-header-cell").First).ToHaveTextAsync("M");
        await Expect(german.Locator(".bit-bfc-month-cell-day.today")).ToHaveTextAsync("16");

        await Expect(Title(Page.Locator("#both-cal"))).ToHaveTextAsync("juillet 2025");
        await Expect(Page.Locator("#both-cal .bit-bfc-month-header-cell").First).ToHaveTextAsync("L");
    }

    [TestMethod]
    public async Task Prerender_TheEventsOnDateChangeFetches_AreAlreadyInTheServerHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{ToolbarPage}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "Loaded Jul 1");
        StringAssert.Contains(html, "id=\"loaded\">2025-07-01|2025-07-31|5");
    }

    [TestMethod]
    public async Task Tooltips_AnEventSaysItsTitleTimesAndDescriptionOnHover_AndADayItsAddHint()
    {
        await OpenMainAsync();

        await Expect(Badge(Cal, "e1")).ToHaveAttributeAsync("title", "Team Standup\n09:00 - 09:45\nDaily sync.");
        await Expect(Badge(Cal, "e4")).ToHaveAttributeAsync("title", "Tech Conference\nJul 17, 2025 09:00 - Jul 19, 2025 17:00\nKeynotes.");
        await Expect(Badge(Cal, "e5")).ToHaveAttributeAsync("title", "Company Holiday\nJul 21, 2025 · All day\nOffices closed.");

        var hint = MonthAdd(Cal, "Friday, July 25, 2025");
        await Expect(hint).ToHaveCSSAsync("opacity", "0");
        await MonthCell(Cal, "Friday, July 25, 2025").HoverAsync();
        await Expect(hint).ToHaveCSSAsync("opacity", "1");
        await Expect(hint).ToHaveTextAsync("Add event");
    }

    [TestMethod]
    public async Task Keyboard_TabWalksTheToolbarThenReachesTheGridOnce_AndAMultiDayEventOncePerWeekRow()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-header-left > .bit-bfc-btn", new() { HasText = "Today" }).FocusAsync();

        var stops = new List<string>();
        for (var i = 0; i < 24; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            stops.Add(await Page.EvaluateAsync<string>("""
                () => {
                    const a = document.activeElement;
                    if (!a || !a.closest('#cal')) return 'outside';
                    if (a.hasAttribute('data-bit-bfc-roving')) return 'grid';
                    // Firefox puts a scrolling container in the tab order of its own accord
                    if (a.matches('button, input, select, textarea, a[href], [tabindex]') === false) return 'scroller';
                    return a.getAttribute('data-bit-bfc-event') ?? a.getAttribute('aria-label') ?? a.textContent.trim();
                }
                """));
        }

        CollectionAssert.AreEqual(new[] { "Previous", "Next", "Events", "Month", "Filter events by color", "Filter events by person in current view", "Add Event", "Settings", "grid" }, stops.Where(s => s != "scroller").Take(9).ToArray(), string.Join(" > ", stops));
        Assert.AreEqual(1, stops.Count(s => s == "grid"), "the month grid is one tab stop");
        Assert.AreEqual(1, stops.Count(s => s == "e4"), "a multi-day event is one stop per week row it runs across");
        CollectionAssert.IsSubsetOf(new[] { "e6", "e1", "e2", "e3", "e4", "e5", "e7" }, stops);
    }

    [TestMethod]
    public async Task Keyboard_TheDayGridIsOneTabStop_TheKeysWalkItsSlots()
    {
        await OpenMainAsync("Day");
        var stop = Cal.Locator(".bit-bfc-timegrid-wrapper [data-bit-bfc-roving][tabindex='0']");
        await Expect(stop).ToHaveCountAsync(1);
        await Expect(stop).ToHaveAttributeAsync("aria-label", "Add event, 00:00");
        await stop.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Add event, 00:30");
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedLabelAsync("Add event, 01:30");
        await Page.Keyboard.PressAsync("End");
        await ExpectFocusedLabelAsync("Add event, 23:30");
        await Page.Keyboard.PressAsync("Home");
        await ExpectFocusedLabelAsync("Add event, 00:00");

        await Page.Keyboard.PressAsync("Space");
        await Expect(StartTrigger(Dialog(Cal))).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 00:00");
    }

    [TestMethod]
    public async Task OnChanging_CancellingAKeyboardMove_PutsTheEventBack()
    {
        await OpenMainAsync("Week");
        await Page.Locator("#btn-refuse-changing").ClickAsync();
        var block = Block(Cal, "e2");
        await block.FocusAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowDown");

        await Expect(Page.Locator("#changing-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-changing")).ToHaveTextAsync("Edit|Drag|Focus Time|2025-07-16 10:30|2025-07-16 10:00");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
        await Expect(block.Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("10:00 - 11:00");
    }

    [TestMethod]
    public async Task More_AnEventDeletedFromTheEventList_LeavesTheListAtOnce()
    {
        await OpenMainAsync();
        await Page.Locator("#btn-add-event").ClickAsync();
        await MonthCell(Cal, "Wednesday, July 16, 2025").Locator(".bit-bfc-month-more").ClickAsync();
        var list = Cal.Locator(".bit-bfc-dialog[role='dialog']").First;
        await Expect(list.Locator(".bit-bfc-agenda-item")).ToHaveCountAsync(4);

        await list.Locator(".bit-bfc-agenda-item", new() { HasText = "Focus Time" }).ClickAsync();
        var details = Cal.Locator(".bit-bfc-dialog[role='dialog']").Last;
        await Expect(details.Locator("h3")).ToHaveTextAsync("Focus Time");
        await details.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Delete\|Dialog\|Focus Time\|"));
        await Expect(Cal.Locator(".bit-bfc-dialog[role='dialog']")).ToHaveCountAsync(1);
        // the focus stays in the dialog still open rather than falling behind it
        await Page.WaitForFunctionAsync("() => document.querySelector('#cal .bit-bfc-dialog[role=\"dialog\"]').contains(document.activeElement)");
    }

    [TestMethod]
    public async Task Notices_ARefusalNotice_GoesAwayOnItsOwn()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");
        await Page.Locator("#btn-overlap").ClickAsync();
        await Block(cal, "r2").FocusAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowDown");

        var toast = ErrorToast(cal);
        await Expect(toast).ToBeVisibleAsync();
        // an error stays six seconds, enough to be read
        await Expect(toast).ToHaveCountAsync(0, new() { Timeout = 12000 });
        await Expect(cal.Locator(".bit-bfc-toast-region[role='alert']")).ToBeAttachedAsync();
    }
}
