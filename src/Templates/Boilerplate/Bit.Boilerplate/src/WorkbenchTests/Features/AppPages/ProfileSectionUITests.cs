using System.Net.Http.Headers;
using System.Text.Json;
using Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;
using Boilerplate.Workbench.Infrastructure.Services.StandIns;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Profile section: the form with its validation, the birth date and gender, and the picture.</summary>
[TestClass, TestCategory("UITest")]
public class ProfileSectionUITests : SettingsPageTest
{
    private ILocator Profile => Section(AppStrings.ProfileTitle);

    private ILocator FullName => Profile.GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true });

    private ILocator BirthDate => Profile.GetByRole(AriaRole.Combobox, new() { Name = AppStrings.BirthDate, Exact = true });

    private ILocator Save => Profile.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true });

    private ILocator RemovePicture => Profile.GetByRole(AriaRole.Button, new() { Name = AppStrings.Remove, Exact = true });

    private ILocator Gender(string name) => Profile.GetByRole(AriaRole.Radio, new() { Name = name, Exact = true });

    [TestMethod, EverySurface]
    public async Task TheForm_Should_ShowTheUsersData(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle, "?user=1");

        await Expect(FullName).ToHaveValueAsync(WorkbenchStrings.DemoUserSaleh);
        await Expect(BirthDate).ToHaveValueAsync("4/18/1991");
        await Expect(Gender(AppStrings.GenderFemale)).ToBeCheckedAsync();
        await Expect(RemovePicture).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AnEmptyName_Should_BeRefusedWithAMessage(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        await FullName.FillAsync(string.Empty);
        await Press(Save);

        await Expect(Profile.GetByText(Message(AppStrings.RequiredAttribute_ValidationError, AppStrings.FullName))).ToBeVisibleAsync();
        await Expect(Snack(AppStrings.ProfileUpdatedSuccessfullyMessage)).ToHaveCountAsync(0);

        await FullName.FillAsync(WorkbenchStrings.DemoUserYas);
        await FullName.PressAsync("Tab");

        await Expect(Profile.GetByText(Message(AppStrings.RequiredAttribute_ValidationError, AppStrings.FullName))).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Saving_Should_SayItWorkedAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        var name = $"Name {Guid.NewGuid():N}";

        await FullName.FillAsync(name);
        await Press(Profile.GetByText(AppStrings.GenderMale, new() { Exact = true }));
        await Press(Save);

        await Expect(Snack(AppStrings.ProfileUpdatedSuccessfullyMessage)).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved(name);

        await Expect(FullName).ToHaveValueAsync(name);
        await Expect(Gender(AppStrings.GenderMale)).ToBeCheckedAsync();
    }

    [TestMethod, EverySurface]
    public async Task ASavedProfile_Should_BelongToTheAccountThatSavedIt(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        var name = $"Name {Guid.NewGuid():N}";

        await FullName.FillAsync(name);
        await Press(Save);

        await Expect(Snack(AppStrings.ProfileUpdatedSuccessfullyMessage)).ToBeVisibleAsync();

        await OpenTools();
        await PickUser(WorkbenchStrings.DemoUserSaleh);
        await CloseTools();

        await Expect(AccountButton(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();
        await Expect(FullName).ToHaveValueAsync(WorkbenchStrings.DemoUserSaleh);

        await OpenTools();
        await PickUser(WorkbenchStrings.DemoUserYas);
        await CloseTools();

        await Expect(AccountButton(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();
        await Expect(FullName).ToHaveValueAsync(name);
    }

    [TestMethod, EverySurface]
    public async Task WhatWasTypedButNotSaved_Should_BeGoneAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        await FullName.FillAsync($"Draft {Guid.NewGuid():N}");
        await FullName.PressAsync("Tab");

        // As in the app, a reload shows the profile as the server has it.
        await ReloadTheView();

        await Expect(FullName).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
    }

    [TestMethod, EverySurface]
    public async Task TheBirthDate_Should_BePickedFromTheCalendarAndCleared(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        await Press(BirthDate);
        await Press(View().GetByRole(AriaRole.Gridcell, new() { NameRegex = new Regex("April 22, 1991") }));

        await Expect(BirthDate).ToHaveValueAsync("4/22/1991");

        await Press(Save);

        await Expect(Snack(AppStrings.ProfileUpdatedSuccessfullyMessage)).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved("1991-04-22");

        await Expect(BirthDate).ToHaveValueAsync("4/22/1991");

        await Press(Profile.GetByRole(AriaRole.Button, new() { Name = "Clear date" }));

        await Expect(BirthDate).ToHaveValueAsync(string.Empty);
        await Expect(BirthDate).ToHaveAttributeAsync("placeholder", AppStrings.SelectBirthDate);
    }

    [TestMethod, EverySurface]
    public async Task TheCalendar_Should_NotOfferAFutureDate(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        await Press(Profile.GetByRole(AriaRole.Button, new() { Name = "Clear date" }));
        await Press(BirthDate);

        await Expect(View().GetByRole(AriaRole.Button, new() { Name = "Go to next month" })).ToBeDisabledAsync();
    }

    // One surface only: an upload is the Workbench's, so every view of the user shows it, and one made by this test on
    // another surface at the same time would bring the picture back after its removal.
    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task ThePicture_Should_BeRemovedAndChosenAgain(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.ProfileTitle);

        await Press(RemovePicture);

        await Expect(RemovePicture).ToHaveCountAsync(0);

        await ReloadTheViewOnceSaved("\"hasProfilePicture\":false");

        await Expect(RemovePicture).ToHaveCountAsync(0);

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Press(Profile.GetByRole(AriaRole.Button, new() { Name = AppStrings.UploadNewImage, Exact = true })));

        await chooser.SetFilesAsync(new FilePayload { Name = "picture.png", MimeType = "image/png", Buffer = [137, 80, 78, 71] });

        // The app's image editor can't open a picture this small, and offers to upload it as it is.
        await Press(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.UploadImageAsIs, Exact = true }));

        await Expect(RemovePicture).ToBeVisibleAsync();
    }

    // On the third user, whose picture no other test looks at or uploads, and on one surface only, as ThePicture_Should_BeRemovedAndChosenAgain.
    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task APictureAnotherViewUploaded_Should_ShowWhateverTheViewSavedBefore(WorkbenchSurface surface)
    {
        // This view had removed its picture, and saved the upload it showed last, one the Workbench forgot when it restarted.
        var (user, sessions) = SettingsSampleAccount.Sample(2, WorkbenchStrings.DemoUserZahra, DateTimeOffset.UtcNow);
        user.HasProfilePicture = false;

        var saved = JsonSerializer.Serialize(new
        {
            ShapedFor = "2/en-US/False/False",
            User = user,
            Sessions = sessions,
            AuthenticatorKey = SettingsSampleAccount.FirstKey,
            SeenUpload = "an upload from before a restart"
        });

        // A preview alone, with no shell around it.
        var remove = Page.GetByRole(AriaRole.Region, new() { Name = AppStrings.ProfileTitle, Exact = true }).GetByRole(AriaRole.Button, new() { Name = AppStrings.Remove, Exact = true });

        await OpenSettingsStore(saved, "&user=2");

        // Nothing uploaded since the restart: the picture stays removed.
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToHaveValueAsync(WorkbenchStrings.DemoUserZahra);
        await Expect(remove).ToHaveCountAsync(0);

        // Another view of the user uploads a picture.
        using (var client = Server.CreateClient())
        {
            using var form = new MultipartFormDataContent { { new ByteArrayContent(Convert.FromBase64String(OnePixelPng)), "file", "picture.png" } };
            using var upload = new HttpRequestMessage(HttpMethod.Post, "api/v1/Attachment/UploadUserProfilePicture") { Content = form };
            upload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WorkbenchClaims.AccessToken(WorkbenchClaims.Of(new() { User = 2 }), DateTimeOffset.UtcNow, elevatedUntil: null));

            using var response = await client.SendAsync(upload);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        await OpenSettingsStore(saved, "&user=2");

        await Expect(remove).ToBeVisibleAsync();
    }

    private const string OnePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
}
