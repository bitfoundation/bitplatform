using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// Shortcuts that work with the palette closed, matched by the browser layer from the chords .NET synced to it:
/// sequences, chords, single keys from markup, keys that must stay typing inside a text field, and handlers that
/// answer with UI and so have to open the palette themselves.
/// </summary>
[TestClass]
public sealed class GlobalShortcutTests : BaletteTest
{
    [TestMethod]
    public async Task G_then_S_navigates_to_the_settings_with_the_palette_closed()
    {
        var page = await OpenAsync("/");

        await page.Keyboard.PressAsync("g");
        await page.Keyboard.PressAsync("s");

        await page.WaitForURLAsync("**/commands#settings");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Registering commands");
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task Plain_keys_typed_in_a_page_field_are_typing_while_chords_still_fire()
    {
        var page = await OpenAsync("/keyboard");
        var field = page.GetByRole(AriaRole.Textbox, new() { Name = "Typing area", Exact = true });

        // G, S (the settings sequence) and C (New issue, declared in markup) are all plain keys: in a text box they
        // are letters, and the page stays where it is.
        await field.ClickAsync();
        await page.Keyboard.TypeAsync("gsc");
        await Assertions.Expect(field).ToHaveValueAsync("gsc");
        Assert.IsTrue(page.Url.EndsWith("/keyboard", StringComparison.Ordinal), $"typing in a text field ran a shortcut and moved to {page.Url}");
        await Assertions.Expect(page.Locator(".statusbar")).ToContainTextAsync("nothing yet");

        // A chord with a modifier is not typing, so it still opens the palette from the field.
        await OpenPaletteAsync(page);
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);
        await Assertions.Expect(field).ToBeFocusedAsync();

        // Outside the field the same plain keys are shortcuts again (and the test above was not vacuous).
        await field.BlurAsync();
        await page.Keyboard.PressAsync("g");
        await page.Keyboard.PressAsync("s");
        await page.WaitForURLAsync("**/commands#settings");
    }

    [TestMethod]
    public async Task A_single_key_shortcut_declared_in_markup_runs_its_item()
    {
        var page = await OpenAsync("/");

        await page.Keyboard.PressAsync("c");

        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("New issue dialog opened");
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task F2_runs_Rename_project_and_opens_the_palette_on_its_prompt()
    {
        var page = await OpenAsync("/");

        // The handler answers with a prompt while the palette is closed: the palette has to open itself to show it,
        // rather than pushing the prompt onto a page nobody can see.
        await page.Keyboard.PressAsync("F2");

        await ExpectOpenAsync(page);
        await Assertions.Expect(Prompt(page)).ToContainTextAsync("New project name");
        await Assertions.Expect(Input(page)).ToHaveValueAsync("Acme Dashboard");

        await Input(page).FillAsync("Renamed by F2");
        await page.Keyboard.PressAsync("Enter");
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Project: Renamed by F2");
    }

    [TestMethod]
    public async Task Mod_slash_lands_on_the_keyboard_page()
    {
        var page = await OpenAsync("/");

        await page.Keyboard.PressAsync("Control+/");

        await page.WaitForURLAsync("**/keyboard#shortcuts");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Keyboard & shortcuts");
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task Chords_run_their_commands_with_the_palette_closed()
    {
        var page = await OpenAsync("/commands");
        var wordWrap = page.Locator("label.setting", new() { HasTextString = "Word wrap" }).Locator("input[type=checkbox]");
        await Assertions.Expect(wordWrap).ToBeCheckedAsync();

        // Alt+Z, registered app-wide by the layout. Both commands answer KeepOpen, which keeps an open palette where it
        // was; with the palette closed - here it has not even been opened once since the page loaded - there is no
        // input to focus, and nothing may try to (a failure to do so is a fatal page error; see BaletteTest).
        await page.Keyboard.PressAsync("Alt+z");
        await Assertions.Expect(wordWrap).Not.ToBeCheckedAsync();
        await page.Keyboard.PressAsync("Alt+z");
        await Assertions.Expect(wordWrap).ToBeCheckedAsync();

        // Ctrl+Up, registered by this page only, while it is on screen.
        await page.Keyboard.PressAsync("Control+ArrowUp");
        await page.Keyboard.PressAsync("Control+ArrowUp");
        await Assertions.Expect(page.Locator(".counter")).ToHaveTextAsync("Counter: 2");
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task With_Vim_bindings_Ctrl_K_inside_the_palette_moves_up_instead_of_closing()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        var ids = await ItemIdsAsync(page);

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(page, ids[2]);

        // Ctrl+K is the toggle outside; inside, the demo's Vim bindings make it "up", and the palette's own chord wins.
        await page.Keyboard.PressAsync("Control+k");
        await ExpectActiveAsync(page, ids[1]);
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Control+j");
        await ExpectActiveAsync(page, ids[2]);

        // Closed again, the same chord is the toggle.
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);
        await OpenPaletteAsync(page);
    }
}
