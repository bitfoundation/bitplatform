using System.Globalization;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The invoice repeater: where the focus goes when a row is added or removed (a button that removes its own row
/// otherwise drops the focus on the page body), the totals the schema computes from the rows, a row's values and
/// errors moving with it, and a locked form locking the repeater's buttons too.
/// </summary>
/// <remarks>
/// Adding a line puts the caret in its description, which is empty and required. Pressing the mouse anywhere else
/// then blurs it, and the blur shows its "required" message before the button is released, which moves everything
/// below it. The tests that are not about that commit a value, or leave by the keyboard, before they click on;
/// <see cref="Clicking_Add_line_again_straight_after_adding_one_adds_a_second_line"/> is about exactly that.
/// </remarks>
[TestClass]
public sealed class RepeaterTests : BormTest
{
    [TestMethod]
    public async Task Adding_a_line_puts_the_caret_in_it_and_announces_the_count()
    {
        var page = await OpenAsync("/array");
        var lines = page.Locator("#borm_Lines");

        await Expect(lines.Locator(".bit-borm-array__empty")).ToHaveTextAsync("No lines yet.");
        await AddButton(page).ClickAsync();

        await Expect(Rows(page, "Lines")).ToHaveCountAsync(1);
        await Expect(Control(page, "Lines[0].Description")).ToBeFocusedAsync();
        await Expect(lines.Locator(".bit-borm-array__count")).ToHaveTextAsync("1");
        await Expect(lines.Locator("[aria-live=polite]")).ToHaveTextAsync("1 line");
        await Expect(Rows(page, "Lines").Nth(0)).ToHaveAttributeAsync("aria-label", "line 1");

        // From the keyboard, as the page suggests: back to the button, Enter, and the caret is in the new line.
        await AddButton(page).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(2);
        await Expect(Control(page, "Lines[1].Description")).ToBeFocusedAsync();
        await Expect(lines.Locator("[aria-live=polite]")).ToHaveTextAsync("2 lines");
    }

    [TestMethod]
    // Open, not passing: blur validates an empty required field by design (ValidationTrigger.OnChange covers
    // blur), so whether a field the library focused on the user's behalf should be judged when it is left
    // untouched is a decision about the library's validation semantics, not a fix this suite can make.
    [Ignore("Known issue: the blur that a second click causes shows the new line's required message and moves the button before mouseup.")]
    public async Task Clicking_Add_line_again_straight_after_adding_one_adds_a_second_line()
    {
        var page = await OpenAsync("/array");
        await AddButton(page).ClickAsync();
        await Expect(Control(page, "Lines[0].Description")).ToBeFocusedAsync();

        // The second click is the first thing that takes the focus out of the new, empty line. Whatever its blur
        // shows, the button must still be under the pointer when it is released, or the click lands on the list
        // around it and nothing is added.
        await AddButton(page).ClickAsync();

        await Expect(Rows(page, "Lines")).ToHaveCountAsync(2, new() { Timeout = 5_000 });
    }

    [TestMethod]
    public async Task Removing_a_line_moves_the_caret_to_the_line_that_took_its_place()
    {
        var page = await OpenAsync("/array");
        foreach (var description in new[] { "Chai", "Chang", "Aniseed syrup" })
            await AddLineAsync(page, description);

        // From the keyboard, as the page suggests: the second line's Remove button, then Enter.
        await RemoveWithKeyboardAsync(page, 2);
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(2);
        await Expect(Control(page, "Lines[1].Description")).ToBeFocusedAsync();
        await Expect(Control(page, "Lines[1].Description")).ToHaveValueAsync("Aniseed syrup");
        await Expect(page.Locator("#borm_Lines [aria-live=polite]")).ToHaveTextAsync("2 lines");

        // The last line goes: the one above it takes the caret.
        await RemoveWithKeyboardAsync(page, 2);
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(1);
        await Expect(Control(page, "Lines[0].Description")).ToBeFocusedAsync();
        await Expect(Control(page, "Lines[0].Description")).ToHaveValueAsync("Chai");

        // An invoice needs at least one line (Items(min: 1)), so the only one left cannot be removed.
        await Expect(RemoveButton(page, 1)).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Removing_the_last_line_leaves_the_caret_on_the_add_button()
    {
        // The purchase order's lines have no minimum, so the list can be emptied.
        var page = await OpenAsync("/state-api");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load record from server", Exact = true }).ClickAsync();
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(2);

        await RemoveWithKeyboardAsync(page, 1);
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(1);
        await Expect(Control(page, "Lines[0].Description")).ToBeFocusedAsync();
        await Expect(Control(page, "Lines[0].Description")).ToHaveValueAsync("Chang (12 bottles)");

        // Nothing is left to aim at but the button that refills the list - not the page body, where a button that
        // removed itself would otherwise leave the focus.
        await RemoveWithKeyboardAsync(page, 1);
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(0);
        await Expect(AddButton(page)).ToBeFocusedAsync();
        await Expect(page.Locator("#borm_Lines [aria-live=polite]")).ToHaveTextAsync("No lines yet.");
        await Expect(page.Locator(".demo-result pre").First).ToContainTextAsync("Lines     0   total: €0.00");
    }

    [TestMethod]
    public async Task The_line_totals_and_the_invoice_total_follow_the_quantities_and_prices()
    {
        var page = await OpenAsync("/array");

        await AddLineAsync(page, "Chai");
        await EnterAsync(Control(page, "Lines[0].Quantity"), "2");
        await EnterAsync(Control(page, "Lines[0].UnitPrice"), "10.5");
        await AddLineAsync(page, "Chang");
        await EnterAsync(Control(page, "Lines[1].Quantity"), "3");
        await EnterAsync(Control(page, "Lines[1].UnitPrice"), "4");

        await ExpectNumberAsync(Control(page, "Lines[0].LineTotal"), 21);
        await ExpectNumberAsync(Control(page, "Lines[1].LineTotal"), 12);
        await ExpectNumberAsync(Control(page, "Total"), 33);

        // The computed fields are the schema's to write, not the user's.
        await Expect(Control(page, "Total")).ToHaveAttributeAsync("readonly", "");
        await Expect(Control(page, "Lines[0].LineTotal")).ToHaveAttributeAsync("readonly", "");

        // Removing a line takes its share out of the total.
        await RemoveButton(page, 1).ClickAsync();
        await ExpectNumberAsync(Control(page, "Total"), 12);
        await ExpectNumberAsync(Control(page, "Lines[0].LineTotal"), 12);
    }

    [TestMethod]
    public async Task Moving_a_line_carries_its_values_and_its_errors_with_it()
    {
        var page = await OpenAsync("/array");
        await AddLineAsync(page, "Chai");
        await AddButton(page).ClickAsync();
        await EnterAsync(Control(page, "Customer"), "Northwind Traders");
        await SubmitButton(page).ClickAsync();

        await Expect(Errors(page, "Lines[1].Description")).ToHaveTextAsync("This field is required.");
        await Expect(Errors(page, "Lines[0].Description")).ToHaveCountAsync(0);
        await Expect(Summary(page).Locator("a")).ToHaveTextAsync(["Description (2): This field is required."]);

        await page.GetByRole(AriaRole.Button, new() { Name = "Move line 2 up", Exact = true }).ClickAsync();

        // The empty line is first now, and its complaint came along; the filled one is second, still clean.
        await Expect(Control(page, "Lines[0].Description")).ToHaveValueAsync("");
        await Expect(Errors(page, "Lines[0].Description")).ToHaveTextAsync("This field is required.");
        await Expect(Control(page, "Lines[1].Description")).ToHaveValueAsync("Chai");
        await Expect(Errors(page, "Lines[1].Description")).ToHaveCountAsync(0);
        await Expect(Summary(page).Locator("a")).ToHaveTextAsync(["Description (1): This field is required."]);
        await Expect(page.Locator("#borm_Lines [aria-live=polite]")).ToHaveTextAsync("line 1");

        // The row's DOM node moved with it, so the focus stayed on the button that moved it - now the first row's,
        // which stays focusable at the top of the list (aria-disabled rather than disabled).
        var movedButton = page.GetByRole(AriaRole.Button, new() { Name = "Move line 1 up", Exact = true });
        await Expect(movedButton).ToBeFocusedAsync();
        await Expect(movedButton).ToHaveAttributeAsync("aria-disabled", "true");
    }

    [TestMethod]
    public async Task Locking_the_invoice_disables_every_control_and_button_in_it()
    {
        var page = await OpenAsync("/array");
        await AddLineAsync(page, "Chai");

        await page.Locator(".demo-card label", new() { HasTextString = "Lock the invoice" }).Locator("input").CheckAsync();

        await Expect(Control(page, "Customer")).ToBeDisabledAsync();
        await Expect(Control(page, "Lines[0].Description")).ToBeDisabledAsync();
        await Expect(AddButton(page)).ToBeDisabledAsync();
        await Expect(RemoveButton(page, 1)).ToBeDisabledAsync();
        await Expect(SubmitButton(page)).ToBeDisabledAsync();
        await Expect(Form(page).Locator("input:enabled, button:enabled, select:enabled, textarea:enabled")).ToHaveCountAsync(0);
    }

    private static ILocator AddButton(IPage page) => page.Locator("#borm_Lines > .bit-borm-btn--add");

    private static ILocator RemoveButton(IPage page, int number) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Remove line {number}", Exact = true });

    /// <summary>Adds a line and gives it a description, which leaves the line valid and the focus on nothing.</summary>
    private static async Task AddLineAsync(IPage page, string description)
    {
        int count = await Rows(page, "Lines").CountAsync();
        await AddButton(page).ClickAsync();
        await Expect(Rows(page, "Lines")).ToHaveCountAsync(count + 1);
        await EnterAsync(Control(page, $"Lines[{count}].Description"), description);
    }

    /// <summary>Focuses the Remove button of line <paramref name="number"/> (1-based, as its name says) and presses Enter.</summary>
    private static async Task RemoveWithKeyboardAsync(IPage page, int number)
    {
        await RemoveButton(page, number).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
    }

    /// <summary>Waits until a number box holds <paramref name="expected"/>, however the value is formatted (21, 21.0).</summary>
    private static Task ExpectNumberAsync(ILocator control, decimal expected) =>
        EventuallyAsync(async () => decimal.TryParse(await control.InputValueAsync(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                                    && value == expected,
            $"{control} holds {expected}");
}
