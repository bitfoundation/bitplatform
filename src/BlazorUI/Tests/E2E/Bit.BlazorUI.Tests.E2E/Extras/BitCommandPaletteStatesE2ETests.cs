namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette's own states at /e2e/command-palette-states: a Disabled palette neither opens nor claims a key,
/// Open is bound both ways, and a search that matched nothing and a page with nothing on it each say so in their words.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteStatesE2ETests : BitCommandPaletteE2ETestBase
{
    private const string StatesRoute = "/e2e/command-palette-states";

    [TestMethod]
    public async Task ADisabledPalette_NeitherOpensNorClaimsItsShortcuts()
    {
        await OpenPageAsync(StatesRoute);
        await Page.Locator("#cpl-disabled").CheckAsync();
        await Expect(Dialog).ToHaveClassAsync(HasClass("bit-dis"));

        // Neither the toggle nor a command's shortcut is claimed: the keys stay the page's.
        Assert.IsFalse(await KeyDownAsync("k", "KeyK", ctrl: true, selector: "body"), "a disabled palette claimed its toggle");
        Assert.IsFalse(await KeyDownAsync("q", "KeyQ", alt: true, selector: "body"), "a disabled palette claimed a command's shortcut");
        await Page.Keyboard.PressAsync("Control+k");
        await Page.Keyboard.PressAsync("Alt+q");
        await Page.WaitForTimeoutAsync(300);
        await ExpectClosedAsync();
        await Expect(Text("cpl-runs")).ToHaveTextAsync("Runs: 0");

        // Enabled again, both are its own.
        await Page.Locator("#cpl-disabled").UncheckAsync();
        await Expect(Dialog).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+q");
        await Expect(Text("cpl-runs")).ToHaveTextAsync("Runs: 1");
        await OpenPaletteAsync();
    }

    [TestMethod]
    public async Task Open_IsBoundBothWays()
    {
        await OpenPageAsync(StatesRoute);
        await Expect(Text("cpl-open-state")).ToHaveTextAsync("Open: False");

        // The palette opened from its toggle tells the page, and so does its close.
        await OpenPaletteAsync();
        await Expect(Text("cpl-open-state")).ToHaveTextAsync("Open: True");
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();
        await Expect(Text("cpl-open-state")).ToHaveTextAsync("Open: False");

        // The page's value opens it.
        await Page.Locator("#cpl-open").ClickAsync();
        await ExpectOpenAsync();
        await Expect(Text("cpl-open-state")).ToHaveTextAsync("Open: True");
    }

    [TestMethod]
    public async Task ASearchThatMatchedNothing_AndAPageWithNothingOnIt_SayEachTheirOwnWords()
    {
        await OpenPageAsync(StatesRoute);
        await OpenPaletteAsync();

        await TypeAsync("zqxjv");
        var noResults = Dialog.Locator(".bit-cpl-nor");
        await Expect(noResults).ToContainTextAsync("Nothing like that here");
        await Expect(noResults).ToHaveAttributeAsync("role", "status");
        await Expect(Items).ToHaveCountAsync(0);

        await ReplaceQueryAsync("zqxjv", "empty page");
        await ExpectFirstItemAsync("empty-page");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveTextAsync(["An empty page"]);
        var empty = Dialog.Locator(".bit-cpl-emp");
        await Expect(empty).ToContainTextAsync("No items found");
        await Expect(Dialog.Locator(".bit-cpl-nor")).ToHaveCountAsync(0);
    }
}
