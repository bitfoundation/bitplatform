using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the theme's light / dark pair (#13687) and for a saved theme the pair no longer
/// covers (#13686), driven through the real first-paint script, the real theme script and real storage: a reload
/// of <c>/regression/theme-host</c> is a plain first paint, resolved by the server from the request's cookies, then
/// by the inline script, then by <c>BitBlazorUI.Theme.init</c>.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitThemePairBrowserTests
public class BitThemePairBrowserTests : PerformanceTestBase
{
    private const string Fluent2 = "light=fluent2-light&dark=fluent2-dark";
    private const string Material = "light=material-light&dark=material-dark";

    [TestMethod]
    public async Task SetPair_WhileFollowingTheOs_KeepsFollowingIt()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Open($"persist=1&{Fluent2}");
        await AssertTheme("fluent2-dark");

        Assert.AreEqual("material-dark", await SetPair("material-light", "material-dark"));
        await AssertTheme("material-dark");

        // Still following: the preference was not pinned to a theme, and the OS keeps deciding.
        Assert.AreNotEqual("material-dark", await Stored("bit-current-theme"));
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await AssertTheme("material-light");

        // The next visit restores the pair, and still follows the OS within it.
        await Open($"persist=1&{Fluent2}");
        await AssertTheme("material-light");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme-dark", "material-dark");
    }

    [TestMethod]
    public async Task SetPair_MovesAPinnedThemeToItsCounterpart()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('fluent2-dark')");

        Assert.AreEqual("material-dark", await SetPair("material-light", "material-dark"));
        Assert.AreEqual("material-dark", await Stored("bit-current-theme"));

        // The toggle flips within the new pair.
        Assert.AreEqual("material-light", await Page.EvaluateAsync<string>("BitBlazorUI.Theme.toggleDarkLight()"));

        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await AssertTheme("material-light");
    }

    [TestMethod]
    public async Task SetPair_WithoutAChoice_RestoresThePageDefaultInThePickedPair()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-dark");

        await SetPair("cupertino-light", "cupertino-dark");
        await AssertTheme("cupertino-dark");

        await Open($"persist=1&{Fluent2}&default=fluent2-dark");
        await AssertTheme("cupertino-dark");
    }

    [TestMethod]
    public async Task SetPair_BackToTheDeclaredPair_StoresNoPair()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");

        await SetPair("material-light", "material-dark");
        Assert.AreEqual("material-light material-dark", await Stored("bit-theme-pair"));

        // No pair: only the marker that the stored theme is the declared pair's light one.
        await SetPair("fluent2-light", "fluent2-dark");
        Assert.AreEqual("light", await Stored("bit-theme-pair"));
        await AssertTheme("fluent2-light");
    }

    [TestMethod]
    public async Task SetPair_IgnoresABlankName()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");

        Assert.AreEqual("fluent2-light", await SetPair("material-light", ""));
        Assert.AreEqual("light", await Stored("bit-theme-pair"));
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme-light", "fluent2-light");
    }

    [TestMethod]
    public async Task SavedThemeOutsideTheDeclaredPair_IsMovedOntoIt()
    {
        // #13686: the visitor picked dark under Fluent 2, then the app switched its pair to Material.
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('fluent2-dark')");

        await Open($"persist=1&{Material}&default=material-light");

        await AssertTheme("material-dark");
        Assert.AreEqual("material-dark", await Stored("bit-current-theme"));
    }

    [TestMethod]
    public async Task SavedTheme_WithNoPairNamed_IsAppliedAsItIs()
    {
        await Open("persist=1&default=light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('material-dark')");

        await Open("persist=1&default=light");

        await AssertTheme("material-dark");
    }

    [TestMethod]
    public async Task CookiePersistence_LetsTheServerPaintThePickedPair()
    {
        await Open($"cookie=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('fluent2-dark')");
        await SetPair("material-light", "material-dark");

        // What the server renders for the next request, before any script of it runs.
        var html = await Page.EvaluateAsync<string>("fetch(location.href).then(r => r.text())");

        StringAssert.Contains(html, "bit-theme=\"material-dark\"");
    }

    [TestMethod]
    public async Task SetPair_ReachesTheOtherTabs()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        var other = await Page.Context.NewPageAsync();
        await other.GotoAsync($"{BaseUrl}/regression/theme-host?persist=1&{Fluent2}&default=fluent2-light");

        await Page.EvaluateAsync("BitBlazorUI.Theme.set('fluent2-dark')");
        await SetPair("material-light", "material-dark");

        await Expect(other.Locator("html")).ToHaveAttributeAsync("bit-theme", "material-dark");
        await Expect(other.Locator("html")).ToHaveAttributeAsync("bit-theme-light", "material-light");
    }

    [TestMethod]
    public async Task SavedThemeOutsideTheDeclaredPair_WithNoSchemeMarker_IsKept()
    {
        // A theme pinned outside the pair - by the app, or by an earlier switcher before the marker existed - is
        // a choice of its own: restored as it is, by the first-paint script and the theme script alike.
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('material-dark')");
        Assert.IsNull(await Stored("bit-theme-pair"));

        await Open($"persist=1&{Fluent2}&default=fluent2-light");

        await AssertTheme("material-dark");
        Assert.AreEqual("material-dark", await Stored("bit-current-theme"));
    }

    [TestMethod]
    public async Task SavedThemeOutsideTheDeclaredPair_IsMovedOntoIt_ByTheServerToo()
    {
        await Open($"cookie=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('fluent2-dark')");

        await Page.GotoAsync($"{BaseUrl}/regression/theme-host?cookie=1&{Material}&default=material-light");
        var html = await Page.EvaluateAsync<string>("fetch(location.href).then(r => r.text())");

        StringAssert.Contains(html, "bit-theme=\"material-dark\"");
        await AssertTheme("material-dark");
    }

    [TestMethod]
    public async Task AStoredPairThePageNowDeclares_IsDroppedFromStorage()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('fluent2-dark')");
        await SetPair("material-light", "material-dark");
        Assert.AreEqual("material-light material-dark", await Stored("bit-theme-pair"));

        // The app now declares Material itself: the visitor's pair is the page's own, so only the marker stays.
        await Open($"persist=1&{Material}&default=material-light");
        await AssertTheme("material-dark");
        Assert.AreEqual("dark", await Stored("bit-theme-pair"));

        // And a later change of the page's pair reaches the visitor.
        await Open($"persist=1&light=cupertino-light&dark=cupertino-dark&default=cupertino-light");
        await AssertTheme("cupertino-dark");
    }

    [TestMethod]
    public async Task ClearedStorage_PutsTheOtherTabsBackOnTheDeclaredPair()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        var other = await Page.Context.NewPageAsync();
        await other.GotoAsync($"{BaseUrl}/regression/theme-host?persist=1&{Fluent2}&default=fluent2-light");

        await SetPair("material-light", "material-dark");
        await Expect(other.Locator("html")).ToHaveAttributeAsync("bit-theme-light", "material-light");

        await Page.EvaluateAsync("localStorage.clear()");

        await Expect(other.Locator("html")).ToHaveAttributeAsync("bit-theme-light", "fluent2-light");
        await Expect(other.Locator("html")).ToHaveAttributeAsync("bit-theme-dark", "fluent2-dark");
    }

    [TestMethod]
    public async Task SetPair_FromAThemeOfNeitherScheme_LandsOnThePair()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('brand')");

        Assert.AreEqual("cupertino-light", await SetPair("cupertino-light", "cupertino-dark"));
        Assert.AreEqual("cupertino-light", await Stored("bit-current-theme"));

        await Page.EvaluateAsync("BitBlazorUI.Theme.set('brand-dark')");
        Assert.AreEqual("material-dark", await SetPair("material-light", "material-dark"));
    }

    [TestMethod]
    public async Task ToggleDarkLightWithin_SwitchesThePairAndFlipsInOneChange()
    {
        await Open($"persist=1&{Fluent2}&default=fluent2-light");
        await Page.EvaluateAsync("BitBlazorUI.Theme.set('dark')");
        await Page.EvaluateAsync("() => { window.__themeChanges = 0; document.addEventListener('bit-theme-change', () => window.__themeChanges++); }");

        Assert.AreEqual("light", await Page.EvaluateAsync<string>("BitBlazorUI.Theme.toggleDarkLightWithin('light', 'dark')"));

        Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.__themeChanges"));
        Assert.AreEqual("light dark", await Stored("bit-theme-pair"));
        Assert.AreEqual("light", await Stored("bit-current-theme"));
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme-dark", "dark");

        // Within the active pair it is a plain toggle, and the pair is left as it is.
        Assert.AreEqual("dark", await Page.EvaluateAsync<string>("BitBlazorUI.Theme.toggleDarkLightWithin('light', 'dark')"));
        Assert.AreEqual("light dark", await Stored("bit-theme-pair"));
    }

    private async Task Open(string query)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/theme-host?{query}");
        await Page.WaitForFunctionAsync("typeof BitBlazorUI !== 'undefined' && !!BitBlazorUI.Theme");
    }

    private Task<string> SetPair(string light, string dark)
        => Page.EvaluateAsync<string>("([l, d]) => BitBlazorUI.Theme.setPair(l, d)", new[] { light, dark });

    private Task<string?> Stored(string key)
        => Page.EvaluateAsync<string?>("k => localStorage.getItem(k)", key);

    private Task AssertTheme(string theme)
        => Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", theme);
}
