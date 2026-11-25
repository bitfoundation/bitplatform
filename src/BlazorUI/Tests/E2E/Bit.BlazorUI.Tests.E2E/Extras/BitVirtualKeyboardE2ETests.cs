namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitVirtualKeyboard at /e2e/virtual-keyboard in a real browser: typing into its own Value and into an input at
/// the caret, keeping the focus in the input, the events @bind hears, Backspace by code point or emoji, the OS keyboard
/// kept away, Shift and the layers, dead keys, long press, follow focus with a docked keyboard and Escape, the purpose
/// read off the input, a type=number input, the grid of a right-to-left layout, and axe.
/// </summary>
[TestClass]
[E2ERetry]
public class BitVirtualKeyboardE2ETests : E2ETestBase
{
    private const string Route = "/e2e/virtual-keyboard";

    // A key of the layer shown, by its accessible name.
    private ILocator Key(string keyboard, string name) =>
        Page.Locator($"#{keyboard} .bit-vkb-lyr[data-bit-vkb-on] > button[aria-label=\"{name}\"]").First;

    // The keys on the page are the ones the script holds: a press while a new layout is on its way is left alone.
    private Task SyncedAsync(string keyboard) =>
        Page.WaitForFunctionAsync("id => { const r = document.getElementById(id); return r && r.getAttribute('data-bit-vkb-v') === r.getAttribute('data-bit-vkb-sync'); }", keyboard);

    private async Task PressAsync(string keyboard, params string[] names)
    {
        foreach (var name in names)
        {
            await SyncedAsync(keyboard);
            await Key(keyboard, name).ClickAsync();
        }
    }

    private Task<string> ActiveIdAsync() => Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''");

    private Task SetValueAsync(string input, string value, int caret) =>
        Page.EvaluateAsync("({ id, value, caret }) => { const el = document.getElementById(id); el.value = value; el.setSelectionRange(caret, caret); }",
                           new { id = input, value, caret });

    private Task<string> ValueAsync(string input) => Page.EvaluateAsync<string>("id => document.getElementById(id).value", input);

    [TestMethod]
    public async Task ValueMode_TypesIntoTheBoundValue_AndBackspaceDeletes()
    {
        await OpenAsync(Route);

        await PressAsync("vkb-value", "q", "w", "e");
        await Expect(Page.Locator("#value-out")).ToHaveTextAsync("qwe");

        await PressAsync("vkb-value", "Backspace");
        await Expect(Page.Locator("#value-out")).ToHaveTextAsync("qw");

        await AssertNoAxeViolationsAsync("#value");
    }

    [TestMethod]
    public async Task AnInput_IsTypedIntoAtTheCaret_AndKeepsTheFocus()
    {
        await OpenAsync(Route);

        await Page.Locator("#tgt").FocusAsync();
        await SetValueAsync("tgt", "ac", 1);

        await PressAsync("vkb-target", "b");

        Assert.AreEqual("abc", await ValueAsync("tgt"));
        Assert.AreEqual("tgt", await ActiveIdAsync(), "the focus stays in the input");
        await Expect(Page.Locator("#tgt-out")).ToHaveTextAsync("abc");

        await Page.EvaluateAsync("() => document.getElementById('tgt').setSelectionRange(0, 2)");
        await PressAsync("vkb-target", "x");
        Assert.AreEqual("xc", await ValueAsync("tgt"), "the selection is replaced");
        Assert.AreEqual("none", await Page.Locator("#tgt").GetAttributeAsync("inputmode"), "the OS keyboard is kept away");

        await AssertNoAxeViolationsAsync("#target");
    }

