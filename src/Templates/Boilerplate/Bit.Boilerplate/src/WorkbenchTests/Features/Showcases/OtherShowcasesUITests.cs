namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The other pages in a browser: the theme page and its overlays, the assistant, the shift planner, the settings copy, the home page and a flow's board.</summary>
[TestClass, TestCategory("UITest")]
public class OtherShowcasesUITests : WorkbenchPageTest
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

    [TestMethod]
    public async Task TheAssistant_Should_AnswerASuggestedQuestion()
    {
        await OpenPreview("assistant", "?device=desktop-1536");

        await Expect(Page.GetByText(WorkbenchStrings.AssistantWelcomeTitle)).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantSalesPrompt }).First.ClickAsync();

        await Expect(Page.GetByText("34,820").First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheShiftPlanner_Should_SumUpTheWeek()
    {
        await OpenPreview("shifts", "?device=desktop-1536");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading })).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex(Regex.Escape(WorkbenchStrings.ShiftsSummary).Replace(@"\{0}", @"\d+").Replace(@"\{1}", @"[\d.,]+")))).ToBeVisibleAsync();
    }

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

    [TestMethod]
    public async Task TheSettingsCopy_Should_AskASignedOutVisitorToSignIn()
    {
        await OpenPreview("settings", "?device=desktop-1536&auth=signedout");

        await Expect(Page.GetByText(AppStrings.YouAreNotAuthorized)).ToBeVisibleAsync();

        await OpenPreview("settings", "?device=desktop-1536");

        await Expect(Page.GetByText(AppStrings.YouAreNotAuthorized)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.DemoUserYas).First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheHomePage_Should_ListEveryPage()
    {
        await Page.GotoAsync(Server.Address.ToString());

        var cards = Page.Locator("a.page-card");

        await Expect(cards).ToHaveCountAsync(WorkbenchPageCatalog.Pages.Count);

        await cards.Filter(new() { HasText = WorkbenchStrings.BuildingsTitle }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/buildings"));
        await WaitForViews();
    }

    [TestMethod]
    public async Task AFlowsBoard_Should_ShowItsScreensAndStepThroughThem()
    {
        var (id, problem) = await Server.Agent.SaveFlow("Test Agent", new(
            $"Browsing the theme {Guid.NewGuid():N}",
            [new("light", "theme", "?device=iphone-se", "Light", null), new("dark", "theme", "?device=iphone-se&theme=dark", "Dark", null)],
            [new("light", "dark", "Switch the theme", null)],
            null,
            null), CancellationToken.None);

        Assert.IsNull(problem);

        await Page.GotoAsync(new Uri(Server.Address, $"flows/{id}").ToString());

        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(2);
        await Expect(Page.Locator(".screen-note")).ToHaveTextAsync(["Light", "Dark"]);

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Present }).ClickAsync();

        var presenter = Page.GetByRole(AriaRole.Toolbar, new() { Name = WorkbenchStrings.Present });

        await Expect(presenter).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenOf, 1, 2));

        await presenter.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.NextScreen }).ClickAsync();

        await Expect(presenter).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenOf, 2, 2));
    }

    [TestMethod]
    public async Task TheFlowsPage_Should_ListTheFlows()
    {
        var name = $"Listed flow {Guid.NewGuid():N}";

        await Server.Agent.SaveFlow("Test Agent", new(name, [new("only", "settings", "", null, null)], null, null, null), CancellationToken.None);

        await Page.GotoAsync(new Uri(Server.Address, "flows").ToString());

        await Expect(Page.GetByText(name)).ToBeVisibleAsync();
    }
}
