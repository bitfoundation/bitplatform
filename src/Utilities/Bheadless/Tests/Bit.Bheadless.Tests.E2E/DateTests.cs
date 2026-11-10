using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// The date components in a browser: the calendar's roving focus, the date field's typing, and the date picker moving the
/// focus into its calendar and back - with axe-core on the open popover.
/// </summary>
[TestClass]
public sealed class DateTests : DemoTest
{
    [TestMethod]
    public async Task Calendar_arrow_keys_move_the_real_focus_and_page_the_months()
    {
        var page = await OpenDemoAsync("/calendar");
        var calendar = page.Locator($"{Content} [role=group][aria-label=Appointment]");
        var focused = calendar.Locator("td[tabindex='0']");
        await focused.FocusAsync();
        var start = await focused.GetAttributeAsync("data-date");

        await page.Keyboard.PressAsync("ArrowRight");
        var next = DateOnly.Parse(start!).AddDays(1).ToString("yyyy-MM-dd");
        await Expect(calendar.Locator($"td[data-date='{next}']:not([data-outside-month])")).ToBeFocusedAsync();

        var heading = calendar.Locator("h3");
        var month = await heading.InnerTextAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(heading).Not.ToHaveTextAsync(month);
        await Expect(calendar.Locator("td[tabindex='0']")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(calendar.Locator("td[tabindex='0']")).ToHaveAttributeAsync("aria-selected", "true");
        await ExpectAccessibleAsync(page);
    }

    [TestMethod]
    public async Task Date_field_typing_fills_the_segments_and_moves_on()
    {
        var page = await OpenDemoAsync("/date-field");
        var group = page.Locator($"{Content} .date-field[role=group]").First;
        await group.Locator("[aria-label=month]").FocusAsync();
        foreach (var key in new[] { "1", "1", "8", "2", "0", "2", "6" }) await page.Keyboard.PressAsync(key);

        await Expect(group.Locator("[aria-label=year]")).ToBeFocusedAsync();
        await Expect(group.Locator("[aria-label=year]")).ToHaveAttributeAsync("aria-valuenow", "2026");
        await Expect(page.Locator($"{Content} .date-field-value").First).ToContainTextAsync("2026-11-08");

        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(group.Locator("[aria-label=day]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(page.Locator($"{Content} .date-field-value").First).ToContainTextAsync("2026-11-09");
    }

    [TestMethod]
    public async Task Date_picker_moves_the_focus_into_the_calendar_and_back_to_the_button()
    {
        var page = await OpenDemoAsync("/date-picker");
        var trigger = page.Locator($"{Content} button[aria-haspopup=dialog]").First;
        await trigger.ClickAsync();

        var dialog = page.Locator("[role=dialog]:visible").First;
        await Expect(dialog).ToBeVisibleAsync();
        await PollAsync(() => page.EvaluateAsync<bool>("() => document.activeElement?.matches('[role=dialog] td[tabindex=\"0\"]') === true"), true,
            "the focus on the calendar's focused date");
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("Enter");
        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.Locator($"{Content} .date-picker-value").First).Not.ToContainTextAsync("none");

        await trigger.ClickAsync();
        await Expect(page.Locator("[role=dialog]:visible").First).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("[role=dialog]")).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Date_time_field_typing_runs_from_the_date_into_the_time()
    {
        var page = await OpenDemoAsync("/date-field");
        var group = page.Locator(Content).GetByRole(AriaRole.Group, new() { Name = "Meeting", Exact = true });
        await group.Locator("[aria-label=month]").FocusAsync();
        foreach (var key in new[] { "1", "1", "8", "2", "0", "2", "6" }) await page.Keyboard.PressAsync(key);

        // The year is full: the focus is on the hour.
        await Expect(group.Locator("[aria-label=hour]")).ToBeFocusedAsync();
        foreach (var key in new[] { "9", "3", "0", "p" }) await page.Keyboard.PressAsync(key);

        await Expect(group.Locator("[aria-label='AM/PM']")).ToBeFocusedAsync();
        await Expect(group.Locator("[aria-label='AM/PM']")).ToHaveAttributeAsync("aria-valuetext", "PM");
        await Expect(page.Locator($"{Content} .date-field-value").Filter(new() { HasText = "2026-11-08 21:30" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Date_time_picker_picks_a_date_and_keeps_the_typed_time()
    {
        var page = await OpenDemoAsync("/date-picker");
        var group = page.Locator(Content).GetByRole(AriaRole.Group, new() { Name = "Appointment", Exact = true });
        await group.Locator("[aria-label=hour]").FocusAsync();
        foreach (var key in new[] { "9", "3", "0", "p" }) await page.Keyboard.PressAsync(key);
        await Expect(group.Locator("[aria-label='AM/PM']")).ToHaveAttributeAsync("aria-valuetext", "PM");

        var trigger = group.Locator("button[aria-haspopup=dialog]");
        await trigger.ClickAsync();
        var dialog = page.Locator("[role=dialog]:visible").First;
        await Expect(dialog).ToBeVisibleAsync();
        await PollAsync(() => page.EvaluateAsync<bool>("() => document.activeElement?.matches('[role=dialog] td[tabindex=\"0\"]') === true"), true,
            "the focus on the calendar's focused date");
        await ExpectAccessibleAsync(page);

        // The calendar picks the date; the time typed is kept.
        await page.Keyboard.PressAsync("Enter");
        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.Locator($"{Content} .date-time-picker-value")).ToContainTextAsync("9:30 PM");
    }

    [TestMethod]
    public async Task Date_range_picker_types_a_whole_range_across_the_start_and_the_end()
    {
        var page = await OpenDemoAsync("/date-range-picker");
        var start = page.Locator(Content).GetByRole(AriaRole.Group, new() { Name = "Start date Stay", Exact = true });
        var end = page.Locator(Content).GetByRole(AriaRole.Group, new() { Name = "End date Stay", Exact = true });
        await start.Locator("[aria-label=month]").FocusAsync();
        foreach (var key in new[] { "1", "1", "8", "2", "0", "2", "6" }) await page.Keyboard.PressAsync(key);

        // The start is full: the focus runs on into the end.
        await Expect(end.Locator("[aria-label=month]")).ToBeFocusedAsync();
        foreach (var key in new[] { "1", "1", "1", "2", "2", "0", "2", "6" }) await page.Keyboard.PressAsync(key);

        await Expect(end.Locator("[aria-label=year]")).ToHaveAttributeAsync("aria-valuenow", "2026");
        await Expect(start.Locator("[aria-label=year]")).ToHaveAttributeAsync("aria-valuenow", "2026");
        await Expect(page.Locator($"{Content} .date-range-picker-value").First).ToContainTextAsync("Nov 8 – Nov 12, 2026 (4 nights)");

        // Backspace empties the end's month, then goes back to the start.
        await end.Locator("[aria-label=month]").FocusAsync();
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(page.Locator($"{Content} .date-range-picker-value").First).ToContainTextAsync("none");
        await page.Keyboard.PressAsync("Backspace");
        await Expect(start.Locator("[aria-label=year]")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Date_range_picker_picks_two_dates_in_its_calendar_and_returns_the_focus()
    {
        var page = await OpenDemoAsync("/date-range-picker");
        var trigger = page.Locator(Content).GetByRole(AriaRole.Button, new() { Name = "Choose date range Stay", Exact = true });
        await trigger.ClickAsync();

        var dialog = page.Locator("[role=dialog]:visible").First;
        await Expect(dialog).ToBeVisibleAsync();
        await PollAsync(() => page.EvaluateAsync<bool>("() => document.activeElement?.matches('[role=dialog] td[tabindex=\"0\"]') === true"), true,
            "the focus on the calendar's focused date");
        await Expect(dialog.Locator("[data-selection-mode=range]")).ToHaveCountAsync(1);
        await ExpectAccessibleAsync(page);

        // The first press sets one end, the second the other: a week.
        await page.Keyboard.PressAsync("Enter");
        await Expect(dialog.Locator("[data-selection-mode=range]")).ToHaveAttributeAsync("data-picking", "");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.Locator($"{Content} .date-range-picker-value").First).ToContainTextAsync("(7 nights)");

        // Escape drops a half-picked range first, then closes.
        await trigger.ClickAsync();
        await Expect(page.Locator("[role=dialog]:visible").First).ToBeVisibleAsync();
        await PollAsync(() => page.EvaluateAsync<bool>("() => document.activeElement?.matches('[role=dialog] td[tabindex=\"0\"]') === true"), true,
            "the focus on the calendar's focused date");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator("[role=dialog] [data-selection-mode=range]")).ToHaveAttributeAsync("data-picking", "");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("[role=dialog] [data-selection-mode=range]")).Not.ToHaveAttributeAsync("data-picking", "");
        await Expect(page.Locator("[role=dialog]:visible").First).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("[role=dialog]")).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(page.Locator($"{Content} .date-range-picker-value").First).ToContainTextAsync("(7 nights)");
    }
}
