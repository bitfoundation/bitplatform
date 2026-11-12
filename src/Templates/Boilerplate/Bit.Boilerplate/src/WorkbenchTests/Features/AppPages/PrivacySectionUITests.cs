//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Privacy section: the consents, downloading the data, and the way to the account's deletion.</summary>
[TestClass, TestCategory("UITest")]
public class PrivacySectionUITests : SettingsPageTest
{
    private ILocator Privacy => Section(AppStrings.PrivacyTitle);

    //#if (appInsights == true)
    [TestMethod, EverySurface]
    public async Task TheAnalyticsConsent_Should_TurnOnAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.PrivacyTitle);

        var consent = Privacy.GetByRole(AriaRole.Switch, new() { Name = AppStrings.ConsentAnalytics, Exact = true });

        await Expect(consent).Not.ToBeCheckedAsync();

        await Press(consent);

        await Expect(consent).ToBeCheckedAsync();

        await ReloadTheViewOnceSaved("\"Analytics\":true");

        await Expect(consent).ToBeCheckedAsync();

        await Press(consent);

        await Expect(consent).Not.ToBeCheckedAsync();
    }
    //#endif

    //#if (ads == true)
    [TestMethod, EverySurface]
    public async Task TheAdvertisingConsent_Should_TurnOnAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.PrivacyTitle);

        var consent = Privacy.GetByRole(AriaRole.Switch, new() { Name = AppStrings.ConsentAdvertising, Exact = true });

        await Expect(consent).Not.ToBeCheckedAsync();

        await Press(consent);

        await Expect(consent).ToBeCheckedAsync();

        await ReloadTheViewOnceSaved("\"Advertising\":true");

        await Expect(consent).ToBeCheckedAsync();
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task TheDownload_Should_GiveAFileWithTheProfile(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.PrivacyTitle);

        var download = await Page.RunAndWaitForDownloadAsync(() => Press(Privacy.GetByRole(AriaRole.Button, new() { Name = AppStrings.ExportPersonalDataButton, Exact = true })));

        var content = await File.ReadAllTextAsync(await download.PathAsync(), TestContext.CancellationToken);

        Assert.Contains(WorkbenchDemoUsers.All[0].Email, content);
    }

    [TestMethod, EverySurface]
    public async Task TheAccountSettingsLink_Should_OpenTheAccountSection(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.PrivacyTitle);

        await Press(Privacy.GetByRole(AriaRole.Button, new() { Name = AppStrings.AccountSettingsLink, Exact = true }));

        await Expect(Section(AppStrings.AccountTitle)).ToBeVisibleAsync();
        await Expect(SectionHeader(AppStrings.PrivacyTitle)).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
