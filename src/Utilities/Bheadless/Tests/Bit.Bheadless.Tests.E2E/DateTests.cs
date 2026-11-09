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
}
