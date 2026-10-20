using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// This site has two palettes: Balette, which this demo is about and which answers to Ctrl+K, and the site's own
/// documentation search in the header (the site kit's CommandPalette), which answers to "/". The keys must never
/// reach both: each key opens the one it belongs to, and whichever palette is open owns the keyboard - typing in
/// it included - while the other stays shut.
/// </summary>
[TestClass]
public sealed class TwoPalettesTests : BaletteTest
{
    [TestMethod]
    public async Task Ctrl_K_opens_Balette_and_slash_opens_the_docs_search_never_both()
    {
        var page = await OpenAsync("/getting-started");

        await OpenPaletteAsync(page);
        await Assertions.Expect(SiteSearch(page)).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);

        await page.Locator("h1").ClickAsync();
        await page.Keyboard.PressAsync("/");
        await Assertions.Expect(SiteSearch(page)).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bb-palette-input input")).ToBeFocusedAsync();
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task While_the_docs_search_is_open_Balettes_shortcuts_stay_quiet_and_its_keys_still_work()
    {
        var page = await OpenAsync("/commands");
        var search = page.Locator(".bb-palette-input input");

        await page.Locator("h1").ClickAsync();
        await page.Keyboard.PressAsync("/");
        await Assertions.Expect(search).ToBeFocusedAsync();

        // Balette's toggle and one of its chords, pressed in the docs search: neither opens Balette on top of it,
        // and the page-scoped Ctrl+Up behind it does not run either.
        await page.Keyboard.PressAsync("Control+k");
        await page.Keyboard.PressAsync("Control+Shift+P");
        await page.Keyboard.PressAsync("Control+ArrowUp");
        await page.WaitForTimeoutAsync(300);
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator(".demo-counter")).ToHaveTextAsync("Counter: 0");

        // ...while the docs search keeps every key of its own: typing (a "g s" that is not a sequence here), the
        // arrows that move its cursor, and Enter, which opens the page under it.
        await search.PressSequentiallyAsync("g s");
        await Assertions.Expect(search).ToHaveValueAsync("g s");
        Assert.IsTrue(page.Url.EndsWith("/commands", StringComparison.Ordinal), $"typing in the docs search ran a Balette shortcut and moved to {page.Url}");

        await search.FillAsync("");
        await search.PressSequentiallyAsync("keyboard");
        var active = page.Locator(".bb-palette-hit.bb-active .bb-palette-hit-url");
        await Assertions.Expect(active).ToHaveTextAsync("/keyboard");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForURLAsync("**/keyboard");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Keyboard & shortcuts");
        await Assertions.Expect(SiteSearch(page)).ToHaveCountAsync(0);

        // Closed, the same chord is Balette's again.
        await OpenPaletteAsync(page);
    }

    [TestMethod]
    public async Task While_Balette_is_open_a_slash_is_typed_into_it_and_never_opens_the_docs_search()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "/");
        await Assertions.Expect(Input(page)).ToHaveValueAsync("/");

        // Even with the focus on one of the dialog's own buttons rather than its input.
        await Input(page).FillAsync("");
        await TypeAsync(page, "theme");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Theme"]);
        await page.Locator("dialog[data-balette='dialog'] .bit-balette-crumb-home").FocusAsync();
        await page.Keyboard.PressAsync("/");
        await page.WaitForTimeoutAsync(300);

        await Assertions.Expect(SiteSearch(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
    }
}
