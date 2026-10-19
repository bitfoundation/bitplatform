using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// Forms from the keyboard alone: filling one in field by field with Tab, the arrow keys and Space, and sending it
/// with Enter; a field asked to take the focus when the page opens taking it; and an answer locked as read-only
/// staying in the tab order, readable and copyable, while it can no longer be changed.
/// </summary>
[TestClass]
public sealed class KeyboardTests : BormTest
{
    [TestMethod]
    public async Task The_signup_form_can_be_filled_and_sent_from_the_keyboard_alone()
    {
        var page = await OpenAsync("/builder");

        // The page's heading has the focus after navigation, so the first Tab lands in the form.
        await page.Keyboard.PressAsync("Tab");
        await Expect(Control(page, "FullName")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("Ada Lovelace");

        await page.Keyboard.PressAsync("Tab");
        await Expect(Control(page, "Email")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("ada@example.com");

        // A radio group is one tab stop, on its checked option; the arrow keys choose within it.
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.Locator("#borm_AccountKind_0_Personal")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator("#borm_AccountKind_1_Business")).ToBeCheckedAsync();

        // The business fields appeared right after the group, so they are the next tab stops.
        await page.Keyboard.PressAsync("Tab");
        await Expect(Control(page, "CompanyName")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("Analytical Engines");
        await page.Keyboard.PressAsync("Tab");
        await Expect(Control(page, "VatNumber")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("GB123456789");

        await page.Keyboard.PressAsync("Tab");
        await Expect(Control(page, "AcceptTerms")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(Control(page, "AcceptTerms")).ToBeCheckedAsync();

        // Enter in a text box sends the form, the same as the button.
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Control(page, "VatNumber")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Result(page)).ToContainTextAsync("Submitted");
        await Expect(Result(page)).ToContainTextAsync("\"CompanyName\": \"Analytical Engines\"");
        await Expect(Result(page)).ToContainTextAsync("\"VatNumber\": \"GB123456789\"");
        await Expect(Result(page)).ToContainTextAsync("\"AcceptTerms\": true");
    }

    [TestMethod]
    public async Task A_field_marked_to_autofocus_has_the_caret_when_the_page_opens()
    {
        var page = await OpenAsync("/layout");

        // Autofocus() is applied by the component once it is interactive: the HTML attribute alone is only
        // honoured on a full page load, which an interactively rendered form almost never is.
        await Expect(Control(page, "FirstName")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("Ada");
        await Expect(Control(page, "FirstName")).ToHaveValueAsync("Ada");
    }

    [TestMethod]
    public async Task A_read_only_answer_stays_in_the_tab_order_but_cannot_be_changed()
    {
        var page = await OpenAsync("/settled-answers");
        var reference = Control(page, "Reference");

        // Confirming the booking with the keyboard locks the reference and the arrival...
        await Control(page, "Confirmed").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(Control(page, "Confirmed")).ToBeCheckedAsync();
        await Expect(reference).ToHaveAttributeAsync("readonly", "");
        await Expect(Control(page, "Arrival")).ToHaveAttributeAsync("readonly", "");

        // ...without disabling them: they are still reached by Tab and read out, just not editable.
        await Expect(reference).ToBeEnabledAsync();
        await Expect(reference).Not.ToBeEditableAsync();
        await page.Locator("h1").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(reference).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("XYZ");
        await Expect(reference).ToHaveValueAsync("bk-1024");

        // The stylesheet marks the locked box as such.
        Assert.AreEqual("rgb(250, 250, 250)", await ComputedAsync(reference, "background-color"), "the read-only background");

        // Unlocking gives it back.
        await Control(page, "Confirmed").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(reference).ToBeEditableAsync();
    }
}
