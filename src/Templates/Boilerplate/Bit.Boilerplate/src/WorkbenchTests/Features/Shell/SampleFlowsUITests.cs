namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>The flows the Workbench comes with, which open as real boards that nobody can change.</summary>
[TestClass, TestCategory("UITest")]
public class SampleFlowsUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheBuildingSample_Should_ShowItsScreensInTheirStates()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow(WorkbenchSampleFlows.BuildingsId)).ToString());

        await Expect(Page.Locator(".screen-card")).ToHaveCountAsync(3);
        await Expect(Page.FrameLocator("iframe[data-screen-id='form']").GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Page.FrameLocator("iframe[data-screen-id='map']").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task ASample_Should_BeReadOnlyEvenInDevMode()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow(WorkbenchSampleFlows.BuildingsId)).ToString());

        await Expect(Page.Locator("main.board.read-only")).ToBeVisibleAsync();
        await Expect(Page.Locator(".screen-remove")).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteFlow })).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheFlowsPage_Should_ListTheSamples()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flows).ToString());

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowBuildings })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();
    }
}
