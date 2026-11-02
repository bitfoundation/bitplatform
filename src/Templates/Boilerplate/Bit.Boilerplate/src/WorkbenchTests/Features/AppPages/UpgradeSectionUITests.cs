//+:cnd:noEmit
//#if (ads == true)
namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Upgrade section: the consent to ads, watching one, and what its data states show.</summary>
[TestClass, TestCategory("UITest")]
public class UpgradeSectionUITests : SettingsPageTest
{
    private ILocator Upgrade => Section(AppStrings.UpgradeTitle);

    private ILocator Agree => Upgrade.GetByRole(AriaRole.Button, new() { Name = AppStrings.AdsConsentAgree, Exact = true });

    private ILocator Watch => Upgrade.GetByRole(AriaRole.Button, new() { Name = AppStrings.Upgrade, Exact = true });

    [TestMethod, EverySurface]
    public async Task WatchingAnAd_Should_TakeTheConsentFirstAndThenUpgradeForGood(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.UpgradeTitle);

        await Expect(Upgrade.GetByText(AppStrings.AdsConsentMessage)).ToBeVisibleAsync();
        await Expect(Watch).ToHaveCountAsync(0);

        await Press(Agree);

        await Expect(Agree).ToHaveCountAsync(0);

        await Press(Watch);

        await Expect(Upgrade.GetByText(AppStrings.UpgradeSuccessMessage)).ToBeVisibleAsync();
        await Expect(Watch).ToHaveCountAsync(0);

        await ReloadTheViewOnceSaved("\"IsAdWatched\":true");

        await Expect(Upgrade.GetByText(AppStrings.UpgradeSuccessMessage)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheConsent_Should_BeTheSameAsThePrivacySections(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.UpgradeTitle);

        await Press(Agree);
        await Expect(Watch).ToBeVisibleAsync();

        await Press(SectionHeader(AppStrings.PrivacyTitle));

        var consent = Section(AppStrings.PrivacyTitle).GetByRole(AriaRole.Switch, new() { Name = AppStrings.ConsentAdvertising, Exact = true });

        await Expect(consent).ToBeCheckedAsync();

        await Press(consent);
        await Expect(consent).Not.ToBeCheckedAsync();

        await Press(SectionHeader(AppStrings.UpgradeTitle));

        await Expect(Agree).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AFailedLoad_Should_SayNoAdIsAvailable(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.UpgradeTitle, "?data=error");

        await Expect(Upgrade.GetByText(AppStrings.UpgradeAdNotAvailableMessage)).ToBeVisibleAsync();
        await Expect(Agree).ToHaveCountAsync(0);
        await Expect(Watch).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ALoadThatNeverEnds_Should_KeepItsSpinner(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.UpgradeTitle, "?data=loading");

        await Expect(Upgrade.Locator(".bit-ldn-rsq")).ToBeVisibleAsync();
        await Expect(Agree).ToHaveCountAsync(0);
    }
}
//#endif
