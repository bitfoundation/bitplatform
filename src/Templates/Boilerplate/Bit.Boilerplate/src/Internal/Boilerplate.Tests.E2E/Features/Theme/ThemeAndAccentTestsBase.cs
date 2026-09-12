using System.Text.RegularExpressions;

namespace Boilerplate.Tests.E2E.Features.Theme;

/// <summary>
/// The theme (BitThemeManager, persisted by the host page's bit-theme-persist) and the accent color (AppMenu's
/// BitAccentColorSwitcher: StoredCss first paint, persisted to localStorage and a cookie - See
/// IClientCoreServiceCollectionExtensions) are both picked from the app menu, and both must outlive a reload or restart.
/// </summary>
public abstract partial class ThemeAndAccentTestsBase : AppTestBase
{
    /// <summary>Not the first swatch: that one is the packaged palette, so picking it looks the same as never picking.</summary>
    private const string pickedAccent = "Green";

    /// <summary>BitAccentColorNames.StorageKey.</summary>
    private const string accentStorageKey = "bit-accent-color";

    /// <summary>Hybrid: the first launch clears the app's data (See the openers), the restart keeps it.</summary>
    protected async Task PickThenRestart(App app, Func<Task<IPage>> restartKeepingData)
    {
        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        var dark = await IsDark(page);
        await ToggleTheme(page);
        var accent = await PickAccent(page);

        // Android's WebView writes localStorage to disk lazily; a force-stop within a second of the write loses it,
        // which is not what a user closing the app does.
        await Task.Delay(TimeSpan.FromSeconds(5));

        page = await restartKeepingData();
        await WaitUntilInteractive(page);

        await ExpectTheme(page, dark: !dark);
        await ExpectAccent(page, accent);
    }

    /// <summary>bit-theme holds the theme's name, e.g. material-dark; ThemeService.ToAppTheme reads it the same way.</summary>
    protected async Task ExpectTheme(IPage page, bool dark)
    {
        await Expect(page.Locator("html")).ToHaveAttributeAsync("bit-theme", ThemeName(dark));
    }

    /// <summary>Through the menu's Light / Dark entry, which is named after the theme in use.</summary>
    protected async Task ToggleTheme(IPage page)
    {
        var dark = await IsDark(page);

        await ClickAppMenuItem(page, dark ? AppStrings.Dark : AppStrings.Light);

        await ExpectTheme(page, dark: !dark);
    }

    /// <summary>Picks <see cref="pickedAccent"/>; returns the token BitAccentColor.ts stored for it.</summary>
    protected async Task<string> PickAccent(IPage page)
    {
        var before = await StoredAccent(page);

        await ClickAppMenuItem(page, SwatchLabel(pickedAccent));

        await page.WaitForFunctionAsync($"before => localStorage.getItem('{accentStorageKey}') !== before", before);

        return (await StoredAccent(page))!;
    }

    /// <summary>
    /// The stored token, the palette style element StoredCss paints from, and the swatch the restored
    /// BitAccentColorService marks as active - storage alone would pass with an app that never read it back.
    /// </summary>
    protected async Task ExpectAccent(IPage page, string accent)
    {
        Assert.AreEqual(accent, await StoredAccent(page), "The picked accent is no longer the stored one.");

        await Expect(page.Locator("style#bit-accent-css").First).ToBeAttachedAsync();

        var swatch = page.GetByRole(AriaRole.Button, new() { Name = SwatchLabel(pickedAccent) });
        await OpenAppMenu(page);
        await Expect(swatch).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private static async Task<bool> IsDark(IPage page)
    {
        return ThemeName(dark: true).IsMatch(await page.Locator("html").GetAttributeAsync("bit-theme") ?? "");
    }

    private static async Task<string?> StoredAccent(IPage page)
    {
        return await page.EvaluateAsync<string?>($"() => localStorage.getItem('{accentStorageKey}')");
    }

    /// <summary>BitAccentColorSwitcher's own label for a swatch (See its GetSwatchAriaLabel).</summary>
    private static string SwatchLabel(string accent) => $"Apply the {accent} accent color";

    private static Regex ThemeName(bool dark) => dark ? DarkThemeName() : LightThemeName();

    [GeneratedRegex("dark$", RegexOptions.IgnoreCase)]
    private static partial Regex DarkThemeName();

    [GeneratedRegex("light$", RegexOptions.IgnoreCase)]
    private static partial Regex LightThemeName();
}
