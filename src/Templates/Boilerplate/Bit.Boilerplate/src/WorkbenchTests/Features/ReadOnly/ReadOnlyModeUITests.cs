namespace Boilerplate.WorkbenchTests.Features.ReadOnly;

/// <summary>
/// What someone sees outside dev mode, like at a shared demo link: what would change the code or the review is read-only and
/// says why, and what the browser keeps by itself stays free to change.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ReadOnlyModeUITests : WorkbenchPageTest
{
    protected override Task<WorkbenchTestServer> ServerToTest => WorkbenchTestsInitializer.ReadOnlyServer;

    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator Heading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    [TestMethod]
    public async Task Inspect_Should_ShowAComponentAndSayWhereItCanChange()
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeInspect).ClickAsync();
        await Heading.ClickAsync();

        var panel = Page.Locator(".component-panel");

        await Expect(panel.Locator(".parameter").First).ToBeVisibleAsync();
        await Expect(panel.GetByText(WorkbenchStrings.ChangesNeedDevMode)).ToBeVisibleAsync();
        await Expect(panel.GetByText(WorkbenchStrings.ComponentPanelNote)).ToHaveCountAsync(0);
        await Expect(panel.GetByText(WorkbenchStrings.ComponentTagNotFound)).ToHaveCountAsync(0);
        await Expect(panel.Locator("[role='switch']").First).ToBeDisabledAsync();
        await Expect(panel.Locator(".parameter input").First).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task EditText_Should_ShowEveryLanguageWithoutLettingItChange()
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await Heading.ClickAsync();

        var panel = Page.Locator(".text-panel");

        await Expect(panel.GetByText(WorkbenchStrings.ChangesNeedDevMode)).ToBeVisibleAsync();
        await Expect(panel.GetByLabel(CultureInfoManager.SupportedCultures[0].DisplayName, new() { Exact = true })).ToHaveValueAsync(WorkbenchStrings.ShiftsHeading);
        await Expect(panel.GetByLabel(CultureInfoManager.SupportedCultures[0].DisplayName, new() { Exact = true })).Not.ToBeEditableAsync();
        await Expect(panel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheReview_Should_ShowTheAgentAsOfflineAndTakeNoComment()
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await Expect(Page.Locator(".text-panel")).ToBeVisibleAsync();

        await Button(WorkbenchStrings.ModeComment).ClickAsync();

        await Expect(Page.Locator(".text-panel")).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.LiveReviewTitle)).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.AgentOffline)).ToBeVisibleAsync();
        await Expect(Button(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    public async Task TheFlowsTab_Should_SayHowFlowsAreDrawn()
    {
        await OpenWorkbench("shifts");

        await Page.Locator("#wb-tab-flows").ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.FlowsReadOnly)).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.FlowsEmpty)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task WhatTheBrowserKeeps_Should_StillChange()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        await Page.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch }).FillAsync("Rotterdam");
        await Expect(Page.Locator(".buildings-grid .bit-dtg-body .name-cell")).ToHaveCountAsync(4);

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => window.__workbenchReady === true");

        await Expect(Page.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch })).ToHaveValueAsync("Rotterdam");
    }
}
