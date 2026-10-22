using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Kiosk;

/// <summary>
/// Kiosk mode, which only the AdminPanel Windows app is built with (<c>Kiosk.Enabled</c> in
/// .github/workflows/admin-sample.cd.yml) - so Todo and Sales are here as the contrast that proves each assertion is
/// about the kiosk rather than about every Client.Windows app.
/// <para>
/// Not parallelized: every Client.Windows app answers on the same hard-coded CDP port 9222 (see
/// <see cref="Features.Smoke.WindowsSmokeTests"/>).
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsKioskTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();

    /// <summary>
    /// The lockdown takes away every way out of the app, so the home page offers one back - but only where it is
    /// needed, since an exit button on a window a user can simply close is noise.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, true, DisplayName = "AdminPanel (kiosk)")]
    [DataRow(App.Todo, false, DisplayName = "Todo")]
    [DataRow(App.Sales, false, DisplayName = "Sales")]
    public async Task HomePage_Should_OfferTheWayOutOnlyInTheKioskApp(App app, bool isKiosk)
    {
        var page = await LaunchApp(app);

        await GoHome(page);

        var exit = ExitButton(page);

        if (isKiosk)
        {
            await Expect(exit).ToBeVisibleAsync();
        }
        else
        {
            await Expect(exit).ToHaveCountAsync(0);
        }
    }

    /// <summary>
    /// What KioskModeManager does to the window: no title bar and no resize border to reach a Close through, above the
    /// taskbar, and over the whole monitor rather than the 1024x768 the form is built with.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, true, DisplayName = "AdminPanel (kiosk)")]
    [DataRow(App.Todo, false, DisplayName = "Todo")]
    public async Task Window_Should_BeBorderlessAndTopMostOnlyInTheKioskApp(App app, bool isKiosk)
    {
        await LaunchApp(app);

        var window = WindowsWindow.Of(DeployedApps.WindowsAppIdOf(app)!);

        Assert.AreEqual(isKiosk is false, WindowsWindow.HasCaption(window), $"{app}'s window {(isKiosk ? "still has" : "lost")} its title bar.");
        Assert.AreEqual(isKiosk, WindowsWindow.IsTopMost(window), $"{app}'s window is {(isKiosk ? "not " : "")}topmost.");

        if (isKiosk)
        {
            Assert.AreEqual(WindowsWindow.ScreenBounds(window), WindowsWindow.RestoredBounds(window), $"{app}'s window does not cover its monitor.");
        }
    }

    /// <summary>
    /// The close a person performs - Alt+F4, or the X that is no longer there. Refused in the kiosk app and nowhere
    /// else; a Windows shutdown and a Task Manager close arrive as other reasons and are let through, which is why
    /// this sends SC_CLOSE rather than WM_CLOSE (see <see cref="WindowsWindow.RequestUserClose"/>).
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, true, DisplayName = "AdminPanel (kiosk)")]
    [DataRow(App.Todo, false, DisplayName = "Todo")]
    public async Task Window_Should_RefuseTheUsersCloseOnlyInTheKioskApp(App app, bool isKiosk)
    {
        await LaunchApp(app);

        var window = WindowsWindow.Of(DeployedApps.WindowsAppIdOf(app)!);

        WindowsWindow.RequestUserClose(window);

        var exited = window.WaitForExit(TimeSpan.FromSeconds(30));

        Assert.AreEqual(isKiosk, exited is false, isKiosk
            ? $"{app} closed although kiosk mode refuses the user's own close."
            : $"{app} did not close within 30 seconds of the close every window accepts.");
    }

    /// <summary>
    /// The home page's button is the only way out of a kiosk, so it has to actually end the process - not navigate,
    /// not sign out.
    /// </summary>
    [TestMethod]
    public async Task ExitButton_Should_EndTheKioskApp()
    {
        var page = await LaunchApp(App.AdminPanel);

        await GoHome(page);

        var window = WindowsWindow.Of(DeployedApps.AdminPanelWindowsAppId);

        try
        {
            await ExitButton(page).ClickAsync();
        }
        catch (PlaywrightException)
        {
            // Environment.Exit takes the WebView down with the process, which can land before the click is answered.
        }

        Assert.IsTrue(window.WaitForExit(TimeSpan.FromSeconds(30)), "The exit button did not end the app.");
        Assert.AreEqual(0, window.ExitCode, "The kiosk app did not exit cleanly.");
    }

    /// <summary>
    /// <see cref="WindowsAppOpener"/>'s launch with a stop that tolerates an app these tests have already ended.
    /// </summary>
    private async Task<IPage> LaunchApp(App app)
    {
        var windowsAppId = DeployedApps.WindowsAppIdOf(app);

        if (windowsAppId is null)
            Assert.Inconclusive($"{app} has no Windows build.");

        var (page, stop) = await Playwright.LaunchWindowsApp(windowsAppId!);

        RegisterForCleanup(async () =>
        {
            try
            {
                await stop();
            }
            catch (PlaywrightException)
            {
                IPlaywrightExtensions.StopWindowsApps();
            }
        });

        return page;
    }

    /// <summary>The home page, rendered by the running app - the banner is not in the prerendered html of any app.</summary>
    private async Task GoHome(IPage page)
    {
        await WaitUntilInteractive(page);
        await GoToWhenInteractive(page, PageUrls.Home);

        await Expect(page.Locator("section .root-stack").First).ToBeVisibleAsync();
    }

    private static ILocator ExitButton(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = AppStrings.KioskExitApp });
}
