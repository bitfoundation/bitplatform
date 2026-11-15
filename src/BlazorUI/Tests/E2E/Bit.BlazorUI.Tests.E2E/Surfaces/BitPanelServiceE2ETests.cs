using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// BitPanelService in a real browser (/e2e/panel-service): a panel shown from code slides in, takes the focus and names
/// itself after its header; its content closes it with a result, after which it slides out and leaves the page and the
/// focus goes back to the button that showed it; the overlay and the Escape key dismiss it; its close guard keeps it open
/// until it agrees; a bottom sheet shown from code is resized by its grabber; and the open panel passes axe.
/// </summary>
[TestClass]
public class BitPanelServiceE2ETests : E2ETestBase
{
    private const string Route = "/e2e/panel-service";

    private ILocator Panel(string cssClass) => Page.Locator($".{cssClass}");

    private ILocator Container(string cssClass) => Page.Locator($".{cssClass} .bit-pnl-cnt");

    private async Task ShowAsync(string button, string cssClass)
    {
        // Pressed from the keyboard: a click does not focus a button in WebKit, which would leave the panel no element
        // to give the focus back to.
        await Page.Locator($"#{button}").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Container(cssClass)).ToHaveClassAsync(new Regex(@"\bbit-pnl-opn\b"));
        await Page.WaitForFunctionAsync("sel => { const e = document.querySelector(sel); return e && getComputedStyle(e).transform === 'none'; }", $".{cssClass} .bit-pnl-cnt");
    }

    [TestMethod]
    public async Task APanelShownFromCode_SlidesIn_AndTakesTheFocus()
    {
        await OpenAsync(Route);
        await ShowAsync("show-panel", "e2e-svc-panel");

        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "From code" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.Locator("#panel-text")).ToHaveTextAsync("Shown from code");

        var box = (await Container("e2e-svc-panel").BoundingBoxAsync())!;
        var width = await Page.EvaluateAsync<double>("() => window.innerWidth");
        Assert.AreEqual(width, box.X + box.Width, 1.5, "the panel sits on the end edge");
        Assert.AreEqual(320, box.Width, 1.5);

        await Page.WaitForFunctionAsync("() => document.querySelector('.e2e-svc-panel').contains(document.activeElement)");
    }

    [TestMethod]
    public async Task ItsContent_ClosesItWithAResult_AndItLeavesThePageAndGivesTheFocusBack()
    {
        await OpenAsync(Route);
        await ShowAsync("show-panel", "e2e-svc-panel");

        await Page.Locator("#panel-done").ClickAsync();

        await Expect(Page.Locator("#result")).ToHaveTextAsync("done");
        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("False");

        // Slides out, then leaves the page.
        await Expect(Panel("e2e-svc-panel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#show-panel")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TheOverlay_DismissesIt()
    {
        await OpenAsync(Route);
        await ShowAsync("show-panel", "e2e-svc-panel");

        await Page.Mouse.ClickAsync(10, 300);

        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#result")).ToHaveTextAsync("null");
        await Expect(Panel("e2e-svc-panel")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheEscapeKey_DismissesIt()
    {
        await OpenAsync(Route);
        await ShowAsync("show-panel", "e2e-svc-panel");
        await Page.WaitForFunctionAsync("() => document.querySelector('.e2e-svc-panel').contains(document.activeElement)");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("True");
        await Expect(Panel("e2e-svc-panel")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ItsCloseGuard_KeepsItOpenUntilItAgrees()
    {
        await OpenAsync(Route);
        await ShowAsync("show-guarded", "e2e-svc-guarded");

        await Container("e2e-svc-guarded").Locator(".bit-pnl-cls").ClickAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(500);
        await Expect(Container("e2e-svc-guarded")).ToHaveClassAsync(new Regex(@"\bbit-pnl-opn\b"));
        await Expect(Page.Locator("#result")).ToHaveTextAsync("open");

        await Page.Locator("#allow-close").EvaluateAsync("e => e.click()");
        await Container("e2e-svc-guarded").Locator(".bit-pnl-cls").ClickAsync();

        await Expect(Page.Locator("#dismissed")).ToHaveTextAsync("True");
        await Expect(Panel("e2e-svc-guarded")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ABottomSheetShownFromCode_IsResizedByItsGrabber()
    {
        await OpenAsync(Route);
        await ShowAsync("show-sheet", "e2e-svc-sheet");

        var vh = await Page.EvaluateAsync<double>("() => window.innerHeight");
        await Page.WaitForFunctionAsync("h => Math.abs(document.querySelector('.e2e-svc-sheet .bit-pnl-cnt').getBoundingClientRect().height - h) < 3", vh * 0.3);

        await Container("e2e-svc-sheet").Locator(".bit-pnl-grb").ClickAsync();

        await Page.WaitForFunctionAsync("h => Math.abs(document.querySelector('.e2e-svc-sheet .bit-pnl-cnt').getBoundingClientRect().height - h) < 3", vh * 0.6);
    }

    [TestMethod]
    public async Task TheOpenPanel_PassesAxe()
    {
        await OpenAsync(Route);
        await ShowAsync("show-panel", "e2e-svc-panel");

        await AssertNoAxeViolationsAsync(".e2e-svc-panel .bit-pnl-cnt");
    }
}
