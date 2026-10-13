using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for BitThemeSwitcher against the real theme script (#13687), in every engine
/// (BROWSER=chromium, firefox, webkit): <c>/regression/theme-switcher</c> declares the Fluent 2 pair and persists the
/// theme, so a reload is a return visit painted by the first-paint script, then restored by the theme script, then
/// read back by the switcher.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitThemeSwitcherBrowserTests
public class BitThemeSwitcherBrowserTests : PerformanceTestBase
{
    private const string Setup = "light=fluent2-light&dark=fluent2-dark&persist=1";

    [TestMethod]
    public async Task PickingADesignSystem_WhileFollowingTheOs_KeepsFollowingIt()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Open();
        await AssertTheme("fluent2-dark");

        await Pick("Material");
        await AssertTheme("material-dark");
        Assert.AreNotEqual("material-dark", await Stored("bit-current-theme"));

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await AssertTheme("material-light");

        await Open();
        await AssertTheme("material-light");
        await AssertPicked("Material");
    }

    [TestMethod]
    public async Task Toggling_FlipsWithinThePickedDesignSystem_AndIsRestored()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Open();
        await Pick("Material");
        await AssertTheme("material-light");

        var changes = await CountChanges();
        await Toggle();
        await AssertTheme("material-dark");
        await Expect(Page.Locator("#v-changed")).ToHaveTextAsync("material-dark");
        Assert.AreEqual(1, await changes(), "One toggle, one theme change.");

        Assert.AreEqual("material-dark", await Stored("bit-current-theme"));
        Assert.AreEqual("material-light material-dark", await Stored("bit-theme-pair"));

        await Open();
        await AssertTheme("material-dark");
        await AssertPicked("Material");

        await Toggle();
        await AssertTheme("material-light");
    }

    [TestMethod]
    public async Task Toggling_ATheme_OfAnotherDesignSystemThanThePairs_FlipsWithinItsOwn()
    {
        // The page declares Fluent 2, and the app applied the core dark theme - Fluent's - on its own.
        await Open();
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('dark')");
        await AssertPicked("Fluent");

        var changes = await CountChanges();
        await Toggle();

        await AssertTheme("light");
        Assert.AreEqual(1, await changes(), "The pair switch and the flip are one theme change.");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme-dark", "dark");
    }

    [TestMethod]
    public async Task PickingADesignSystem_FromAThemeOfNeitherScheme_AppliesThePick()
    {
        await Open();
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('brand')");

        await Pick("Cupertino");

        await AssertTheme("cupertino-light");
        await AssertPicked("Cupertino");
        await Expect(Page.Locator("#v-changed")).ToHaveTextAsync("cupertino-light");
        Assert.AreEqual("cupertino-light", await Stored("bit-current-theme"));
    }

    [TestMethod]
    public async Task AThemeStoredByAnEarlierSwitcher_IsRestoredAsItIs()
    {
        // An earlier switcher pinned the picked design system's theme, with nothing stored beside it.
        await Open();
        await Page.EvaluateAsync("() => { localStorage.setItem('bit-current-theme', 'material-dark'); localStorage.removeItem('bit-theme-pair'); }");

        await Open();

        await AssertTheme("material-dark");
        await AssertPicked("Material");
        Assert.AreEqual("material-dark", await Stored("bit-current-theme"));

        // And the toggle flips within it.
        await Toggle();
        await AssertTheme("material-light");
    }

    [TestMethod]
    public async Task PickingTheDeclaredDesignSystem_StoresNoPair()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Open();
        await Pick("Material");
        await Toggle();
        await AssertTheme("material-dark");

        await Pick("Fluent 2");

        await AssertTheme("fluent2-dark");
        // The visitor's dark choice is kept as the declared pair's dark theme, so it follows the page's pair.
        Assert.AreEqual("dark", await Stored("bit-theme-pair"));
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme-light", "fluent2-light");
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/theme-switcher?{Setup}");
        await Expect(Page.Locator("#status")).ToHaveTextAsync("Ready");
    }

    private async Task Pick(string designSystem)
    {
        await Page.Locator(".bit-ths-dds").ClickAsync();
        await Page.Locator(".bit-drp-itm", new() { HasTextString = designSystem }).First.ClickAsync();
        await AssertPicked(designSystem);
    }

    private Task Toggle() => Page.Locator(".bit-ths-csb:visible").ClickAsync();

    private Task AssertPicked(string designSystem)
        => Expect(Page.Locator(".bit-ths-dds .bit-drp-tdp")).ToHaveTextAsync(designSystem);

    private Task<string?> Stored(string key)
        => Page.EvaluateAsync<string?>("k => localStorage.getItem(k)", key);

    private Task AssertTheme(string theme)
        => Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", theme);

    /// <summary>Starts counting bit-theme-change events; the returned call reads the count once the page is settled.</summary>
    private async Task<System.Func<Task<int>>> CountChanges()
    {
        await Page.EvaluateAsync("() => { window.__themeChanges = 0; document.addEventListener('bit-theme-change', () => window.__themeChanges++); }");

        return async () =>
        {
            // Let a second change, if one was going to come, arrive before the count is read.
            await Page.WaitForTimeoutAsync(300);
            return await Page.EvaluateAsync<int>("() => window.__themeChanges");
        };
    }
}
