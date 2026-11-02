namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The app's chrome around a view: the side menu and the account menu, which change the view's scenario instead of calling a server.</summary>
[TestClass, TestCategory("UITest")]
public class AccountUITests : WorkbenchPageTest
{
    private ILocator VisibleButton(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).Locator("visible=true");

    private ILocator AccountMenu => Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DemoUserYas }).Locator("visible=true").First;

    [TestMethod]
    public async Task ANewView_Should_ListTheOtherAccountsWithTheFirstUserActive()
    {
        await OpenPreview("theme", "?device=desktop-1536");

        await AccountMenu.ClickAsync();

        await Expect(Page.GetByText(WorkbenchDemoUsers.All[0].Email).Locator("visible=true")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DemoUserSaleh }).Locator("visible=true")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DemoUserZahra }).Locator("visible=true")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SignOut_Should_AskAndThenSignTheViewOut()
    {
        await OpenPreview("theme", "?device=desktop-1536");

        await AccountMenu.ClickAsync();
        await VisibleButton(AppStrings.SignOut).ClickAsync();

        var dialog = Page.GetByRole(AriaRole.Alertdialog);

        await Expect(dialog.GetByText(AppStrings.SignOutPrompt)).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.Cancel, Exact = true }).ClickAsync();
        await Expect(AccountMenu).ToBeVisibleAsync();

        await AccountMenu.ClickAsync();
        await VisibleButton(AppStrings.SignOut).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut, Exact = true }).ClickAsync();

        await Expect(Page.Locator(".nav-panel").GetByText(AppStrings.SignIn, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SignInAndSignUp_Should_ExplainInsteadOfChangingTheView()
    {
        await OpenPreview("theme", "?device=desktop-1536&auth=signedout");

        var signUp = Page.Locator(".nav-panel").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true });

        await signUp.ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.SignInNote)).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Dialog).Locator("visible=true").GetByRole(AriaRole.Button).Last.ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.SignInNote)).ToHaveCountAsync(0);
        await Expect(signUp).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SignInOnAPhone_Should_PointAtTheUserPickerOverTheToolsPanel()
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Page.GotoAsync(new Uri(Server.Address, "/workbench/buildings?auth=signedout").ToString());

        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn, Exact = true }).Locator("visible=true").First.ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.SignInGuide)).ToBeVisibleAsync();

        var picker = Page.Locator("[data-bonboarding-anchor='sign-in']");

        await Expect(picker).ToBeInViewportAsync(new() { Ratio = 1 });

        var signedIn = picker.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SignedIn, Exact = true });

        await signedIn.ClickAsync();
        await Expect(signedIn).ToHaveAttributeAsync("aria-pressed", "true");

        await picker.GetByRole(AriaRole.Combobox).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.DemoUserZahra }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.GotIt, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.SignInGuide)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task SignUpInAView_Should_BringTheUserPickerIntoSightAndPointAtIt()
    {
        await OpenWorkbench("theme", "?device=desktop-1536&auth=signedout");

        await Page.Locator("#wb-tab-review").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Tools, Exact = true }).ClickAsync();
        await Expect(Page.Locator("#wb-tab-review")).Not.ToBeInViewportAsync();

        await View().Locator(".nav-panel").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.SignInGuide)).ToBeVisibleAsync();
        await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("[data-bonboarding-anchor='sign-in']")).ToBeInViewportAsync(new() { Ratio = 1 });

        var signedIn = Page.Locator("[data-bonboarding-anchor='sign-in']").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SignedIn, Exact = true });

        await signedIn.ClickAsync();
        await Expect(signedIn).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Locator("[data-bonboarding-anchor='sign-in']").GetByRole(AriaRole.Combobox).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.DemoUserZahra }).ClickAsync();
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DemoUserZahra }).Locator("visible=true").First).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.GotIt, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.SignInGuide)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheSideMenu_Should_OfferASignedOutVisitorOnlyTheThemePage()
    {
        await OpenPreview("theme", "?device=desktop-1536&auth=signedout");

        var menu = Page.Locator(".nav-panel");

        await Expect(menu.GetByText(WorkbenchStrings.ThemePageTitle, new() { Exact = true })).ToBeVisibleAsync();

        foreach (var title in new[] { WorkbenchStrings.PosDashboardTitle, WorkbenchStrings.BuildingsTitle, WorkbenchStrings.AssistantTitle, WorkbenchStrings.ShiftsTitle })
        {
            await Expect(menu.GetByText(title, new() { Exact = true })).ToHaveCountAsync(0);
        }

        await OpenPreview("theme", "?device=desktop-1536");

        await Expect(menu.GetByText(WorkbenchStrings.ShiftsTitle, new() { Exact = true })).ToBeVisibleAsync();
    }
}
