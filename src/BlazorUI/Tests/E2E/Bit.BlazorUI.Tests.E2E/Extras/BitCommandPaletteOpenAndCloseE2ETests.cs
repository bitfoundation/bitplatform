namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// What only the native dialog can settle: that BitCommandPalette really is modal - in the top layer, with the page
/// behind it inert and its scrolling held still - that the toggle chords reach it from anywhere, and that closing it
/// gives the focus back to whatever had it.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteOpenAndCloseE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task CtrlK_OpensAModalDialogInTheTopLayer_WithTheFocusInTheInput()
    {
        await OpenPageAsync();

        await OpenPaletteAsync();

        // :modal is only true for a dialog opened with showModal(), which is what puts it in the top layer. The point at
        // the centre of the input must hit the input itself: nothing the page stacks can cover it.
        var probe = await Page.EvaluateAsync<bool[]>(
            """
            () => {
              const dialog = document.querySelector('dialog[data-bit-cpl="dialog"]');
              const input = dialog.querySelector('[data-bit-cpl="input"]');
              const r = input.getBoundingClientRect();
              const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
              return [dialog.matches(':modal'), dialog.open, hit === input, document.activeElement === input];
            }
            """);
        Assert.IsTrue(probe[0], "the dialog is not modal, so it is not in the top layer");
        Assert.IsTrue(probe[1], "the dialog is not open");
        Assert.IsTrue(probe[2], "something covers the palette's input");
        Assert.IsTrue(probe[3], "the input does not have the focus");

        // Esc closes it. (Not the same chord: the page turns the Vim bindings on, under which Ctrl+K inside the palette
        // moves up.)
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => document.querySelector('dialog[data-bit-cpl=\"dialog\"]').open"), "the dialog is hidden but still open");
    }

    [TestMethod]
    public async Task CtrlShiftP_AsTheToggle_OpensIt_AndInsideItPinsTheActiveRowInsteadOfClosing()
    {
        await OpenPageAsync(MainRoute + "?toggle=Mod%2BShift%2BP");

        await Page.Keyboard.PressAsync("Control+Shift+P");
        await ExpectOpenAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        var first = (await ItemIdsAsync())[0];

        // Inside the palette the chord is its own (pin or unpin the active row) and wins over the toggle bound to the
        // same keys, so it must neither close the palette nor reach the page.
        await Page.Keyboard.PressAsync("Control+Shift+P");
        await Expect(GroupHeadings.First).ToHaveTextAsync("Pinned");
        await ExpectFirstItemAsync(first);
        await Expect(Dialog).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Escape_ClosesIt_AndGivesTheFocusBackToTheFieldThatHadIt()
    {
        await OpenPageAsync();
        var field = Page.GetByRole(AriaRole.Textbox, new() { Name = "Typing area", Exact = true });
        await field.FocusAsync();

        // A chord with a modifier is not typing, so it opens the palette from inside a text field.
        await OpenPaletteAsync();
        await Page.Keyboard.PressAsync("Escape");

        await ExpectClosedAsync();
        await Expect(field).ToBeFocusedAsync();
        // The scroll lock goes with the palette.
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow-y", "hidden");
    }

    [TestMethod]
    public async Task TheTrigger_OpensIt_AndGetsTheFocusBackWhenItCloses()
    {
        await OpenPageAsync();
        var trigger = Page.Locator("#cpl-trigger");

        await trigger.ClickAsync();
        await ExpectOpenAsync();
        await Page.Keyboard.PressAsync("Escape");

        await ExpectClosedAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task APageButtonThatOpenedIt_GetsTheFocusBack_EvenWhereAClickDoesNotFocusIt()
    {
        // WebKit, and Firefox on macOS, focus the nearest focusable ancestor of a clicked button rather than the button -
        // the page's <main tabindex="-1"> - so the palette has to know which control it was.
        await OpenPageAsync();
        var button = Page.Locator("#cpl-search-counter");

        await button.ClickAsync();
        await ExpectOpenAsync();
        await Expect(Input).ToHaveValueAsync("counter");
        await Page.Keyboard.PressAsync("Escape");

        await ExpectClosedAsync();
        await Expect(button).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AClickOnTheBackdrop_ClosesIt_AndAClickInsideDoesNot()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        // Inside the dialog: the click lands on its own content, not on the dialog element (the backdrop).
        await Input.ClickAsync();
        await Expect(Dialog).ToBeVisibleAsync();

        // The bottom left corner of the viewport is never under the dialog, so it is the ::backdrop.
        await Page.Mouse.ClickAsync(12, Page.ViewportSize!.Height - 12);
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task ThePageBehindIt_CanBeNeitherFocusedNorClicked()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        // A modal dialog makes the rest of the document inert: focusing a page button from script does nothing, and the
        // point at its centre hits the backdrop (the dialog element) rather than the button.
        var probe = await Page.EvaluateAsync<bool[]>(
            """
            () => {
              const button = document.querySelector('#cpl-mode-commands');
              button.focus();
              const r = button.getBoundingClientRect();
              const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
              return [document.activeElement === button, hit === button || button.contains(hit),
                      document.activeElement?.dataset.bitCpl === 'input'];
            }
            """);
        Assert.IsFalse(probe[0], "a button behind the modal dialog took the focus");
        Assert.IsFalse(probe[1], "a button behind the modal dialog is still hit by the pointer");
        Assert.IsTrue(probe[2], "the input lost the focus");

        // Tab never walks out to the page: the palette claims it (it opens the action panel).
        for (var i = 0; i < 4; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.AreEqual("cpl:input", await FocusedAsync(), $"Tab #{i + 1} moved the focus out of the input");
        }

        await Expect(Dialog).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ThePage_DoesNotScrollWhileItIsOpen()
    {
        await OpenPageAsync();
        await Page.EvaluateAsync("() => { document.body.style.minHeight = '4000px'; window.scrollTo(0, 0); }");
        await OpenPaletteAsync();

        // The core's scroll lock holds the body still while the palette is open.
        await Expect(Page.Locator("body")).ToHaveCSSAsync("overflow-y", "hidden");

        // The wheel over the backdrop: without the lock it would scroll the document under the dialog.
        var viewport = Page.ViewportSize!;
        await Page.Mouse.MoveAsync(12, viewport.Height - 12);
        await Page.Mouse.WheelAsync(0, 800);
        await Page.WaitForTimeoutAsync(300);
        Assert.AreEqual(0d, await Page.EvaluateAsync<double>("() => window.scrollY"), "the page scrolled behind the open palette");

        // The lock, and not the modal dialog by itself, is what holds the page: without it the same wheel over the same
        // backdrop scrolls the document. (Put back at once, so the close below hands back what it took.)
        await Page.EvaluateAsync("() => document.body.style.overflow = ''");
        await Page.Mouse.WheelAsync(0, 800);
        await Page.WaitForFunctionAsync("() => window.scrollY > 0", null, new() { Timeout = 5_000 });
        await Page.EvaluateAsync("() => { document.body.style.overflow = 'hidden'; window.scrollTo(0, 0); }");

        // Closed, the same wheel scrolls: the lock was the palette's, and it is gone with it.
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow-y", "hidden");
        await Page.Mouse.WheelAsync(0, 800);
        await Page.WaitForFunctionAsync("() => window.scrollY > 0", null, new() { Timeout = 5_000 });
    }
}
