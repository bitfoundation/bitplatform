namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The app's header in a view, the device's status bar over it, and the switch in the tools that takes the app's chrome away.</summary>
[TestClass, TestCategory("UITest")]
public class HeaderUITests : AppViewPageTest
{
    [TestMethod, EverySurface]
    public async Task TheHeader_Should_NameThePageAndTheUser(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts", "?user=2");

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.ShiftsTitle);
        await Expect(AccountButton(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheHeader_Should_ShowASignedOutVisitorTheMenuWithoutAName(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.ThemePageTitle);
        await Expect(AccountButton(AppStrings.AccountMenuTitle)).ToBeVisibleAsync();
        await Expect(Header.Locator(".menu-displayname")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheStatusBar_Should_ShowInAPhonesFrameAndNotOnARealPhone(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        if (IsPhone)
        {
            await Expect(View().Locator(".status-bar")).ToHaveCountAsync(0);
        }
        else
        {
            await Expect(View().Locator(".status-bar")).ToContainTextAsync("9:41");
        }
    }

    // A phone's screen is too narrow for the name beside the picture and has no status bar of its own to draw.
    [TestMethod, OnDesktops]
    public async Task AWideHeader_Should_NameTheUserBesideThePictureUnderNoStatusBar(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=desktop-1536&user=1");

        await Expect(Header.Locator(".menu-displayname")).ToHaveTextAsync(WorkbenchStrings.DemoUserSaleh);
        await Expect(View().Locator(".status-bar")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheAppChromeSwitch_Should_TakeTheChromeAwayAndBringItBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await Expect(Header).ToBeVisibleAsync();
        await Expect(BottomBar).ToBeVisibleAsync();

        var chrome = Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.AppChrome });

        await OpenTools();
        await Press(chrome);
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex("chrome=false"));
        await WaitForViews();
        await Expect(Header).ToHaveCountAsync(0);
        await Expect(BottomBar).ToHaveCountAsync(0);
        await Expect(SideMenu).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToBeVisibleAsync();

        await OpenTools();
        await Press(chrome);
        await CloseTools();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("chrome="));
        await WaitForViews();
        await Expect(Header).ToBeVisibleAsync();
        await Expect(BottomBar).ToBeVisibleAsync();
    }
}
