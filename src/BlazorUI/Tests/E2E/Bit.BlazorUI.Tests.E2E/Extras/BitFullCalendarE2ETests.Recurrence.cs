using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Recurring events of BitFullCalendar (/e2e/full-calendar/recurrence): the rules expanding into their occurrences, the
/// repeat summary of the details dialog, the scope prompt asking whether a delete or an edit means one occurrence or the
/// series, the repeat fields of the add/edit dialog with their validation, and dragging an occurrence out of its series.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string RecurrencePage = "/e2e/full-calendar/recurrence";

    private ILocator RecCal => Page.Locator("#rec-cal");

    private ILocator ScopeDialog => RecCal.Locator(".bit-bfc-scope-dialog[role='dialog']");

    private ILocator Occurrence(string id, string yyyymmdd) => RecCal.Locator($"[data-bit-bfc-event='{id}@{yyyymmdd}'][role='button']");

    [TestMethod]
    public async Task Recurrence_TheRulesExpandIntoTheirOccurrences_OnTheirDatesOnly()
    {
        await OpenAsync(RecurrencePage);

        await Expect(RecCal.Locator("[data-bit-bfc-event^='standup@'][role='button']")).ToHaveCountAsync(12);
        await Expect(Occurrence("standup", "20250716")).ToBeVisibleAsync();
        await Expect(Occurrence("standup", "20250715")).ToHaveCountAsync(0);
        await Expect(Occurrence("standup", "20250719")).ToHaveCountAsync(0);
        // the skipped date
        await Expect(Occurrence("standup", "20250721")).ToHaveCountAsync(0);
        await Expect(Occurrence("standup", "20250722")).ToBeVisibleAsync();

        await Expect(Occurrence("review", "20250718")).ToBeVisibleAsync();
        await Expect(Occurrence("review", "20250801")).ToBeVisibleAsync();
        await Expect(Occurrence("board", "20250715")).ToBeVisibleAsync();
        // the added date, outside the pattern
        await Expect(Occurrence("board", "20250724")).ToBeVisibleAsync();
        await Expect(RecCal.Locator("[data-bit-bfc-event^='backup@'][role='button']")).ToHaveCountAsync(3);
        await Expect(Occurrence("backup", "20250731")).ToBeVisibleAsync();
        await Expect(Occurrence("monthend", "20250725")).ToBeVisibleAsync();
        // a master is never drawn on its own
        await Expect(RecCal.Locator("[data-bit-bfc-event='standup']")).ToHaveCountAsync(0);

        await RecCal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(Occurrence("review", "20250815")).ToBeVisibleAsync();
        // three times only
        await Expect(Occurrence("review", "20250829")).ToHaveCountAsync(0);
        await Expect(Occurrence("board", "20250819")).ToBeVisibleAsync();
        await Expect(Occurrence("monthend", "20250829")).ToBeVisibleAsync();
        await Expect(Occurrence("backup", "20250815")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("standup", "20250722", "Weekly · on Mon, Tue, Wed, Thu, Fri")]
    [DataRow("review", "20250718", "Weekly · every 2 · on Fri · 3 times")]
    [DataRow("board", "20250715", "Monthly · on the third Tuesday · until 1/16/2026")]
    [DataRow("backup", "20250716", "Daily · every 15")]
    [DataRow("monthend", "20250725", "Monthly · on the last Friday")]
    public async Task Recurrence_TheDetailsOfAnOccurrence_SayHowItsSeriesRepeats(string id, string date, string summary)
    {
        await OpenAsync(RecurrencePage);

        await Occurrence(id, date).ClickAsync();

        await Expect(Dialog(RecCal).Locator(".bit-bfc-event-detail-row", new() { HasText = "Repeats" }).Locator(".bit-bfc-event-detail-value")).ToHaveTextAsync(summary);
    }

    [TestMethod]
    public async Task Recurrence_DeletingOneOccurrence_SkipsItsDateOnTheSeries()
    {
        await OpenAsync(RecurrencePage);
        await Occurrence("standup", "20250722").ClickAsync();
        await Dialog(RecCal).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(ScopeDialog.Locator("h3")).ToHaveTextAsync("Delete recurring event");
        await Expect(ScopeDialog.Locator(".bit-bfc-dialog-body")).ToHaveAttributeAsync("role", "radiogroup");
        await Expect(ScopeDialog.GetByLabel("This event")).ToBeCheckedAsync();
        await Expect(ScopeDialog.GetByLabel("All events in the series")).Not.ToBeCheckedAsync();
        await ScopeDialog.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();

        await Expect(Dialog(RecCal)).ToHaveCountAsync(0);
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#changes li").Last).ToHaveTextAsync(new Regex(@"^Edit\|Dialog\|Daily Standup\|.*skip:2025-07-21,2025-07-22/"));
        await Expect(Occurrence("standup", "20250722")).ToHaveCountAsync(0);
        await Expect(Occurrence("standup", "20250723")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Recurrence_DeletingTheSeries_RemovesEveryOccurrence()
    {
        await OpenAsync(RecurrencePage);
        await Occurrence("standup", "20250722").ClickAsync();
        await Dialog(RecCal).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await ScopeDialog.GetByLabel("All events in the series").CheckAsync();
        await ScopeDialog.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();

        await Expect(Page.Locator("#changes li").Last).ToHaveTextAsync(new Regex(@"^Delete\|Dialog\|Daily Standup\|"));
        await Expect(RecCal.Locator("[data-bit-bfc-event^='standup@']")).ToHaveCountAsync(0);
        await Expect(Occurrence("review", "20250718")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Recurrence_TheScopePrompt_CancelAndEscapeBackOutOfItAlone()
    {
        await OpenAsync(RecurrencePage);
        await Occurrence("board", "20250715").ClickAsync();
        var details = RecCal.Locator(".bit-bfc-dialog[role='dialog']:not(.bit-bfc-scope-dialog)");

        await details.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await Expect(ScopeDialog.Locator("h3")).ToHaveTextAsync("Edit recurring event");
        await Expect(ScopeDialog).ToHaveAttributeAsync("data-bit-esc", "claim");
        await ScopeDialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(ScopeDialog).ToHaveCountAsync(0);
        await Expect(details).ToBeVisibleAsync();

        await details.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(ScopeDialog).ToBeVisibleAsync();
        // the focus moved into the prompt
        Assert.IsTrue(await ScopeDialog.EvaluateAsync<bool>("d => d.contains(document.activeElement)"));
        await PressEscapeAsync();
        await Expect(ScopeDialog).ToHaveCountAsync(0);
        await Expect(details).ToBeVisibleAsync();

        await PressEscapeAsync();
        await Expect(details).ToHaveCountAsync(0);
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Recurrence_EditingOneOccurrence_DetachesItAsAOneOff()
    {
        await OpenAsync(RecurrencePage);
        await Occurrence("standup", "20250723").ClickAsync();
        await Dialog(RecCal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await ScopeDialog.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();

        var edit = EditDialog(RecCal);
        await Expect(edit).ToBeVisibleAsync();
        // a one-off has no rule to edit
        await Expect(edit.GetByLabel("Repeat", new() { Exact = true })).ToHaveCountAsync(0);
        await edit.GetByPlaceholder("Event title").FillAsync("Standup (moved)");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#changes li").Nth(0)).ToHaveTextAsync(new Regex(@"^Edit\|Dialog\|Daily Standup\|.*skip:2025-07-21,2025-07-23/"));
        await Expect(Page.Locator("#changes li").Nth(1)).ToHaveTextAsync("Add|Dialog|Standup (moved)|2025-07-23 09:00|2025-07-23 09:15|none");
        await Expect(Occurrence("standup", "20250723")).ToHaveCountAsync(0);
        await Expect(MonthCell(RecCal, "Wednesday, July 23, 2025").Locator(".bit-bfc-event-badge-title", new() { HasText = "Standup (moved)" })).ToBeVisibleAsync();
        await Expect(Occurrence("standup", "20250724")).ToContainTextAsync("Daily Standup");
    }

    [TestMethod]
    public async Task Recurrence_EditingTheSeries_ChangesEveryOccurrence()
    {
        await OpenAsync(RecurrencePage);
        await Occurrence("standup", "20250723").ClickAsync();
        await Dialog(RecCal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await ScopeDialog.GetByLabel("All events in the series").CheckAsync();
        await ScopeDialog.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();

        var edit = EditDialog(RecCal);
        await Expect(edit.GetByLabel("Repeat", new() { Exact = true })).ToHaveValueAsync("Weekly");
        await Expect(edit.GetByLabel("Repeat every")).ToHaveValueAsync("1");
        await Expect(edit.Locator(".bit-bfc-repeat-day[aria-pressed='true']")).ToHaveTextAsync(ServerOrWasm(["M", "T", "W", "T", "F"], ["Mo", "Tu", "We", "Th", "Fr"]));
        await Expect(edit.Locator(".bit-bfc-repeat-skipped .bit-bfc-date-chip-skipped")).ToContainTextAsync("7/21/2025");
        await edit.GetByPlaceholder("Event title").FillAsync("Team Sync");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        await Expect(Page.Locator("#changes li").Last).ToHaveTextAsync(new Regex(@"^Edit\|Dialog\|Team Sync\|2025-07-16 09:00\|2025-07-16 09:15\|Weekly/1/"));
        await Expect(Occurrence("standup", "20250716")).ToContainTextAsync("Team Sync");
        await Expect(Occurrence("standup", "20250731")).ToContainTextAsync("Team Sync");
    }

    [TestMethod]
    public async Task Recurrence_TheAddDialog_BuildsAWeeklyRuleThatEndsAfterACount()
    {
        await OpenAsync(RecurrencePage);
        await RecCal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(RecCal);
        await dialog.GetByPlaceholder("Event title").FillAsync("Gym");

        var repeat = dialog.GetByLabel("Repeat", new() { Exact = true });
        await Expect(repeat.Locator("option")).ToHaveTextAsync(["Does not repeat", "Daily", "Weekly", "Monthly", "Yearly"]);
        await repeat.SelectOptionAsync("Weekly");
        await Expect(dialog.Locator(".bit-bfc-repeat-row:has(input.bit-bfc-repeat-number)").First).ToContainTextAsync("week(s)");
        // the start date's own weekday is picked to begin with
        await Expect(dialog.Locator(".bit-bfc-repeat-day[aria-pressed='true']")).ToHaveTextAsync(ServerOrWasm(["W"], ["We"]));
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Friday" }).ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Friday" })).ToHaveAttributeAsync("aria-pressed", "true");
        // the last weekday picked stays picked
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Wednesday" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Friday" }).ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Friday" })).ToHaveAttributeAsync("aria-pressed", "true");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Wednesday" }).ClickAsync();

        await dialog.GetByLabel("Ends").SelectOptionAsync("AfterCount");
        var count = dialog.Locator("input[aria-label='occurrence(s)']");
        await Expect(count).ToHaveValueAsync("10");
        await count.FillAsync("3");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        await Expect(Page.Locator("#changes li").Last).ToHaveTextAsync("Add|Dialog|Gym|2025-07-16 08:00|2025-07-16 08:30|Weekly/1/Wednesday,Friday//3//skip:/add:");
        var occurrences = RecCal.Locator("[role='button'][aria-label^='Gym, ']");
        await Expect(occurrences).ToHaveCountAsync(3);
        await Expect(occurrences.Nth(0)).ToHaveAttributeAsync("aria-label", "Gym, 7/16/2025 08:00 - 7/16/2025 08:30");
        await Expect(occurrences.Nth(1)).ToHaveAttributeAsync("aria-label", "Gym, 7/18/2025 08:00 - 7/18/2025 08:30");
        await Expect(occurrences.Nth(2)).ToHaveAttributeAsync("aria-label", "Gym, 7/23/2025 08:00 - 7/23/2025 08:30");
    }

    [TestMethod]
    public async Task Recurrence_TheRepeatFields_AreValidated_AndTheMonthlyChoicesOffered()
    {
        await OpenAsync(RecurrencePage);
        await RecCal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(RecCal);
        await dialog.GetByPlaceholder("Event title").FillAsync("Ritual");
        var repeat = dialog.GetByLabel("Repeat", new() { Exact = true });

        await repeat.SelectOptionAsync("Daily");
        var interval = dialog.GetByLabel("Repeat every");
        await interval.FillAsync("0");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-field-error", new() { HasText = "Must be at least 1" })).ToBeVisibleAsync();
        await Expect(interval).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(interval).ToBeFocusedAsync();
        await interval.FillAsync("2");

        await dialog.GetByLabel("Ends").SelectOptionAsync("OnDate");
        var until = dialog.Locator("button.bit-bfc-dtp-trigger[aria-label^='On date']");
        await Expect(until).ToHaveAttributeAsync("aria-label", "On date: 10/16/2025");
        await until.ClickAsync();
        var panel = dialog.Locator(".bit-bfc-dtp-panel");
        for (var i = 0; i < 4; i++)
        {
            await panel.GetByRole(AriaRole.Button, new() { Name = "Previous month" }).ClickAsync();
        }
        await panel.GetByRole(AriaRole.Button, new() { Name = "Sunday, June 15, 2025" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-field-error", new() { HasText = "The series cannot end before it starts" })).ToBeVisibleAsync();
        await Expect(until).ToBeFocusedAsync();

        await repeat.SelectOptionAsync("Monthly");
        var on = dialog.GetByLabel("Repeat on");
        await Expect(on.Locator("option")).ToHaveTextAsync(["Day 16", "The first", "The second", "The third", "The fourth", "The last"]);
        await on.SelectOptionAsync("Third");
        await Expect(dialog.Locator(".bit-bfc-repeat-day[aria-pressed='true']")).ToHaveTextAsync(ServerOrWasm(["W"], ["We"]));
        await repeat.SelectOptionAsync("Yearly");
        await Expect(dialog.GetByLabel("Repeat on").Locator("option").First).ToHaveTextAsync("July 16");

        // single dates are skipped or added
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Skip date" }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-repeat-skipped")).ToContainTextAsync("7/16/2025");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Add date" }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-repeat-skipped")).ToHaveCountAsync(0);
        await Expect(dialog.Locator(".bit-bfc-repeat-added")).ToContainTextAsync("7/16/2025");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Remove date: 7/16/2025" }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-repeat-added")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Recurrence_DraggingAnOccurrence_MovesItAloneOutOfItsSeries()
    {
        await OpenAsync(RecurrencePage);

        await Occurrence("standup", "20250722").DragToAsync(MonthCell(RecCal, "Saturday, July 26, 2025"), new() { TargetPosition = new() { X = 20, Y = 8 } });

        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#changes li").Nth(0)).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Daily Standup\|.*skip:2025-07-21,2025-07-22/"));
        await Expect(Page.Locator("#changes li").Nth(1)).ToHaveTextAsync("Add|Drag|Daily Standup|2025-07-26 09:00|2025-07-26 09:15|none");
        await Expect(Occurrence("standup", "20250722")).ToHaveCountAsync(0);
        await Expect(MonthCell(RecCal, "Saturday, July 26, 2025").Locator(".bit-bfc-event-badge-title", new() { HasText = "Daily Standup" })).ToBeVisibleAsync();
        await Expect(RecCal.Locator(".bit-bfc-body")).ToHaveAttributeAsync("data-bit-bfc-moved-from", "standup@20250722");
    }
}
