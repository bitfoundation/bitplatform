namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>
/// A phone has no mode buttons, so it doesn't offer Inspect and Edit text and a press acts on the page. Its tour still shows
/// Inspect in the wider screens' shell, and a phone turned sideways gets that shell too, and neither may leave a mode behind.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ModesOnAPhoneUITests : WorkbenchPageTest
{
    private ILocator Popover => Page.Locator(".bit-bonboarding-popover");

    private ILocator Customize => View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true });

    private ILocator Mode(string name) => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task ExpectThePageInUse()
    {
        await Expect(Page.Locator(".phone-view iframe")).ToBeVisibleAsync();
        await WaitForViews();
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("data-wb-mode", new Regex("."));

        await Press(Customize);

        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();
    }

    private async Task StartTheTour()
    {
        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();
    }

    private async Task AdvanceTo(string title)
    {
        string[] steps = [WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle, WorkbenchStrings.HomeWhyLiveTitle];

        foreach (var step in steps[..(Array.IndexOf(steps, title) + 1)])
        {
            await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourNext, Exact = true }));
            await Expect(Popover.GetByText(step, new() { Exact = true })).ToBeVisibleAsync();
        }
    }

    private async Task TurnSidewaysAndPick(string modeName)
    {
        var size = Page.ViewportSize!;

        await OpenWorkbench("pos-dashboard");

        await Page.SetViewportSizeAsync(size.Height, size.Width);
        await Expect(Page.Locator(".modes")).ToBeVisibleAsync();
        await WaitForViews();

        await Press(Mode(modeName));
        await Expect(Mode(modeName)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", new Regex("."));

        await Page.SetViewportSizeAsync(size.Width, size.Height);
        await Expect(Page.Locator(".phone-layout")).ToBeVisibleAsync();
    }

    [TestMethod, OnPhones]
    public async Task APhone_Should_OfferNoModeButtonsAroundTheViewOrInItsTools(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(Page.Locator(".modes")).ToHaveCountAsync(0);

        string[] modes = [WorkbenchStrings.ModeInspect, WorkbenchStrings.ModeText, WorkbenchStrings.ModeBrowse];

        foreach (var mode in modes)
        {
            await Expect(Button(mode)).ToHaveCountAsync(0);
        }

        await OpenTools();

        await Expect(Tools.GetByText(WorkbenchStrings.PageStatus, new() { Exact = true })).ToBeVisibleAsync();

        foreach (var mode in modes)
        {
            await Expect(Button(mode)).ToHaveCountAsync(0);
        }

        await Expect(Page.Locator(".component-panel")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".text-panel")).ToHaveCountAsync(0);
    }

    [TestMethod, OnPhones]
    public async Task APress_Should_ActOnThePage(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard");

        await ExpectThePageInUse();
    }

    [TestMethod, OnPhones]
    public async Task LeavingTheTourOnItsInspectStep_Should_LeaveNoModeBehind(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard");

        await StartTheTour();
        await AdvanceTo(WorkbenchStrings.HomeWhyRealTitle);

        await Expect(Mode(WorkbenchStrings.ModeInspect)).ToHaveAttributeAsync("aria-pressed", "true");

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page.Locator(".modes")).ToHaveCountAsync(0);
        await ExpectThePageInUse();
    }

    [TestMethod, OnPhones]
    public async Task LeavingTheTourOnItsCommentStep_Should_LeaveNoModeBehind(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard");

        await StartTheTour();
        await AdvanceTo(WorkbenchStrings.HomeWhyLiveTitle);

        await Press(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourSkip, Exact = true }));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page.Locator(".pick-banner")).ToHaveCountAsync(0);
        await ExpectThePageInUse();
    }
}
