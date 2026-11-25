namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Key presses as keyboards other than a US one produce them. Playwright types with a US layout, so these tests dispatch
/// the events a Russian or Persian layout, a held key or an input method editor fire - the same KeyboardEvent the browser
/// would, with its key, code, repeat and isComposing - and check what BitCommandPalette's browser layer makes of them.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteKeyboardLayoutE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task CtrlK_OnARussianLayout_OpensThePalette()
    {
        await OpenPageAsync();

        // On a Russian layout the K key types 'л': the browser layer reads the key it sits on.
        Assert.IsTrue(await KeyDownAsync("л", "KeyK", ctrl: true, selector: "body"));

        await ExpectOpenAsync();
    }

    [TestMethod]
    public async Task ASequenceTypedOnAPersianLayout_Runs()
    {
        await OpenPageAsync();

        // G then S, typed on the standard Persian layout: 'ل' and 'س'.
        await KeyDownAsync("ل", "KeyG", selector: "body");
        await KeyDownAsync("س", "KeyS", selector: "body");

        await ExpectUrlAsync("/e2e/command-palette/settings");
    }

    [TestMethod]
    public async Task CtrlNAndCtrlP_MoveTheSelection_OnARussianLayout()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        var ids = await ItemIdsAsync();
        await ExpectActiveAsync(ids[0]);

        Assert.IsTrue(await KeyDownAsync("т", "KeyN", ctrl: true));
        await ExpectActiveAsync(ids[1]);

        Assert.IsTrue(await KeyDownAsync("з", "KeyP", ctrl: true));
        await ExpectActiveAsync(ids[0]);
    }

    [TestMethod]
    public async Task AHeldToggleShortcut_OpensThePaletteOnce()
    {
        await OpenPageAsync();

        await KeyDownAsync("k", "KeyK", ctrl: true, selector: "body");
        await ExpectOpenAsync();

        // The auto-repeats of the held keys are claimed - the browser must not act on them either - but run nothing.
        for (var i = 0; i < 5; i++)
        {
            Assert.IsTrue(await KeyDownAsync("k", "KeyK", ctrl: true, repeat: true));
        }

        await Page.WaitForTimeoutAsync(300);
        await ExpectOpenAsync();
    }

    [TestMethod]
    public async Task KeysThatBelongToAnInputMethodComposition_RunNoShortcut()
    {
        await OpenPageAsync();

        // isComposing, and Safari's commit key (keyCode 229 with isComposing false).
        Assert.IsFalse(await KeyDownAsync("g", "KeyG", composing: true, selector: "body"));
        Assert.IsFalse(await KeyDownAsync("s", "KeyS", keyCode: 229, selector: "body"));
        await Page.WaitForTimeoutAsync(300);
        Assert.IsTrue(Page.Url.EndsWith("/e2e/command-palette", StringComparison.Ordinal), $"a composition key ran a shortcut and moved to {Page.Url}");

        // Not vacuous: the same keys outside a composition are the settings sequence.
        await KeyDownAsync("g", "KeyG", selector: "body");
        await KeyDownAsync("s", "KeyS", selector: "body");
        await ExpectUrlAsync("/e2e/command-palette/settings");
    }

    [TestMethod]
    public async Task EnterThatCommitsACompositionInsideThePalette_RunsNothing()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("settings");
        await ExpectFirstItemAsync("navigation.settings");

        Assert.IsFalse(await KeyDownAsync("Enter", "Enter", keyCode: 229));
        await Page.WaitForTimeoutAsync(300);
        await ExpectOpenAsync();
        Assert.IsTrue(Page.Url.EndsWith("/e2e/command-palette", StringComparison.Ordinal), $"a composition's Enter ran the active row and moved to {Page.Url}");
    }
}
