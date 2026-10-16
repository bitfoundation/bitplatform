using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// End-to-end tests of BitPanel in a real desktop browser (/e2e/panel and /e2e/panel/prerender): the closed panel left in
/// the page inert and off the screen; every Placement entering from its own edge, Size and FullSize; the bound IsOpen, the
/// Open/Close/Toggle methods and the order and arguments of every event (OnOpen, OnToggle, OnTransitionEnd, OnOverlayClick,
/// OnEscapeKeyDown, OnDismissing with its reasons, OnDismiss); the header/body/footer chrome; ModeFull, Blocking,
/// NoDismissOnEscape and Modeless; refusing a dismissal; the dialog semantics, Role and IsAlert; focus moved in, trapped and
/// restored and the No* opt-outs; the page scroll lock, NoScrollLock, AutoToggleScroll, ScrollerSelector and the gesture
/// forwarding; mouse swipes towards each edge with SwipeTrigger and NoSwipe; nested panels where one Escape closes one layer
/// and ZIndex; AbsolutePosition; KeepMounted; BitParams; close icons; Style/Class/Styles/Classes and the --bit-Panel-*
/// variables; RTL, explicit and inherited; reduced motion; the base parameters and Disabled; prerendering and unmounting.
/// The touch swipe on a phone is in <see cref="BitPanelTouchE2ETests"/>.
/// </summary>
[TestClass]
public class BitPanelE2ETests : E2ETestBase
{
    private const string PagePath = "/e2e/panel";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Container(string id) => Page.Locator($"#{id} > .bit-pnl-cnt");

    private ILocator Overlay(string id) => Page.Locator($"#{id} > .bit-pnl-ovl");

    private ILocator Part(string id, string selector) => Page.Locator($"#{id} > .bit-pnl-cnt {selector}");

    private async Task OpenPanelAsync(string buttonId, string panelId, bool waitForFocus = true)
    {
        await PressAsync(Page.Locator($"#{buttonId}"));
        await WaitOpenAsync(panelId, waitForFocus);
    }

    private async Task WaitOpenAsync(string panelId, bool waitForFocus = true)
    {
        await Expect(Container(panelId)).ToHaveClassAsync(HasClass("bit-pnl-opn"));
        await WaitForTransitionsAsync(panelId);

        if (waitForFocus) await WaitForFocusInsideAsync(panelId);
    }

