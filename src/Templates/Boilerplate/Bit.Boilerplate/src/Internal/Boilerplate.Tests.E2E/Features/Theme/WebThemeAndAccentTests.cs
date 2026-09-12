using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Theme;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebThemeAndAccentTests : ThemeAndAccentTestsBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.Sales, true, DisplayName = "Sales (prerendered), dark system")]
    [DataRow(App.Todo, false, DisplayName = "Todo (prerendered), light system")]
    [DataRow(App.AdminPanel, true, DisplayName = "AdminPanel (prerendered, bit Brouter), dark system")]
    [DataRow(App.AdminPanelWasmStandalone, false, DisplayName = "AdminPanelWasmStandalone (not prerendered), light system")]
    [DataRow(App.TodoAot, true, DisplayName = "TodoAot (not prerendered), dark system")]
    public async Task Theme_Should_FollowTheSystem_ThenKeepWhatTheUserPicked_AndSoShouldTheAccent(App app, bool systemIsDark)
    {
        // Before the first visit: with nothing stored, bit-theme-system follows prefers-color-scheme.
        await Page.EmulateMediaAsync(new() { ColorScheme = systemIsDark ? ColorScheme.Dark : ColorScheme.Light });

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);
        await ExpectTheme(page, dark: systemIsDark);

        await ToggleTheme(page);
        var accent = await PickAccent(page);

        await page.ReloadAsync();
        await WaitUntilInteractive(page);

        // The picked theme outranks the system's from now on.
        await ExpectTheme(page, dark: !systemIsDark);
        await ExpectAccent(page, accent);
    }
}
