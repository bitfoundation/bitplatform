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
    public async Task TheMenuButton_Should_OpenTheDrawerAndItsOverlayShouldCloseIt(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(Search).Not.ToBeInViewportAsync();

        await OpenSideMenu();

        await Expect(SideMenuLink(WorkbenchStrings.ThemePageTitle)).ToBeVisibleAsync();

        await CloseSideMenu();

        await Expect(Overlay).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnOpenDrawer_Should_StayOpenAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        await ReloadTheViewOnceSaved("\"IsNavPanelOpen\":true");

        await Expect(Search).ToBeInViewportAsync();
    }

    // A swipe takes a finger, and a desktop's view has a mouse.
    [TestMethod, OnPhones]
    public async Task ASwipe_Should_CloseTheDrawer(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        var drawer = await WorkbenchTestReviewer.WaitFor(async () => (await SideMenu.BoundingBoxAsync())!, box => box.X >= 0);

        await SwipeWithAFinger((drawer.Width - 10, 300), (10, 300));

        await Expect(Search).Not.ToBeInViewportAsync();
    }

    [TestMethod, EverySurface]
    public async Task AGroup_Should_CollapseAndExpand(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        await Expect(SideMenuLink(WorkbenchStrings.BuildingsTitle)).ToBeVisibleAsync();

        await Press(SideMenuGroup(WorkbenchStrings.NavShowcases));

        await Expect(SideMenuLink(WorkbenchStrings.BuildingsTitle)).ToHaveCountAsync(0);

        await Press(SideMenuGroup(WorkbenchStrings.NavShowcases));

        await Expect(SideMenuLink(WorkbenchStrings.BuildingsTitle)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ASignedOutVisitor_Should_BeOfferedOnlyThePagesTheyCanOpen(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await OpenSideMenu();

        await Expect(SideMenuLink(WorkbenchStrings.ThemePageTitle)).ToBeVisibleAsync();

        foreach (var title in new[] { AppStrings.Home, AppStrings.Terms, AppStrings.About })
        {
            await Expect(SideMenuLink(title)).ToBeVisibleAsync();
        }

        foreach (var title in new[] { WorkbenchStrings.PosDashboardTitle, WorkbenchStrings.BuildingsTitle, WorkbenchStrings.AssistantTitle, WorkbenchStrings.ShiftsTitle, AppStrings.Settings })
        {
            await Expect(SideMenuLink(title)).ToHaveCountAsync(0);
        }

        await Expect(SideMenuGroup(AppStrings.Management)).ToHaveCountAsync(0);

        //#if (module == "Admin")
        await Expect(SideMenuGroup(AppStrings.AdminPanel)).ToHaveCountAsync(0);
        //#endif
    }

    [TestMethod, EverySurface]
    public async Task AGlobalAdmin_Should_BeOfferedEverything(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        foreach (var title in new[] { WorkbenchStrings.PosDashboardTitle, WorkbenchStrings.BuildingsTitle, WorkbenchStrings.AssistantTitle, WorkbenchStrings.ShiftsTitle, AppStrings.Settings })
        {
            await Expect(SideMenuLink(title)).ToBeVisibleAsync();
        }

        //#if (module == "Admin")
        await Press(SideMenuGroup(AppStrings.AdminPanel));

        foreach (var title in new[] { AppStrings.Dashboard, AppStrings.Categories, AppStrings.Products })
        {
            await Expect(SideMenuLink(title)).ToBeVisibleAsync();
        }
        //#endif

        await Press(SideMenuGroup(AppStrings.Management));

        foreach (var title in new[] { AppStrings.UserGroups, AppStrings.Users, AppStrings.OAuthClients, AppStrings.Operations })
        {
            await Expect(SideMenuLink(title)).ToBeVisibleAsync();
        }

        //#if (signalR == true)
        await Expect(SideMenuLink(AppStrings.SystemPromptsTitle)).ToBeVisibleAsync();
        //#endif

        //#if (multitenant == true)
        await Expect(SideMenuLink(AppStrings.ManageAllTenants)).ToBeVisibleAsync();
        //#endif
    }

    //#if (multitenant == true)
    [TestMethod, EverySurface]
    public async Task ATenantAdmin_Should_ManageTheTenantButNotTheSystem(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?user=1");

        await OpenSideMenu();
        await Press(SideMenuGroup(AppStrings.Management));

        foreach (var title in new[] { AppStrings.UserGroups, AppStrings.Users })
        {
            await Expect(SideMenuLink(title)).ToBeVisibleAsync();
        }

        foreach (var title in new[] { AppStrings.OAuthClients, AppStrings.Operations, AppStrings.ManageAllTenants })
        {
            await Expect(SideMenuLink(title)).ToHaveCountAsync(0);
        }
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task ADemoUser_Should_NotBeOfferedManagement(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?user=2");

        await OpenSideMenu();

        await Expect(SideMenuLink(AppStrings.Settings)).ToBeVisibleAsync();
        await Expect(SideMenuGroup(AppStrings.Management)).ToHaveCountAsync(0);

        //#if (module == "Admin")
        await Press(SideMenuGroup(AppStrings.AdminPanel));

        await Expect(SideMenuLink(AppStrings.Dashboard)).ToBeVisibleAsync();
        //#endif
    }

    [TestMethod, EverySurface]
    public async Task ThePickerInTheTools_Should_ChangeWhatTheMenuOffers(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        await Expect(SideMenuGroup(AppStrings.Management)).ToBeVisibleAsync();

        await CloseSideMenu();

        await OpenTools();
        await PickUser(WorkbenchStrings.DemoUserZahra);
        await CloseTools();

        await WaitForTheViewToReloadWith("user=2");
        await Expect(AccountButton(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();

        await OpenSideMenu();

        await Expect(SideMenuGroup(AppStrings.Management)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnItem_Should_MoveTheViewToThatPageAndTheShellShouldFollow(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();
        await Press(SideMenuLink(WorkbenchStrings.BuildingsTitle));

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.BuildingsTitle);
        await Expect(View().GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch })).ToBeVisibleAsync();
        await Expect(Search).Not.ToBeInViewportAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/buildings"));

        await OpenTools();

        await Expect(CurrentPageInTheShell).ToContainTextAsync(WorkbenchStrings.BuildingsTitle);
    }

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

    //#if (module == "Admin")
    [TestMethod, EverySurface]
    public async Task TheDashboardItem_Should_MoveTheViewToTheDashboard(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();
        await Press(SideMenuGroup(AppStrings.AdminPanel));
        await Press(SideMenuLink(AppStrings.Dashboard));

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.AppDashboardTitle);
        await Expect(View().GetByText(AppStrings.TotalProducts, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/dashboard"));
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task MovingToAnotherPage_Should_KeepTheViewsSettings(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?theme=dark&auth=signedin&user=1");

        await OpenSideMenu();
        await Press(SideMenuLink(WorkbenchStrings.ShiftsTitle));

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.ShiftsTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/shifts"));
        await Expect(Page).ToHaveURLAsync(new Regex("theme=dark"));
        await Expect(Page).ToHaveURLAsync(new Regex("auth=signedin"));
        await Expect(Page).ToHaveURLAsync(new Regex("user=1"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(AccountButton(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();
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

    [TestMethod, EverySurface]
    public async Task TheSearch_Should_FilterTheItemsAndSayWhenNothingMatches(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        await Search.FillAsync(WorkbenchStrings.BuildingsTitle);

        await Expect(SideMenuLink(WorkbenchStrings.BuildingsTitle)).ToBeVisibleAsync();
        await Expect(SideMenuLink(WorkbenchStrings.ShiftsTitle)).ToHaveCountAsync(0);
        await Expect(SideMenuLink(AppStrings.Terms)).ToHaveCountAsync(0);

        await Search.FillAsync($"{Guid.NewGuid():N}");

        await Expect(SideMenuLink(WorkbenchStrings.BuildingsTitle)).ToHaveCountAsync(0);
        await Expect(SideMenu.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);

        await Search.FillAsync(string.Empty);

        await Expect(SideMenuLink(WorkbenchStrings.ShiftsTitle)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheFootersSignInButtons_Should_PointAtTheUserPicker(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        foreach (var name in new[] { AppStrings.SignIn, AppStrings.SignInByModal, AppStrings.SignUp })
        {
            await OpenSideMenu();
            await Press(SideMenu.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }));

            await Expect(SignInGuide).ToBeVisibleAsync();

            await Press(Button(WorkbenchStrings.GotIt));

            await Expect(SignInGuide).ToHaveCountAsync(0);

            await CloseTools();
            await CloseSideMenu();
        }
    }

    [TestMethod, EverySurface]
    public async Task TheFooter_Should_ShowTheVersionToEveryone(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await OpenSideMenu();

        await Expect(SideMenu.Locator(".app-version")).ToBeVisibleAsync();
    }

    //#if (multitenant == true)
    [TestMethod, EverySurface]
    public async Task TheFooter_Should_ShowTheTenantPickedInTheAccountMenu(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenSideMenu();

        var tenant = SideMenu.Locator(".current-tenant");

        await Expect(tenant).ToContainTextAsync(WorkbenchStrings.DemoTenantStore);

        await CloseSideMenu();

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.Tenant));
        await Press(AccountMenu.GetByText(WorkbenchStrings.DemoTenantHeadOffice, new() { Exact = true }));
        await CloseAccountMenu();

        await OpenSideMenu();

        await Expect(tenant).ToContainTextAsync(WorkbenchStrings.DemoTenantHeadOffice);

        await ReloadTheViewOnceSaved($"\"TenantKey\":\"{nameof(WorkbenchStrings.DemoTenantHeadOffice)}\"");

        await Expect(tenant).ToContainTextAsync(WorkbenchStrings.DemoTenantHeadOffice);
    }

    [TestMethod, EverySurface]
    public async Task TheFooter_Should_ShowNoTenantToASignedOutVisitor(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await OpenSideMenu();

        await Expect(SideMenu.Locator(".current-tenant")).ToHaveCountAsync(0);
    }
    //#endif

    // A phone's screen is too narrow for the column, which only a wide view shows.
    [TestMethod, OnDesktops]
    public async Task AWideView_Should_ShowTheMenuAsAColumnWithoutItsButton(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", Wide);

        await Expect(SideMenuLink(WorkbenchStrings.ThemePageTitle)).ToBeVisibleAsync();
        await Expect(Header.GetByRole(AriaRole.Button, new() { Name = AppStrings.OpenNavigationMenuTitle, Exact = true })).ToHaveCountAsync(0);
        await Expect(Overlay).ToHaveCountAsync(0);

        await Press(SideMenuLink(WorkbenchStrings.BuildingsTitle));

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.BuildingsTitle);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/buildings"));
        await Expect(CurrentPageInTheShell).ToContainTextAsync(WorkbenchStrings.BuildingsTitle);
    }

    // The column to collapse is only in a wide view, which a phone's screen is too narrow for.
    [TestMethod, OnDesktops]
    public async Task ACollapsedColumn_Should_ShowOnlyIconsAndStayCollapsedAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", $"{Wide}&auth=signedout");

        var signUp = SideMenu.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true });

        await Expect(signUp).ToContainTextAsync(AppStrings.SignUp);
        await Expect(Search).ToBeVisibleAsync();

        var toggle = SideMenu.Locator(".bit-npn-tbn");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        await Press(toggle);

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Search).Not.ToBeVisibleAsync();
        await Expect(signUp).Not.ToContainTextAsync(AppStrings.SignUp);
        await Expect(SideMenu.Locator(".app-version")).ToHaveCountAsync(0);

        await ReloadTheViewOnceSaved("\"IsNavPanelToggled\":true");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");

        await Press(toggle);

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Search).ToBeVisibleAsync();
    }

    // A tablet-sized view is wider than a phone's screen.
    [TestMethod, OnDesktops]
    public async Task ATabletView_Should_StillReachTheMenu(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=ipad-mini");

        var link = SideMenuLink(WorkbenchStrings.ShiftsTitle);

        if (await Header.GetByRole(AriaRole.Button, new() { Name = AppStrings.OpenNavigationMenuTitle, Exact = true }).IsVisibleAsync())
        {
            await OpenSideMenu();
        }

        await Expect(link).ToBeInViewportAsync();

        await Press(link);

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.ShiftsTitle);
    }
}
