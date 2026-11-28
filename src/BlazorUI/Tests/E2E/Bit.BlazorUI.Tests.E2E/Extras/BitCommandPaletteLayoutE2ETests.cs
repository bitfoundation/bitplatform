namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette as the browser computes it: the colours the page's theme and the public CSS variables resolve to,
/// and the detail pane coming and going with the dialog's width. Every value is read back with getComputedStyle or
/// getBoundingClientRect, never from the markup.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteLayoutE2ETests : BitCommandPaletteE2ETestBase
{
    private const string Teal = "rgb(13, 148, 136)";

    private Task<string> DialogStyleAsync(string property) => Dialog.EvaluateAsync<string>($"e => getComputedStyle(e).{property}");

    private Task<double> DialogWidthAsync() => Dialog.EvaluateAsync<double>("e => e.getBoundingClientRect().width");

    [TestMethod]
    public async Task ThePalette_FollowsThePagesTheme()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        var lightBackground = await DialogStyleAsync("backgroundColor");
        var lightColor = await DialogStyleAsync("color");
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        // <html bit-theme> restyles the palette with the page, through the theme's tokens.
        await Page.EvaluateAsync("() => document.documentElement.setAttribute('bit-theme', 'dark')");
        await OpenPaletteAsync();
        await Expect(Dialog).Not.ToHaveCSSAsync("background-color", lightBackground);
        await Expect(Dialog).Not.ToHaveCSSAsync("color", lightColor);
    }

    [TestMethod]
    public async Task ThePublicVariables_RecolourTheModeChipAndTheHighlights()
    {
        await OpenPageAsync();
        await Page.Locator("#cpl-mode-commands").ClickAsync();
        await ExpectOpenAsync();
        await TypeAsync("ren");
        // Over a Blazor Server circuit the highlights arrive a round trip after the keys.
        await Expect(Page.Locator("[data-bit-cpl='mark']").First).ToBeVisibleAsync();
        var defaultChip = await ModeChip.EvaluateAsync<string>("e => getComputedStyle(e).color");
        var defaultMark = await Page.Locator("[data-bit-cpl='mark']").First.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        // Set on :root, they reach every palette of the page.
        await Page.EvaluateAsync($"() => {{ const s = document.documentElement.style; s.setProperty('--bit-CommandPalette-accent', '{Teal}'); s.setProperty('--bit-CommandPalette-mark-background', 'rgb(204, 251, 241)'); }}");
        await Page.Locator("#cpl-mode-commands").ClickAsync();
        await ExpectOpenAsync();
        await TypeAsync("ren");
        await Expect(Page.Locator("[data-bit-cpl='mark']").First).ToBeVisibleAsync();

        await Expect(ModeChip).ToHaveCSSAsync("color", Teal);
        await Expect(Page.Locator("[data-bit-cpl='mark']").First).ToHaveCSSAsync("background-color", "rgb(204, 251, 241)");
        TestContext.WriteLine($"mode chip: default {defaultChip}; mark background: default {defaultMark}");
        Assert.AreNotEqual(Teal, defaultChip, "the default accent is already the one set");
    }

    [TestMethod]
    public async Task TheDetailPane_ComesAndGoesWithItsToggle_AndWidensTheDialog()
    {
        await OpenPageAsync();

        await OpenPaletteAsync();
        await Expect(Dialog).ToHaveAttributeAsync("data-has-detail", "false");
        await Expect(Detail).ToHaveCountAsync(0);
        AssertInRange(await DialogWidthAsync(), 620, 660, "the dialog's width without a detail pane");
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        // The checkbox on the page...
        await Page.Locator("#cpl-detail").CheckAsync();
        await OpenPaletteAsync();
        await Expect(Detail).ToBeVisibleAsync();
        await Expect(Detail).ToContainTextAsync((await ItemIdsAsync())[0]);
        await Expect(Dialog).ToHaveAttributeAsync("data-has-detail", "true");
        AssertInRange(await DialogWidthAsync(), 900, 940, "the dialog's width with a detail pane");

        // ...and the palette's own toggle, which changes the very palette it is listed in while it stays open.
        await TypeAsync("show detail");
        await ExpectFirstItemAsync("toggle:detail");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Detail).ToHaveCountAsync(0);
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Item("toggle:detail").Locator(".bit-cpl-toggle")).ToHaveAttributeAsync("data-on", "false");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Detail).ToBeVisibleAsync();
        await Expect(Item("toggle:detail").Locator(".bit-cpl-toggle")).ToHaveAttributeAsync("data-on", "true");
        await Expect(Page.Locator("#cpl-detail")).ToBeCheckedAsync();
    }
}
