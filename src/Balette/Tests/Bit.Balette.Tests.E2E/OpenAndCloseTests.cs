using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// What only the native dialog can settle: that the palette really is modal - in the top layer, with the page
/// behind it inert and its scrolling locked - that the toggle chords reach it from anywhere, and that closing it
/// gives the focus back to whatever had it.
/// </summary>
[TestClass]
public sealed class OpenAndCloseTests : BaletteTest
{
    [TestMethod]
    public async Task Ctrl_K_opens_a_modal_dialog_in_the_top_layer_with_the_focus_in_the_input()
    {
        var page = await OpenAsync("/");

        await OpenPaletteAsync(page);

        // :modal is only true for a dialog opened with showModal(), which is what puts it in the top layer. The point
        // at the centre of the input must hit the input itself: nothing the page stacks with a z-index of its own (the
        // sticky top bar) can cover it.
        var probe = await page.EvaluateAsync<bool[]>(
            """
            () => {
              const dialog = document.querySelector('dialog[data-balette="dialog"]');
              const input = dialog.querySelector('[data-balette="input"]');
              const r = input.getBoundingClientRect();
              const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
              return [dialog.matches(':modal'), dialog.open, hit === input, document.activeElement === input];
            }
            """);
        Assert.IsTrue(probe[0], "the dialog is not modal, so it is not in the top layer");
        Assert.IsTrue(probe[1], "the dialog is not open");
        Assert.IsTrue(probe[2], "something covers the palette's input");
        Assert.IsTrue(probe[3], "the input does not have the focus");

        // Esc closes it. (Not the same chord: the demo turns the Vim bindings on, under which Ctrl+K inside the
        // palette moves up - see GlobalShortcutTests.)
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);
        Assert.IsFalse(await page.EvaluateAsync<bool>("() => document.querySelector('dialog[data-balette=\"dialog\"]').open"), "the dialog is hidden but still open");
    }

    [TestMethod]
    public async Task Ctrl_Shift_P_opens_it_too_and_inside_it_pins_the_active_row_instead_of_closing()
    {
        var page = await OpenAsync("/");

        await page.Keyboard.PressAsync("Control+Shift+P");
        await ExpectOpenAsync(page);
        var first = (await ItemIdsAsync(page))[0];

        // Inside the palette the chord is its own (pin or unpin the active row) and wins over the toggle bound to the
        // same keys, so it must neither close the palette nor reach the page.
        await page.Keyboard.PressAsync("Control+Shift+P");
        await Assertions.Expect(GroupHeadings(page).First).ToHaveTextAsync("Pinned");
        await ExpectFirstItemAsync(page, first);
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Escape_closes_it_and_gives_the_focus_back_to_the_field_that_had_it()
    {
        var page = await OpenAsync("/search");
        var query = page.GetByRole(AriaRole.Textbox, new() { Name = "Query", Exact = true });
        await query.FocusAsync();

        // A chord with a modifier is not typing, so it opens the palette from inside a text field.
        await OpenPaletteAsync(page);
        await page.Keyboard.PressAsync("Escape");

        await ExpectClosedAsync(page);
        await Assertions.Expect(query).ToBeFocusedAsync();
        Assert.IsFalse(await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('bit-balette-scroll-lock')"),
            "the scroll lock outlived the palette");
    }

    [TestMethod]
    public async Task The_trigger_opens_it_and_gets_the_focus_back_when_it_closes()
    {
        var page = await OpenAsync("/");
        var trigger = page.Locator("[data-balette='trigger']");

        await trigger.ClickAsync();
        await ExpectOpenAsync(page);
        await page.Keyboard.PressAsync("Escape");

        await ExpectClosedAsync(page);
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task A_click_on_the_backdrop_closes_it_and_a_click_inside_does_not()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        // Inside the dialog: the click lands on the dialog's own content, not on the dialog element (the backdrop).
        await Input(page).ClickAsync();
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();

        // The bottom left corner of the viewport is never under the dialog, so it is the ::backdrop.
        var viewport = page.ViewportSize!;
        await page.Mouse.ClickAsync(12, viewport.Height - 12);
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task The_page_behind_it_can_be_neither_focused_nor_clicked()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        // A modal dialog makes the rest of the document inert: focusing a page button from script does nothing, and
        // the point at its centre hits the backdrop (the dialog element) rather than the button.
        var probe = await page.EvaluateAsync<bool[]>(
            """
            () => {
              const button = document.querySelector('.topbar-modes button');
              button.focus();
              const r = button.getBoundingClientRect();
              const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
              return [document.activeElement === button, hit === button || button.contains(hit),
                      document.activeElement?.dataset.balette === 'input'];
            }
            """);
        Assert.IsFalse(probe[0], "a button behind the modal dialog took the focus");
        Assert.IsFalse(probe[1], "a button behind the modal dialog is still hit by the pointer");
        Assert.IsTrue(probe[2], "the input lost the focus");

        // Tab never walks out to the page: the palette claims it (it opens the action panel).
        for (int i = 0; i < 4; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            Assert.AreEqual("balette:input", await FocusedAsync(page), $"Tab #{i + 1} moved the focus out of the input");
        }
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_page_does_not_scroll_while_it_is_open()
    {
        var page = await OpenAsync("/");
        await page.EvaluateAsync("() => { document.body.style.minHeight = '4000px'; window.scrollTo(0, 0); }");
        await OpenPaletteAsync(page);

        var locked = await page.EvaluateAsync<string[]>(
            "() => [String(document.documentElement.classList.contains('bit-balette-scroll-lock')), getComputedStyle(document.documentElement).overflowY]");
        Assert.AreEqual("true", locked[0], "the scroll lock class is not on <html>");
        Assert.AreEqual("hidden", locked[1], "the scroll lock does not hide the overflow");

        // The wheel over the backdrop: without the lock it would scroll the document under the dialog.
        var viewport = page.ViewportSize!;
        await page.Mouse.MoveAsync(12, viewport.Height - 12);
        await page.Mouse.WheelAsync(0, 800);
        await page.WaitForTimeoutAsync(300);
        Assert.AreEqual(0d, await page.EvaluateAsync<double>("() => window.scrollY"), "the page scrolled behind the open palette");

        // The lock, and not the modal dialog by itself, is what holds the page: without the class the same wheel over
        // the same backdrop scrolls the document. (Put back at once, so the close below removes it as it would.)
        await page.EvaluateAsync("() => document.documentElement.classList.remove('bit-balette-scroll-lock')");
        await page.Mouse.WheelAsync(0, 800);
        await page.WaitForFunctionAsync("() => window.scrollY > 0", null, new() { Timeout = 5_000 });
        await page.EvaluateAsync("() => { document.documentElement.classList.add('bit-balette-scroll-lock'); window.scrollTo(0, 0); }");

        // Closed, the same wheel scrolls: the lock was the palette's, and it is gone with it.
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);
        await page.Mouse.WheelAsync(0, 800);
        await page.WaitForFunctionAsync("() => window.scrollY > 0", null, new() { Timeout = 5_000 });
    }
}
