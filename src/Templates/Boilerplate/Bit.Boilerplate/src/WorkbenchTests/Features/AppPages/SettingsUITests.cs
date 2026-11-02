namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The copy of the app's Settings page.</summary>
[TestClass, TestCategory("UITest")]
public class SettingsUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheSettingsCopy_Should_AskASignedOutVisitorToSignIn()
    {
        await OpenPreview("settings", "?device=desktop-1536&auth=signedout");

        await Expect(Page.GetByText(AppStrings.YouAreNotAuthorized)).ToBeVisibleAsync();

        await OpenPreview("settings", "?device=desktop-1536");

        await Expect(Page.GetByText(AppStrings.YouAreNotAuthorized)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.DemoUserYas).First).ToBeVisibleAsync();
    }
}
