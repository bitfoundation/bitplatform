//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The app's bottom bar in a view: what it lists, and moving between pages from it with the shell following.</summary>
[TestClass, TestCategory("UITest")]
public class BottomBarUITests : AppViewPageTest
{
    private ILocator BarLink(string name) => BottomBar.GetByRole(AriaRole.Link, new() { Name = name, Exact = true });


    [TestMethod, EverySurface]
    public async Task ABottomBarLinkToAPageThatIsNotHere_Should_LeaveTheViewOnItsPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await Press(BarLink(AppStrings.Terms));

        await ExpectTheViewStillOn("settings", WorkbenchStrings.SettingsTitle);
        await Expect(View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToBeVisibleAsync();
    }
}
