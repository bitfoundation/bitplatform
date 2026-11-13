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
    public async Task WatchingAnAd_Should_TakeTheConsentFirstAndThenUpgrade(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.UpgradeTitle);

        await Expect(Upgrade.GetByText(AppStrings.AdsConsentMessage)).ToBeVisibleAsync();
        await Expect(Watch).ToHaveCountAsync(0);

        await Press(Agree);

        await Expect(Agree).ToHaveCountAsync(0);

        await Press(Watch);

        await Expect(Upgrade.GetByText(AppStrings.UpgradeSuccessMessage)).ToBeVisibleAsync();
        await Expect(Watch).ToHaveCountAsync(0);

        // The consent stays with the view's device, so a reload offers the ad again without asking.
        await ReloadTheViewOnceSaved("Advertising=1");

        await Expect(Watch).ToBeVisibleAsync();
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

        // As in the app, the ad that was ready isn't shown once the consent is withdrawn.
        await Press(SectionHeader(AppStrings.UpgradeTitle));
        await Press(Watch);

        await Expect(View().GetByText(AppStrings.UpgradeFailMessage).First).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AFailedLoad_Should_SayNoAdIsAvailable(WorkbenchSurface surface)
    {
        await OpenWithTheConsent();

        await PressInTheTools(WorkbenchStrings.DataError);

        await Expect(Upgrade.GetByText(AppStrings.UpgradeAdNotAvailableMessage)).ToBeVisibleAsync();
        await Expect(Agree).ToHaveCountAsync(0);
        await Expect(Watch).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ALoadThatNeverEnds_Should_KeepItsSpinner(WorkbenchSurface surface)
    {
        await OpenWithTheConsent();

        await PressInTheTools(WorkbenchStrings.DataLoading);

        await Expect(Upgrade.Locator(".bit-ldn-rsq")).ToBeVisibleAsync();
        await Expect(Agree).ToHaveCountAsync(0);
    }

    /// <summary>Opens the section with the advertising consent given, which the app asks for before it loads an ad.</summary>
    private async Task OpenWithTheConsent()
    {
        await OpenSection(AppStrings.UpgradeTitle);

        await Press(Agree);
        await Expect(Watch).ToBeVisibleAsync();
        await WaitUntilSaved("Advertising=1");
    }
}
//#endif
