//+:cnd:noEmit
using System.IO.Compression;

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

        await ReloadTheViewOnceSaved("Analytics=1");

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

        await ReloadTheViewOnceSaved("Advertising=1");

        await Expect(consent).ToBeCheckedAsync();
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task TheDownload_Should_GiveAFileWithTheProfile(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.PrivacyTitle);

        var download = await Page.RunAndWaitForDownloadAsync(() => Press(Privacy.GetByRole(AriaRole.Button, new() { Name = AppStrings.ExportPersonalDataButton, Exact = true })));

        // The server's zip: the data, with the profile picture beside it.
        using var archive = ZipFile.OpenRead(await download.PathAsync());
        using var data = new StreamReader(archive.GetEntry("data.json")!.Open());

        Assert.Contains(WorkbenchDemoUsers.All[0].Email, await data.ReadToEndAsync());
        Assert.IsNotNull(archive.Entries.FirstOrDefault(entry => entry.FullName.StartsWith("files/attachments/profile-picture", StringComparison.Ordinal)));
    }

    // On the second user, whose picture no test uploads: an upload is the Workbench's, and the first user's (See
    // ProfileSectionUITests) would bring the picture back into this view after its removal.
    [TestMethod, EverySurface]
    public async Task TheDownload_Should_HoldWhatThePageChanged(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle, "?user=1");

        var profile = Section(AppStrings.ProfileTitle);
        var name = $"Name {Guid.NewGuid():N}";

        await profile.GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true }).FillAsync(name);
        await Press(profile.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true }));

        await Expect(Snack(AppStrings.ProfileUpdatedSuccessfullyMessage)).ToBeVisibleAsync();

        var removePicture = profile.GetByRole(AriaRole.Button, new() { Name = AppStrings.Remove, Exact = true });

        await Press(removePicture);

        await Expect(removePicture).ToHaveCountAsync(0);

        await Press(SectionHeader(AppStrings.PrivacyTitle));

        var download = await Page.RunAndWaitForDownloadAsync(() => Press(Privacy.GetByRole(AriaRole.Button, new() { Name = AppStrings.ExportPersonalDataButton, Exact = true })));

        // As the server's, it holds the account as it is now: the name just saved, and no picture since it was removed.
        using var archive = ZipFile.OpenRead(await download.PathAsync());
        using var data = new StreamReader(archive.GetEntry("data.json")!.Open());

        Assert.Contains(name, await data.ReadToEndAsync());
        Assert.IsNull(archive.Entries.FirstOrDefault(entry => entry.FullName.StartsWith("files/attachments/", StringComparison.Ordinal)));
    }

    [TestMethod, EverySurface]
    public async Task TheAccountSettingsLink_Should_OpenTheAccountSection(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.PrivacyTitle);

        await Press(Privacy.GetByRole(AriaRole.Link, new() { Name = AppStrings.AccountSettingsLink, Exact = true }));

        await Expect(Section(AppStrings.AccountTitle)).ToBeVisibleAsync();
        await Expect(SectionHeader(AppStrings.PrivacyTitle)).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
