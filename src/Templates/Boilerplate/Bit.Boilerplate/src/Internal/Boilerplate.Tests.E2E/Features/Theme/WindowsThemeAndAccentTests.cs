using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Theme;

/// <summary>Not parallelized: every Client.Windows app answers on the same CDP port (See WindowsSmokeTests).</summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsThemeAndAccentTests : ThemeAndAccentTestsBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    [DataRow(App.Sales, DisplayName = "Sales")]
    public async Task ThemeAndAccent_Should_SurviveClosingTheApp(App app)
    {
        await PickThenRestart(app, async () =>
        {
            // Killed and started again with its WebView2 profile kept: the user closing the app, not clearing it.
            var (page, stop) = await Playwright.LaunchWindowsApp(DeployedApps.WindowsAppIdOf(app)!, clearAppData: false);
            RegisterForCleanup(stop);
            return page;
        });
    }
}
