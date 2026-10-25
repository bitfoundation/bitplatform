using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// End-to-end tests of BitModal in a real browser (/e2e/modal and /e2e/modal/prerender): the bound IsOpen and its events
/// (OnOpen, OnDismiss, OnOverlayClick, OnEscapeKeyDown, IsOpenChanged); the exit animation; the dialog semantics and the
/// accessible name; the header/body/footer chrome; Blocking, NoDismissOnEscape and the CanClose guard with their refusal
/// pulse (and the ring under reduced motion); focus moved in, trapped and restored and the No* opt-outs; the page scroll
/// lock with scrollbar compensation, NoScrollLock, AutoToggleScroll, ScrollerSelector and the gesture forwarding; ModeFull,
/// Modeless and AriaModal; every Position and AbsolutePosition; mouse dragging; KeepMounted; Open/Close/Toggle; nested
/// modals where one Escape closes one layer; BitParams; close icons; the size parameters; Style/Class/Styles/Classes and the
/// --bit-Modal-* variables; RTL, explicit and inherited; the base parameters and Disabled; prerendering and unmounting.
/// </summary>
[TestClass]
public class BitModalE2ETests : E2ETestBase
{
    private const string PagePath = "/e2e/modal";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Content(string id) => Page.Locator($"#{id} > .bit-mdl-ctn");

    private ILocator Part(string id, string selector) => Page.Locator($"#{id} {selector}");

    private async Task OpenModalAsync(string buttonId, string modalId, bool waitForFocus = true)
    {
        await PressAsync(Page.Locator($"#{buttonId}"));
        await Expect(Content(modalId)).ToBeVisibleAsync();
        await WaitForAnimationsAsync(Root(modalId));

        if (waitForFocus) await WaitForFocusInsideAsync(modalId);
    }

