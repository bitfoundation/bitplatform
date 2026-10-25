using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// The palette rendered in a page (the settings section of /commands): searched and driven by keyboard in place, next to
/// the layout's modal palette without either running anything twice, and audited by axe-core like the dialog.
/// </summary>
[TestClass]
public sealed class InlinePaletteTests : BaletteTest
{
    private static ILocator Inline(IPage page) => page.Locator("section[data-balette='inline']");

    private static ILocator InlineInput(IPage page) => Inline(page).Locator("[data-balette='input']");

    private static ILocator WordWrap(IPage page) =>
        page.Locator("label.demo-setting", new() { HasTextString = "Word wrap" }).Locator("input[type=checkbox]");

    /// <summary>Dispatches a keydown on the focused element (or <paramref name="selector"/>) and says whether it was claimed.</summary>
    private static Task<bool> KeyDownAsync(IPage page, string key, string code = "", bool alt = false, string? selector = null)
        => page.EvaluateAsync<bool>(
            """
            ([key, code, alt, selector]) => {
              const target = selector ? document.querySelector(selector) : (document.activeElement ?? document.body);
              const e = new KeyboardEvent('keydown', { key, code, altKey: alt, bubbles: true, cancelable: true });
              target.dispatchEvent(e);
              return e.defaultPrevented;
            }
            """,
            new object?[] { key, code, alt, selector });

    private static Task<bool> InlineHasFocusAsync(IPage page) =>
        page.EvaluateAsync<bool>("() => document.querySelector(\"section[data-balette='inline']\").contains(document.activeElement)");

    [TestMethod]
    public async Task It_is_searched_and_driven_in_place_and_its_switches_follow_the_page()
    {
        var page = await OpenAsync("/commands");
        await Assertions.Expect(WordWrap(page)).ToBeCheckedAsync();
        await Assertions.Expect(Inline(page).Locator("[data-balette='item']")).ToHaveCountAsync(6);

        await InlineInput(page).ClickAsync();
        await InlineInput(page).PressSequentiallyAsync("wrap");
        await Assertions.Expect(Inline(page).Locator("[data-balette='item']")).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(WordWrap(page)).Not.ToBeCheckedAsync();
        await Assertions.Expect(Inline(page).Locator("[data-value='setting:wordwrap'] .bit-balette-toggle")).ToHaveAttributeAsync("data-on", "false");
        await ExpectClosedAsync(page);
        await Assertions.Expect(InlineInput(page)).ToBeFocusedAsync();

        // Esc clears the query; nothing closes.
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(InlineInput(page)).ToHaveValueAsync("");
        await Assertions.Expect(Inline(page).Locator("[data-balette='item']")).ToHaveCountAsync(6);
    }

    [TestMethod]
    public async Task Next_to_the_modal_palette_each_key_reaches_one_of_them_only()
    {
        var page = await OpenAsync("/commands");
        await Assertions.Expect(WordWrap(page)).ToBeCheckedAsync();

        // Ctrl+K from the inline palette's own input opens the layout's modal palette, not the inline one.
        await InlineInput(page).ClickAsync();
        await OpenPaletteAsync(page);
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);

        // A global shortcut runs once: the inline palette claims none, so word wrap flips once, not twice.
        await page.Locator("h1").ClickAsync();
        await page.Keyboard.PressAsync("Alt+z");
        await Assertions.Expect(WordWrap(page)).Not.ToBeCheckedAsync();
        await page.WaitForTimeoutAsync(300);
        await Assertions.Expect(WordWrap(page)).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Tab_moves_on_and_Escape_is_claimed_only_when_there_is_something_to_undo()
    {
        var page = await OpenAsync("/commands");
        await InlineInput(page).ClickAsync();

        // Nothing typed, no page to go back from: Esc is the page's, so a drawer around the palette could close on it.
        Assert.IsFalse(await KeyDownAsync(page, "Escape"));

        await InlineInput(page).PressSequentiallyAsync("wrap");
        Assert.IsTrue(await KeyDownAsync(page, "Escape"));
        await Assertions.Expect(InlineInput(page)).ToHaveValueAsync("");

        // No focus trap in a page.
        await page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await InlineHasFocusAsync(page), "Tab did not move the focus out of the inline palette");
    }

    [TestMethod]
    public async Task A_character_typed_with_Option_in_a_field_is_typing_not_a_shortcut()
    {
        var page = await OpenAsync("/commands");
        await Assertions.Expect(WordWrap(page)).ToBeCheckedAsync();
        await InlineInput(page).ClickAsync();

        // Option+Z types 'Ω' on a Mac, and Alt+Z is the word-wrap shortcut: in a text field it is the character.
        Assert.IsFalse(await KeyDownAsync(page, "Ω", "KeyZ", alt: true));
        await page.WaitForTimeoutAsync(300);
        await Assertions.Expect(WordWrap(page)).ToBeCheckedAsync();

        // Not vacuous: outside a field the same key press is the shortcut.
        Assert.IsTrue(await KeyDownAsync(page, "Ω", "KeyZ", alt: true, selector: "body"));
        await Assertions.Expect(WordWrap(page)).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task It_passes_axe()
    {
        var page = await OpenAsync("/commands");
        await InlineInput(page).ClickAsync();
        await InlineInput(page).PressSequentiallyAsync("theme");
        await Assertions.Expect(Inline(page).Locator("[data-balette='item']")).ToHaveCountAsync(3);

        var violations = await Axe.ViolationsAsync(Inline(page));
        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }
}
