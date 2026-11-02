//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The app's bottom bar in a view: what it lists, and moving between pages from it with the shell following.</summary>
[TestClass, TestCategory("UITest")]
public class BottomBarUITests : AppViewPageTest
{
    private ILocator BarLink(string name) => BottomBar.GetByRole(AriaRole.Link, new() { Name = name, Exact = true });

    [TestMethod, EverySurface]
    public async Task TheBottomBar_Should_ListThePageInTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        foreach (var name in new[] { AppStrings.Home, WorkbenchStrings.BuildingsTitle, AppStrings.Terms })
        {
            await Expect(BarLink(name)).ToBeVisibleAsync();
        }

        //#if (module == "Admin")
        await Expect(BarLink(AppStrings.Dashboard).First).ToBeVisibleAsync();
        //#endif
    }

    [TestMethod, EverySurface]
    public async Task TheBottomBar_Should_LeaveOutWhatASignedOutVisitorCannotOpen(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings", "?auth=signedout");

        await Expect(BarLink(AppStrings.Home)).ToBeVisibleAsync();
        await Expect(BarLink(WorkbenchStrings.SettingsTitle)).ToHaveCountAsync(0);

        //#if (module == "Admin")
        await Expect(BarLink(AppStrings.Dashboard)).ToHaveCountAsync(0);
        //#endif
    }

    [TestMethod, EverySurface]
    public async Task TheBottomBar_Should_ListThePageASignedOutVisitorCanOpen(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await Expect(BarLink(WorkbenchStrings.ThemePageTitle)).ToBeVisibleAsync();
    }

    //#if (module == "Admin")
    [TestMethod, EverySurface]
    public async Task TheBottomBarsDashboard_Should_MoveTheViewAndTheShellShouldFollow(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await Press(BarLink(AppStrings.Dashboard).First);

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.AppDashboardTitle);
        await Expect(View().GetByText(AppStrings.TotalProducts, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/dashboard"));
    }

    [TestMethod, EverySurface]
    public async Task OnTheDashboard_TheBottomBar_Should_NotListItTwice(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await Expect(BarLink(AppStrings.Dashboard).First).ToBeVisibleAsync();

        var links = await BarLink(AppStrings.Dashboard).CountAsync();

        await OpenWorkbench("dashboard");

        await Expect(BarLink(AppStrings.Dashboard)).ToHaveCountAsync(links);
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task ABottomBarLinkToAPageThatIsNotHere_Should_LeaveTheViewOnItsPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await Press(BarLink(AppStrings.Terms));

        await ExpectTheViewStillOn("settings", WorkbenchStrings.SettingsTitle);
        await Expect(View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToBeVisibleAsync();
    }

    // A phone's view is always narrow enough for the bottom bar, which goes away only in a wide view.
    [TestMethod, OnDesktops]
    public async Task AWideView_Should_HaveNoBottomBar(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=desktop-1536");

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.ThemePageTitle);
        await Expect(BottomBar).Not.ToBeVisibleAsync();
    }
}
