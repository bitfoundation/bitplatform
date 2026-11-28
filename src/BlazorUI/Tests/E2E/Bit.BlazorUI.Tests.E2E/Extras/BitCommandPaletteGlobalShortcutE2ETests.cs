namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette's shortcuts that work with the palette closed, matched by the browser layer from the chords .NET
/// synced to it: sequences, chords, single keys from markup, keys that must stay typing inside a text field, and handlers
/// that answer with UI and so have to open the palette themselves.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteGlobalShortcutE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task GThenS_NavigatesToTheSettings_WithThePaletteClosed()
    {
        await OpenPageAsync();

        await Page.Keyboard.PressAsync("g");
        await Page.Keyboard.PressAsync("s");

        await ExpectUrlAsync("/e2e/command-palette/settings");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Settings");
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task AfterTheFirstKeyOfASequence_ThePageSaysTheNextOneIsAwaited()
    {
        await OpenPageAsync();
        var hint = Page.Locator("[data-bit-cpl='sequence']");
        await Expect(hint).ToHaveAttributeAsync("role", "status");
        await Expect(hint).Not.ToHaveAttributeAsync("data-pending", "true");

        await Page.Keyboard.PressAsync("g");

        await Expect(hint).ToHaveAttributeAsync("data-pending", "true");
        await Expect(hint).ToContainTextAsync("waiting for the next key");
        await Expect(hint).ToBeVisibleAsync();

        // Completed, the sequence runs and the hint goes; so it does when the sequence times out.
        await Page.Keyboard.PressAsync("s");
        await ExpectUrlAsync("/e2e/command-palette/settings");
        await Expect(hint).Not.ToHaveAttributeAsync("data-pending", "true");

        await Page.Keyboard.PressAsync("g");
        await Expect(hint).ToHaveAttributeAsync("data-pending", "true");
        await Expect(hint).Not.ToHaveAttributeAsync("data-pending", "true", new() { Timeout = 5_000 });
    }

    [TestMethod]
    public async Task PlainKeysTypedInAPageField_AreTyping_WhileChordsStillFire()
    {
        await OpenPageAsync();
        var field = Page.GetByRole(AriaRole.Textbox, new() { Name = "Typing area", Exact = true });

        // G, S (the settings sequence) and C (New issue, declared in markup) are all plain keys: in a text box they are
        // letters, and the page stays where it is.
        await field.ClickAsync();
        await Page.Keyboard.TypeAsync("gsc");
        await Expect(field).ToHaveValueAsync("gsc");
        await Page.WaitForTimeoutAsync(300);
        Assert.IsTrue(Page.Url.EndsWith("/e2e/command-palette", StringComparison.Ordinal), $"typing in a text field ran a shortcut and moved to {Page.Url}");
        await Expect(Text("cpl-log")).ToHaveTextAsync("Last action: nothing yet");

        // A chord with a modifier is not typing, so it still opens the palette from the field.
        await OpenPaletteAsync();
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        await Expect(field).ToBeFocusedAsync();

        // Outside the field the same plain keys are shortcuts again (and the test above was not vacuous).
        await field.BlurAsync();
        await Page.Keyboard.PressAsync("g");
        await Page.Keyboard.PressAsync("s");
        await ExpectUrlAsync("/e2e/command-palette/settings");
    }

    [TestMethod]
    public async Task AChordAnEditorClaims_IsTheEditors_AndOpensNoPalette()
    {
        await OpenPageAsync();
        var editor = Page.GetByRole(AriaRole.Textbox, new() { Name = "Link editor", Exact = true });

        // The editor handles Ctrl+K itself and prevents its default: the palette hears the key only after it, and leaves it.
        await editor.ClickAsync();
        await Page.Keyboard.PressAsync("Control+k");
        await Expect(editor).ToHaveValueAsync("[link]");
        await Page.WaitForTimeoutAsync(300);
        await ExpectClosedAsync();
        await Expect(editor).ToBeFocusedAsync();

        // A field that does not bind the chord still opens the palette with it (the test above was not vacuous).
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Typing area", Exact = true }).ClickAsync();
        await OpenPaletteAsync();
    }

    [TestMethod]
    public async Task ASingleKeyShortcutDeclaredInMarkup_RunsItsOption()
    {
        await OpenPageAsync();

        await Page.Keyboard.PressAsync("c");

        await Expect(Text("cpl-log")).ToHaveTextAsync("Last action: New issue dialog opened");
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task F2_RunsRenameProject_AndOpensThePaletteOnItsPrompt()
    {
        await OpenPageAsync();

        // The handler answers with a prompt while the palette is closed: the palette has to open itself to show it.
        await Page.Keyboard.PressAsync("F2");

        await ExpectOpenAsync();
        await Expect(Prompt).ToContainTextAsync("New project name");
        await Expect(Input).ToHaveValueAsync("Acme Dashboard");

        await ReplaceQueryAsync("Acme Dashboard", "Renamed by F2");
        await Page.Keyboard.PressAsync("Enter");
        await ExpectClosedAsync();
        await Expect(Text("cpl-project")).ToHaveTextAsync("Project: Renamed by F2");
    }

    [TestMethod]
    public async Task ModSlash_LandsOnTheKeyboardSection()
    {
        await OpenPageAsync();

        await Page.Keyboard.PressAsync("Control+/");

        await ExpectUrlAsync("/e2e/command-palette/keyboard#shortcuts");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Keyboard and shortcuts");
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task Chords_RunTheirCommands_WithThePaletteClosed()
    {
        await OpenPageAsync();
        var wordWrap = Page.Locator("#cpl-wordwrap");
        await Expect(wordWrap).ToBeCheckedAsync();

        // Alt+Z, registered app-wide. The command answers KeepOpen, which keeps an open palette where it was; with the
        // palette closed - here it has not even been opened once - there is no input to focus, and nothing may try to.
        await Page.Keyboard.PressAsync("Alt+z");
        await Expect(wordWrap).Not.ToBeCheckedAsync();
        await Page.Keyboard.PressAsync("Alt+z");
        await Expect(wordWrap).ToBeCheckedAsync();

        // Ctrl+Up, registered by the page only, while it is on screen.
        await Page.Keyboard.PressAsync("Control+ArrowUp");
        await Expect(Text("cpl-counter")).ToHaveTextAsync("Counter: 1");
        await Page.Keyboard.PressAsync("Control+ArrowUp");
        await Expect(Text("cpl-counter")).ToHaveTextAsync("Counter: 2");
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task WithVimBindings_CtrlKInsideThePalette_MovesUpInsteadOfClosing()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        var ids = await ItemIdsAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(ids[1]);
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(ids[2]);

        // Ctrl+K is the toggle outside; inside, the Vim bindings make it "up", and the palette's own chord wins.
        await Page.Keyboard.PressAsync("Control+k");
        await ExpectActiveAsync(ids[1]);
        await Expect(Dialog).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Control+j");
        await ExpectActiveAsync(ids[2]);

        // Closed again, the same chord is the toggle.
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        await OpenPaletteAsync();
    }
}
