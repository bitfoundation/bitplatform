namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette's multi-step flows, driven with the keyboard end to end and checked by what they do to the page
/// around the palette: a theme on the page, a renamed project, labels, a deleted project, a pin and a recent that survive
/// closing the palette and reloading the page.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteFlowE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task ANestedPage_HasBreadcrumbs_AndItsChoiceAppliesToThePage()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        var lightBackground = await Dialog.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        await TypeAsync("theme");
        await ExpectFirstItemAsync("preferences.theme");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveTextAsync(["Theme"]);
        await Expect(Items).ToHaveCountAsync(3);
        Assert.AreEqual("theme:auto,theme:light,theme:dark", string.Join(",", await ItemIdsAsync()));
        await Expect(Item("theme:auto").Locator(".bit-cpl-toggle")).ToHaveAttributeAsync("data-on", "true");

        // The option's handler answers Back: the theme is set on <html>, and the palette lands on the list again -
        // painted dark with the page.
        await TypeAsync("dark");
        await ExpectFirstItemAsync("theme:dark");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Crumbs).ToHaveCountAsync(0);
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Dialog).Not.ToHaveCSSAsync("background-color", lightBackground);

        // The choice is the reader's, so it is remembered: after a reload the page is still dark, and the Theme command
        // still says so rather than "Auto".
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        await ReloadPageAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Text("cpl-theme")).ToHaveTextAsync("Theme: dark");
        await OpenPaletteAsync();
        await TypeAsync("theme");
        await ExpectFirstItemAsync("preferences.theme");
        await Expect(Item("preferences.theme")).ToContainTextAsync("Dark");
    }

    [TestMethod]
    public async Task TheLazyPage_ShowsItsLoadingState_AndThenItsItems()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("assign");
        await ExpectFirstItemAsync("actions.assign-to");

        await Page.Keyboard.PressAsync("Enter");

        // The page opens at once, with the loading state, and fills in when the provider returns 400 ms later.
        await Expect(Crumbs).ToHaveTextAsync(["Assign to…"]);
        await Expect(Page.Locator("[data-bit-cpl='loading']")).ToBeVisibleAsync();
        await Expect(Items).ToHaveCountAsync(10);
        await Expect(Page.Locator("[data-bit-cpl='loading']")).ToHaveCountAsync(0);
        await Expect(Input).ToHaveAttributeAsync("placeholder", "Pick an assignee…");
        await Expect(Input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TheRenamePrompt_IsPrefilled_AndItsValueRenamesTheProject()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("rename");
        await ExpectFirstItemAsync("actions.rename-project");

        await Page.Keyboard.PressAsync("Enter");

        await Expect(Prompt).ToContainTextAsync("New project name");
        await Expect(Input).ToHaveValueAsync("Acme Dashboard");
        await Expect(Input).ToBeFocusedAsync();
        // The caret is at the end of the current name, ready to edit it rather than replace it by accident.
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('dialog [data-bit-cpl=\"input\"]'); return i.selectionStart === 14 && i.selectionEnd === 14; }",
            null, new() { Timeout = 5_000 });

        await ReplaceQueryAsync("Acme Dashboard", "Zeta Board");
        await Page.Keyboard.PressAsync("Enter");

        await ExpectClosedAsync();
        await Expect(Text("cpl-project")).ToHaveTextAsync("Project: Zeta Board");
    }

    [TestMethod]
    public async Task TheToastOfAHandlerThatClosesThePalette_IsShownBeforeItCloses()
    {
        await OpenPageAsync();
        await Page.Keyboard.PressAsync("F2");
        await ExpectOpenAsync();
        await Expect(Prompt).ToContainTextAsync("New project name");

        // The prompt answers with a toast that closes the palette: the message is on screen for a moment before the close,
        // so the reader sees what happened.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Renamed to “Acme Dashboard”", new() { Timeout = 5_000 });
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task AMultiSelectPage_TogglesWithEnterAndSpace_AndAppliesWithCtrlEnter()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("set labels");
        await ExpectFirstItemAsync("actions.set-labels");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog).ToHaveAttributeAsync("data-multiselect", "true");
        await Expect(Items).ToHaveCountAsync(5);

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Item("label:bug")).ToHaveAttributeAsync("data-checked", "true");
        await Page.Keyboard.PressAsync("ArrowDown");
        // A multi-select page marks its rows checked, not selected: only aria-activedescendant says which is active.
        await Expect(ActiveItem).ToHaveAttributeAsync("data-value", "label:enhancement");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Item("label:enhancement")).ToHaveAttributeAsync("data-checked", "true");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Item("label:enhancement")).Not.ToHaveAttributeAsync("data-checked", "true");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Item("label:enhancement")).ToHaveAttributeAsync("data-checked", "true");

        // Space toggled rows instead of typing spaces, and the box says how many are checked.
        await Expect(Input).ToHaveValueAsync("");
        await Expect(Page.Locator("[data-bit-cpl='selected-count']")).ToHaveTextAsync("2 selected");
        await Expect(Item("label:bug")).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Keyboard.PressAsync("Control+Enter");
        await Expect(Toast).ToHaveTextAsync("2 labels applied");
        await ExpectClosedAsync();
        await Expect(Text("cpl-labels")).ToHaveTextAsync("Labels: bug, enhancement");
    }

    [TestMethod]
    public async Task ADestructiveCommand_NeedsASecondEnter_AndEscapeDisarmsIt()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("delete project");
        await ExpectFirstItemAsync("danger-zone.delete-project");
        var row = Item("danger-zone.delete-project");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(row).ToHaveAttributeAsync("data-confirming", "true");
        await Expect(Toast).ToContainTextAsync("This permanently deletes the project");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(row).Not.ToHaveAttributeAsync("data-confirming", "true");
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Text("cpl-log")).ToHaveTextAsync("Last action: nothing yet");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(row).ToHaveAttributeAsync("data-confirming", "true");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Project deleted");
        await ExpectClosedAsync();
        await Expect(Text("cpl-log")).ToHaveTextAsync("Last action: Project deleted (not really)");
    }

    [TestMethod]
    public async Task PinToTop_FromTheActionPanel_ListsTheCommandUnderPinnedWhenReopened()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("nuget");
        await ExpectFirstItemAsync("navigation.nuget-package");

        await Page.Keyboard.PressAsync("Tab");
        await ExpectFirstItemAsync("__pin:navigation.nuget-package");
        await Expect(Items.First).ToContainTextAsync("Pin to top");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        await OpenPaletteAsync();

        await Expect(GroupHeadings.First).ToHaveTextAsync("Pinned");
        await ExpectFirstItemAsync("navigation.nuget-package");
        await Expect(Items.First.Locator(".bit-cpl-pin")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ARecentCommand_SurvivesAReload()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("copy share");
        await ExpectFirstItemAsync("actions.copy-share-link");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Link copied to clipboard");
        await ExpectClosedAsync();

        // The run is recorded in localStorage without being awaited; reload only once it is there.
        await Page.WaitForFunctionAsync("() => (localStorage.getItem('bit-command-palette.history') ?? '').includes('actions.copy-share-link')",
            null, new() { Timeout = 10_000 });
        await ReloadPageAsync();

        await OpenPaletteAsync();
        await Expect(GroupHeadings.First).ToHaveTextAsync("Recent");
        await ExpectFirstItemAsync("actions.copy-share-link");
    }

    [TestMethod]
    public async Task AToastShowsInTheFooter_AndADisabledRowExplainsItself()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("what's new");
        await ExpectFirstItemAsync("help.what-s-new");

        // A toast that keeps the palette open replaces the footer's hints, then goes after a few seconds.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("You are on the latest version");
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Toast).ToHaveCountAsync(0, new() { Timeout = 10_000 });
        await Expect(Page.Locator("[data-bit-cpl='footer'] .bit-cpl-hint").First).ToBeVisibleAsync();

        // A disabled row cannot run; Enter on it says why instead.
        await ReplaceQueryAsync("what's new", "export");
        await ExpectFirstItemAsync("actions.export");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveTextAsync(["Export format"]);
        await Page.Keyboard.PressAsync("End");
        await Expect(ActiveItem).ToHaveAttributeAsync("data-disabled", "true");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Requires the Pro plan");
        await Expect(Dialog).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ChoosingAPage_NavigatesThere_AndClosesThePalette()
    {
        await OpenPageAsync();

        // From the keyboard.
        await OpenPaletteAsync();
        await TypeAsync("large registry");
        await ExpectFirstItemAsync("navigation.large-registry");
        await Page.Keyboard.PressAsync("Enter");
        await ExpectUrlAsync("/e2e/command-palette/large-registry");
        await ExpectClosedAsync();
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Large registry");

        // And with the mouse: a click on a row runs it.
        await OpenPaletteAsync();
        await TypeAsync("modes and sources");
        await ExpectFirstItemAsync("navigation.modes-and-sources");
        await Items.First.ClickAsync();
        await ExpectUrlAsync("/e2e/command-palette/modes");
        await ExpectClosedAsync();
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Modes and sources");
    }
}
