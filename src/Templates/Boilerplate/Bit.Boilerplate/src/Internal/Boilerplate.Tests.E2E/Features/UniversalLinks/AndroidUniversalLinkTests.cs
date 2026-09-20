using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.UniversalLinks;

/// <summary>
/// What the installed Android app does with a link the OS hands it: closed or already running, the url naming a
/// culture or not. A link that names one has to outrank the culture in storage in every one of those states. Each test
/// runs on Todo, which routes with Blazor's Router, and AdminPanel, which routes with bit Brouter. Not parallelized:
/// one connected device.
/// </summary>
[TestClass, TestCategory(TestCategories.Android), Retry(2), DoNotParallelize]
public class AndroidUniversalLinkTests : AppTestBase
{
    private const string linkCulture = "fa-IR";

    /// <summary>
    /// Neither the links' culture nor the device's, so a page in it can only have come from what the app remembered -
    /// en-US would be indistinguishable from falling back to the OS.
    /// </summary>
    private const string rememberedCulture = "nl-NL";

    protected override IAppOpener AppOpener => new AndroidAppOpener();

    private static string LinkTo(App app, string path, string? culture = null)
        => new Uri(new Uri(DeployedApps.AddressOf(app)), culture is null ? path : $"/{culture}{path}").ToString();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task ClosedApp_Should_OpenTheLinksCulture_WhenItDisagreesWithTheRememberedOne(App app)
    {
        await RememberCulture(app, rememberedCulture);

        var (page, stop) = await LaunchApp(app, startedByLink: LinkTo(app, PageUrls.About, linkCulture), clearAppData: false);
        RegisterForCleanup(stop);

        await AssertAboutPageIn(app, page, linkCulture, cultureInUrl: linkCulture);
    }

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task RunningApp_Should_OpenTheLinksCulture_WhenItDisagreesWithTheRememberedOne(App app)
    {
        var page = await OpenRunningApp(app);
        await ChangeCulture(page, rememberedCulture);

        await Playwright.OpenAndroidAppLink(LinkTo(app, PageUrls.About, linkCulture));

        await AssertAboutPageIn(app, page, linkCulture, cultureInUrl: linkCulture);
    }

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task ClosedApp_Should_KeepTheRememberedCulture_WhenTheLinkNamesNone(App app)
    {
        await RememberCulture(app, rememberedCulture);

        var (page, stop) = await LaunchApp(app, startedByLink: LinkTo(app, PageUrls.About), clearAppData: false);
        RegisterForCleanup(stop);

        await AssertAboutPageIn(app, page, rememberedCulture);
    }

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task RunningApp_Should_KeepTheRememberedCulture_WhenTheLinkNamesNone(App app)
    {
        var page = await OpenRunningApp(app);
        await ChangeCulture(page, rememberedCulture);

        await Playwright.OpenAndroidAppLink(LinkTo(app, PageUrls.About));

        await AssertAboutPageIn(app, page, rememberedCulture);
    }

    /// <summary>The -p:ApplicationTitle each workflow publishes its Android app with.</summary>
    private static string AppNameOf(App app) => app is App.Todo ? "TodoSample" : "AdminPanel";

    private Task<(IPage Page, Func<Task> Stop)> LaunchApp(App app, string? startedByLink = null, bool clearAppData = true)
        => Playwright.LaunchAndroidApp(DeployedApps.AndroidAppIdOf(app)!, startedByLink: startedByLink, clearAppData: clearAppData);

    /// <summary>
    /// Leaves <paramref name="cultureName"/> in storage and the app closed - on a fresh install a link's culture
    /// survives by accident rather than because anything honoured it.
    /// </summary>
    private async Task RememberCulture(App app, string cultureName)
    {
        var (page, stop) = await LaunchApp(app);

        await WaitUntilInteractive(page);
        await ChangeCulture(page, cultureName);

        await stop(); // Stops the app without clearing it, so the culture just picked is what it will boot into.
    }

    private async Task<IPage> OpenRunningApp(App app)
    {
        var page = await OpenApp(app);
        await WaitUntilInteractive(page);
        return page;
    }

    private async Task AssertAboutPageIn(App app, IPage page, string cultureName, string? cultureInUrl = null)
    {
        // The About page is Client.Maui's own, showing the native AppInfo.Name and pid: the link landed in exactly
        // this installed app, not the other one, nor a browser.
        var appInfoCard = page.Locator(".app-info-card");
        await Expect(appInfoCard.GetByText(AppNameOf(app), new() { Exact = true })).ToBeVisibleAsync();

        var processId = await Playwright.GetAndroidAppProcessId(DeployedApps.AndroidAppIdOf(app)!);
        await Expect(appInfoCard.Locator(".info-item").Filter(new() { HasText = "Process ID" }).Locator(".info-value"))
            .ToHaveTextAsync(processId);

        var culture = CultureInfoManager.GetCultureInfo(cultureName)!;

        // The whole path, culture segment included: a dropped culture and a carried-then-ignored one are different
        // bugs.
        var expectedPath = cultureInUrl is null ? PageUrls.About : $"/{cultureInUrl}{PageUrls.About}";
        await Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(expectedPath)}/?$"));

        // The title, not a body string: AppPageData renders PageTitle, so it is unambiguously this page's own copy in
        // the culture the app ended up in.
        await Expect(page).ToHaveTitleAsync(AppStrings.ResourceManager.GetString(nameof(AppStrings.AboutPageTitle), culture)!);
    }
}
