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
}
