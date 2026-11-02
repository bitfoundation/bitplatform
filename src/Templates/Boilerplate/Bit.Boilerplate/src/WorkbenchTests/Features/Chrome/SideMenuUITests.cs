//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The app's side menu in a view: what it offers each user and a signed-out visitor, moving between pages from it with the shell following, and its search and footer.</summary>
[TestClass, TestCategory("UITest")]
public class SideMenuUITests : AppViewPageTest
{
    private const string Wide = "?device=desktop-1536";

    private ILocator Search => SideMenu.GetByRole(AriaRole.Searchbox);

    private ILocator Overlay => View().Locator(".bit-npn-ovl");

    /// <summary>The page the shell has selected in its list of pages, which a phone keeps in the tools.</summary>
    private ILocator CurrentPageInTheShell => Page.GetByRole(AriaRole.Navigation, new() { Name = WorkbenchStrings.Pages, Exact = true }).Locator("[aria-current='page']");


    [TestMethod, EverySurface]
    public async Task EveryPageOfTheMenu_Should_OpenAndNameItself(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        foreach (var (link, title) in new[]
        {
            (WorkbenchStrings.PosDashboardTitle, WorkbenchStrings.PosDashboardTitle),
            (WorkbenchStrings.AssistantTitle, WorkbenchStrings.AssistantTitle),
            (WorkbenchStrings.ShiftsTitle, WorkbenchStrings.ShiftsTitle),
            (AppStrings.Settings, WorkbenchStrings.SettingsTitle),
            (WorkbenchStrings.ThemePageTitle, WorkbenchStrings.ThemePageTitle)
        })
        {
            await OpenSideMenu();
            await Press(SideMenuLink(link));

            await Expect(PageTitle).ToHaveTextAsync(title);
            await WaitForViews();
        }
    }


    [TestMethod, EverySurface]
    public async Task ALinkToAPageThatIsNotHere_Should_LeaveTheViewOnItsPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await OpenSideMenu();
        await Press(SideMenuLink(AppStrings.Terms));

        await Expect(Search).Not.ToBeInViewportAsync();
        await ExpectTheViewStillOn("settings", WorkbenchStrings.SettingsTitle);
        await Expect(View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheHomeItemAndTheLogo_Should_LeaveTheViewOnItsPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await OpenSideMenu();
        await Press(SideMenuLink(AppStrings.Home));
        await Expect(Search).Not.ToBeInViewportAsync();

        await ExpectTheViewStillOn("settings", WorkbenchStrings.SettingsTitle);

        await OpenSideMenu();
        await Press(SideMenu.Locator(".bit-npn-hln"));
        await CloseSideMenu();

        await ExpectTheViewStillOn("settings", WorkbenchStrings.SettingsTitle);
    }
}
