using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The templates of BitFullCalendar (/e2e/full-calendar/templates): the event content of the day, week and month views,
/// the extra content of a month day and the cell it is handed, an agenda row, and the app's own part of the details and
/// the add/edit dialogs - a location kept in Data that the editor's draft saves, with its field's Escape its own.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string TemplatesPage = "/e2e/full-calendar/templates";

    private ILocator Tpl => Page.Locator("#tpl");

    [TestMethod]
    public async Task Templates_TheMonthEventAndCellTemplates_DrawTheirContent()
    {
        await OpenAsync(TemplatesPage);

        var badge = Badge(Tpl, "p1");
        await Expect(badge.Locator(".month-badge")).ToHaveTextAsync("Pin: Planning");
        await Expect(badge.Locator(".bit-bfc-event-badge-title")).ToHaveCountAsync(0);
        // the calendar keeps the badge's name, color and drag
        await Expect(badge).ToHaveAttributeAsync("aria-label", "Planning, 7/16/2025 09:00 - 7/16/2025 10:00");
        await Expect(badge).ToHaveAttributeAsync("draggable", "true");
        await Expect(badge).ToHaveAttributeAsync("style", new Regex("--bit-bfc-evt-color"));

        await Expect(MonthCell(Tpl, "Sunday, July 20, 2025").Locator(".bit-bfc-month-cell-content .holiday")).ToHaveTextAsync("Holiday: Founders' Day");
        await Expect(MonthCell(Tpl, "Sunday, July 27, 2025").Locator(".holiday")).ToHaveTextAsync("Holiday: Public holiday");
        await Expect(MonthCell(Tpl, "Monday, July 21, 2025").Locator(".holiday")).ToHaveCountAsync(0);
        // the cell it is handed: the day number, whether it belongs to the month, the date
        var borrowed = Tpl.Locator(".cell-info[data-date='2025-06-29']");
        await Expect(borrowed).ToHaveAttributeAsync("data-day", "29");
        await Expect(borrowed).ToHaveAttributeAsync("data-current", "False");
        await Expect(Tpl.Locator(".cell-info[data-date='2025-07-16']")).ToHaveAttributeAsync("data-current", "True");
    }

    [TestMethod]
    [DataRow("Week")]
    [DataRow("Day")]
    public async Task Templates_TheDayAndWeekTemplates_ReplaceTheBlockContent_KeepingItsBehaviour(string view)
    {
        await OpenAsync(TemplatesPage);
        await SwitchViewAsync(Tpl, view);

        var block = Block(Tpl, "p1");
        await Expect(block.Locator(".event-card-title")).ToHaveTextAsync("Planning");
        await Expect(block.Locator(".event-card-desc")).ToHaveTextAsync("Quarter goals.");
        await Expect(block.Locator(".bit-bfc-event-block-title")).ToHaveCountAsync(0);
        await Expect(block).ToHaveAttributeAsync("aria-label", "Planning, 09:00 - 10:00");
        await Expect(block.Locator(".bit-bfc-resize-handle")).ToHaveCountAsync(2);

        await block.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(Page.Locator("#tpl-change")).ToHaveTextAsync("Edit|Drag|Planning|Room 4B|old:Room 4B");
    }

    [TestMethod]
    public async Task Templates_TheAgendaRowTemplate_IsTheRowsContent_AndTheRowStillOpensTheEvent()
    {
        await OpenAsync(TemplatesPage);
        await SwitchViewAsync(Tpl, "Agenda");

        var row = Tpl.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='p1']");
        await Expect(row.Locator(".agenda-row")).ToContainTextAsync("Planning");
        await Expect(row.Locator(".agenda-row-time")).ToHaveTextAsync("09:00");
        await Expect(row.Locator(".bit-bfc-agenda-title")).ToHaveCountAsync(0);

        await row.ClickAsync();
        await Expect(Dialog(Tpl).Locator("h3")).ToHaveTextAsync("Planning");
    }

    [TestMethod]
    public async Task Templates_TheDetailsTemplate_AddsTheAppsRowToTheDetails()
    {
        await OpenAsync(TemplatesPage);

        await Badge(Tpl, "p1").ClickAsync();
        await Expect(Dialog(Tpl).Locator(".bit-bfc-dialog-custom .location-row")).ToHaveTextAsync("Location: Room 4B");
        await PressEscapeAsync();

        await Badge(Tpl, "p2").ClickAsync();
        await Expect(Dialog(Tpl).Locator(".location-row")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Templates_TheEditorTemplate_EditsTheDraft_WhichOnlyASaveCommits()
    {
        await OpenAsync(TemplatesPage);
        await Badge(Tpl, "p1").ClickAsync();
        await Dialog(Tpl).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        var edit = EditDialog(Tpl);
        var location = edit.Locator(".bit-bfc-dialog-custom input");
        await Expect(location).ToHaveValueAsync("Room 4B");

        // Cancel leaves the event as it was
        await location.FillAsync("Nowhere");
        await location.PressAsync("Tab");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(Dialog(Tpl).Locator(".location-row")).ToHaveTextAsync("Location: Room 4B");
        await Expect(Page.Locator("#tpl-change")).ToHaveTextAsync("");

        await Dialog(Tpl).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await location.FillAsync("Room 9");
        await location.PressAsync("Tab");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
        await Expect(Page.Locator("#tpl-change")).ToHaveTextAsync("Edit|Dialog|Planning|Room 9|old:Room 4B");

        await Badge(Tpl, "p1").ClickAsync();
        await Expect(Dialog(Tpl).Locator(".location-row")).ToHaveTextAsync("Location: Room 9");
    }

    [TestMethod]
    public async Task Templates_AnEscapeAFieldOfTheEditorClaims_ClearsTheField_AndOnlyTheNextClosesTheDialog()
    {
        await OpenAsync(TemplatesPage);
        await Tpl.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(Tpl);
        var location = dialog.Locator(".bit-bfc-dialog-custom input");

        await location.FillAsync("Rooftop");
        await Expect(location).ToHaveAttributeAsync("data-bit-esc", "claim");
        await PressEscapeAsync();

        await Expect(location).ToHaveValueAsync("");
        await Expect(dialog).ToBeVisibleAsync();

        await PressEscapeAsync();
        await Expect(dialog).ToHaveCountAsync(0);
    }
}
