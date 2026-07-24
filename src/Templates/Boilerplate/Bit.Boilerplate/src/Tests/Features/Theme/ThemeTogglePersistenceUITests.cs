namespace Boilerplate.Tests.Features.Theme;

[TestClass, TestCategory("UITest")]
public partial class ThemeTogglePersistenceUITests : AppPageTest
{
    /// <summary>
    /// An anonymous visitor flips the dark/light theme from the home page and the choice sticks across a refresh:
    /// <list type="number">
    /// <item>Open the public home page - no sign-in needed - and read the active theme from the <c>bit-theme</c> attribute that Bit.BlazorUI keeps on the <c>html</c> element (server-rendered as "dark"; See <c>App.razor</c> and Client.Web's <c>index.html</c>).</item>
    /// <item>Open the header user menu by its chevron opener and flip the theme with the <c>BitToggle</c> switch inside the <c>.app-menu-callout</c> (See <c>AppMenu.ToggleTheme</c> -> <c>ThemeService.ToggleTheme</c> -> <c>BitThemeManager.ToggleDarkLightAsync</c>). The toggle lives outside any <c>AuthorizeView</c>, so a signed-out user can reach it. Assert the <c>html[bit-theme]</c> attribute flips to the opposite value, and that the choice is written to <c>localStorage</c> under the <c>bit-current-theme</c> key.</item>
    /// <item>Reload the whole page and assert the flipped theme survived the refresh (persistence).</item>
    /// <item>Toggle once more and assert the theme reverts to its original value.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task AnonymousUser_Should_ToggleTheme_AndPersistAcrossRefresh()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // The home page is public, so opening it needs no sign-in.
        await Page.GotoAsync(new Uri(serverAddress, PageUrls.Home).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var htmlElement = Page.Locator("html");

        // Bit.BlazorUI tracks the active theme via the `bit-theme` attribute on <html> (server-rendered as "dark").
        // Read it dynamically instead of assuming a literal, so the test stays robust if the default ever changes.
        var initialTheme = await htmlElement.GetAttributeAsync("bit-theme");
        Assert.IsFalse(string.IsNullOrEmpty(initialTheme), "The <html> element should carry a bit-theme attribute.");
        var toggledTheme = initialTheme == "dark" ? "light" : "dark";

        // Flip the theme from the anonymous user menu, then confirm the DOM signal changed.
        await ToggleThemeFromUserMenu();
        await Expect(htmlElement).ToHaveAttributeAsync("bit-theme", toggledTheme);

        // The switch persists the choice in localStorage under the `bit-current-theme` key.
        var persistedTheme = await Page.EvaluateAsync<string?>("() => localStorage.getItem('bit-current-theme')");
        Assert.AreEqual(toggledTheme, persistedTheme, "The toggled theme should be persisted in localStorage.");

        // A full page refresh must keep the toggled theme.
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(htmlElement).ToHaveAttributeAsync("bit-theme", toggledTheme);

        // Toggling again reverts to the original theme.
        await ToggleThemeFromUserMenu();
        await Expect(htmlElement).ToHaveAttributeAsync("bit-theme", initialTheme!);
    }

    /// <summary>
    /// Opens the header user menu - available to anonymous users - by clicking its chevron opener, then flips the
    /// dark/light theme using the BitToggle switch (role="switch") inside the app menu callout. The BitToggle is
    /// used instead of the theme action button because that button's label reflects the current theme and therefore
    /// is not a stable target across toggles.
    /// </summary>
    private async Task ToggleThemeFromUserMenu()
    {
        // The user menu (AppMenu) is a BitDropMenu; clicking its chevron opens the callout.
        await Page.Locator(".menu-chevron").ClickAsync();

        var callout = Page.Locator(".app-menu-callout");
        await Expect(callout).ToBeVisibleAsync();

        await callout.GetByRole(AriaRole.Switch).ClickAsync();
    }
}
