namespace Boilerplate.WorkbenchTests.Features.Checks;

/// <summary>The Checks tab: an accessibility check of the selected view and a search for colors outside the theme.</summary>
[TestClass, TestCategory("UITest")]
public class ChecksUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheChecksTab_Should_CheckTheSelectedView()
    {
        await OpenWorkbench("pos-dashboard", "?device=iphone-se");

        await Page.GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.ChecksTab }).ClickAsync();
        await Button(WorkbenchStrings.RunChecks).ClickAsync();

        await Expect(Page.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeFindings, 0))).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex($"^{Regex.Escape(WorkbenchStrings.AccessibilityFindings).Replace(@"\{0}", @"\d+")}$"))).ToBeVisibleAsync();
    }
}
