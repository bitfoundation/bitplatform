using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.UniversalLinks;

/// <summary>
/// What the installed Android app does with a link the OS hands it: closed or already running, the url naming a
/// culture or not. A link that names one has to outrank the culture in storage in every one of those states. Not
/// parallelized: one connected device.
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

    private static string LinkTo(string path, string? culture = null)
        => new Uri(new Uri(DeployedApps.AdminPanel), culture is null ? path : $"/{culture}{path}").ToString();

    [TestMethod]
    public async Task ClosedApp_Should_OpenTheLinksCulture_WhenItDisagreesWithTheRememberedOne()
    {
        await RememberCulture(rememberedCulture);

        var (page, stop) = await Playwright.LaunchAndroidApp(DeployedApps.AdminPanelAndroidAppId,
            startedByLink: LinkTo(PageUrls.About, linkCulture), clearAppData: false);
        RegisterForCleanup(stop);

        await AssertAboutPageIn(page, linkCulture, cultureInUrl: linkCulture);
    }

    [TestMethod]
    public async Task RunningApp_Should_OpenTheLinksCulture_WhenItDisagreesWithTheRememberedOne()
    {
        var page = await OpenRunningApp();
        await ChangeCulture(page, rememberedCulture);

        await Playwright.OpenAndroidAppLink(LinkTo(PageUrls.About, linkCulture));

        await AssertAboutPageIn(page, linkCulture, cultureInUrl: linkCulture);
    }

    [TestMethod]
    public async Task ClosedApp_Should_KeepTheRememberedCulture_WhenTheLinkNamesNone()
    {
        await RememberCulture(rememberedCulture);

        var (page, stop) = await Playwright.LaunchAndroidApp(DeployedApps.AdminPanelAndroidAppId,
            startedByLink: LinkTo(PageUrls.About), clearAppData: false);
        RegisterForCleanup(stop);

        await AssertAboutPageIn(page, rememberedCulture);
    }

    [TestMethod]
    public async Task RunningApp_Should_KeepTheRememberedCulture_WhenTheLinkNamesNone()
    {
        var page = await OpenRunningApp();
        await ChangeCulture(page, rememberedCulture);

        await Playwright.OpenAndroidAppLink(LinkTo(PageUrls.About));

        await AssertAboutPageIn(page, rememberedCulture);
    }

    /// <summary>
    /// Leaves <paramref name="cultureName"/> in storage and the app closed - on a fresh install a link's culture
    /// survives by accident rather than because anything honoured it.
    /// </summary>
    private async Task RememberCulture(string cultureName)
    {
        var (page, stop) = await Playwright.LaunchAndroidApp(DeployedApps.AdminPanelAndroidAppId);

        await WaitUntilInteractive(page);
        await ChangeCulture(page, cultureName);

        await stop(); // Stops the app without clearing it, so the culture just picked is what it will boot into.
    }

    private async Task<IPage> OpenRunningApp()
    {
        var page = await OpenApp(App.AdminPanel);
        await WaitUntilInteractive(page);
        return page;
    }

    private async Task AssertAboutPageIn(IPage page, string cultureName, string? cultureInUrl = null)
    {
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