    [TestMethod]
    public async Task Backspace_DeletesACodePoint_ButAWholeEmoji()
    {
        await OpenAsync(Route);
        await Page.Locator("#tgt").FocusAsync();

        await SetValueAsync("tgt", "é", 2);
        await PressAsync("vkb-target", "Backspace");
        Assert.AreEqual("e", await ValueAsync("tgt"));

        await Page.EvaluateAsync("() => { const el = document.getElementById('tgt'); el.value = 'a\\u{1F44D}\\u{1F3FD}'; el.setSelectionRange(el.value.length, el.value.length); }");
        await PressAsync("vkb-target", "Backspace");
        Assert.AreEqual("a", await ValueAsync("tgt"));

        await Page.EvaluateAsync("() => { const el = document.getElementById('tgt'); el.value = 'mi\\u{200C}'; el.setSelectionRange(3, 3); }");
        await PressAsync("vkb-target", "Backspace");
        Assert.AreEqual("mi", await ValueAsync("tgt"));
    }

    [TestMethod]
    public async Task Shift_TypesOneCapital_AndADoubleTapLocksIt()
    {
        await OpenAsync(Route);
        await Page.Locator("#tgt").FocusAsync();
        await SetValueAsync("tgt", "", 0);

        await PressAsync("vkb-target", "Shift");
        await Expect(Key("vkb-target", "Shift")).ToHaveAttributeAsync("aria-pressed", "true");
        await PressAsync("vkb-target", "Q", "w");
        Assert.AreEqual("Qw", await ValueAsync("tgt"));

        await Key("vkb-target", "Shift").DblClickAsync();
        await PressAsync("vkb-target", "E", "R");
        Assert.AreEqual("QwER", await ValueAsync("tgt"), "a locked Shift stays");
    }

