using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// The multi-step flows of the demo, driven with the keyboard end to end and checked by what they do to the app
/// around the palette: a theme on the page, a renamed project, labels, a deleted project, a pin and a recent that
/// survive closing the palette and reloading the page.
/// </summary>
[TestClass]
public sealed class FlowTests : BaletteTest
{
    [TestMethod]
    public async Task A_nested_page_has_breadcrumbs_and_its_choice_applies_to_the_page()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "theme");
        await ExpectFirstItemAsync(page, "preferences.theme");

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Theme"]);
        Assert.AreEqual("theme:auto,theme:light,theme:dark", string.Join(",", await ItemIdsAsync(page)));
        await Assertions.Expect(Item(page, "theme:auto").Locator(".bit-balette-toggle")).ToHaveAttributeAsync("data-on", "true");

        // The option's handler answers Back: the theme is set on <html>, and the palette lands on the list again.
        await TypeAsync(page, "dark");
        await ExpectFirstItemAsync(page, "theme:dark");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_lazy_page_shows_its_loading_state_and_then_its_items()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "assign");
        await ExpectFirstItemAsync(page, "actions.assign-to");

        await page.Keyboard.PressAsync("Enter");

        // The page opens at once, with the loading state, and fills in when the provider returns 400 ms later.
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Assign to…"]);
        await Assertions.Expect(page.Locator("[data-balette='loading']")).ToBeVisibleAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(10);
        await Assertions.Expect(page.Locator("[data-balette='loading']")).ToHaveCountAsync(0);
        await Assertions.Expect(Input(page)).ToHaveAttributeAsync("placeholder", "Pick an assignee…");
        await Assertions.Expect(Input(page)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task The_rename_prompt_is_prefilled_and_its_value_renames_the_project()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "rename");
        await ExpectFirstItemAsync(page, "actions.rename-project");

        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Prompt(page)).ToContainTextAsync("New project name");
        await Assertions.Expect(Input(page)).ToHaveValueAsync("Acme Dashboard");
        await Assertions.Expect(Input(page)).ToBeFocusedAsync();
        // The caret is at the end of the current name, ready to edit it rather than replace it by accident.
        Assert.AreEqual("14,14", await page.EvaluateAsync<string>("() => { const i = document.querySelector('[data-balette=\"input\"]'); return i.selectionStart + ',' + i.selectionEnd; }"));

        await Input(page).FillAsync("Zeta Board");
        await page.Keyboard.PressAsync("Enter");

        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Project: Zeta Board");
    }

    [TestMethod]
    public async Task The_toast_of_a_handler_that_closes_the_palette_is_shown_before_it_closes()
    {
        var page = await OpenAsync("/");
        await page.Keyboard.PressAsync("F2");
        await ExpectOpenAsync(page);
        await Assertions.Expect(Prompt(page)).ToContainTextAsync("New project name");

        // The prompt answers with a toast that closes the palette: the message is on screen for the 900 ms before the
        // close, so the reader sees what happened. Nothing else re-renders the palette in that window here - no async
        // source is pending on a prompt page - so this is the toast's own render or none at all.
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Renamed to “Acme Dashboard”", new() { Timeout = 5_000 });
        await ExpectClosedAsync(page);
    }

    [TestMethod]
    public async Task A_multi_select_page_toggles_with_Enter_and_Space_and_applies_with_Ctrl_Enter()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "set labels");
        await ExpectFirstItemAsync(page, "actions.set-labels");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Dialog(page)).ToHaveAttributeAsync("data-multiselect", "true");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(5);

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Item(page, "label:bug")).ToHaveAttributeAsync("data-checked", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync(" ");
        await Assertions.Expect(Item(page, "label:enhancement")).ToHaveAttributeAsync("data-checked", "true");
        await page.Keyboard.PressAsync(" ");
        await Assertions.Expect(Item(page, "label:enhancement")).Not.ToHaveAttributeAsync("data-checked", "true");
        await page.Keyboard.PressAsync(" ");
        await Assertions.Expect(Item(page, "label:enhancement")).ToHaveAttributeAsync("data-checked", "true");

        // Space toggled rows instead of typing spaces, and the box says how many are checked.
        await Assertions.Expect(Input(page)).ToHaveValueAsync("");
        await Assertions.Expect(page.Locator("[data-balette='selected-count']")).ToHaveTextAsync("2 selected");
        await Assertions.Expect(Item(page, "label:bug")).ToHaveAttributeAsync("aria-checked", "true");

        await page.Keyboard.PressAsync("Control+Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("2 labels applied");
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Labels: bug, enhancement");
    }

    [TestMethod]
    public async Task A_destructive_command_needs_a_second_Enter_and_Escape_disarms_it()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "delete project");
        await ExpectFirstItemAsync(page, "danger-zone.delete-project");
        var row = Item(page, "danger-zone.delete-project");

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(row).ToHaveAttributeAsync("data-confirming", "true");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Confirm: This permanently deletes the project");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(row).Not.ToHaveAttributeAsync("data-confirming", "true");
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("main")).Not.ToContainTextAsync("Project deleted");

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(row).ToHaveAttributeAsync("data-confirming", "true");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Project deleted");
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Project deleted (not really)");
    }

    [TestMethod]
    public async Task Pin_to_top_from_the_action_panel_lists_the_command_under_Pinned_when_reopened()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "nuget");
        await ExpectFirstItemAsync(page, "navigation.nuget-package");

        await page.Keyboard.PressAsync("Tab");
        await ExpectFirstItemAsync(page, "__pin:navigation.nuget-package");
        await Assertions.Expect(Items(page).First).ToContainTextAsync("Pin to top");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(0);

        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);
        await OpenPaletteAsync(page);

        await Assertions.Expect(GroupHeadings(page).First).ToHaveTextAsync("Pinned");
        await ExpectFirstItemAsync(page, "navigation.nuget-package");
        await Assertions.Expect(Items(page).First.Locator(".bit-balette-pin")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task A_recent_command_survives_a_reload()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "copy share");
        await ExpectFirstItemAsync(page, "actions.copy-share-link");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Link copied to clipboard");
        await ExpectClosedAsync(page);

        // The run is recorded in localStorage without being awaited; reload only once it is there.
        await page.WaitForFunctionAsync("() => (localStorage.getItem('bit-balette.history') ?? '').includes('actions.copy-share-link')",
            null, new() { Timeout = 10_000 });
        await ReloadAsync(page);

        await OpenPaletteAsync(page);
        await Assertions.Expect(GroupHeadings(page).First).ToHaveTextAsync("Recent");
        await ExpectFirstItemAsync(page, "actions.copy-share-link");
    }

    [TestMethod]
    public async Task A_toast_shows_in_the_footer_and_a_disabled_row_explains_itself()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "what's new");
        await ExpectFirstItemAsync(page, "help.what-s-new");

        // A toast that keeps the palette open replaces the footer's hints, then goes after a few seconds.
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("You are on the latest version");
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
        await Assertions.Expect(Toast(page)).ToHaveCountAsync(0, new() { Timeout = 10_000 });
        await Assertions.Expect(page.Locator("[data-balette='footer'] .bit-balette-hint").First).ToBeVisibleAsync();

        // A disabled row cannot run; Enter on it says why instead.
        await Input(page).FillAsync("");
        await TypeAsync(page, "export");
        await ExpectFirstItemAsync(page, "actions.export");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Export format"]);
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(ActiveItem(page)).ToHaveAttributeAsync("data-disabled", "true");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Requires the Pro plan");
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Choosing_a_page_navigates_there_and_closes_the_palette()
    {
        var page = await OpenAsync("/");

        // From the keyboard.
        await OpenPaletteAsync(page);
        await TypeAsync(page, "large registry");
        await ExpectFirstItemAsync(page, "navigation.large-registry");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForURLAsync("**/performance");
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Large registry");

        // And with the mouse: a click on a row runs it.
        await OpenPaletteAsync(page);
        await TypeAsync(page, "modes and sources");
        await ExpectFirstItemAsync(page, "navigation.modes-and-sources");
        await Items(page).First.ClickAsync();
        await page.WaitForURLAsync("**/modes");
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modes & sources");
    }
}
