//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The copy of the app's Settings page as a whole: who may open it, and its sections, of which one is open at a time.</summary>
[TestClass, TestCategory("UITest")]
public class SettingsUITests : SettingsPageTest
{
    private static IEnumerable<string> SectionTitles =>
    [
        AppStrings.ProfileTitle,
        AppStrings.AccountTitle,
        AppStrings.TfaTitle,
        AppStrings.SessionsTitle,
        AppStrings.PrivacyTitle,
        //#if (ads == true)
        AppStrings.UpgradeTitle,
        //#endif
    ];

    [TestMethod, EverySurface]
    public async Task TheSettingsCopy_Should_AskASignedOutVisitorToSignIn(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings", "?auth=signedout");

        await Expect(View().GetByText(AppStrings.YouAreNotAuthorized)).ToBeVisibleAsync();
        await Expect(SectionHeader(AppStrings.ProfileTitle)).ToHaveCountAsync(0);

        await OpenWorkbench("settings", "?auth=signedin");

        await Expect(View().GetByText(AppStrings.YouAreNotAuthorized)).ToHaveCountAsync(0);
        await Expect(Section(AppStrings.ProfileTitle).GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
    }

    [TestMethod, EverySurface]
    public async Task EveryUser_Should_OpenTheSettingsPage(WorkbenchSurface surface)
    {
        foreach (var (user, name) in new[] { (1, WorkbenchStrings.DemoUserSaleh), (2, WorkbenchStrings.DemoUserZahra) })
        {
            await OpenWorkbench("settings", $"?user={user}");

            await Expect(Section(AppStrings.ProfileTitle).GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToHaveValueAsync(name);
        }
    }

    [TestMethod, EverySurface]
    public async Task ANewView_Should_ListEverySectionWithTheProfileOpen(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        foreach (var title in SectionTitles)
        {
            await Expect(SectionHeader(title)).ToBeVisibleAsync();
        }

        await Expect(SectionHeader(AppStrings.ProfileTitle)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Section(AppStrings.ProfileTitle)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task EverySection_Should_OpenInPlaceOfTheOneThatWasOpen(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        var previous = AppStrings.ProfileTitle;

        foreach (var title in SectionTitles.Skip(1))
        {
            await Press(SectionHeader(title));

            await Expect(Section(title)).ToBeVisibleAsync();
            await Expect(SectionHeader(title)).ToHaveAttributeAsync("aria-expanded", "true");
            await Expect(SectionHeader(previous)).ToHaveAttributeAsync("aria-expanded", "false");
            await Expect(Section(previous)).Not.ToBeVisibleAsync();

            previous = title;
        }
    }

    [TestMethod, EverySurface]
    public async Task AnOpenSection_Should_CloseWithItsHeaderAndStayAsItWasAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        await ReloadTheViewOnceSaved($"\"ExpandedSection\":\"{PageUrls.SettingsSections.Sessions}\"");

        await Expect(Section(AppStrings.SessionsTitle)).ToBeVisibleAsync();
        await Expect(SectionHeader(AppStrings.ProfileTitle)).ToHaveAttributeAsync("aria-expanded", "false");

        await Press(SectionHeader(AppStrings.SessionsTitle));

        await Expect(SectionHeader(AppStrings.SessionsTitle)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Section(AppStrings.SessionsTitle)).Not.ToBeVisibleAsync();

        await ReloadTheViewOnceSaved("\"ExpandedSection\":null");

        await Expect(SectionHeader(AppStrings.SessionsTitle)).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
