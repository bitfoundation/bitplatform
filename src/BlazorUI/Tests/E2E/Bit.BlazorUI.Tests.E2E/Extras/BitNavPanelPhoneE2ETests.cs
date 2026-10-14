using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitNavPanel at /e2e/nav-panel/drawer on a phone viewport with touch: the off-canvas drawer opened from a
/// menu button of the page, its overlay, the dialog it is while it covers the page, the scroll lock, the focus it
/// takes, holds and hands back, the close button, Escape, a click on the overlay, a swipe towards its edge, a
/// navigation that selects an item and closes it, the search inside it, and a drawer that opts out of every one of
/// those (NoAutoClose, NoSwipe, NoFocusTrap, NoScrollLock, NoRestoreFocus). The desktop column is covered by
/// BitNavPanelE2ETests.
/// </summary>
[TestClass]
public class BitNavPanelPhoneE2ETests : E2ETestBase
{
    private const string Route = "/e2e/nav-panel/drawer";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = !IsFirefox
    };

    private ILocator Panel => Page.Locator("#npd-panel");

    private ILocator Overlay => Panel.Locator("xpath=preceding-sibling::div[contains(@class,'bit-npn-ovl')][1]");

    private ILocator Item(string text) => Page.Locator($"#npd-panel .bit-nav-ict[title='{text}']");

    private async Task OpenDrawerAsync()
    {
        await Page.Locator("#btn-menu").ClickAsync();
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("True");
        await Expect(Panel).ToBeVisibleAsync();
        await Page.WaitForFunctionAsync("e => Math.abs(e.getBoundingClientRect().left) < 1", await Panel.ElementHandleAsync());
    }

    private Task<bool> IsScrollLockedAsync()
    {
        return Page.EvaluateAsync<bool>("() => [document.documentElement, document.body].some(e => e.style.overflow === 'hidden')");
    }

    [TestMethod]
    public async Task Drawer_Closed_IsOffCanvasAndOutOfTheTabOrder()
    {
        await OpenAsync(Route);

        await Expect(Panel).ToHaveClassAsync(HasClass("bit-npn-drw"));
        await Expect(Panel).ToHaveClassAsync(HasClass("bit-npn-cls"));
        await Expect(Panel).ToBeHiddenAsync();
        await Expect(Panel).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Overlay).ToHaveCountAsync(0);
        Assert.IsFalse(await IsScrollLockedAsync());
    }

    [TestMethod]
    public async Task Drawer_TheMenuButton_OpensAModalDrawerOverAnOverlayAndLocksThePage()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        await Expect(Panel).ToHaveCSSAsync("position", "fixed");
        await Expect(Panel).ToHaveAttributeAsync("role", "dialog");
        await Expect(Panel).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Panel).ToHaveAttributeAsync("aria-label", "Main menu");
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Main menu" })).ToBeVisibleAsync();
        await Expect(Overlay).ToBeVisibleAsync();
        await Expect(Panel.Locator(".bit-npn-tbn")).ToBeHiddenAsync();
        await Expect(Panel.Locator(".bit-npn-cbn")).ToBeVisibleAsync();
        Assert.IsTrue(await IsScrollLockedAsync(), "The page behind the drawer is held.");

        var box = await Panel.BoundingBoxAsync();
        Assert.AreEqual(0, box!.X, 1);
        Assert.AreEqual(0, box.Y, 1);
        Assert.AreEqual(844, box.Height, 2, "The drawer runs the full height of the screen.");

        // The focus is taken into the drawer and held there.
        await Page.WaitForFunctionAsync("p => p.contains(document.activeElement)", await Panel.ElementHandleAsync());
        for (var i = 0; i < 6; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.IsTrue(await Panel.EvaluateAsync<bool>("p => p.contains(document.activeElement)"), $"Tab {i} left the drawer.");
        }
    }

    [TestMethod]
    public async Task Drawer_Escape_ClosesItAndHandsTheFocusBack()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
        await Expect(Panel).ToBeHiddenAsync();
        await Expect(Page.Locator("#btn-menu")).ToBeFocusedAsync();
        Assert.IsFalse(await IsScrollLockedAsync());
    }

    [TestMethod]
    public async Task Drawer_Escape_InAnActiveSearch_ClearsItBeforeClosing()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        var search = Panel.Locator(".bit-srb-inp");
        await search.FillAsync("set");
        await Expect(Item("Home")).ToHaveCountAsync(0);
        await Expect(Item("Settings")).ToBeVisibleAsync();

        await search.PressAsync("Escape");
        await Expect(search).ToHaveValueAsync("");
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("True");
        await Expect(Item("Home")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Drawer_TheOverlay_ClosesIt()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        await Overlay.TapAsync(new() { Position = new() { X = 370, Y = 400 } });
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
        await Expect(Overlay).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Drawer_TheCloseButton_ClosesIt()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        var close = Panel.Locator(".bit-npn-cbn");
        await Expect(close).ToHaveAttributeAsync("aria-label", new Regex(".+"));
        await close.TapAsync();
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Drawer_ASwipeTowardsItsEdge_ClosesIt()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        var box = (await Panel.BoundingBoxAsync())!;
        var y = box.Y + 500;
        await Page.Mouse.MoveAsync(box.X + box.Width - 20, y);
        await Page.Mouse.DownAsync();
        for (var x = box.X + box.Width - 30; x > box.X + 10; x -= 15)
        {
            await Page.Mouse.MoveAsync(x, y);
        }

        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
        await Expect(Panel).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Drawer_ASwipeTheOtherWay_LeavesItOpen()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        var box = (await Panel.BoundingBoxAsync())!;
        var y = box.Y + 500;
        await Page.Mouse.MoveAsync(box.X + 20, y);
        await Page.Mouse.DownAsync();
        for (var x = box.X + 30; x < box.X + box.Width - 10; x += 15)
        {
            await Page.Mouse.MoveAsync(x, y);
        }

        await Page.Mouse.UpAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("True");
        await Page.WaitForFunctionAsync("e => Math.abs(e.getBoundingClientRect().left) < 1", await Panel.ElementHandleAsync());
    }

    [TestMethod]
    public async Task Drawer_TappingAnItem_NavigatesSelectsItAndClosesTheDrawer()
    {
        await OpenAsync(Route);
        await OpenDrawerAsync();

        await Item("Settings").TapAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/nav-panel/drawer/settings$"));
        await Expect(Page.Locator("#npd-section")).ToHaveTextAsync("settings");
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#npd-clicks")).ToHaveTextAsync("Settings");

        await OpenDrawerAsync();
        await Expect(Item("Settings")).ToHaveAttributeAsync("aria-current", "page");

        // A group header only expands: it navigates nowhere, so the drawer stays.
        await Item("Products").TapAsync();
        await Expect(Item("Phones")).ToBeVisibleAsync();
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("True");
        await Item("Phones").TapAsync();
        await Expect(Page.Locator("#npd-section")).ToHaveTextAsync("phones");
        await Expect(Page.Locator("#npd-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Drawer_TheOptOuts_LeaveThePageTheFocusAndTheDrawerAlone()
    {
        await OpenAsync(Route);

        var opener = Page.Locator("#btn-open-noauto");
        await opener.ClickAsync();
        var panel = Page.Locator("#npd-noauto");
        await Expect(panel).ToBeVisibleAsync();

        // Neither the focus nor the page is held.
        await Expect(opener).ToBeFocusedAsync();
        Assert.IsFalse(await IsScrollLockedAsync());
        await Expect(panel).Not.ToHaveAttributeAsync("aria-modal", new Regex(".*"));

        // A navigation leaves it open.
        await panel.Locator(".bit-nav-ict[title='Relaxed home']").TapAsync();
        await Expect(Page.Locator("#npd-section")).ToHaveTextAsync("relaxed");
        await Expect(panel).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Rotation_FromAPhoneToAWideScreen_TheDrawerBecomesAColumn()
    {
        await OpenAsync(Route);

        await Expect(Panel).ToHaveClassAsync(HasClass("bit-npn-drw"));
        await Page.SetViewportSizeAsync(1280, 800);
        await Expect(Panel).ToHaveClassAsync(HasClass("bit-npn-col"));
        await Expect(Panel).ToBeVisibleAsync();
        await Expect(Panel).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Page.SetViewportSizeAsync(390, 844);
        await Expect(Panel).ToHaveClassAsync(HasClass("bit-npn-drw"));
        await Expect(Panel).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task ExpandOnHover_OnAWideScreen_WidensTheRailUnderThePointer()
    {
        await Page.SetViewportSizeAsync(1280, 800);
        await OpenAsync(Route);

        await Page.Locator("#btn-hover").ClickAsync();
        await Panel.Locator(".bit-npn-tbn").ClickAsync();
        await Expect(Page.Locator("#npd-toggled")).ToHaveTextAsync("True");
        await Page.Mouse.MoveAsync(1200, 400);
        await Page.WaitForFunctionAsync("e => e.getBoundingClientRect().width < 150", await Panel.ElementHandleAsync());

        await Panel.HoverAsync();
        await Page.WaitForFunctionAsync("e => e.getBoundingClientRect().width > 200", await Panel.ElementHandleAsync());
        await Expect(Page.Locator("#npd-toggled")).ToHaveTextAsync("True");
    }
}
