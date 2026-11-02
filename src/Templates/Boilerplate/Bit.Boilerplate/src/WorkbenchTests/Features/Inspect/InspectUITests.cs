namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>Inspect mode: a click on a component shows its parameters, and in dev mode a change goes into its tag in the code.</summary>
[TestClass, TestCategory("UITest")]
public class InspectUITests : WorkbenchPageTest
{
    private ILocator Heading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task Inspect_Should_ShowAComponentWithoutLettingItChange(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeInspect).ClickAsync();
        await Heading.ClickAsync();

        var panel = Page.Locator(".component-panel");

        await Expect(panel.Locator(".parameter").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
        await Expect(panel.GetByText(WorkbenchStrings.ComponentPanelNote)).ToHaveCountAsync(0);
        await Expect(panel.GetByText(WorkbenchStrings.ComponentTagNotFound)).ToHaveCountAsync(0);
        await Expect(panel.Locator("[role='switch']").First).ToBeDisabledAsync();
        await Expect(panel.Locator(".parameter input").First).ToBeDisabledAsync();
    }
}
