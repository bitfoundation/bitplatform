namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette rendered in a page (Inline, at /e2e/command-palette-inline): searched and driven by keyboard in
/// place, next to the modal palette without either running anything twice, and audited by axe-core like the dialog.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteInlineE2ETests : BitCommandPaletteE2ETestBase
{
    private ILocator Inline => Page.Locator("section[data-bit-cpl='inline']");

    private ILocator InlineInput => Inline.Locator("[data-bit-cpl='input']");

    private ILocator InlineItems => Inline.Locator("[data-bit-cpl='item']");

    private ILocator WordWrap => Page.Locator("#cpl-wordwrap");

    private Task<bool> InlineHasFocusAsync() => Inline.EvaluateAsync<bool>("e => e.contains(document.activeElement)");

    [TestMethod]
    public async Task ItIsSearchedAndDrivenInPlace_AndItsSwitchesFollowThePage()
    {
        await OpenPageAsync(InlineRoute);
        await Expect(WordWrap).ToBeCheckedAsync();
        await Expect(InlineItems).ToHaveCountAsync(6);

        await InlineInput.ClickAsync();
        await InlineInput.PressSequentiallyAsync("wrap");
        await Expect(InlineItems).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Enter");

        await Expect(WordWrap).Not.ToBeCheckedAsync();
        await Expect(Inline.Locator("[data-value='setting:wordwrap'] .bit-cpl-toggle")).ToHaveAttributeAsync("data-on", "false");
        await ExpectClosedAsync();
        await Expect(InlineInput).ToBeFocusedAsync();

        // Esc clears the query; nothing closes.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(InlineInput).ToHaveValueAsync("");
        await Expect(InlineItems).ToHaveCountAsync(6);
    }

    [TestMethod]
    public async Task NextToTheModalPalette_EachKeyReachesOneOfThemOnly()
    {
        await OpenPageAsync(InlineRoute);
        await Expect(WordWrap).ToBeCheckedAsync();

        // Ctrl+K from the inline palette's own input opens the modal palette, not the inline one.
        await InlineInput.ClickAsync();
        await OpenPaletteAsync();
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        // A global shortcut runs once: the inline palette claims none, so word wrap flips once, not twice.
        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+z");
        await Expect(WordWrap).Not.ToBeCheckedAsync();
        await Page.WaitForTimeoutAsync(300);
        await Expect(WordWrap).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task TabMovesOn_AndEscapeIsClaimedOnlyWhenThereIsSomethingToUndo()
    {
        await OpenPageAsync(InlineRoute);
        await InlineInput.ClickAsync();

        // Nothing typed, no page to go back from: Esc is the page's, so a drawer around the palette could close on it.
        Assert.IsFalse(await KeyDownAsync("Escape", "Escape"));

        await InlineInput.PressSequentiallyAsync("wrap");
        await Expect(InlineItems).ToHaveCountAsync(1);
        Assert.IsTrue(await KeyDownAsync("Escape", "Escape"));
        await Expect(InlineInput).ToHaveValueAsync("");

        // No focus trap in a page.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#cpl-after")).ToBeFocusedAsync();
        Assert.IsFalse(await InlineHasFocusAsync(), "Tab did not move the focus out of the inline palette");
    }

    [TestMethod]
    public async Task ACharacterTypedWithOptionInAField_IsTyping_NotAShortcut()
    {
        await OpenPageAsync(InlineRoute);
        await Expect(WordWrap).ToBeCheckedAsync();
        await InlineInput.ClickAsync();

        // Option+Z types 'Ω' on a Mac, and Alt+Z is the word-wrap shortcut: in a text field it is the character.
        Assert.IsFalse(await KeyDownAsync("Ω", "KeyZ", alt: true));
        await Page.WaitForTimeoutAsync(300);
        await Expect(WordWrap).ToBeCheckedAsync();

        // Not vacuous: outside a field the same key press is the shortcut.
        Assert.IsTrue(await KeyDownAsync("Ω", "KeyZ", alt: true, selector: "body"));
        await Expect(WordWrap).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task ItPassesAxe()
    {
        await OpenPageAsync(InlineRoute);
        await InlineInput.ClickAsync();
        await InlineInput.PressSequentiallyAsync("theme");
        await Expect(InlineItems).ToHaveCountAsync(3);

        await AssertNoAxeViolationsAsync("section[data-bit-cpl='inline']");
    }
}
