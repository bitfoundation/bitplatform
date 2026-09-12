using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.ErrorPages;

/// <summary>
/// NotFoundPage and NotAuthorizedPage reached through a universal link the OS hands the installed app, on Todo (Blazor
/// Router) and AdminPanel (bit Brouter). Only paths MainActivity's IntentFilter claims reach the app at all, so the
/// unknown route sits under one of them. Not parallelized: one connected device.
/// </summary>
[TestClass, TestCategory(TestCategories.Android), Retry(2), DoNotParallelize]
public class AndroidErrorPageTests : AppTestBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();

    /// <summary>Under the claimed /about prefix, with no route of its own in either router.</summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task LinkToARouteTheAppDoesNotHave_Should_OpenTheNotFoundPage(App app)
    {
        var page = await LaunchByLink(app, $"{PageUrls.About}/e2e-route-that-does-not-exist");

        await Expect(page.GetByText(AppStrings.NotFoundText, new() { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
    }

    /// <summary>A fresh install is signed out, and /settings needs a signed-in user.</summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task LinkToASignedInOnlyPage_Should_OpenTheNotAuthorizedPage_WhenSignedOut(App app)
    {
        var page = await LaunchByLink(app, PageUrls.Settings);

        await Expect(page.GetByText(AppStrings.YouAreNotAuthorized, new() { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });

        await Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(PageUrls.Settings)}/?$"));
    }

    /// <summary>Cleared first, so the app starts signed out and cold - started by the link itself.</summary>
    private async Task<IPage> LaunchByLink(App app, string path)
    {
        var link = new Uri(new Uri(DeployedApps.AddressOf(app)), path).ToString();

        var (page, stop) = await Playwright.LaunchAndroidApp(DeployedApps.AndroidAppIdOf(app)!, startedByLink: link);
        RegisterForCleanup(stop);

        return page;
    }
}
