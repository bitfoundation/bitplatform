namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The theme page: the theme's colors and every component the app uses, in their states and overlays.</summary>
[TestClass, TestCategory("UITest")]
public class ThemePageUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheThemePage_Should_OpenAnOverlayALinkNames()
    {
        await OpenPreview("theme", "?device=desktop-1536&sw-overlay=dialog");

        await Expect(Page.Locator("[role='dialog'], [role='alertdialog']").Filter(new() { HasText = WorkbenchStrings.ThemeDialogTitle })).ToBeVisibleAsync();

        await OpenPreview("theme", "?device=desktop-1536");

        await Expect(Page.Locator("[role='dialog'], [role='alertdialog']").Filter(new() { HasText = WorkbenchStrings.ThemeDialogTitle })).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.ThemeStatesNote)).ToBeVisibleAsync();
    }
}