    [TestMethod]
    public async Task ACommittedInput_GetsOneChange_WhenTheFocusLeaves()
    {
        await OpenAsync(Route);
        await Page.Locator("#btn-target-change").ClickAsync();
        await Page.Locator("#tgt-change").FocusAsync();

        await PressAsync("vkb-change", "h", "i");
        await Expect(Page.Locator("#tgt-change-out")).ToHaveTextAsync("");

        await Page.Locator("#tgt").FocusAsync();
        await Expect(Page.Locator("#tgt-change-out")).ToHaveTextAsync("hi");
        await Expect(Page.Locator("#tgt-change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task FollowFocus_OpensTheDockedKeyboard_AndEscapeClosesIt()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#vkb-follow > .bit-vkb-pnl");
        await Page.Locator("#ff-text").FocusAsync();
        await Expect(panel).ToHaveAttributeAsync("data-bit-vkb-open", "");
        await Expect(Page.Locator("#ff-open")).ToHaveTextAsync("True");
        Assert.AreEqual("none", await Page.Locator("#ff-text").GetAttributeAsync("inputmode"));

        await PressAsync("vkb-follow", "h", "i");
        await Expect(Page.Locator("#ff-text-out")).ToHaveTextAsync("hi");
        Assert.AreEqual("ff-text", await ActiveIdAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(panel).Not.ToHaveAttributeAsync("data-bit-vkb-open", "");
        await Expect(Page.Locator("#ff-open")).ToHaveTextAsync("False");

        await Page.Locator("#ff-plain").FocusAsync();
        await Expect(Page.Locator("#ff-text")).Not.ToHaveAttributeAsync("inputmode", "none");
    }

    [TestMethod]
    public async Task ThePurpose_IsReadOffTheInput()
    {
        await OpenAsync(Route);

        await Page.Locator("#ff-tel").FocusAsync();
        await Expect(Page.Locator("#vkb-follow .bit-vkb-lyr[data-bit-vkb-on] .bit-vkb-hnt").First).ToHaveTextAsync("ABC");

        await Page.Locator("#ff-email").FocusAsync();
        await Expect(Key("vkb-follow", "@")).ToBeVisibleAsync();
        await Expect(Key("vkb-follow", ".com")).ToBeVisibleAsync();

        await Page.Locator("#ff-date").FocusAsync();
        await Expect(Page.Locator("#ff-date")).Not.ToHaveAttributeAsync("inputmode", "none");
    }

    [TestMethod]
    public async Task ANumberInput_TakesADecimalTypedKeyByKey()
    {
        await OpenAsync(Route);

        await Page.Locator("#ff-num").FocusAsync();
        await PressAsync("vkb-follow", "1", ".", "5");

        Assert.AreEqual("1.5", await ValueAsync("ff-num"));
    }

    [TestMethod]
    public async Task ADeadKey_ComposesWithTheNextLetter()
    {
        await OpenAsync(Route);
        await Page.Locator("#de-in").FocusAsync();

        await PressAsync("vkb-de", "^", "e", "^", "x");

        Assert.AreEqual("ê^x", await ValueAsync("de-in"));
    }

    [TestMethod]
    public async Task ALongPress_OffersTheAlternates()
    {
        await OpenAsync(Route);
        await Page.Locator("#cp-in").FocusAsync();

        var e = Key("vkb-cp", "e");
        await e.ScrollIntoViewIfNeededAsync(); // a raw mouse press does not scroll the key into view as a click does
        var box = (await e.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Expect(e).ToHaveAttributeAsync("data-bit-vkb-down", "", new() { Timeout = 5000 });
        await Expect(Page.Locator("#vkb-cp .bit-vkb-pop")).ToHaveAttributeAsync("data-bit-vkb-show", "", new() { Timeout = 5000 });
        await Page.Mouse.UpAsync();

        await Page.Locator("#vkb-cp .bit-vkb-ppk", new() { HasText = "é" }).ClickAsync();
        Assert.AreEqual("é", await ValueAsync("cp-in"));
        Assert.AreEqual("cp-in", await ActiveIdAsync());
    }

    [TestMethod]
    public async Task APersianLayout_KeepsItsGridLeftToRight_AndTypesPersian()
    {
        await OpenAsync(Route);
        await Page.Locator("#fa-in").FocusAsync();

        var zad = (await Key("vkb-fa", "ض").BoundingBoxAsync())!;
        var che = (await Key("vkb-fa", "چ").BoundingBoxAsync())!;
        Assert.IsTrue(zad.X < che.X, "ض stays at the left of its row, as on the hardware");

        await PressAsync("vkb-fa", "س", "ل", "ا", "م");
        Assert.AreEqual("سلام", await ValueAsync("fa-in"));

        await AssertNoAxeViolationsAsync("#rtl");
    }

    [TestMethod]
    public async Task TheContextMenuOfAKey_OpensItsAlternates_WithoutAPointerHold()
    {
        await OpenAsync(Route);
        await Page.Locator("#cp-in").FocusAsync();

        await Key("vkb-cp", "e").DispatchEventAsync("contextmenu");
        await Expect(Page.Locator("#vkb-cp .bit-vkb-pop")).ToHaveAttributeAsync("data-bit-vkb-show", "");

        await Page.Locator("#vkb-cp .bit-vkb-ppk", new() { HasText = "è" }).ClickAsync();
        Assert.AreEqual("è", await ValueAsync("cp-in"));
    }

    [TestMethod]
    public async Task HoldingBackspace_RepeatsUntilItIsLetGo()
    {
        await OpenAsync(Route);
        await Page.Locator("#tgt").FocusAsync();
        await SetValueAsync("tgt", "abcdefghijklmnopqrstuvwxyz", 26);

        var backspace = Key("vkb-target", "Backspace");
        await backspace.ScrollIntoViewIfNeededAsync();
        var box = (await backspace.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.WaitForTimeoutAsync(1000);
        await Page.Mouse.UpAsync();

        var length = (await ValueAsync("tgt")).Length;
        Assert.IsTrue(length < 23, $"the held key repeated ({26 - length} deleted)");

        await Page.WaitForTimeoutAsync(300);
        Assert.AreEqual(length, (await ValueAsync("tgt")).Length, "it stops once let go");
    }

    [TestMethod]
    public async Task Enter_SubmitsTheForm_MovesToTheNextField_OrTypesANewLine()
    {
        await OpenAsync(Route);

        await Page.Locator("#ff-email").FocusAsync();
        await PressAsync("vkb-follow", "Enter");
        await Expect(Page.Locator("#ff-submitted")).ToHaveTextAsync("1");

        await Page.Locator("#ff-text").FocusAsync();
        await PressAsync("vkb-follow", "Next");
        await Expect(Page.Locator("#ff-next")).ToBeFocusedAsync();

        await Page.Locator("#ff-area").FocusAsync();
        await PressAsync("vkb-follow", "a", "Enter", "b");
        Assert.AreEqual("a\nb", await ValueAsync("ff-area"));
    }

    [TestMethod]
    public async Task AKeydownThePageCancels_KeepsTheKeyFromActing()
    {
        await OpenAsync(Route);
        await Page.EvaluateAsync("() => document.getElementById('ff-text').addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === 'Backspace') e.preventDefault(); })");

        await Page.Locator("#ff-text").FocusAsync();
        await PressAsync("vkb-follow", "a", "Backspace", "Next");

        Assert.AreEqual("a", await ValueAsync("ff-text"), "Backspace was cancelled");
        await Expect(Page.Locator("#ff-text")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task CapsLock_UpperCasesTheLettersOnly_AndAltGrTypesTheThirdCharacter()
    {
        await OpenAsync(Route);
        await Page.Locator("#de-in").FocusAsync();

        await PressAsync("vkb-de", "Caps lock");
        await Expect(Key("vkb-de", "Caps lock")).ToHaveAttributeAsync("aria-pressed", "true");
        await PressAsync("vkb-de", "Q", "1");
        await PressAsync("vkb-de", "Caps lock");

        await PressAsync("vkb-de", "AltGr");
        await PressAsync("vkb-de", "@");
        await PressAsync("vkb-de", "q");

        Assert.AreEqual("Q1@q", await ValueAsync("de-in"), "AltGr is let go after one character");
    }

    [TestMethod]
    public async Task APasswordInput_ShowsNothingOfAPress()
    {
        await OpenAsync(Route);
        await Page.Locator("#ff-pass").FocusAsync();
        await Expect(Page.Locator("#vkb-follow")).ToHaveAttributeAsync("data-bit-vkb-sec", "");

        var key = Key("vkb-follow", "q");
        await key.ScrollIntoViewIfNeededAsync();
        var box = (await key.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Expect(key).Not.ToHaveAttributeAsync("data-bit-vkb-down", "");
        await Expect(Page.Locator("#vkb-follow .bit-vkb-prv")).Not.ToHaveAttributeAsync("data-bit-vkb-show", "");
        await Page.Mouse.UpAsync();

        Assert.AreEqual("q", await ValueAsync("ff-pass"));
    }

    [TestMethod]
    public async Task TypingIntoItsValue_TheKeysAreAGridTheArrowsMoveThrough()
    {
        await OpenAsync(Route);

        var first = Page.Locator("#vkb-value .bit-vkb-lyr[data-bit-vkb-on] > button[tabindex='0']");
        await Expect(first).ToHaveCountAsync(1);
        await first.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Key("vkb-value", "1")).ToBeFocusedAsync();
        await Expect(Key("vkb-value", "1")).ToHaveAttributeAsync("tabindex", "0");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#value-out")).ToHaveTextAsync("q");
    }

    [TestMethod]
    public async Task AnOpenDockedKeyboard_KeepsItsHeightInThePage()
    {
        await OpenAsync(Route);

        await Page.Locator("#ff-text").FocusAsync();
        await Expect(Page.Locator("#vkb-follow > .bit-vkb-pnl")).ToHaveAttributeAsync("data-bit-vkb-open", "");

        var height = await Page.Locator("#vkb-follow > .bit-vkb-rsv").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var panel = await Page.Locator("#vkb-follow > .bit-vkb-pnl").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        Assert.IsTrue(height > 100 && Math.Abs(height - panel) < 2, $"the spacer ({height}) holds the panel's height ({panel})");

        await Page.Locator("#ff-plain").FocusAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#vkb-follow > .bit-vkb-rsv').getBoundingClientRect().height === 0");
    }
}