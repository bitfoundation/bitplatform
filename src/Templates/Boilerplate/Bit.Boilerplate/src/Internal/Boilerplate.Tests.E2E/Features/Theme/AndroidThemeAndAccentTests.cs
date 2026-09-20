using Bit.BlazorUI;
using ImageMagick;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Theme;

/// <summary>Not parallelized: both apps run on the single connected device/emulator (See AndroidSmokeTests).</summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidThemeAndAccentTests : ThemeAndAccentTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    public async Task ThemeAndAccent_Should_SurviveClosingTheApp(App app)
    {
        await PickThenRestart(app, async () =>
        {
            // Force stopped and started again with its data kept: the user closing the app, not clearing it.
            var (page, stop) = await Playwright.LaunchAndroidApp(DeployedApps.AndroidAppIdOf(app)!, clearAppData: false);
            RegisterForCleanup(stop);
            return page;
        });
    }

    /// <summary>
    /// The status bar is painted by MauiDeviceCoordinator.ApplyTheme, outside the WebView, so no assertion on the page
    /// covers it - and what it has to match is the theme the app LAUNCHES with. That is the theme the user picked last
    /// time, which the app can only know from what ThemeService left in Preferences: the WebView holding
    /// bit-current-theme is not up yet when the native chrome is painted, and the OS setting is a different answer the
    /// moment the user picks against it.
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    public async Task StatusBar_Should_TakeTheThemeTheAppLaunchedWith(App app)
    {
        // The opener clears the app's data, so this launch has nothing stored and follows the OS.
        var page = await OpenApp(app);
        await WaitUntilInteractive(page);
        await ExpectStatusBarToMatchTheme(page);

        var dark = await IsDark(page);
        await ToggleTheme(page);

        // Repainted while the app runs, so the picked theme is on the screen before any restart is involved.
        await ExpectStatusBarToMatchTheme(page);

        // Android's WebView writes localStorage to disk lazily; a force-stop within a second of the write loses it,
        // which is not what a user closing the app does.
        await Task.Delay(TimeSpan.FromSeconds(5));

        var (restarted, stop) = await Playwright.LaunchAndroidApp(DeployedApps.AndroidAppIdOf(app)!, clearAppData: false);
        RegisterForCleanup(stop);
        await WaitUntilInteractive(restarted);

        // The theme now differs from the OS setting this device is on, so a status bar that still matches is one
        // painted from what the user picked.
        await ExpectTheme(restarted, dark: !dark);
        await ExpectStatusBarToMatchTheme(restarted);
    }

    /// <summary>
    /// The background of the preset the page is on - BitExtraThemeSurfaces is the table the app's own native code
    /// reads, so a theme switch (the demos run Cupertino and Material) needs no change here.
    /// </summary>
    private async Task ExpectStatusBarToMatchTheme(IPage page)
    {
        var theme = await page.Locator("html").GetAttributeAsync("bit-theme");
        var expected = new MagickColor(BitExtraThemeSurfaces.BackgroundPrimary[theme!]);

        IMagickColor<ushort>? painted = null;
        var deadline = DateTimeOffset.UtcNow + statusBarDeadline;

        while (DateTimeOffset.UtcNow < deadline)
        {
            painted = await StatusBarColor();

            if (expected.Equals(painted))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        Assert.Fail($"The status bar is {painted?.ToHexString()} while the app is on '{theme}', whose background is {expected.ToHexString()}.");
    }

    /// <summary>
    /// The most frequent color of the screen's top rows. Only the status bar's background is up there - its clock and
    /// icons sit below them - so the winner of that band is the color the status bar was painted with.
    /// </summary>
    private async Task<IMagickColor<ushort>> StatusBarColor()
    {
        using var screen = new MagickImage(await Playwright.TakeAndroidScreenshot());

        screen.Crop(new MagickGeometry(0, 0, screen.Width, statusBarRows));

        return screen.Histogram().MaxBy(color => color.Value).Key;
    }

    private const uint statusBarRows = 8;

    /// <summary>The paint follows the app's own startup, which WaitUntilInteractive has already waited out.</summary>
    private static readonly TimeSpan statusBarDeadline = TimeSpan.FromSeconds(15);
}