    private static Task WaitForAnimationsAsync(ILocator locator)
    {
        return locator.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished.catch(() => null)))");
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
        return Page.WaitForFunctionAsync("id => { const el = document.getElementById(id); return !!el && el.contains(document.activeElement); }", id);
    }

    private Task<bool> IsFocusInsideAsync(string id)
    {
        return Page.EvaluateAsync<bool>("id => document.getElementById(id)?.contains(document.activeElement) ?? false", id);
    }

    private Task ClickOverlayAsync(string id)
    {
        return Part(id, ".bit-mdl-ovl").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
    }

    private Task<string> BodyOverflowAsync()
    {
        return Page.EvaluateAsync<string>("() => document.body.style.overflow");
    }

    private Task<string> ResolveColorAsync(string variable)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = `var(${v})`;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return color;
        }", variable);
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        return (await locator.BoundingBoxAsync())!;
    }

    private async Task DragAsync(ILocator handle, double dx, double dy, double offsetX = 20)
    {
        var box = await BoxAsync(handle);
        var x = box.X + offsetX;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(x + dx / 2), (float)(y + dy / 2), new() { Steps = 5 });
        await Page.Mouse.MoveAsync((float)(x + dx), (float)(y + dy), new() { Steps = 5 });
        await Page.Mouse.UpAsync();
    }

    // Records every class value the element is given from now on, so a state that only lasts a few frames - the exit
    // animation of a closing Modal - can be read back after it is over.
    private Task RecordClassesAsync(string id)
    {
        return Page.EvaluateAsync(@"id => {
            const el = document.getElementById(id);
            window.__e2eClasses = [el.className];
            new MutationObserver(records => records.forEach(r => window.__e2eClasses.push(r.target.className)))
                .observe(el, { attributes: true, attributeFilter: ['class'] });
        }", id);
    }

    private Task<string[]> RecordedClassesAsync()
    {
        return Page.EvaluateAsync<string[]>("() => window.__e2eClasses ?? []");
    }

    private static void AssertPlaced(LocatorBoundingBoxResult area, LocatorBoundingBoxResult box, string horizontal, string vertical)
    {
        const double tolerance = 2;

        var x = horizontal switch
        {
            "start" => box.X - area.X,
            "end" => area.X + area.Width - (box.X + box.Width),
            _ => Math.Abs(box.X + box.Width / 2 - (area.X + area.Width / 2))
        };
        Assert.IsTrue(x <= tolerance, $"horizontal {horizontal}: area {area.X},{area.Width} box {box.X},{box.Width}");

        var y = vertical switch
        {
            "start" => box.Y - area.Y,
            "end" => area.Y + area.Height - (box.Y + box.Height),
            _ => Math.Abs(box.Y + box.Height / 2 - (area.Y + area.Height / 2))
        };
        Assert.IsTrue(y <= tolerance, $"vertical {vertical}: area {area.Y},{area.Height} box {box.Y},{box.Height}");
    }

    // ---------- Basic ----------

    [TestMethod]
    public async Task Basic_OpenButton_RendersTheContentAsAModalDialog()
    {
        await OpenAsync(PagePath);

        await Expect(Root("basic-modal")).ToHaveCountAsync(0);

        await OpenModalAsync("btn-basic-open", "basic-modal");

        var content = Content("basic-modal");
        await Expect(content).ToHaveAttributeAsync("role", "dialog");
        await Expect(content).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(content).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(content).Not.ToHaveClassAsync(HasClass("bit-mdl-chr"));
        await Expect(Part("basic-modal", ".bit-mdl-ctn > #basic-content")).ToBeVisibleAsync();
        await Expect(Part("basic-modal", ".bit-mdl-ovl")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Root("basic-modal")).ToHaveClassAsync(HasClass("bit-mdl-bdr"));
        await Expect(Root("basic-modal")).ToHaveAttributeAsync("tabindex", "-1");

        // Without ModeFull the overlay catches the clicks but does not dim the page.
        await Expect(Part("basic-modal", ".bit-mdl-ovl")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        // The default top border in the primary color.
        await Expect(content).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(content).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("--bit-clr-pri"));

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen");
    }

    [TestMethod]
    public async Task Basic_EscapeKey_ClosesWithOnEscapeKeyDownAndOnDismiss()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-basic-open", "basic-modal");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("basic-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnEscapeKeyDown:Escape,OnDismiss");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Basic_OverlayClick_ClosesWithOnOverlayClickAndOnDismiss()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-basic-open", "basic-modal");

        await ClickOverlayAsync("basic-modal");

        await Expect(Root("basic-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnOverlayClick,OnDismiss");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Basic_PageSetsIsOpenFalse_ClosesAndStillReportsOnDismiss()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-basic-open", "basic-modal");

        await Page.Locator("#basic-page-close").ClickAsync();

        await Expect(Root("basic-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnDismiss");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Basic_Closing_PlaysTheExitAnimationInertBeforeLeavingThePage()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-basic-open", "basic-modal");
        await RecordClassesAsync("basic-modal");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("basic-modal")).ToHaveCountAsync(0);
        var classes = await RecordedClassesAsync();
        Assert.IsTrue(classes.Any(c => Regex.IsMatch(c, @"(^|\s)bit-mdl-lvg(\s|$)")), string.Join(" | ", classes));
    }

    [TestMethod]
    public async Task Basic_ParameterChangedWhileOpen_ReRendersTheContent()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-basic-open", "basic-modal");

        await Page.Locator("#basic-rename").ClickAsync();

        await Expect(Page.Locator("#basic-heading")).ToHaveTextAsync("Renamed");
    }

    [TestMethod]
    public async Task Basic_Open_MovesFocusInTrapsTabAndRestoresFocusOnClose()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-focus-open", "focus-modal");

        await Expect(Page.Locator("#focus-first")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#focus-second")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#focus-last")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#focus-first")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#focus-last")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#focus-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#btn-focus-open")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Basic_Open_HoldsThePageAndCompensatesForTheScrollbar()
    {
        await OpenAsync(PagePath);

        var before = await Page.EvaluateAsync<double[]>("() => [window.innerWidth - document.documentElement.clientWidth, parseFloat(getComputedStyle(document.body).paddingRight)]");

        await OpenModalAsync("btn-basic-open", "basic-modal");

        Assert.AreEqual("hidden", await BodyOverflowAsync());
        if (before[0] > 0)
        {
            var padding = await Page.EvaluateAsync<double>("() => parseFloat(document.body.style.paddingRight)");
            Assert.AreEqual(before[0] + before[1], padding, 0.5, "The room the scrollbar took is added back as padding.");
        }

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === '' && document.body.style.paddingRight === ''");
    }

    [TestMethod]
    public async Task NoBorder_RemovesTheTopBorder()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-noborder-open", "noborder-modal");

        await Expect(Root("noborder-modal")).Not.ToHaveClassAsync(HasClass("bit-mdl-bdr"));
        await Expect(Content("noborder-modal")).ToHaveCSSAsync("border-top-style", "none");
    }

    // ---------- Sizing ----------

    [TestMethod]
    public async Task MaxWidth_CapsTheWidthOfTheContent()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-maxwidth-open", "maxwidth-modal");

        Assert.AreEqual(512, (await BoxAsync(Content("maxwidth-modal"))).Width, 1);
    }

    [TestMethod]
    public async Task WidthAndHeight_SetTheSizeOfTheContent()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-fixed-open", "fixed-modal");

        var box = await BoxAsync(Content("fixed-modal"));
        Assert.AreEqual(384, box.Width, 1);
        Assert.AreEqual(288, box.Height, 1);
    }

    [TestMethod]
    public async Task Width_OnAPhoneNarrowerThanIt_IsCappedToTheScreen()
    {
        await Page.SetViewportSizeAsync(360, 640);
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-fixed-open", "fixed-modal");

        var area = await BoxAsync(Root("fixed-modal"));
        var box = await BoxAsync(Content("fixed-modal"));
        Assert.AreEqual(area.Width, box.Width, 1, "A 24rem modal never outgrows a phone.");
        Assert.AreEqual(area.X, box.X, 1);
    }

    [TestMethod]
    public async Task MaxHeight_CapsTheHeightAndTheContentScrollsInside()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-maxheight-open", "maxheight-modal");

        var box = await BoxAsync(Content("maxheight-modal"));
        Assert.AreEqual(256, box.Height, 1);
        Assert.AreEqual(480, box.Width, 1);
        Assert.IsTrue(await Content("maxheight-modal").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));
    }

    [TestMethod]
    [DataRow("btn-fullwidth-open", "fullwidth-modal", true, false)]
    [DataRow("btn-fullheight-open", "fullheight-modal", false, true)]
    [DataRow("btn-fullsize-open", "fullsize-modal", true, true)]
    public async Task FullWidthFullHeightFullSize_StretchTheContentAcrossTheScreen(string buttonId, string modalId, bool wide, bool tall)
    {
        await OpenAsync(PagePath);
        await OpenModalAsync(buttonId, modalId);

        var area = await BoxAsync(Root(modalId));
        var box = await BoxAsync(Content(modalId));

        if (wide)
        {
            await Expect(Root(modalId)).ToHaveClassAsync(HasClass("bit-mdl-fwi"));
            Assert.AreEqual(area.Width, box.Width, 1);
        }
        else
        {
            Assert.IsTrue(box.Width < area.Width - 50);
        }

        if (tall)
        {
            await Expect(Root(modalId)).ToHaveClassAsync(HasClass("bit-mdl-fhe"));
            Assert.AreEqual(area.Height, box.Height, 1);
        }
        else
        {
            Assert.IsTrue(box.Height < area.Height - 50);
        }
    }

    // ---------- Chrome ----------

    [TestMethod]
    public async Task HeaderText_RendersAHeadingThatNamesTheModalAndACloseButton()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-headertext-open", "headertext-modal");

        var header = Part("headertext-modal", ".bit-mdl-hcn > .bit-mdl-hdr");
        await Expect(header).ToHaveTextAsync("Release notes");
        await Expect(header).ToHaveAttributeAsync("role", "heading");
        await Expect(header).ToHaveAttributeAsync("aria-level", "2");
        await Expect(Content("headertext-modal")).ToHaveClassAsync(HasClass("bit-mdl-chr"));
        await Expect(Content("headertext-modal")).ToHaveAttributeAsync("aria-labelledby", (await header.GetAttributeAsync("id"))!);
        await Expect(Part("headertext-modal", ".bit-mdl-bdy #headertext-body")).ToBeVisibleAsync();

        var close = Part("headertext-modal", ".bit-mdl-hcn > .bit-mdl-cls");
        await Expect(close).ToHaveAttributeAsync("title", "Close");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Close");
        await Expect(close.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await close.ClickAsync();
        await Expect(Page.Locator("#headertext-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Header_RendersTheTemplateAndTitleAriaIdNamesTheModal()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-headertpl-open", "headertpl-modal");

        await Expect(Part("headertpl-modal", ".bit-mdl-hcn > .bit-mdl-hdr #headertpl-title")).ToHaveTextAsync("Templated header");
        await Expect(Part("headertpl-modal", ".bit-mdl-hdr")).Not.ToHaveAttributeAsync("role", "heading");
        await Expect(Content("headertpl-modal")).ToHaveAttributeAsync("aria-labelledby", "headertpl-title");
        await Expect(Part("headertpl-modal", ".bit-mdl-cls")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Footer_RendersUnderTheBodyAndItsContentWorks()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-footer-open", "footer-modal");

        var body = await BoxAsync(Part("footer-modal", ".bit-mdl-bdy"));
        var footer = await BoxAsync(Part("footer-modal", ".bit-mdl-fcn"));
        Assert.IsTrue(footer.Y >= body.Y + body.Height - 1);

        await Page.Locator("#footer-leave").ClickAsync();
        await Expect(Page.Locator("#footer-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task FooterText_RendersAFooterWithoutAHeader()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-footertext-open", "footertext-modal");

        await Expect(Part("footertext-modal", ".bit-mdl-fcn")).ToHaveTextAsync("This is a footer text");
        await Expect(Part("footertext-modal", ".bit-mdl-hcn")).ToHaveCountAsync(0);
        await Expect(Part("footertext-modal", ".bit-mdl-bdy")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Body_OnItsOwn_BuildsTheChrome()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-body-open", "body-modal");

        await Expect(Part("body-modal", ".bit-mdl-ctn.bit-mdl-chr > .bit-mdl-bdy #body-only")).ToBeVisibleAsync();
        await Expect(Part("body-modal", ".bit-mdl-hcn")).ToHaveCountAsync(0);
        await Expect(Part("body-modal", ".bit-mdl-fcn")).ToHaveCountAsync(0);
    }

    // ---------- Dismissal ----------

    [TestMethod]
    public async Task Blocking_RefusesTheOverlayWithAPulseButEscapeStillCloses()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-blocking-open", "blocking-modal");

        await Expect(Content("blocking-modal")).ToHaveAttributeAsync("role", "alertdialog");

        await ClickOverlayAsync("blocking-modal");

        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("OnOverlayClick");
        await Expect(Content("blocking-modal")).ToHaveClassAsync(HasClass("bit-mdl-bna"));
        await Expect(Page.Locator("#blocking-open")).ToHaveTextAsync("True");

        await ClickOverlayAsync("blocking-modal");
        await Expect(Content("blocking-modal")).ToHaveClassAsync(HasClass("bit-mdl-bnb"));

        await WaitForFocusInsideAsync("blocking-modal");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#blocking-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("OnOverlayClick,OnOverlayClick,OnDismiss");
    }

    [TestMethod]
    public async Task NoDismissOnEscape_RefusesEscapeWithAPulseAndStillReportsTheKey()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-noescape-open", "noescape-modal");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("OnEscapeKeyDown");
        await Expect(Content("noescape-modal")).ToHaveClassAsync(HasClass("bit-mdl-bna"));
        await Expect(Page.Locator("#noescape-open")).ToHaveTextAsync("True");

        await ClickOverlayAsync("noescape-modal");
        await Expect(Page.Locator("#noescape-open")).ToHaveTextAsync("True");

        await Page.Locator("#noescape-close").ClickAsync();
        await Expect(Page.Locator("#noescape-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task CanClose_AnsweringFalse_KeepsTheModalOpenUntilItAnswersTrue()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-canclose-open", "canclose-modal");

        await Part("canclose-modal", ".bit-mdl-cls").ClickAsync();
        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("CanClose:False");
        await Expect(Content("canclose-modal")).ToHaveClassAsync(HasClass("bit-mdl-bna"));

        await Page.Locator("#canclose-allow").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("CanClose:False,CanClose:False");

        await ClickOverlayAsync("canclose-modal");
        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("CanClose:False,CanClose:False,CanClose:False");
        await Expect(Page.Locator("#canclose-open")).ToHaveTextAsync("True");

        await Page.Locator("#canclose-allow").ClickAsync();
        await Part("canclose-modal", ".bit-mdl-cls").ClickAsync();

        await Expect(Page.Locator("#canclose-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("CanClose:False,CanClose:False,CanClose:False,CanClose:True,OnDismiss");
    }

    [TestMethod]
    public async Task ReducedMotion_RefusedDismissal_PlaysTheRingInsteadOfThePulse()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-blocking-open", "blocking-modal");

        await ClickOverlayAsync("blocking-modal");

        var content = Content("blocking-modal");
        await Expect(content).ToHaveClassAsync(HasClass("bit-mdl-bna"));
        await Expect(content).ToHaveCSSAsync("animation-name", "bit-mdl-refuse-ring-a");
    }

    // ---------- Focus ----------

    [TestMethod]
    public async Task Focus_DataAutofocus_LandsOnTheMarkedElement()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-autofocus-open", "autofocus-modal");

        await Expect(Page.Locator("#autofocus-target")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_NoAutoFocusNoFocusTrapNoRestoreFocus_LeaveTheKeyboardAlone()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-nofocus-open", "nofocus-modal", waitForFocus: false);
        await NextFramesAsync();

        await Expect(Page.Locator("#btn-nofocus-open")).ToBeFocusedAsync();

        await Page.Locator("#nofocus-close").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await IsFocusInsideAsync("nofocus-modal"), "Tab leaves a modal that does not trap the focus.");

        await Page.Locator("#nofocus-close").ClickAsync();
        await Expect(Page.Locator("#nofocus-open")).ToHaveTextAsync("False");
        await Expect(Root("nofocus-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#btn-nofocus-open")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_NothingFocusableInside_FocusesTheContentBox()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-emptyfocus-open", "emptyfocus-modal");

        await Expect(Content("emptyfocus-modal")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_PressOnARefusingOverlay_KeepsTheFocusInsideTheModal()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-blocking-open", "blocking-modal");

        await ClickOverlayAsync("blocking-modal");

        await Expect(Page.Locator("#dismissal-log")).ToHaveTextAsync("OnOverlayClick");
        Assert.IsTrue(await IsFocusInsideAsync("blocking-modal"));
    }

    // ---------- Scroll ----------

    [TestMethod]
    public async Task NoScrollLock_LeavesThePageScrolling()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-noscroll-open", "noscroll-modal");

        Assert.AreNotEqual("hidden", await BodyOverflowAsync());
    }

    [TestMethod]
    public async Task AutoToggleScroll_TakesThePageOverflowAndHandsItBack()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-autotoggle-open", "autotoggle-modal");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#autotoggle-open")).ToHaveTextAsync("False");
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");
    }

    [TestMethod]
    public async Task ScrollerSelector_HoldsTheNamedScrollerInsteadOfThePage()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-scrollsel-open", "scrollsel-modal");

        await Expect(Page.Locator("#mdl-scroll-box")).ToHaveCSSAsync("overflow-y", "hidden");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#scrollsel-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdl-scroll-box")).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task ScrollerElement_HoldsTheElementGivenByReferenceInsteadOfThePage()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-scrollel-open", "scrollel-modal");

        await Expect(Page.Locator("#mdl-scrollel-box")).ToHaveCSSAsync("overflow-y", "hidden");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#scrollel-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mdl-scrollel-box")).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task NoScrollLockWithAScroller_WheelOverTheOverlayScrollsThatScroller()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-forward-open", "forward-modal");

        var box = Page.Locator("#mdl-forward-box");
        Assert.AreEqual(0, await box.EvaluateAsync<double>("el => el.scrollTop"));

        await Page.Mouse.MoveAsync(900, 500);
        await Page.Mouse.WheelAsync(0, 200);

        await Page.WaitForFunctionAsync("() => document.getElementById('mdl-forward-box').scrollTop > 0");
    }

    // ---------- Overlay ----------

    [TestMethod]
    public async Task ModeFull_DimsThePageBehindTheModal()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-modefull-open", "modefull-modal");

        await Expect(Root("modefull-modal")).ToHaveClassAsync(HasClass("bit-mdl-mfl"));
        await Expect(Part("modefull-modal", ".bit-mdl-ovl")).ToHaveCSSAsync("background-color", await ResolveColorAsync("--bit-clr-bg-overlay"));
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await ResolveColorAsync("--bit-clr-bg-overlay"));
    }

    [TestMethod]
    public async Task Modeless_LeavesThePageUsableAndNeitherTrapsNorHolds()
    {
        await OpenAsync(PagePath);
        await Page.Locator("#behind-button").ScrollIntoViewIfNeededAsync();
        await OpenModalAsync("btn-modeless-open", "modeless-modal");

        await Expect(Part("modeless-modal", ".bit-mdl-ovl")).ToHaveCountAsync(0);
        await Expect(Content("modeless-modal")).ToHaveAttributeAsync("aria-modal", "false");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await IsFocusInsideAsync("modeless-modal"));

        await Page.Locator("#behind-button").ClickAsync();
        await Expect(Page.Locator("#behind-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#modeless-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task AriaModalFalse_KeepsTheOverlayButNeitherTrapsTheFocusNorHoldsThePage()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-ariamodal-open", "ariamodal-modal");

        await Expect(Content("ariamodal-modal")).ToHaveAttributeAsync("aria-modal", "false");
        await Expect(Part("ariamodal-modal", ".bit-mdl-ovl")).ToHaveCountAsync(1);
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await IsFocusInsideAsync("ariamodal-modal"));
    }

    // ---------- Position ----------

    [TestMethod]
    [DataRow("", "", "center", "center")]
    [DataRow("TopLeft", "bit-mdl-tlf", "start", "start")]
    [DataRow("TopCenter", "bit-mdl-tcr", "center", "start")]
    [DataRow("TopRight", "bit-mdl-trg", "end", "start")]
    [DataRow("TopStart", "bit-mdl-tst", "start", "start")]
    [DataRow("TopEnd", "bit-mdl-ten", "end", "start")]
    [DataRow("CenterLeft", "bit-mdl-clf", "start", "center")]
    [DataRow("Center", "bit-mdl-ctr", "center", "center")]
    [DataRow("CenterRight", "bit-mdl-crg", "end", "center")]
    [DataRow("CenterStart", "bit-mdl-cst", "start", "center")]
    [DataRow("CenterEnd", "bit-mdl-cen", "end", "center")]
    [DataRow("BottomLeft", "bit-mdl-blf", "start", "end")]
    [DataRow("BottomCenter", "bit-mdl-bcr", "center", "end")]
    [DataRow("BottomRight", "bit-mdl-brg", "end", "end")]
    [DataRow("BottomStart", "bit-mdl-bst", "start", "end")]
    [DataRow("BottomEnd", "bit-mdl-ben", "end", "end")]
    public async Task Position_EachValue_PlacesTheContentAtThatSpotOfTheScreen(string position, string cssClass, string horizontal, string vertical)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#position-select").SelectOptionAsync(position);
        await OpenModalAsync("btn-position-open", "position-modal");

        if (cssClass.Length > 0) await Expect(Root("position-modal")).ToHaveClassAsync(HasClass(cssClass));

        AssertPlaced(await BoxAsync(Root("position-modal")), await BoxAsync(Content("position-modal")), horizontal, vertical);
    }

    [TestMethod]
    public async Task AbsolutePosition_CoversTheContainingBoxOnly()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-abs-open", "abs-modal");

        await Expect(Root("abs-modal")).ToHaveClassAsync(HasClass("bit-mdl-abs"));
        await Expect(Root("abs-modal")).ToHaveCSSAsync("position", "absolute");

        var box = await BoxAsync(Page.Locator("#mdl-abs-box"));
        var root = await BoxAsync(Root("abs-modal"));
        Assert.AreEqual(box.Height - 2, root.Height, 2);
        Assert.AreEqual(box.Y + 1, root.Y, 2);
        AssertPlaced(root, await BoxAsync(Content("abs-modal")), "center", "center");
    }

    // ---------- Draggable ----------

    [TestMethod]
    public async Task Draggable_TheContentDraggedWithTheMouse_MovesTheModal()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-drag-open", "drag-modal");

        await Expect(Content("drag-modal")).ToHaveCSSAsync("cursor", "move");
        await Expect(Content("drag-modal")).ToHaveClassAsync(HasClass("bit-mdl-nta"));

        var before = await BoxAsync(Content("drag-modal"));
        await DragAsync(Page.Locator("#drag-body"), 110, -70);
        var after = await BoxAsync(Content("drag-modal"));

        Assert.AreEqual(before.X + 110, after.X, 3);
        Assert.AreEqual(before.Y - 70, after.Y, 3);

        // A drag that starts on a field is the field's.
        await DragAsync(Page.Locator("#drag-input"), 60, 60, offsetX: 5);
        var afterInput = await BoxAsync(Content("drag-modal"));
        Assert.AreEqual(after.X, afterInput.X, 1);
    }

    [TestMethod]
    public async Task DragElementSelector_OnlyTheNamedHandleDrags()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-handle-open", "handle-modal");

        await Expect(Page.Locator("#mdl-drag-handle")).ToHaveCSSAsync("cursor", "move");

        var before = await BoxAsync(Content("handle-modal"));
        await DragAsync(Page.Locator("#handle-body"), 100, 50);
        Assert.AreEqual(before.X, (await BoxAsync(Content("handle-modal"))).X, 1);

        await DragAsync(Page.Locator("#mdl-drag-handle"), 100, 50);
        var after = await BoxAsync(Content("handle-modal"));
        Assert.AreEqual(before.X + 100, after.X, 3);
        Assert.AreEqual(before.Y + 50, after.Y, 3);
    }

    // ---------- Accessibility ----------

    [TestMethod]
    public async Task TitleAriaIdAndSubtitleAriaId_PointAtTheConsumersElements()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-labelled-open", "labelled-modal");

        await Expect(Content("labelled-modal")).ToHaveAttributeAsync("aria-labelledby", "mdl-title");
        await Expect(Content("labelled-modal")).ToHaveAttributeAsync("aria-describedby", "mdl-subtitle");
    }

    [TestMethod]
    public async Task IsAlert_AnnouncesAnAlertDialogNamedByItsAriaLabel()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-alert-open", "alert-modal");

        await Expect(Content("alert-modal")).ToHaveAttributeAsync("role", "alertdialog");
        await Expect(Content("alert-modal")).ToHaveAttributeAsync("aria-label", "Delete the project");
    }

    [TestMethod]
    public async Task IsAlertFalse_OnABlockingModal_KeepsThePlainDialogRole()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-notalert-open", "notalert-modal");

        await Expect(Content("notalert-modal")).ToHaveAttributeAsync("role", "dialog");
    }

    [TestMethod]
    public async Task AriaLabel_WinsOverTheHeaderTextAsTheName()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-arialabel-open", "arialabel-modal");

        await Expect(Content("arialabel-modal")).ToHaveAttributeAsync("aria-label", "Spoken name");
        await Expect(Content("arialabel-modal")).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".+"));
    }

    // ---------- Keep mounted ----------

    [TestMethod]
    public async Task KeepMounted_ClosedModal_StaysHiddenInThePageAndKeepsItsState()
    {
        await OpenAsync(PagePath);
        await Expect(Root("kept-modal")).ToHaveCountAsync(0);

        await OpenModalAsync("btn-kept-open", "kept-modal");
        await Page.Locator("#kept-input").FillAsync("kept text");
        await Page.Locator("#kept-close").ClickAsync();

        var root = Root("kept-modal");
        await Expect(root).ToHaveClassAsync(HasClass("bit-mdl-hid"));
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Expect(root).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(root).ToHaveAttributeAsync("inert", "");

        await OpenModalAsync("btn-kept-open", "kept-modal");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-mdl-hid"));
        await Expect(Page.Locator("#kept-input")).ToHaveValueAsync("kept text");
    }

    [TestMethod]
    public async Task KeepMounted_Default_StartsFreshOnTheNextOpening()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-unkept-open", "unkept-modal");
        await Page.Locator("#unkept-input").FillAsync("lost text");
        await Page.Locator("#unkept-close").ClickAsync();

        await Expect(Root("unkept-modal")).ToHaveCountAsync(0);

        await OpenModalAsync("btn-unkept-open", "unkept-modal");
        await Expect(Page.Locator("#unkept-input")).ToHaveValueAsync("");
    }

    // ---------- Programmatic ----------

    [TestMethod]
    public async Task Methods_OpenToggleAndClose_DriveAnUncontrolledModal()
    {
        await OpenAsync(PagePath);

        await Page.Locator("#btn-prog-open").ClickAsync();
        await Expect(Content("prog-modal")).ToBeVisibleAsync();

        await Page.Locator("#btn-prog-toggle").ClickAsync();
        await Expect(Root("prog-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#prog-dismiss")).ToHaveTextAsync("1");

        await Page.Locator("#btn-prog-toggle").ClickAsync();
        await Expect(Content("prog-modal")).ToBeVisibleAsync();

        await Page.Locator("#btn-prog-close").ClickAsync();
        await Expect(Root("prog-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#prog-dismiss")).ToHaveTextAsync("2");
    }

    // ---------- Nested ----------

    [TestMethod]
    public async Task Nested_Escape_ClosesOnlyTheInnermostModalAndIsReportedByItAlone()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-outer-open", "outer-modal");
        await OpenModalAsync("btn-inner-open", "inner-modal");

        await Expect(Content("inner-modal")).ToHaveAttributeAsync("role", "alertdialog");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#nested-log")).ToHaveTextAsync("inner");

        await WaitForFocusInsideAsync("outer-modal");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#nested-log")).ToHaveTextAsync("inner,outer");
    }

    [TestMethod]
    public async Task Nested_InnerModal_TrapsTabInsideItself()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-outer-open", "outer-modal");
        await OpenModalAsync("btn-inner-open", "inner-modal");

        await Expect(Page.Locator("#inner-first")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#inner-last")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#inner-first")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Nested_EscapeClaimedByAControl_LeavesTheModalOpen()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-outer-open", "outer-modal");

        await Page.Locator("#nested-claim").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        // The key is still reported - it is the dismissal that the claim keeps back.
        await Expect(Page.Locator("#nested-log")).ToHaveTextAsync("outer");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");
    }

    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_FillInWhatAModalLeavesUnsetAndOwnValuesWin()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-casc-open", "casc-modal");

        await Expect(Root("casc-modal")).ToHaveClassAsync(HasClass("bit-mdl-tcr"));
        await Expect(Root("casc-modal")).ToHaveClassAsync(HasClass("bit-mdl-mfl"));
        await Expect(Part("casc-modal", ".bit-mdl-cls")).ToBeVisibleAsync();
        await Expect(Content("casc-modal")).ToHaveClassAsync(HasClass("cascaded-content"));
        Assert.AreEqual(320, (await BoxAsync(Content("casc-modal"))).Width, 1.5);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("casc-modal")).ToHaveCountAsync(0);

        await OpenModalAsync("btn-cascown-open", "cascown-modal");
        await Expect(Root("cascown-modal")).ToHaveClassAsync(HasClass("bit-mdl-bcr"));
        await Expect(Root("cascown-modal")).Not.ToHaveClassAsync(HasClass("bit-mdl-tcr"));
    }

    // ---------- External icons ----------

    [TestMethod]
    public async Task CloseIconNameAndCloseButtonTitle_ChangeTheGlyphAndTheName()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-iconname-open", "iconname-modal");

        await Expect(Part("iconname-modal", ".bit-mdl-cls")).ToHaveAttributeAsync("aria-label", "Dismiss");
        await Expect(Part("iconname-modal", ".bit-mdl-cls")).ToHaveAttributeAsync("title", "Dismiss");
        await Expect(Part("iconname-modal", ".bit-mdl-cls i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
    }

    [TestMethod]
    public async Task CloseIcon_ExternalIcon_WinsOverCloseIconName()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-iconfa-open", "iconfa-modal");

        var icon = Part("iconfa-modal", ".bit-mdl-cls i");
        await Expect(icon).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(icon).ToHaveClassAsync(HasClass("fa-xmark"));
        Assert.IsFalse((await icon.GetAttributeAsync("class"))!.Contains("bit-icon--"));
    }

    // ---------- Style, Class, Styles, Classes, CSS variables ----------

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-styled-open", "styled-modal");

        await Expect(Root("styled-modal")).ToHaveClassAsync(HasClass("my-root"));
        await Expect(Root("styled-modal")).ToHaveCSSAsync("letter-spacing", "1px");
    }

    [TestMethod]
    [DataRow("", "root")]
    [DataRow(" > .bit-mdl-ovl", "overlay")]
    [DataRow(" > .bit-mdl-ctn", "content")]
    [DataRow(" .bit-mdl-hcn", "header-container")]
    [DataRow(" .bit-mdl-hdr", "header")]
    [DataRow(" .bit-mdl-cls", "close-button")]
    [DataRow(" .bit-mdl-cls > i", "close-icon")]
    [DataRow(" .bit-mdl-bdy", "body")]
    [DataRow(" .bit-mdl-fcn", "footer")]
    public async Task StylesAndClasses_ReachEachPartByName(string selector, string part)
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-styled-open", "styled-modal");

        var value = await Page.Locator($"#styled-modal{selector}").EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--e2e-part')");
        Assert.AreEqual(part, value.Trim(), $"Styles.{part}");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("styled-modal")).ToHaveCountAsync(0);

        await OpenModalAsync("btn-classed-open", "classed-modal");
        await Expect(Page.Locator($"#classed-modal{selector}")).ToHaveClassAsync(HasClass($"part-{part}"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnTheStyle_ReskinTheModal()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-vars-open", "vars-modal");

        var root = Root("vars-modal");
        await Expect(root).ToHaveCSSAsync("z-index", "4321");
        await Expect(root).ToHaveCSSAsync("padding-top", "32px");

        var content = Content("vars-modal");
        await Expect(content).ToHaveCSSAsync("background-color", "rgb(30, 27, 75)");
        await Expect(content).ToHaveCSSAsync("color", "rgb(224, 231, 255)");
        await Expect(content).ToHaveCSSAsync("border-top-color", "rgb(165, 180, 252)");
        await Expect(content).ToHaveCSSAsync("border-top-width", "6px");
        await Expect(content).ToHaveCSSAsync("border-bottom-left-radius", "13px");
        await Expect(content).ToHaveCSSAsync("box-shadow", "none");

        var box = await BoxAsync(content);
        Assert.AreEqual(300, box.Width, 1, "--bit-Modal-max-width");
        Assert.AreEqual(200, box.Height, 1, "--bit-Modal-max-height");


        var overlay = Part("vars-modal", ".bit-mdl-ovl");
        await Expect(overlay).ToHaveCSSAsync("background-color", "rgba(30, 27, 75, 0.6)");
        await Expect(overlay).ToHaveCSSAsync("backdrop-filter", "blur(4px)");

        await Expect(Part("vars-modal", ".bit-mdl-hdr")).ToHaveCSSAsync("font-size", "27px");
        await Expect(Part("vars-modal", ".bit-mdl-hcn")).ToHaveCSSAsync("font-weight", "300");
        await Expect(Part("vars-modal", ".bit-mdl-bdy")).ToHaveCSSAsync("padding-left", "21px");
    }

    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_Dir_LaysTheModalOutRightToLeftAndTopStartLandsTopRight()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-rtl-open", "rtl-modal");

        await Expect(Root("rtl-modal")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("rtl-modal")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Part("rtl-modal", ".bit-mdl-cls")).ToHaveAttributeAsync("aria-label", "بستن");

        var close = await BoxAsync(Part("rtl-modal", ".bit-mdl-cls"));
        var header = await BoxAsync(Part("rtl-modal", ".bit-mdl-hdr"));
        Assert.IsTrue(close.X < header.X, "The close button sits on the left of the header.");

        AssertPlaced(await BoxAsync(Root("rtl-modal")), await BoxAsync(Content("rtl-modal")), "end", "start");
    }

    [TestMethod]
    [DataRow("btn-rtlleft-open", "rtlleft-modal", "start")]
    [DataRow("btn-rtlinherit-open", "rtlinherit-modal", "end")]
    public async Task Rtl_LeftStaysPhysicalAndAnInheritedDirectionMovesStart(string buttonId, string modalId, string horizontal)
    {
        await OpenAsync(PagePath);
        await OpenModalAsync(buttonId, modalId);

        AssertPlaced(await BoxAsync(Root(modalId)), await BoxAsync(Content(modalId)), horizontal, "start");
    }

    // ---------- Base parameters ----------

    [TestMethod]
    public async Task BaseParameters_IdHtmlAttributesAriaLabelAndTabIndex_LandOnTheirElements()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-base-open", "base-modal");

        await Expect(Root("base-modal")).ToHaveAttributeAsync("data-e2e", "modal-root");
        await Expect(Root("base-modal")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Content("base-modal")).ToHaveAttributeAsync("aria-label", "Named by AriaLabel");
    }

    [TestMethod]
    [DataRow("Hidden")]
    [DataRow("Collapsed")]
    public async Task Visibility_TakesTheModalOutOfViewAndItsHoldsWithIt(string visibility)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#visibility-select").SelectOptionAsync(visibility);
        await Page.Locator("#btn-base-open").ClickAsync();
        await Expect(Page.Locator("#base-open")).ToHaveTextAsync("True");
        await Expect(Root("base-modal")).ToHaveCountAsync(1);

        if (visibility == "Hidden") await Expect(Root("base-modal")).ToHaveCSSAsync("visibility", "hidden");
        else await Expect(Root("base-modal")).ToHaveCSSAsync("display", "none");

        await Expect(Content("base-modal")).ToBeHiddenAsync();
        await NextFramesAsync();

        // A Modal out of view neither holds the page nor takes the keyboard.
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());
        Assert.IsFalse(await IsFocusInsideAsync("base-modal"));
    }

    [TestMethod]
    public async Task Disabled_MarksTheMarkupAndRefusesEveryGesture()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-disabled-open", "disabled-modal");

        await Expect(Root("disabled-modal")).ToHaveClassAsync(HasClass("bit-dis"));
        var close = Part("disabled-modal", ".bit-mdl-cls");
        await Expect(close).ToBeDisabledAsync();
        await Expect(close).ToHaveAttributeAsync("aria-disabled", "true");
        await close.DispatchEventAsync("click");

        await ClickOverlayAsync("disabled-modal");
        await Page.Locator("#disabled-page-close").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#disabled-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#disabled-log")).ToHaveTextAsync("");

        await Page.Locator("#disabled-page-close").ClickAsync();
        await Expect(Root("disabled-modal")).ToHaveCountAsync(0);
    }

    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_DefaultIsOpenModal_IsInTheServerHtmlAndClosesOnItsOwn()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/modal/prerender");
        var html = await response.TextAsync();
        StringAssert.Contains(html, "Prerendered modal");
        StringAssert.Contains(html, "bit-mdl-ctn");

        await OpenAsync("/e2e/modal/prerender");

        await Expect(Content("default-open-modal")).ToBeVisibleAsync();
        await Page.Locator("#btn-default-close").ClickAsync();

        await Expect(Root("default-open-modal")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#default-dismiss")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Unmount_WhileOpen_ReleasesThePageWithoutErrors()
    {
        await OpenAsync(PagePath);
        await OpenModalAsync("btn-unmount-open", "unmount-modal");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Locator("#unmount-now").ClickAsync();

        await Expect(Page.Locator("#unmount-shown")).ToHaveTextAsync("False");
        await Expect(Root("unmount-modal")).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