    // The panel slides in and out with CSS transitions, so anything measured is measured once they have run.
    private Task WaitForTransitionsAsync(string panelId)
    {
        return Root(panelId).EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished.catch(() => null)))");
    }

    // Presses a button the way that leaves the focus on it in every engine: WebKit does not focus a button it clicks, so
    // there it is focused and activated with the keyboard - which is what a focus handed back to it is measured against.
    private async Task PressAsync(ILocator button)
    {
        if (IsWebKit)
        {
            await button.FocusAsync();
            await button.PressAsync("Enter");
        }
        else
        {
            await button.ClickAsync();
        }
    }

    private Task WaitForFocusInsideAsync(string id)
    {
        return Page.WaitForFunctionAsync("id => { const el = document.querySelector(`#${id} > .bit-pnl-cnt`); return !!el && el.contains(document.activeElement); }", id);
    }

    private Task<bool> IsFocusInsideAsync(string id)
    {
        return Page.EvaluateAsync<bool>("id => document.querySelector(`#${id} > .bit-pnl-cnt`)?.contains(document.activeElement) ?? false", id);
    }

    private Task ClickOverlayAsync(string id, float x = 4, float y = 300)
    {
        return Overlay(id).ClickAsync(new() { Position = new() { X = x, Y = y } });
    }

    private Task<string> BodyOverflowAsync()
    {
        return Page.EvaluateAsync<string>("() => document.body.style.overflow");
    }

    private Task<double[]> ViewportAsync()
    {
        return Page.EvaluateAsync<double[]>("() => [document.documentElement.clientWidth, window.innerHeight]");
    }

    // The panel's event log, in order: OnTransitionEnd comes after the OnToggle and OnOpen (or OnToggle and OnDismiss) of the
    // change it ends, even on an engine whose transition ends while the open and close sequences make their round trips.
    private async Task ExpectBasicLogAsync(string expected, bool contains = false)
    {
        try
        {
            await Page.WaitForFunctionAsync(@"([expected, contains]) => {
                const actual = document.getElementById('basic-log').textContent;
                return contains ? actual.includes(expected) : actual === expected;
            }", new object[] { expected, contains }, new() { Timeout = DefaultTimeout });
        }
        catch (TimeoutException)
        {
            Assert.Fail($"basic-log: expected '{expected}', was '{await Page.Locator("#basic-log").TextContentAsync()}'");
        }
    }

    // The translation the stylesheet gives the closed panel, which says the edge it slides in from.
    private Task<double[]> TranslationAsync(string id)
    {
        return Container(id).EvaluateAsync<double[]>("el => { const m = new DOMMatrix(getComputedStyle(el).transform); return [m.m41, m.m42]; }");
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        return (await locator.BoundingBoxAsync())!;
    }

    // A mouse drag of the panel. The swipe follows pointer events on desktop. It starts on the side of the element away
    // from the direction of the drag, so the whole drag stays inside the window: a pointer leaving the window leaves the
    // panel, which ends the gesture where it is.
    private async Task MouseSwipeAsync(ILocator from, double dx, double dy)
    {
        if (IsWebKit) Assert.Inconclusive("Playwright's WebKit reports a screenX of 0 on every pointer event, and the swipe measures the drag in screen coordinates.");

        var box = await BoxAsync(from);
        var x = dx > 0 ? box.X + 12 : dx < 0 ? box.X + box.Width - 12 : box.X + box.Width / 2;
        var y = dy > 0 ? box.Y + 10 : dy < 0 ? box.Y + box.Height - 10 : box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= 10; i++)
        {
            await Page.Mouse.MoveAsync((float)(x + dx * i / 10), (float)(y + dy * i / 10));
        }
        await Page.Mouse.UpAsync();
    }

    // ---------- Basic ----------

    [TestMethod]
    public async Task Basic_ClosedPanel_StaysInThePageInertAndOffTheScreen()
    {
        await OpenAsync(PagePath);

        var root = Root("basic-panel");
        await Expect(root).ToHaveClassAsync(HasClass("bit-pnl"));
        await Expect(root).ToHaveAttributeAsync("inert", "");
        await Expect(Container("basic-panel")).Not.ToHaveClassAsync(HasClass("bit-pnl-opn"));
        await Expect(Container("basic-panel")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Container("basic-panel")).Not.ToHaveAttributeAsync("aria-modal", new Regex(".*"));
        await Expect(Overlay("basic-panel")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#basic-content")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Basic_Open_SlidesInFromTheEndAsAModalDialogAndRaisesTheOpeningEvents()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");

        var container = Container("basic-panel");
        await Expect(container).ToHaveClassAsync(HasClass("bit-pnl-end"));
        await Expect(container).ToHaveAttributeAsync("role", "dialog");
        await Expect(container).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(container).ToHaveAttributeAsync("aria-label", "Basic panel");
        await Expect(container).ToHaveCSSAsync("transform", "none");
        await Expect(Root("basic-panel")).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Expect(Page.Locator("#basic-content")).ToBeVisibleAsync();

        await Expect(Overlay("basic-panel")).ToHaveClassAsync(HasClass("bit-pnl-ovl-opn"));
        await Expect(Overlay("basic-panel")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        var viewport = await ViewportAsync();
        var box = await BoxAsync(container);
        Assert.AreEqual(viewport[0], box.X + box.Width, 1, "The panel is against the right edge.");

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("True");
        await ExpectBasicLogAsync("OnToggle:True,OnOpen,OnTransitionEnd:True");
    }

    [TestMethod]
    public async Task Basic_EscapeKey_ClosesThroughOnDismissingAndSlidesTheContentOut()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");
        await ExpectBasicLogAsync("OnToggle:True,OnOpen,OnTransitionEnd:True");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await ExpectBasicLogAsync("OnToggle:True,OnOpen,OnTransitionEnd:True,OnEscapeKeyDown:Escape,OnDismissing:Escape:n,OnToggle:False,OnDismiss,OnTransitionEnd:False");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-content")).ToHaveCountAsync(0);
        await Expect(Root("basic-panel")).ToHaveAttributeAsync("inert", "");
        await Expect(Container("basic-panel")).ToHaveCSSAsync("visibility", "hidden");
    }

    [TestMethod]
    public async Task Basic_OverlayClick_ClosesWithTheClickOnTheDismissArguments()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");

        await ClickOverlayAsync("basic-panel");

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await ExpectBasicLogAsync("OnOverlayClick,OnDismissing:Overlay:m,OnToggle:False,OnDismiss", contains: true);
    }

    [TestMethod]
    public async Task Basic_PageSetsIsOpenFalse_ClosesAndReportsOnDismissWithoutAskingOnDismissing()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");
        await ExpectBasicLogAsync("OnToggle:True,OnOpen,OnTransitionEnd:True");

        await Page.Locator("#basic-page-close").ClickAsync();

        await ExpectBasicLogAsync("OnToggle:True,OnOpen,OnTransitionEnd:True,OnToggle:False,OnDismiss,OnTransitionEnd:False");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Basic_OpenCloseAndToggleMethods_DriveThePanel()
    {
        await OpenAsync(PagePath);

        await Page.Locator("#btn-basic-method-open").ClickAsync();
        await WaitOpenAsync("basic-panel");
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("True");

        await Page.Locator("#basic-method-close").ClickAsync();
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await ExpectBasicLogAsync("OnDismissing:Programmatic:n,OnToggle:False,OnDismiss", contains: true);

        await Page.Locator("#btn-basic-toggle").ClickAsync();
        await WaitOpenAsync("basic-panel");
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Basic_Open_MovesTheFocusInTrapsTabAndRestoresItOnClose()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");

        await Expect(Page.Locator("#basic-input")).ToBeFocusedAsync();

        for (var i = 0; i < 4; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.IsTrue(await IsFocusInsideAsync("basic-panel"), $"Tab {i + 1} stays inside the panel.");
        }

        await Expect(Page.Locator("#basic-input")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#basic-rename")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#btn-basic-open")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Basic_Open_HoldsThePageAndHandsItBackOnClose()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");
    }

    [TestMethod]
    public async Task Basic_ParameterChangedWhileOpen_ReRendersTheContent()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");

        await Page.Locator("#basic-rename").ClickAsync();

        await Expect(Page.Locator("#basic-heading")).ToHaveTextAsync("Renamed");
    }

    // ---------- Chrome ----------

    [TestMethod]
    public async Task HeaderTextFooterTextAndCloseButton_BuildTheChromeAroundAScrollingBody()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-headertext-open", "headertext-panel");

        var header = Part("headertext-panel", ".bit-pnl-hcn > .bit-pnl-hdr");
        await Expect(header).ToHaveTextAsync("Notifications");
        await Expect(header).ToHaveAttributeAsync("role", "heading");
        await Expect(header).ToHaveAttributeAsync("aria-level", "2");
        await Expect(Container("headertext-panel")).ToHaveClassAsync(HasClass("bit-pnl-sec"));
        await Expect(Container("headertext-panel")).ToHaveAttributeAsync("aria-labelledby", (await header.GetAttributeAsync("id"))!);
        await Expect(Part("headertext-panel", ".bit-pnl-ftr")).ToHaveTextAsync("Updated a minute ago");

        var close = Part("headertext-panel", ".bit-pnl-hcn > .bit-pnl-cls");
        await Expect(close).ToHaveAttributeAsync("title", "Close notifications");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Close notifications");
        await Expect(close.Locator(".bit-pnl-cli")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        // The body takes the scrolling, so the header and the footer stay put.
        var headerBefore = await BoxAsync(header);
        await Part("headertext-panel", ".bit-pnl-bdy").EvaluateAsync("el => el.scrollTop = 500");
        await Page.WaitForFunctionAsync("() => document.querySelector('#headertext-panel .bit-pnl-bdy').scrollTop > 0");
        Assert.AreEqual(headerBefore.Y, (await BoxAsync(header)).Y, 1);
        await Expect(Part("headertext-panel", ".bit-pnl-ftr")).ToBeInViewportAsync();

        await close.ClickAsync();
        await Expect(Page.Locator("#headertext-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task HeaderBodyAndFooterTemplates_RenderInTheirSlotsAndAriaLabelNamesThePanel()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-template-open", "template-panel");

        await Expect(Part("template-panel", ".bit-pnl-hdr #template-header")).ToHaveTextAsync("Filters");
        await Expect(Part("template-panel", ".bit-pnl-hdr")).Not.ToHaveAttributeAsync("role", "heading");
        await Expect(Part("template-panel", ".bit-pnl-bdy #template-body")).ToBeVisibleAsync();
        await Expect(Part("template-panel", ".bit-pnl-ftr #template-apply")).ToBeVisibleAsync();
        await Expect(Container("template-panel")).ToHaveAttributeAsync("aria-label", "Filters");
        await Expect(Container("template-panel")).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".+"));

        await Page.Locator("#template-apply").ClickAsync();
        await Expect(Page.Locator("#template-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ShowCloseButton_Alone_BuildsAHeaderRowWithOnlyTheButton()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-closeonly-open", "closeonly-panel");

        await Expect(Part("closeonly-panel", ".bit-pnl-hcn > .bit-pnl-cls")).ToBeVisibleAsync();
        await Expect(Part("closeonly-panel", ".bit-pnl-hdr")).ToHaveCountAsync(0);
        await Expect(Part("closeonly-panel", ".bit-pnl-ftr")).ToHaveCountAsync(0);
    }

    // ---------- Placement and size ----------

    [TestMethod]
    [DataRow("", "bit-pnl-end", "right")]
    [DataRow("Top", "bit-pnl-top", "top")]
    [DataRow("Bottom", "bit-pnl-bottom", "bottom")]
    [DataRow("Start", "bit-pnl-start", "left")]
    [DataRow("End", "bit-pnl-end", "right")]
    [DataRow("Left", "bit-pnl-left", "left")]
    [DataRow("Right", "bit-pnl-right", "right")]
    [DataRow("Center", "bit-pnl-end", "right")]
    [DataRow("TopAndBottom", "bit-pnl-end", "right")]
    [DataRow("StartAndEnd", "bit-pnl-end", "right")]
    public async Task Placement_EachValue_SlidesInFromItsEdgeAtTheGivenSize(string placement, string cssClass, string edge)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#placement-select").SelectOptionAsync(placement);
        await Expect(Container("position-panel")).ToHaveClassAsync(HasClass(cssClass));
        await WaitForTransitionsAsync("position-panel");

        // Closed, the panel waits just beyond its edge.
        var closed = await TranslationAsync("position-panel");
        switch (edge)
        {
            case "right": Assert.IsTrue(closed[0] > 0 && closed[1] == 0, $"{closed[0]},{closed[1]}"); break;
            case "left": Assert.IsTrue(closed[0] < 0 && closed[1] == 0, $"{closed[0]},{closed[1]}"); break;
            case "top": Assert.IsTrue(closed[1] < 0 && closed[0] == 0, $"{closed[0]},{closed[1]}"); break;
            case "bottom": Assert.IsTrue(closed[1] > 0 && closed[0] == 0, $"{closed[0]},{closed[1]}"); break;
        }

        await OpenPanelAsync("btn-position-open", "position-panel");
        await Expect(Page.Locator("#position-transition")).ToHaveTextAsync("open");
        await Expect(Container("position-panel")).ToHaveCSSAsync("transform", "none");

        var viewport = await ViewportAsync();
        var box = await BoxAsync(Container("position-panel"));
        switch (edge)
        {
            case "right":
                Assert.AreEqual(viewport[0], box.X + box.Width, 1);
                Assert.AreEqual(300, box.Width, 1);
                Assert.AreEqual(viewport[1], box.Height, 1);
                break;
            case "left":
                Assert.AreEqual(0, box.X, 1);
                Assert.AreEqual(300, box.Width, 1);
                break;
            case "top":
                Assert.AreEqual(0, box.Y, 1);
                Assert.AreEqual(300, box.Height, 1);
                Assert.AreEqual(viewport[0], box.Width, 1);
                break;
            case "bottom":
                Assert.AreEqual(viewport[1], box.Y + box.Height, 1);
                Assert.AreEqual(300, box.Height, 1);
                break;
        }
    }

    [TestMethod]
    public async Task FullSize_CoversTheWholeScreenOverTheSize()
    {
        await OpenAsync(PagePath);
        await Page.Locator("#fullsize-check").CheckAsync();
        await OpenPanelAsync("btn-position-open", "position-panel");

        await Expect(Container("position-panel")).ToHaveClassAsync(HasClass("bit-pnl-fsz"));
        var viewport = await ViewportAsync();
        var box = await BoxAsync(Container("position-panel"));
        Assert.AreEqual(viewport[0], box.Width, 1);
        Assert.AreEqual(viewport[1], box.Height, 1);
    }

    [TestMethod]
    public async Task ModeFull_DimsThePageBehindThePanel()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-modefull-open", "modefull-panel");

        await Expect(Overlay("modefull-panel")).ToHaveClassAsync(HasClass("bit-pnl-ovl-mfl"));
        var color = await Overlay("modefull-panel").EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", color);
    }

    // ---------- Overlay and dismissal ----------

    [TestMethod]
    public async Task Blocking_OverlayClickIsReportedButDoesNotClose()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-blocking-open", "blocking-panel");

        await ClickOverlayAsync("blocking-panel");

        await Expect(Page.Locator("#overlay-log")).ToHaveTextAsync("OnOverlayClick");
        await Expect(Page.Locator("#blocking-open")).ToHaveTextAsync("True");

        await WaitForFocusInsideAsync("blocking-panel");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#blocking-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task NoDismissOnEscape_EscapeIsReportedButDoesNotClose()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-noescape-open", "noescape-panel");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#overlay-log")).ToHaveTextAsync("OnEscapeKeyDown");
        await Page.WaitForTimeoutAsync(200);
        await Expect(Page.Locator("#noescape-open")).ToHaveTextAsync("True");

        await ClickOverlayAsync("noescape-panel");
        await Expect(Page.Locator("#noescape-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Modeless_LeavesThePageUsableAndNeitherTrapsNorHolds()
    {
        await OpenAsync(PagePath);
        await Page.Locator("#behind-button").ScrollIntoViewIfNeededAsync();
        await OpenPanelAsync("btn-modeless-open", "modeless-panel");

        await Expect(Overlay("modeless-panel")).ToHaveCountAsync(0);
        await Expect(Container("modeless-panel")).Not.ToHaveAttributeAsync("aria-modal", new Regex(".*"));
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Locator("#modeless-inner").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await IsFocusInsideAsync("modeless-panel"), "Tab leaves a modeless panel.");

        await Page.Locator("#behind-button").ClickAsync();
        await Expect(Page.Locator("#behind-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#modeless-open")).ToHaveTextAsync("True");
    }

    // ---------- Refusing a dismissal ----------

    [TestMethod]
    public async Task OnDismissing_Cancel_RefusesEveryClosingAndNamesItsReason()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-guarded-open", "guarded-panel");

        await Part("guarded-panel", ".bit-pnl-cls").ClickAsync();
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CloseButton+mouse");

        await ClickOverlayAsync("guarded-panel");
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CloseButton+mouse,Overlay+mouse");

        await Page.Locator("#guarded-toggle").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CloseButton+mouse,Overlay+mouse,Escape");

        await Page.Locator("#guarded-close-method").ClickAsync();
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CloseButton+mouse,Overlay+mouse,Escape,Programmatic");

        await Expect(Page.Locator("#guarded-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#guarded-dismiss")).ToHaveTextAsync("0");

        await Page.Locator("#guarded-toggle").ClickAsync();
        await Expect(Page.Locator("#guarded-guard")).ToHaveTextAsync("False");
        await Part("guarded-panel", ".bit-pnl-cls").ClickAsync();

        await Expect(Page.Locator("#guarded-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#guarded-dismiss")).ToHaveTextAsync("1");
    }

    // ---------- Accessibility ----------

    [TestMethod]
    public async Task TitleAriaIdAndSubtitleAriaId_NameAndDescribeThePanel()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-a11y-open", "a11y-panel");

        await Expect(Container("a11y-panel")).ToHaveAttributeAsync("role", "dialog");
        await Expect(Container("a11y-panel")).ToHaveAttributeAsync("aria-labelledby", "pnl-a11y-title");
        await Expect(Container("a11y-panel")).ToHaveAttributeAsync("aria-describedby", "pnl-a11y-subtitle");
    }

    [TestMethod]
    public async Task IsAlert_AnnouncesAnAlertDialog()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-alert-open", "a11y-panel");

        await Expect(Container("a11y-panel")).ToHaveAttributeAsync("role", "alertdialog");
        await Expect(Container("a11y-panel")).ToHaveAttributeAsync("aria-modal", "true");
    }

    [TestMethod]
    public async Task Role_ReplacesTheDialogRoleAndDropsAriaModal()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-role-open", "role-panel");

        await Expect(Container("role-panel")).ToHaveAttributeAsync("role", "complementary");
        await Expect(Container("role-panel")).ToHaveAttributeAsync("aria-label", "Side notes");
        await Expect(Container("role-panel")).Not.ToHaveAttributeAsync("aria-modal", new Regex(".*"));
    }

    [TestMethod]
    public async Task NoAutoFocus_LeavesTheFocusWhereItWas()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-nofocus-open", "nofocus-panel", waitForFocus: false);
        await NextFramesAsync();

        await Expect(Page.Locator("#btn-nofocus-open")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NoFocusTrapAndNoRestoreFocus_LetTabLeaveAndHandNothingBack()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-notrap-open", "notrap-panel");

        await Part("notrap-panel", ".bit-pnl-cls").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");
        Assert.IsFalse(await IsFocusInsideAsync("notrap-panel"), "Shift+Tab leaves a panel that does not trap the focus.");

        await Part("notrap-panel", ".bit-pnl-cls").ClickAsync();
        await Expect(Page.Locator("#notrap-open")).ToHaveTextAsync("False");
        await NextFramesAsync();
        await Expect(Page.Locator("#btn-notrap-open")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DataAutofocus_LandsTheFocusOnTheMarkedElement()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-autofocus-open", "autofocus-panel");

        await Expect(Page.Locator("#autofocus-target")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NothingFocusableInside_FocusesTheContainerItself()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-empty-open", "empty-panel");

        await Expect(Container("empty-panel")).ToBeFocusedAsync();
    }

    // ---------- Page scrolling ----------

    [TestMethod]
    public async Task NoScrollLock_LeavesThePageScrolling()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-noscroll-open", "noscroll-panel");

        Assert.AreNotEqual("hidden", await BodyOverflowAsync());
    }

    [TestMethod]
    public async Task AutoToggleScroll_TakesThePageOverflowAndHandsItBack()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-autotoggle-open", "autotoggle-panel");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#autotoggle-open")).ToHaveTextAsync("False");
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");
    }

    [TestMethod]
    public async Task ScrollerSelector_HoldsTheNamedScrollerInsteadOfThePage()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-scrollsel-open", "scrollsel-panel");

        await Expect(Page.Locator("#pnl-scroll-box")).ToHaveCSSAsync("overflow-y", "hidden");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#scrollsel-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#pnl-scroll-box")).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task ScrollerElement_HoldsTheElementGivenByReferenceInsteadOfThePage()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-scrollel-open", "scrollel-panel");

        await Expect(Page.Locator("#pnl-scrollel-box")).ToHaveCSSAsync("overflow-y", "hidden");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#scrollel-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#pnl-scrollel-box")).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task NoScrollLockWithAScroller_WheelOverTheOverlayScrollsThatScroller()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-forward-open", "forward-panel");

        await Page.Mouse.MoveAsync(1000, 400);
        await Page.Mouse.WheelAsync(0, 200);

        await Page.WaitForFunctionAsync("() => document.getElementById('pnl-forward-box').scrollTop > 0");
    }

    // ---------- Swipe (mouse) ----------

    private async Task OpenSwipeAsync(string placement = "End", string trigger = "0.25", bool noSwipe = false)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#swipe-placement-select").SelectOptionAsync(placement);
        await Page.Locator("#swipe-trigger-select").SelectOptionAsync(trigger);
        if (noSwipe) await Page.Locator("#noswipe-check").CheckAsync();
        await OpenPanelAsync("btn-swipe-open", "swipe-panel");
    }

    [TestMethod]
    public async Task Swipe_DraggedTowardsItsEdgePastTheTrigger_DismissesWithTheSwipeReason()
    {
        await OpenSwipeAsync();

        await MouseSwipeAsync(Page.Locator("#swipe-surface"), 200, 0);

        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("OnSwipeStart,OnDismissing:Swipe,OnSwipeEnd");
        Assert.AreEqual(200, double.Parse(await Page.Locator("#swipe-end").TextContentAsync() ?? "0", System.Globalization.CultureInfo.InvariantCulture), 2);
        Assert.IsTrue(int.Parse((await Page.Locator("#swipe-moves").TextContentAsync())!) > 0, "OnSwipeMove is raised while dragging.");
        Assert.IsTrue(double.Parse((await Page.Locator("#swipe-start").TextContentAsync())!, System.Globalization.CultureInfo.InvariantCulture) > 0);
    }

    [TestMethod]
    public async Task Swipe_DraggedShortOfTheTrigger_SnapsBack()
    {
        await OpenSwipeAsync();

        await MouseSwipeAsync(Page.Locator("#swipe-surface"), 50, 0);

        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("OnSwipeStart,OnSwipeEnd");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
        await Expect(Container("swipe-panel")).ToHaveCSSAsync("transform", "none");
    }

    [TestMethod]
    public async Task Swipe_DraggedAwayFromItsEdge_DoesNotDismiss()
    {
        await OpenSwipeAsync();

        await MouseSwipeAsync(Page.Locator("#swipe-surface"), -200, 0);

        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("OnSwipeStart,OnSwipeEnd");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    [DataRow("Start", -200, 0)]
    [DataRow("Left", -200, 0)]
    [DataRow("Right", 200, 0)]
    [DataRow("Top", 0, -200)]
    [DataRow("Bottom", 0, 200)]
    public async Task Swipe_EachPlacement_IsDismissedTowardsItsOwnEdge(string placement, double dx, double dy)
    {
        await OpenSwipeAsync(placement);

        await MouseSwipeAsync(Page.Locator("#swipe-surface"), dx, dy);

        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#swipe-log")).ToContainTextAsync("OnDismissing:Swipe");
    }

    [TestMethod]
    [DataRow("0.5", 120, true)]
    [DataRow("0.5", 180, false)]
    [DataRow("0.9", 200, true)]
    [DataRow("5", 100, false)]
    public async Task SwipeTrigger_IsTheFractionOfTheSizeToDragAndFallsBackWhenOutOfRange(string trigger, double distance, bool staysOpen)
    {
        await OpenSwipeAsync(trigger: trigger);

        await MouseSwipeAsync(Page.Locator("#swipe-surface"), distance, 0);

        await Expect(Page.Locator("#swipe-log")).ToContainTextAsync("OnSwipeEnd");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync(staysOpen ? "True" : "False");
    }

    [TestMethod]
    public async Task SwipeTrigger_ChangedTwiceInARow_LeavesTheGestureRegisteredOnce()
    {
        await OpenAsync(PagePath);

        // Two clicks sent in one go: the second change reaches the panel while it is still re-registering its
        // gesture for the first - a round trip to the browser that cannot come back before the second click is in.
        await Page.Locator("#btn-swipe-trigger-step").EvaluateAsync("el => { el.click(); el.click(); }");
        await Expect(Page.Locator("#swipe-trigger")).ToHaveTextAsync("0.25");
        await Page.WaitForTimeoutAsync(500);

        var registrations = await Container("swipe-panel").EvaluateAsync<int>("el => BitBlazorUI.Swipes._swipes.filter(s => s.id === el.id).length");
        Assert.AreEqual(1, registrations, "The gesture is registered once.");

        await OpenPanelAsync("btn-swipe-open", "swipe-panel");
        await MouseSwipeAsync(Page.Locator("#swipe-surface"), 200, 0);

        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("False");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("OnSwipeStart,OnDismissing:Swipe,OnSwipeEnd");
    }

    [TestMethod]
    public async Task NoSwipe_TurnsTheGestureOff()
    {
        await OpenSwipeAsync(noSwipe: true);

        await MouseSwipeAsync(Page.Locator("#swipe-surface"), 250, 0);
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    [DataRow("#swipe-input")]
    [DataRow("#swipe-noswipe")]
    public async Task Swipe_StartedOnAFieldOrANoSwipeRegion_IsLeftToIt(string selector)
    {
        await OpenSwipeAsync();

        await MouseSwipeAsync(Page.Locator(selector), 250, 0);
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#swipe-log")).Not.ToContainTextAsync("OnDismissing");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Swipe_AMouseDragThatSelectsText_IsGivenBackToTheSelection()
    {
        if (IsFirefox) Assert.Inconclusive("Firefox makes no selection of a user-select:text element inside a user-select:none ancestor when the mouse is dragged across it, so there is no selection for the swipe to give the drag back to.");

        await OpenSwipeAsync();

        var text = await BoxAsync(Page.Locator("#swipe-text"));
        await Page.Mouse.MoveAsync(text.X + 2, text.Y + text.Height / 2);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= 10; i++)
        {
            await Page.Mouse.MoveAsync(text.X + 2 + 25 * i, text.Y + text.Height / 2);
        }
        await Page.Mouse.UpAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#swipe-log")).Not.ToContainTextAsync("OnDismissing");
        Assert.IsTrue((await Page.EvaluateAsync<string>("() => window.getSelection().toString()")).Length > 0, "The drag selected text.");
    }

    // ---------- Nested panels ----------

    [TestMethod]
    public async Task Nested_Escape_ClosesOnlyTheInnerPanel()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-outer-open", "outer-panel");
        await Page.Locator("#btn-inner-open").ClickAsync();
        await WaitOpenAsync("inner-panel");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-open")).ToHaveTextAsync("False");
        await Page.WaitForTimeoutAsync(200);
        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");

        await WaitForFocusInsideAsync("outer-panel");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Nested_EscapeClaimedByAControl_LeavesThePanelOpen()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-outer-open", "outer-panel");

        await Page.Locator("#outer-claim").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task ZIndex_LayersASiblingPanelAboveTheOneItWasOpenedFrom()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-outer-open", "outer-panel");
        await Page.Locator("#btn-sibling-open").ClickAsync();
        await WaitOpenAsync("sibling-panel");

        await Expect(Overlay("sibling-panel")).ToHaveCSSAsync("z-index", "1310");
        await Expect(Container("sibling-panel")).ToHaveCSSAsync("z-index", "1311");

        // The sibling's overlay is what lies over the outer panel now.
        var outer = await BoxAsync(Container("outer-panel"));
        var topmost = await Page.EvaluateAsync<string>("([x, y]) => { const el = document.elementFromPoint(x, y); return el.closest('.bit-pnl')?.id ?? ''; }",
            new object[] { (double)(outer.X + outer.Width / 2), (double)(outer.Y + outer.Height / 2) });
        Assert.AreEqual("sibling-panel", topmost);
    }

    // ---------- Inside a container ----------

    [TestMethod]
    public async Task AbsolutePosition_KeepsThePanelAndItsOverlayInsideTheBox()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-abs-open", "abs-panel");

        await Expect(Root("abs-panel")).ToHaveClassAsync(HasClass("bit-pnl-abs"));
        Assert.AreNotEqual("hidden", await BodyOverflowAsync(), "A contained panel leaves the page alone.");

        var box = await BoxAsync(Page.Locator("#pnl-abs-box"));
        var panel = await BoxAsync(Container("abs-panel"));
        var overlay = await BoxAsync(Overlay("abs-panel"));
        Assert.AreEqual(box.X + box.Width - 1, panel.X + panel.Width, 2, "against the box's right edge");
        Assert.AreEqual(220, panel.Width, 1);
        Assert.IsTrue(overlay.Width <= box.Width && overlay.Height <= box.Height, "The overlay covers the box only.");
    }

    // ---------- Rendering and events ----------

    [TestMethod]
    public async Task KeepMountedFalse_TakesTheContentOutOnceTheSlideOutIsOver()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-render-open", "render-panel");

        await Expect(Page.Locator("#render-opens")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#render-toggle")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#render-settled")).ToHaveTextAsync("True");

        await Page.Locator("#render-input").FillAsync("typed");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#render-toggle")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#render-settled")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#render-input")).ToHaveCountAsync(0);

        await OpenPanelAsync("btn-render-open", "render-panel");
        await Expect(Page.Locator("#render-opens")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#render-input")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task KeepMounted_KeepsTheContentAndItsStateAcrossAClose()
    {
        await OpenAsync(PagePath);
        await Page.Locator("#keepmounted-check").CheckAsync();
        await OpenPanelAsync("btn-render-open", "render-panel");

        await Page.Locator("#render-input").FillAsync("typed");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#render-settled")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#render-input")).ToHaveCountAsync(1);

        await OpenPanelAsync("btn-render-open", "render-panel");
        await Expect(Page.Locator("#render-input")).ToHaveValueAsync("typed");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheSlideAndStillReportsItsEnd()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-render-open", "render-panel");

        var duration = await Container("render-panel").EvaluateAsync<double>("el => Math.max(...getComputedStyle(el).transitionDuration.split(',').map(d => parseFloat(d)))");
        Assert.IsTrue(duration < 0.01, $"transition-duration {duration}s");
        await Expect(Page.Locator("#render-settled")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#render-settled")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#render-input")).ToHaveCountAsync(0);
    }

    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_FillInWhatAPanelLeavesUnsetAndOwnValuesWin()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-casc-open", "casc-panel");

        await Expect(Container("casc-panel")).ToHaveClassAsync(HasClass("bit-pnl-start"));
        await Expect(Overlay("casc-panel")).ToHaveClassAsync(HasClass("bit-pnl-ovl-mfl"));
        await Expect(Part("casc-panel", ".bit-pnl-cls")).ToBeVisibleAsync();
        var box = await BoxAsync(Container("casc-panel"));
        Assert.AreEqual(0, box.X, 1);
        Assert.AreEqual(320, box.Width, 1);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Container("casc-panel")).Not.ToHaveClassAsync(HasClass("bit-pnl-opn"));

        await OpenPanelAsync("btn-cascown-open", "cascown-panel");
        await Expect(Container("cascown-panel")).ToHaveClassAsync(HasClass("bit-pnl-end"));
        Assert.AreEqual(320, (await BoxAsync(Container("cascown-panel"))).Width, 1);
    }

    // ---------- External icons ----------

    [TestMethod]
    public async Task CloseIcon_ExternalIcon_WinsOverCloseIconName()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-iconfa-open", "iconfa-panel");

        var icon = Part("iconfa-panel", ".bit-pnl-cli");
        await Expect(icon).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(icon).ToHaveClassAsync(HasClass("fa-xmark"));
        Assert.IsFalse((await icon.GetAttributeAsync("class"))!.Contains("bit-icon--"));
    }

    [TestMethod]
    public async Task CloseIconName_SwapsTheBuiltInGlyph()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-iconname-open", "iconname-panel");

        await Expect(Part("iconname-panel", ".bit-pnl-cli")).ToHaveClassAsync(HasClass("bit-icon--ChromeBack"));
    }

    // ---------- Style, Class, Styles, Classes, CSS variables ----------

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-styled-open", "styled-panel");

        await Expect(Root("styled-panel")).ToHaveClassAsync(HasClass("my-root"));
        await Expect(Root("styled-panel")).ToHaveCSSAsync("letter-spacing", "1px");
    }

    [TestMethod]
    [DataRow("", "root")]
    [DataRow(" > .bit-pnl-ovl", "overlay")]
    [DataRow(" > .bit-pnl-cnt", "container")]
    [DataRow(" .bit-pnl-hcn", "header-container")]
    [DataRow(" .bit-pnl-hdr", "header")]
    [DataRow(" .bit-pnl-cls", "close-button")]
    [DataRow(" .bit-pnl-cli", "close-icon")]
    [DataRow(" .bit-pnl-bdy", "body")]
    [DataRow(" .bit-pnl-ftr", "footer")]
    public async Task StylesAndClasses_ReachEachPartByName(string selector, string part)
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-styled-open", "styled-panel");

        var value = await Page.Locator($"#styled-panel{selector}").EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--e2e-part')");
        Assert.AreEqual(part, value.Trim(), $"Styles.{part}");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Container("styled-panel")).Not.ToHaveClassAsync(HasClass("bit-pnl-opn"));

        await OpenPanelAsync("btn-classed-open", "classed-panel");
        await Expect(Page.Locator($"#classed-panel{selector}")).ToHaveClassAsync(HasClass($"part-{part}"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnTheStyle_ReskinThePanel()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-vars-open", "vars-panel");

        var container = Container("vars-panel");
        Assert.AreEqual(352, (await BoxAsync(container)).Width, 1);
        await Expect(container).ToHaveCSSAsync("border-top-left-radius", "24px");
        await Expect(container).ToHaveCSSAsync("border-left-width", "2px");
        await Expect(container).ToHaveCSSAsync("border-left-color", "rgb(1, 2, 3)");
        await Expect(container).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(container).ToHaveCSSAsync("box-shadow", "none");
        await Expect(container).ToHaveCSSAsync("z-index", "2101");
        await Expect(Root("vars-panel")).ToHaveCSSAsync("color", "rgb(200, 210, 220)");

        var overlay = Overlay("vars-panel");
        await Expect(overlay).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0.4)");
        await Expect(overlay).ToHaveCSSAsync("backdrop-filter", "blur(4px)");
        await Expect(overlay).ToHaveCSSAsync("z-index", "2100");

        await Expect(Part("vars-panel", ".bit-pnl-bdy")).ToHaveCSSAsync("padding-left", "32px");
        await Expect(Part("vars-panel", ".bit-pnl-hdr")).ToHaveCSSAsync("font-size", "28px");
        await Expect(Part("vars-panel", ".bit-pnl-hdr")).ToHaveCSSAsync("font-weight", "300");
    }

    [TestMethod]
    public async Task ZIndex_LayersTheOverlayAndThePanelOneAboveIt()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-zindex-open", "zindex-panel");

        await Expect(Overlay("zindex-panel")).ToHaveCSSAsync("z-index", "2000");
        await Expect(Container("zindex-panel")).ToHaveCSSAsync("z-index", "2001");
    }

    [TestMethod]
    public async Task MaxSizeVariable_CapsASizeLargerThanTheScreen()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-maxsize-open", "maxsize-panel");

        var viewport = await ViewportAsync();
        Assert.AreEqual(viewport[0] / 2, (await BoxAsync(Container("maxsize-panel"))).Width, 1);
    }

    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_StartSlidesInFromTheRightAndMirrorsTheHeader()
    {
        await OpenAsync(PagePath);

        await Expect(Root("rtlstart-panel")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("rtlstart-panel")).ToHaveAttributeAsync("dir", "rtl");
        Assert.IsTrue((await TranslationAsync("rtlstart-panel"))[0] > 0, "Closed, a right-to-left start panel waits beyond the right edge.");

        await OpenPanelAsync("btn-rtlstart-open", "rtlstart-panel");

        var viewport = await ViewportAsync();
        var box = await BoxAsync(Container("rtlstart-panel"));
        Assert.AreEqual(viewport[0], box.X + box.Width, 1);

        var close = await BoxAsync(Part("rtlstart-panel", ".bit-pnl-cls"));
        var header = await BoxAsync(Part("rtlstart-panel", ".bit-pnl-hdr"));
        Assert.IsTrue(close.X < header.X, "The close button sits on the left of the header.");
        await Expect(Part("rtlstart-panel", ".bit-pnl-cls")).ToHaveAttributeAsync("aria-label", "بستن");
    }

    [TestMethod]
    [DataRow("btn-rtlleft-open", "rtlleft-panel", "left")]
    [DataRow("btn-rtlinherit-open", "rtlinherit-panel", "right")]
    public async Task Rtl_LeftStaysPhysicalAndAnInheritedDirectionMovesStart(string buttonId, string panelId, string edge)
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync(buttonId, panelId);

        var viewport = await ViewportAsync();
        var box = await BoxAsync(Container(panelId));
        if (edge == "left") Assert.AreEqual(0, box.X, 1);
        else Assert.AreEqual(viewport[0], box.X + box.Width, 1);
    }

    // ---------- Base parameters ----------

    [TestMethod]
    public async Task BaseParameters_IdHtmlAttributesAndTabIndex_LandOnTheRoot()
    {
        await OpenAsync(PagePath);

        await Expect(Root("base-panel")).ToHaveAttributeAsync("data-e2e", "panel-root");
        await Expect(Root("base-panel")).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    [DataRow("Hidden")]
    [DataRow("Collapsed")]
    public async Task Visibility_TakesThePanelOutOfViewAndItsHoldOnThePageWithIt(string visibility)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#visibility-select").SelectOptionAsync(visibility);
        await Page.Locator("#btn-base-open").ClickAsync();
        await Expect(Page.Locator("#base-open")).ToHaveTextAsync("True");

        if (visibility == "Hidden") await Expect(Root("base-panel")).ToHaveCSSAsync("visibility", "hidden");
        else await Expect(Root("base-panel")).ToHaveCSSAsync("display", "none");

        await Expect(Container("base-panel")).ToBeHiddenAsync();
        await NextFramesAsync();
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());
    }

    [TestMethod]
    public async Task Disabled_RefusesTheUserButNotTheCode()
    {
        await OpenAsync(PagePath);

        await Page.Locator("#btn-disabled-method-open").ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        await Expect(Container("disabled-panel")).Not.ToHaveClassAsync(HasClass("bit-pnl-opn"));

        await OpenPanelAsync("btn-disabled-open", "disabled-panel");

        await Expect(Root("disabled-panel")).ToHaveClassAsync(HasClass("bit-dis"));
        var close = Part("disabled-panel", ".bit-pnl-cls");
        await Expect(close).ToBeDisabledAsync();
        await Expect(close).ToHaveAttributeAsync("aria-disabled", "true");
        await close.DispatchEventAsync("click");

        await ClickOverlayAsync("disabled-panel");
        await Page.Locator("#disabled-method-close").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#disabled-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#disabled-log")).ToHaveTextAsync("");

        await Page.Locator("#disabled-method-close").ClickAsync();
        await Expect(Page.Locator("#disabled-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#disabled-log")).ToHaveTextAsync("OnDismissing:Programmatic");
    }

    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_TheClosedPanelAndAnOpenOneAreInTheServerHtml()
    {
        var page = await (await Page.APIRequest.GetAsync($"{BaseUrl}{PagePath}")).TextAsync();
        StringAssert.Contains(page, "id=\"basic-panel\"");
        StringAssert.Contains(page, "bit-pnl-cnt");

        var open = await (await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/panel/prerender")).TextAsync();
        StringAssert.Contains(open, "Prerendered panel");
        StringAssert.Contains(open, "bit-pnl-opn");

        await OpenAsync("/e2e/panel/prerender");
        await Expect(Container("initial-panel")).ToHaveClassAsync(HasClass("bit-pnl-opn"));
        await Expect(Page.Locator("#initial-content")).ToBeVisibleAsync();

        await Part("initial-panel", ".bit-pnl-cls").ClickAsync();
        await Expect(Page.Locator("#initial-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Unmount_WhileOpen_ReleasesThePageWithoutErrors()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-unmount-open", "unmount-panel");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Locator("#unmount-now").ClickAsync();

        await Expect(Page.Locator("#unmount-shown")).ToHaveTextAsync("False");
        await Expect(Root("unmount-panel")).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
