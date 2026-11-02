namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Profile section: the form with its validation and draft, the birth date and gender, and the picture.</summary>
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
}
