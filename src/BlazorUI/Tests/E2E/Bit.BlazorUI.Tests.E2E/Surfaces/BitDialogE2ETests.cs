namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// End-to-end tests of BitDialog in a real browser (/e2e/dialog and /e2e/dialog/prerender): opening through the bound
/// IsOpen, Show() and the Open/Close/Toggle methods; the built-in header, message, Ok/Cancel actions and close button and
/// every way of closing (Result, DismissReason and the order and count of the callbacks); the awaited OnOk; the refused
/// dismissals of Blocking, CloseOnOverlayClick, CloseOnEscape and OnDismissing with their shake (and the ring under reduced
/// motion); Modeless; focus moved in, trapped, restored, and the AutoFocus* parameters; the fifteen positions; scroll holds;
/// mouse dragging; nested dialogs where one Escape closes one layer; KeepMounted; BitParams; every Color; close icons;
/// the size parameters; Style/Class/Styles/Classes and the --bit-Dialog-* variables; RTL, explicit and inherited; the base
/// parameters and Disabled; prerendering and unmounting while open.
/// </summary>
[TestClass]
public class BitDialogE2ETests : E2ETestBase
{
    private const string PagePath = "/e2e/dialog";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Container(string id) => Page.Locator($"#{id} .bit-dlg-ctn");

    private ILocator Part(string id, string selector) => Page.Locator($"#{id} {selector}");

    // Opens a dialog from its button and waits until its open sequence has run: the surface is on the screen and,
    // for a dialog that moves the focus in, the focus is inside it - which is also when its Escape is listened for.
    private async Task OpenDialogAsync(string buttonId, string dialogId, bool waitForFocus = true)
    {
        await PressAsync(Page.Locator($"#{buttonId}"));
        await Expect(Container(dialogId)).ToBeVisibleAsync();
        await WaitForAnimationsAsync(Root(dialogId));

        if (waitForFocus) await WaitForFocusInsideAsync(dialogId);
    }

    // The surface grows into place with an entrance animation, so anything measured is measured once it has run.
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

    private Task ClickOverlayAsync(string id)
    {
        return Part(id, ".bit-dlg-ovl").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
    }

    private Task<string> ActiveElementIdAsync()
    {
        return Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''");
    }

    private Task<bool> IsFocusInsideAsync(string id)
    {
        return Page.EvaluateAsync<bool>("id => document.getElementById(id)?.contains(document.activeElement) ?? false", id);
    }

    private Task<string> BodyOverflowAsync()
    {
        return Page.EvaluateAsync<string>("() => document.body.style.overflow");
    }

    // The computed color a CSS custom property of the theme resolves to, the way a component reading it paints it.
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

    // Records, at every change of its classes, what the surface of a dialog carries - its classes, outline and animation -
    // so the refusal it plays back, which lasts a few hundred milliseconds, can be read back after it is over whatever
    // the pace of the engine.
    private Task RecordSurfaceAsync(string id)
    {
        return Container(id).EvaluateAsync(@"el => {
            const take = () => { const s = getComputedStyle(el); window.__e2eSurface.push(el.className + '|' + s.outlineStyle + '|' + s.animationName); };
            window.__e2eSurface = [];
            new MutationObserver(take).observe(el, { attributes: true, attributeFilter: ['class'] });
        }");
    }

    private async Task<string[]> WaitForRecordedSurfaceAsync(string className)
    {
        await Page.WaitForFunctionAsync("c => (window.__e2eSurface ?? []).some(r => r.split('|')[0].split(' ').includes(c))", className);
        return await Page.EvaluateAsync<string[]>("() => window.__e2eSurface");
    }

    private static bool Has(string record, string className) => record.Split('|')[0].Split(' ').Contains(className);

    // ---------- Basic ----------

    [TestMethod]
    public async Task Basic_OpenButton_ShowsTitleMessageActionsAndDialogSemantics()
    {
        await OpenAsync(PagePath);

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);

        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        var container = Container("basic-dialog");
        await Expect(container).ToHaveAttributeAsync("role", "dialog");
        await Expect(container).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Part("basic-dialog", "h2.bit-dlg-ttl")).ToHaveTextAsync("Missing subject");
        await Expect(Part("basic-dialog", ".bit-dlg-msg")).ToHaveTextAsync("Do you want to send this message without a subject?");

        var titleId = await Part("basic-dialog", ".bit-dlg-ttl").GetAttributeAsync("id");
        var messageId = await Part("basic-dialog", ".bit-dlg-msg").GetAttributeAsync("id");
        await Expect(container).ToHaveAttributeAsync("aria-labelledby", titleId!);
        await Expect(container).ToHaveAttributeAsync("aria-describedby", messageId!);

        await Expect(Part("basic-dialog", ".bit-dlg-okb")).ToHaveTextAsync("Ok");
        await Expect(Part("basic-dialog", ".bit-dlg-cnb")).ToHaveTextAsync("Cancel");
        await Expect(Part("basic-dialog", ".bit-dlg-cls")).ToHaveAttributeAsync("title", "Close");
        await Expect(Part("basic-dialog", ".bit-dlg-cls")).ToHaveAttributeAsync("aria-label", "Close");
        await Expect(Part("basic-dialog", ".bit-dlg-cli")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Root("basic-dialog")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Part("basic-dialog", ".bit-dlg-ovl")).ToBeVisibleAsync();

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen");
    }

    [TestMethod]
    public async Task Basic_OkButton_ClosesWithOkResultAndRaisesOnOkThenOnDismiss()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Part("basic-dialog", ".bit-dlg-okb").ClickAsync();

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnOk,OnDismiss");
        await Expect(Page.Locator("#basic-result")).ToHaveTextAsync("Ok");
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync("OkButton");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Basic_CancelButton_ClosesWithCancelResult()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Part("basic-dialog", ".bit-dlg-cnb").ClickAsync();

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnCancel,OnDismiss");
        await Expect(Page.Locator("#basic-result")).ToHaveTextAsync("Cancel");
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync("CancelButton");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Basic_CloseButton_DismissesWithoutAResult()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Part("basic-dialog", ".bit-dlg-cls").ClickAsync();

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnClose,OnDismiss");
        await Expect(Page.Locator("#basic-result")).ToHaveTextAsync("-");
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync("CloseButton");
    }

    [TestMethod]
    public async Task Basic_OverlayClick_DismissesWithTheOverlayReason()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await ClickOverlayAsync("basic-dialog");

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnOverlayClick,OnDismiss");
        await Expect(Page.Locator("#basic-result")).ToHaveTextAsync("-");
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync("OverlayClick");
    }

    [TestMethod]
    public async Task Basic_EscapeKey_DismissesWithTheEscapeReason()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen,OnDismiss");
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync("Escape");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Basic_PageSetsIsOpenFalse_ClosesProgrammaticallyWithoutOnDismiss()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Page.Locator("#basic-page-close").ClickAsync();

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Page.Locator("#btn-basic-refresh").ClickAsync();
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync("Programmatic");
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("OnOpen");
        await Expect(Page.Locator("#basic-changed")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Basic_ParameterChangedWhileOpen_ReRendersTheDialog()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Page.Locator("#basic-retitle").ClickAsync();

        await Expect(Part("basic-dialog", ".bit-dlg-ttl")).ToHaveTextAsync("Renamed subject");
        await Expect(Container("basic-dialog")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Basic_WithoutAutoToggleScroll_LeavesThePageScrolling()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        Assert.AreNotEqual("hidden", await BodyOverflowAsync());
    }

    // ---------- Buttons ----------

    [TestMethod]
    public async Task Buttons_OkTextAndCancelText_RelabelTheActions()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-labels-open", "labels-dialog");

        await Expect(Part("labels-dialog", ".bit-dlg-okb")).ToHaveTextAsync("Move to trash");
        await Expect(Part("labels-dialog", ".bit-dlg-cnb")).ToHaveTextAsync("Keep it");
    }

    [TestMethod]
    public async Task Buttons_ShowCancelAndCloseButtonFalse_LeaveOnlyTheOkAction()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-single-open", "single-dialog");

        await Expect(Part("single-dialog", ".bit-dlg-okb")).ToHaveTextAsync("Got it");
        await Expect(Part("single-dialog", ".bit-dlg-cnb")).ToHaveCountAsync(0);
        await Expect(Part("single-dialog", ".bit-dlg-cls")).ToHaveCountAsync(0);
        await Expect(Part("single-dialog", ".bit-dlg-hdr")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Buttons_ShowOkButtonFalse_LeavesOnlyTheCancelAction()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-nook-open", "nook-dialog");

        await Expect(Part("nook-dialog", ".bit-dlg-okb")).ToHaveCountAsync(0);
        await Expect(Part("nook-dialog", ".bit-dlg-cnb")).ToHaveTextAsync("Back");

        await Part("nook-dialog", ".bit-dlg-cnb").ClickAsync();
        await Expect(Root("nook-dialog")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Buttons_IsOkButtonEnabledFalse_HoldsOkShutUntilTheAnswerExists()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-gated-open", "gated-dialog");

        var ok = Part("gated-dialog", ".bit-dlg-okb");
        await Expect(ok).ToBeDisabledAsync();
        await Expect(ok).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Part("gated-dialog", ".bit-dlg-cnb")).ToBeEnabledAsync();

        await Page.Locator("#gated-agree").CheckAsync();

        await Expect(ok).ToBeEnabledAsync();
        await Expect(ok).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await ok.ClickAsync();
        await Expect(Page.Locator("#gated-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Buttons_IsCancelButtonEnabledFalse_DisablesCancelAndLeavesTheOtherWaysOut()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-gated-open", "gated-dialog");

        await Page.Locator("#gated-toggle-cancel").ClickAsync();

        var cancel = Part("gated-dialog", ".bit-dlg-cnb");
        await Expect(cancel).ToBeDisabledAsync();
        await Expect(cancel).ToHaveAttributeAsync("aria-disabled", "true");

        await Page.Locator("#gated-toggle-cancel").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#gated-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Buttons_NoTitleAndNoCloseButton_RenderNoHeader()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-noheader-open", "noheader-dialog");

        await Expect(Part("noheader-dialog", ".bit-dlg-hdr")).ToHaveCountAsync(0);
        await Expect(Container("noheader-dialog")).Not.ToHaveAttributeAsync("aria-labelledby", new System.Text.RegularExpressions.Regex(".+"));

        // The message takes over the top spacing a header would otherwise have given it.
        var marginTop = await ComputedStyleAsync("#noheader-dialog .bit-dlg-msg", "margin-top");
        Assert.IsTrue(double.Parse(marginTop.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture) > 0, marginTop);
    }

    // ---------- Header and footer ----------

    [TestMethod]
    public async Task Subtitle_RendersUnderTheTitleAndDescribesTheDialogWithTheMessage()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-subtitle-open", "subtitle-dialog");

        var subtitle = Part("subtitle-dialog", ".bit-dlg-htc > .bit-dlg-sub");
        await Expect(subtitle).ToHaveTextAsync("Version 4.2.0");

        var subtitleId = await subtitle.GetAttributeAsync("id");
        var messageId = await Part("subtitle-dialog", ".bit-dlg-msg").GetAttributeAsync("id");
        await Expect(Container("subtitle-dialog")).ToHaveAttributeAsync("aria-describedby", $"{subtitleId} {messageId}");

        var titleBox = await BoxAsync(Part("subtitle-dialog", ".bit-dlg-ttl"));
        var subtitleBox = await BoxAsync(subtitle);
        Assert.IsTrue(subtitleBox.Y >= titleBox.Y + titleBox.Height - 1, "The subtitle sits under the title.");
    }

    [TestMethod]
    public async Task HeaderTemplate_ReplacesTheTitleAndKeepsTheCloseButtonBesideIt()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-header-open", "header-dialog");

        await Expect(Part("header-dialog", ".bit-dlg-hdr .bit-dlg-htc #custom-header-content")).ToHaveTextAsync("Storage almost full");
        await Expect(Part("header-dialog", ".bit-dlg-ttl")).ToHaveCountAsync(0);
        await Expect(Part("header-dialog", ".bit-dlg-hdr > .bit-dlg-cls")).ToBeVisibleAsync();
        await Expect(Container("header-dialog")).ToHaveAttributeAsync("aria-label", "Storage almost full");
        await Expect(Container("header-dialog")).Not.ToHaveAttributeAsync("aria-labelledby", new System.Text.RegularExpressions.Regex(".+"));
    }

    [TestMethod]
    public async Task FooterTemplate_RendersABandUnderTheBodyWhoseContentWorks()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-footer-open", "footer-dialog");

        await Expect(Part("footer-dialog", ".bit-dlg-ftr #footer-not-now")).ToBeVisibleAsync();
        await Expect(Part("footer-dialog", ".bit-dlg-bct")).ToHaveCountAsync(0);

        var bodyBox = await BoxAsync(Part("footer-dialog", ".bit-dlg-scr-cnt"));
        var footerBox = await BoxAsync(Part("footer-dialog", ".bit-dlg-ftr"));
        Assert.IsTrue(footerBox.Y >= bodyBox.Y + bodyBox.Height - 1, "The footer sits under the body.");

        await Page.Locator("#footer-not-now").ClickAsync();
        await Expect(Page.Locator("#footer-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task TitleAriaIdAndSubtitleAriaId_NameAndDescribeTheDialogWithOwnElements()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-custom-open", "custom-dialog");

        await Expect(Container("custom-dialog")).ToHaveAttributeAsync("aria-labelledby", "custom-title");
        await Expect(Container("custom-dialog")).ToHaveAttributeAsync("aria-describedby", "custom-desc");
        await Expect(Part("custom-dialog", ".bit-dlg-scr-cnt #custom-title")).ToHaveTextAsync("All emails together");
        await Expect(Part("custom-dialog", ".bit-dlg-hdr")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Body_RendersInTheScrollingBodyLikeChildContent()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-body-open", "body-dialog");

        await Expect(Part("body-dialog", ".bit-dlg-scr-cnt #body-content")).ToHaveTextAsync("Rendered through Body.");
    }

    // ---------- Result ----------

    [TestMethod]
    [DataRow("ok", "Ok")]
    [DataRow("cancel", "Cancel")]
    [DataRow("close", "null")]
    [DataRow("escape", "null")]
    [DataRow("overlay", "null")]
    public async Task Show_EndedByAGesture_CompletesTheAwaitedTaskWithItsAnswer(string gesture, string expected)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-show-await", "await-dialog");

        switch (gesture)
        {
            case "ok": await Part("await-dialog", ".bit-dlg-okb").ClickAsync(); break;
            case "cancel": await Part("await-dialog", ".bit-dlg-cnb").ClickAsync(); break;
            case "close": await Part("await-dialog", ".bit-dlg-cls").ClickAsync(); break;
            case "escape": await Page.Keyboard.PressAsync("Escape"); break;
            case "overlay": await ClickOverlayAsync("await-dialog"); break;
        }

        await Expect(Root("await-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#awaited")).ToHaveTextAsync(expected);
    }

    [TestMethod]
    public async Task Show_ClosedThroughCloseMethod_CompletesWithNull()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-show-await", "await-dialog");

        await Page.Locator("#await-close-method").ClickAsync();

        await Expect(Root("await-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#awaited")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Show_CalledAgainWhilePending_ReleasesThePreviousAwaiterWithNull()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-show-await", "await-dialog");

        await Page.Locator("#await-show-again").ClickAsync();

        await Expect(Page.Locator("#awaited")).ToHaveTextAsync("null");
        await Expect(Container("await-dialog")).ToBeVisibleAsync();

        await Part("await-dialog", ".bit-dlg-okb").ClickAsync();

        await Expect(Page.Locator("#awaited")).ToHaveTextAsync("null,Ok");
    }

    // ---------- Events ----------

    [TestMethod]
    public async Task OnOk_Awaited_ShowsASpinnerAndHoldsEveryOtherWayOutUntilItReturns()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-slow-open", "slow-dialog");

        var ok = Part("slow-dialog", ".bit-dlg-okb");
        await ok.ClickAsync();

        var spinner = Part("slow-dialog", ".bit-dlg-okb > .bit-dlg-spn");
        await Expect(spinner).ToBeVisibleAsync();
        await Expect(spinner).ToHaveClassAsync(HasClass("part-spinner"));
        Assert.AreEqual("spinner", (await spinner.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--e2e-part')")).Trim());
        await Expect(ok).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(ok).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(ok).ToHaveAttributeAsync("aria-label", "Save");
        await Expect(ok).Not.ToHaveAttributeAsync("disabled", new System.Text.RegularExpressions.Regex(".*"));
        // WebKit leaves the focus on a button it activated by the keyboard only while nothing re-renders it, so the kept
        // focus is checked in the engines that keep it.
        if (IsWebKit is false) await Expect(ok).ToBeFocusedAsync();
        await Expect(Part("slow-dialog", ".bit-dlg-cnb")).ToBeDisabledAsync();
        await Expect(Part("slow-dialog", ".bit-dlg-cls")).ToBeDisabledAsync();
        await Expect(Page.Locator("#slow-log")).ToHaveTextAsync("OnOk:start");

        await Page.Keyboard.PressAsync("Escape");
        await ClickOverlayAsync("slow-dialog");
        await Expect(Page.Locator("#slow-log")).ToHaveTextAsync("OnOk:start,OnOverlayClick");
        await Expect(Container("slow-dialog")).ToBeVisibleAsync();

        await Page.Locator("#slow-release").ClickAsync();

        await Expect(Root("slow-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#slow-log")).ToHaveTextAsync("OnOk:start,OnOverlayClick,OnOk:end,OnDismiss");
    }

    // ---------- Dismiss behavior ----------

    [TestMethod]
    public async Task Blocking_OverlayClickAndEscape_AreRefusedWithAShakeAndOnDismissPrevented()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-blocking-open", "blocking-dialog");

        var container = Container("blocking-dialog");
        await Expect(container).ToHaveAttributeAsync("role", "alertdialog");

        await RecordSurfaceAsync("blocking-dialog");
        await ClickOverlayAsync("blocking-dialog");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("OverlayClick");
        var records = await WaitForRecordedSurfaceAsync("bit-dlg-pvn");
        Assert.IsTrue(records.Any(r => Has(r, "bit-dlg-prv") && Has(r, "bit-dlg-pva") && r.EndsWith("|bit-dlg-prevented-animation-a")), string.Join(" ; ", records));
        await Expect(container).ToHaveClassAsync(HasClass("bit-dlg-pvn"));
        await Expect(container).ToHaveCSSAsync("animation-name", "none");
        await Expect(Page.Locator("#blocking-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("OverlayClick,Escape");
        await Expect(container).ToBeVisibleAsync();

        await Part("blocking-dialog", ".bit-dlg-okb").ClickAsync();
        await Expect(Page.Locator("#blocking-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Blocking_SecondRefusalWhileShaking_ReplaysTheMovementUnderTheOtherName()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-blocking-open", "blocking-dialog");

        await RecordSurfaceAsync("blocking-dialog");

        // Two presses in a row: the second refusal arrives while the first one is still being played back.
        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("Escape,Escape");
        var records = await WaitForRecordedSurfaceAsync("bit-dlg-pvb");
        Assert.IsTrue(records.Any(r => Has(r, "bit-dlg-pvb") && r.EndsWith("|bit-dlg-prevented-animation-b")), string.Join(" ; ", records));
    }

    [TestMethod]
    public async Task CloseOnOverlayClickFalse_RefusesTheOverlayButEscapeCloses()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-nooverlay-open", "nooverlay-dialog");

        await ClickOverlayAsync("nooverlay-dialog");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("OverlayClick");
        await Expect(Container("nooverlay-dialog")).ToBeVisibleAsync();

        await WaitForFocusInsideAsync("nooverlay-dialog");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#nooverlay-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task CloseOnEscapeFalse_RefusesEscapeButTheOverlayCloses()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-noescape-open", "noescape-dialog");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("Escape");
        await Expect(Container("noescape-dialog")).ToBeVisibleAsync();

        await ClickOverlayAsync("noescape-dialog");

        await Expect(Page.Locator("#noescape-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Modeless_HasNoOverlayLeavesThePageUsableAndDoesNotTrapTheFocus()
    {
        await OpenAsync(PagePath);
        await Page.Locator("#behind-button").ScrollIntoViewIfNeededAsync();
        await OpenDialogAsync("btn-modeless-open", "modeless-dialog");

        await Expect(Part("modeless-dialog", ".bit-dlg-ovl")).ToHaveCountAsync(0);
        await Expect(Container("modeless-dialog")).ToHaveAttributeAsync("aria-modal", "false");
        await Expect(Container("modeless-dialog")).ToHaveAttributeAsync("role", "dialog");
        await Expect(Root("modeless-dialog")).ToHaveClassAsync(HasClass("bit-dlg-mls"));

        await Part("modeless-dialog", ".bit-dlg-cnb").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await IsFocusInsideAsync("modeless-dialog"), "Tab leaves a modeless dialog.");

        await Page.Locator("#behind-button").ClickAsync();

        await Expect(Page.Locator("#behind-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#modeless-open")).ToHaveTextAsync("True");
        await Expect(Container("modeless-dialog")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("btn-notalert-open", "notalert-dialog", "dialog")]
    [DataRow("btn-alert-open", "alert-dialog", "alertdialog")]
    public async Task IsAlert_OverridesTheRoleBlockingWouldGive(string buttonId, string dialogId, string role)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync(buttonId, dialogId);

        await Expect(Container(dialogId)).ToHaveAttributeAsync("role", role);
    }

    [TestMethod]
    public async Task NoDismissPreventedAnimation_RefusesWithoutShakingButStillRaisesTheCallback()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-noanim-open", "noanim-dialog");

        await ClickOverlayAsync("noanim-dialog");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("OverlayClick");
        await NextFramesAsync();
        var classes = await Container("noanim-dialog").GetAttributeAsync("class");
        Assert.IsFalse(classes!.Contains("bit-dlg-prv") || classes.Contains("bit-dlg-pv"), classes);
        await Expect(Page.Locator("#noanim-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task ReducedMotion_RefusedDismissal_RingsTheSurfaceInsteadOfShaking()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-blocking-open", "blocking-dialog");
        await RecordSurfaceAsync("blocking-dialog");

        await Page.Keyboard.PressAsync("Escape");

        var records = await WaitForRecordedSurfaceAsync("bit-dlg-prv");
        Assert.IsTrue(records.Any(r => Has(r, "bit-dlg-prv") && r.Split('|')[1] == "solid"), string.Join(" ; ", records));
    }

    [TestMethod]
    public async Task ReducedMotion_ForceAnimation_KeepsTheShakeAndNeedsNoRing()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-forceanim-open", "forceanim-dialog");

        await Expect(Root("forceanim-dialog")).ToHaveClassAsync(HasClass("bit-fam"));

        await RecordSurfaceAsync("forceanim-dialog");
        await Page.Keyboard.PressAsync("Escape");

        var records = await WaitForRecordedSurfaceAsync("bit-dlg-pva");
        var shaking = records.First(r => Has(r, "bit-dlg-pva"));
        Assert.AreNotEqual("solid", shaking.Split('|')[1], shaking);
        Assert.AreEqual("bit-dlg-prevented-animation-a", shaking.Split('|')[2], shaking);
    }

    // ---------- Guarded close ----------

    [TestMethod]
    public async Task OnDismissing_Cancel_RefusesEveryGestureAndNamesItsReason()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-guarded-open", "guarded-dialog");

        await Part("guarded-dialog", ".bit-dlg-cnb").ClickAsync();
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CancelButton");

        await Part("guarded-dialog", ".bit-dlg-cls").ClickAsync();
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CancelButton,CloseButton");

        await ClickOverlayAsync("guarded-dialog");
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CancelButton,CloseButton,OverlayClick");

        await Page.Locator("#guarded-toggle").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CancelButton,CloseButton,OverlayClick,Escape");

        await Page.Locator("#guarded-close-method").ClickAsync();
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("CancelButton,CloseButton,OverlayClick,Escape,Programmatic");

        await Expect(Page.Locator("#guarded-prevented")).ToHaveTextAsync("CancelButton,CloseButton,OverlayClick,Escape,Programmatic");
        await Expect(Container("guarded-dialog")).ToBeVisibleAsync();
        await Expect(Page.Locator("#guarded-open")).ToHaveTextAsync("True");

        await Part("guarded-dialog", ".bit-dlg-okb").ClickAsync();

        await Expect(Page.Locator("#guarded-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#guarded-result")).ToHaveTextAsync("Ok");
        await Expect(Page.Locator("#guarded-reason")).ToHaveTextAsync("OkButton");
    }

    [TestMethod]
    public async Task OnDismissing_RefusedCancel_TakesTheAnswerBack()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-guarded-open", "guarded-dialog");

        await RecordSurfaceAsync("guarded-dialog");
        await Part("guarded-dialog", ".bit-dlg-cnb").ClickAsync();

        await Expect(Page.Locator("#guarded-prevented")).ToHaveTextAsync("CancelButton");
        await Expect(Page.Locator("#guarded-result")).ToHaveTextAsync("-");
        await WaitForRecordedSurfaceAsync("bit-dlg-prv");
    }

    [TestMethod]
    public async Task OnDismissing_NotCancelled_LetsTheGestureClose()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-guarded-open", "guarded-dialog");

        await Page.Locator("#guarded-toggle").ClickAsync();
        await Expect(Page.Locator("#guarded-guard")).ToHaveTextAsync("False");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#guarded-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#guarded-reasons")).ToHaveTextAsync("Escape");
        await Expect(Page.Locator("#guarded-reason")).ToHaveTextAsync("Escape");
    }

    // ---------- Focus management ----------

    [TestMethod]
    public async Task Focus_Default_MovesIntoTheFirstFocusableElementAndTrapsTab()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-focus-open", "focus-dialog");

        await Expect(Page.Locator("#focus-input")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Part("focus-dialog", ".bit-dlg-okb")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Part("focus-dialog", ".bit-dlg-cnb")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#focus-input")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Part("focus-dialog", ".bit-dlg-cnb")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_DialogCloses_HandsTheFocusBackToTheOpener()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-focus-open", "focus-dialog");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#focus-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#btn-focus-open")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_AutoFocusAndTrapFocusOff_LeaveTheFocusOutsideAndLetTabLeave()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-nofocus-open", "nofocus-dialog", waitForFocus: false);
        await NextFramesAsync();

        await Expect(Page.Locator("#btn-nofocus-open")).ToBeFocusedAsync();

        await Part("nofocus-dialog", ".bit-dlg-cnb").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        Assert.IsFalse(await IsFocusInsideAsync("nofocus-dialog"), "Tab leaves a dialog that does not trap the focus.");
    }

    [TestMethod]
    [DataRow("btn-focusbtn-ok", ".bit-dlg-okb")]
    [DataRow("btn-focusbtn-cancel", ".bit-dlg-cnb")]
    [DataRow("btn-focusbtn-close", ".bit-dlg-cls")]
    public async Task Focus_AutoFocusButton_LandsOnTheNamedButton(string buttonId, string selector)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync(buttonId, "focusbtn-dialog");

        await Expect(Part("focusbtn-dialog", selector)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_AutoFocusSelector_LandsOnTheSelectedElementRatherThanTheFirst()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-focussel-open", "focussel-dialog");

        await Expect(Page.Locator("#focussel-input")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_DataAutofocus_LandsOnTheMarkedElement()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-dataauto-open", "dataauto-dialog");

        await Expect(Page.Locator("#dataauto-second")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Focus_RestoreFocusFalse_DoesNotHandTheFocusBack()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-norestore-open", "norestore-dialog");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#norestore-open")).ToHaveTextAsync("False");
        await NextFramesAsync();
        Assert.AreNotEqual("btn-norestore-open", await ActiveElementIdAsync());
    }

    [TestMethod]
    public async Task Focus_NothingFocusableInside_FocusesTheSurfaceItself()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-empty-open", "empty-dialog");

        await Expect(Container("empty-dialog")).ToBeFocusedAsync();
        await Expect(Container("empty-dialog")).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task Focus_PressOnARefusingOverlay_KeepsTheFocusInsideTheDialog()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-nooverlay-open", "nooverlay-dialog");

        await ClickOverlayAsync("nooverlay-dialog");

        await Expect(Page.Locator("#prevented")).ToHaveTextAsync("OverlayClick");
        Assert.IsTrue(await IsFocusInsideAsync("nooverlay-dialog"), "A press on the overlay leaves the focus inside the dialog.");
        await Expect(Container("nooverlay-dialog")).ToBeFocusedAsync();
    }

    // ---------- Position ----------

    [TestMethod]
    [DataRow("TopLeft", "bit-dlg-tl", "start", "start")]
    [DataRow("TopCenter", "bit-dlg-tc", "center", "start")]
    [DataRow("TopRight", "bit-dlg-tr", "end", "start")]
    [DataRow("TopStart", "bit-dlg-ts", "start", "start")]
    [DataRow("TopEnd", "bit-dlg-te", "end", "start")]
    [DataRow("CenterLeft", "bit-dlg-cl", "start", "center")]
    [DataRow("Center", "bit-dlg-ctr", "center", "center")]
    [DataRow("CenterRight", "bit-dlg-cr", "end", "center")]
    [DataRow("CenterStart", "bit-dlg-cs", "start", "center")]
    [DataRow("CenterEnd", "bit-dlg-ce", "end", "center")]
    [DataRow("BottomLeft", "bit-dlg-bl", "start", "end")]
    [DataRow("BottomCenter", "bit-dlg-bc", "center", "end")]
    [DataRow("BottomRight", "bit-dlg-br", "end", "end")]
    [DataRow("BottomStart", "bit-dlg-bs", "start", "end")]
    [DataRow("BottomEnd", "bit-dlg-be", "end", "end")]
    public async Task Position_EachValue_PlacesTheSurfaceAtThatSpotOfTheScreen(string position, string cssClass, string horizontal, string vertical)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#position-select").SelectOptionAsync(position);
        await OpenDialogAsync("btn-position-open", "position-dialog");

        await Expect(Part("position-dialog", ".bit-dlg-doc")).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Part("position-dialog", ".bit-dlg-ttl")).ToHaveTextAsync(position);

        AssertPlaced(await BoxAsync(Part("position-dialog", ".bit-dlg-doc")), await BoxAsync(Container("position-dialog")), horizontal, vertical);
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

    // ---------- Scroll ----------

    [TestMethod]
    public async Task AutoToggleScroll_HoldsThePageWhileOpenAndHandsItBackOnClose()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-scroll-open", "scroll-dialog");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#scroll-open")).ToHaveTextAsync("False");

        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");
    }

    [TestMethod]
    public async Task AbsolutePosition_CoversAndHoldsItsBoxAtTheScrolledOffset()
    {
        await OpenAsync(PagePath);
        await Page.Locator("#abs-box").EvaluateAsync("el => el.scrollTop = 200");
        await OpenDialogAsync("btn-abs-open", "abs-dialog");

        await Expect(Root("abs-dialog")).ToHaveClassAsync(HasClass("bit-dlg-abs"));
        await Expect(Root("abs-dialog")).ToHaveCSSAsync("position", "absolute");
        await Expect(Page.Locator("#abs-box")).ToHaveCSSAsync("overflow-y", "hidden");
        await Expect(Root("abs-dialog")).ToHaveCSSAsync("top", "200px");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        var box = await BoxAsync(Page.Locator("#abs-box"));
        var root = await BoxAsync(Root("abs-dialog"));
        Assert.IsTrue(Math.Abs(root.Y - box.Y) <= 2 && root.Height <= box.Height + 1, $"box {box.Y},{box.Height} root {root.Y},{root.Height}");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#abs-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#abs-box")).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task ScrollerElement_HoldsTheElementGivenByReference()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-scrollel-open", "scrollel-dialog");

        await Expect(Page.Locator("#scrollel-box")).ToHaveCSSAsync("overflow-y", "hidden");
        Assert.AreNotEqual("hidden", await BodyOverflowAsync());

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#scrollel-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#scrollel-box")).ToHaveCSSAsync("overflow-y", "auto");
    }

    // ---------- Draggable ----------

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

    [TestMethod]
    public async Task IsDraggable_HeaderDraggedWithTheMouse_MovesTheSurface()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-drag-open", "drag-dialog");

        var header = Part("drag-dialog", ".bit-dlg-hdr");
        await Expect(header).ToHaveCSSAsync("cursor", "move");
        await Expect(header).ToHaveClassAsync(HasClass("bit-mdl-nta"));

        var before = await BoxAsync(Container("drag-dialog"));
        await DragAsync(header, 120, 60);
        var after = await BoxAsync(Container("drag-dialog"));

        Assert.AreEqual(before.X + 120, after.X, 3, "moved horizontally");
        Assert.AreEqual(before.Y + 60, after.Y, 3, "moved vertically");
    }

    [TestMethod]
    public async Task IsDraggable_TurnedOffWhileOpen_StopsTheDrag()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-drag-open", "drag-dialog");

        await Page.Locator("#drag-toggle").ClickAsync();
        await Expect(Page.Locator("#drag-on")).ToHaveTextAsync("False");

        var header = Part("drag-dialog", ".bit-dlg-hdr");
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-mdl-nta"));
        await Expect(header).Not.ToHaveCSSAsync("cursor", "move");

        var before = await BoxAsync(Container("drag-dialog"));
        await DragAsync(header, 120, 60);
        var after = await BoxAsync(Container("drag-dialog"));

        Assert.AreEqual(before.X, after.X, 1);
        Assert.AreEqual(before.Y, after.Y, 1);
    }

    [TestMethod]
    public async Task IsDraggable_DraggedPastTheEdge_StaysInsideItsArea()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-drag-open", "drag-dialog");

        await DragAsync(Part("drag-dialog", ".bit-dlg-hdr"), -3000, -3000);

        var area = await BoxAsync(Part("drag-dialog", ".bit-dlg-doc"));
        var after = await BoxAsync(Container("drag-dialog"));
        Assert.AreEqual(area.X, after.X, 2);
        Assert.AreEqual(area.Y, after.Y, 2);
    }

    [TestMethod]
    public async Task IsDraggable_ShownAgain_ComesBackWhereItWasLaidOut()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-drag-open", "drag-dialog");

        var laidOut = await BoxAsync(Container("drag-dialog"));
        await DragAsync(Part("drag-dialog", ".bit-dlg-hdr"), 150, 80);
        Assert.AreNotEqual(laidOut.X, (await BoxAsync(Container("drag-dialog"))).X, 10);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#drag-open")).ToHaveTextAsync("False");

        await OpenDialogAsync("btn-drag-open", "drag-dialog");
        var again = await BoxAsync(Container("drag-dialog"));
        Assert.AreEqual(laidOut.X, again.X, 2);
        Assert.AreEqual(laidOut.Y, again.Y, 2);
    }

    [TestMethod]
    public async Task DragElementSelector_OnlyTheNamedHandleDragsAndItsButtonStillWorks()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-handle-open", "handle-dialog");

        await Expect(Page.Locator("#handle-bar")).ToHaveCSSAsync("cursor", "move");

        var before = await BoxAsync(Container("handle-dialog"));
        await DragAsync(Page.Locator("#handle-body"), 100, 50);
        var afterBody = await BoxAsync(Container("handle-dialog"));
        Assert.AreEqual(before.X, afterBody.X, 1, "a drag on the body does not move the dialog");

        // The press on the body selected text, and that selection is left in place: a press on the handle is a move all
        // the same - Firefox would otherwise drag the selected text away instead of moving the dialog.
        await Expect(Page.Locator("#handle-bar")).ToHaveCSSAsync("user-select", "none");

        await DragAsync(Page.Locator("#handle-bar"), 100, 50, offsetX: 10);
        var afterHandle = await BoxAsync(Container("handle-dialog"));
        Assert.AreEqual(before.X + 100, afterHandle.X, 3, "a drag on the handle moves the dialog");
        Assert.AreEqual(before.Y + 50, afterHandle.Y, 3);

        await Page.Locator("#handle-close").ClickAsync();
        await Expect(Page.Locator("#handle-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task IsDraggable_WithoutAHeader_TheWholeSurfaceIsTheHandle()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-draghead-open", "draghead-dialog");

        await Expect(Container("draghead-dialog")).ToHaveCSSAsync("cursor", "move");

        var before = await BoxAsync(Container("draghead-dialog"));
        await DragAsync(Part("draghead-dialog", ".bit-dlg-msg"), -90, 40);
        var after = await BoxAsync(Container("draghead-dialog"));

        Assert.AreEqual(before.X - 90, after.X, 3);
        Assert.AreEqual(before.Y + 40, after.Y, 3);
    }

    // ---------- Nested ----------

    [TestMethod]
    public async Task Nested_Escape_ClosesOnlyTheInnermostDialog()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-outer-open", "outer-dialog");
        await OpenDialogAsync("btn-inner-open", "inner-dialog");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-open")).ToHaveTextAsync("False");
        await Expect(Root("inner-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");
        await Expect(Container("outer-dialog")).ToBeVisibleAsync();

        await Expect(Page.Locator("#btn-inner-open")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Nested_InnerDialog_TrapsTabInsideItself()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-outer-open", "outer-dialog");
        await OpenDialogAsync("btn-inner-open", "inner-dialog");

        await Expect(Page.Locator("#inner-input")).ToBeFocusedAsync();

        for (var i = 0; i < 4; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.IsTrue(await IsFocusInsideAsync("inner-dialog"), $"Tab {i + 1} stays in the inner dialog.");
        }
    }

    [TestMethod]
    public async Task Nested_EscapeClaimedByAControl_LeavesTheDialogOpen()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-outer-open", "outer-dialog");

        await Page.Locator("#nested-claim").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await NextFramesAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");
        await Expect(Container("outer-dialog")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Nested_OpenDropdownList_EscapeClosesOnlyTheList()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-outer-open", "outer-dialog");

        await Page.Locator("#nested-dropdown").ClickAsync();
        await Expect(Page.Locator("#nested-dd-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#nested-dd-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#outer-open")).ToHaveTextAsync("True");
        await Expect(Container("outer-dialog")).ToBeVisibleAsync();
    }

    // ---------- Keep mounted ----------

    [TestMethod]
    public async Task KeepMounted_ClosedDialog_StaysHiddenInThePageAndKeepsItsState()
    {
        await OpenAsync(PagePath);
        await Expect(Root("kept-dialog")).ToHaveCountAsync(0);

        await OpenDialogAsync("btn-kept-open", "kept-dialog");
        await Page.Locator("#kept-input").FillAsync("kept text");
        await Part("kept-dialog", ".bit-dlg-cnb").ClickAsync();

        await Expect(Page.Locator("#kept-open")).ToHaveTextAsync("False");
        var root = Root("kept-dialog");
        await Expect(root).ToHaveCountAsync(1);
        await Expect(root).ToHaveClassAsync(HasClass("bit-dlg-hdn"));
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Expect(root).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(root).ToHaveAttributeAsync("inert", "");

        await OpenDialogAsync("btn-kept-open", "kept-dialog");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dlg-hdn"));
        await Expect(Page.Locator("#kept-input")).ToHaveValueAsync("kept text");
    }

    [TestMethod]
    public async Task KeepMounted_Default_RemovesTheDialogAndStartsFresh()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-unkept-open", "unkept-dialog");
        await Page.Locator("#unkept-input").FillAsync("lost text");
        await Part("unkept-dialog", ".bit-dlg-cnb").ClickAsync();

        await Expect(Root("unkept-dialog")).ToHaveCountAsync(0);

        await OpenDialogAsync("btn-unkept-open", "unkept-dialog");
        await Expect(Page.Locator("#unkept-input")).ToHaveValueAsync("");
    }

    // ---------- Programmatic ----------

    [TestMethod]
    public async Task Methods_OpenToggleAndClose_DriveAnUncontrolledDialog()
    {
        await OpenAsync(PagePath);

        await Page.Locator("#btn-prog-open").ClickAsync();
        await Expect(Container("prog-dialog")).ToBeVisibleAsync();

        await Page.Locator("#btn-prog-toggle").ClickAsync();
        await Expect(Root("prog-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#prog-dismiss")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#prog-reasons")).ToHaveTextAsync("Programmatic");

        await Page.Locator("#btn-prog-toggle").ClickAsync();
        await Expect(Container("prog-dialog")).ToBeVisibleAsync();

        await Page.Locator("#btn-prog-close").ClickAsync();
        await Expect(Root("prog-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#prog-dismiss")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#prog-reasons")).ToHaveTextAsync("Programmatic,Programmatic");

        await Page.Locator("#btn-prog-close").ClickAsync();
        await Page.Locator("#btn-prog-refresh").ClickAsync();
        await Expect(Page.Locator("#prog-dismiss")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("False");
    }

    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_FillInWhatADialogLeavesUnsetAndOwnValuesWin()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-casc-file-open", "casc-file-dialog");

        await Expect(Part("casc-file-dialog", ".bit-dlg-okb")).ToHaveTextAsync("Delete");
        await Expect(Part("casc-file-dialog", ".bit-dlg-cnb")).ToHaveTextAsync("Keep");
        await Expect(Part("casc-file-dialog", ".bit-dlg-cls")).ToHaveCountAsync(0);
        await Expect(Part("casc-file-dialog", ".bit-dlg-doc")).ToHaveClassAsync(HasClass("bit-dlg-tc"));
        await Expect(Root("casc-file-dialog")).ToHaveClassAsync(HasClass("bit-dlg-err"));
        await Expect(Container("casc-file-dialog")).ToHaveClassAsync(HasClass("cascaded-container"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("casc-file-dialog")).ToHaveCountAsync(0);

        await OpenDialogAsync("btn-casc-folder-open", "casc-folder-dialog");
        await Expect(Part("casc-folder-dialog", ".bit-dlg-okb")).ToHaveTextAsync("Delete folder");
        await Expect(Part("casc-folder-dialog", ".bit-dlg-cnb")).ToHaveTextAsync("Keep");
    }

    // ---------- Color ----------

    [TestMethod]
    [DataRow("", "", "--bit-clr-pri")]
    [DataRow("Primary", "bit-dlg-pri", "--bit-clr-pri")]
    [DataRow("Secondary", "bit-dlg-sec", "--bit-clr-sec")]
    [DataRow("Tertiary", "bit-dlg-ter", "--bit-clr-ter")]
    [DataRow("Info", "bit-dlg-inf", "--bit-clr-inf")]
    [DataRow("Success", "bit-dlg-suc", "--bit-clr-suc")]
    [DataRow("Warning", "bit-dlg-wrn", "--bit-clr-wrn")]
    [DataRow("SevereWarning", "bit-dlg-swr", "--bit-clr-swr")]
    [DataRow("Error", "bit-dlg-err", "--bit-clr-err")]
    [DataRow("PrimaryBackground", "bit-dlg-pbg", "--bit-clr-bg-pri")]
    [DataRow("SecondaryBackground", "bit-dlg-sbg", "--bit-clr-bg-sec")]
    [DataRow("TertiaryBackground", "bit-dlg-tbg", "--bit-clr-bg-ter")]
    [DataRow("PrimaryForeground", "bit-dlg-pfg", "--bit-clr-fg-pri")]
    [DataRow("SecondaryForeground", "bit-dlg-sfg", "--bit-clr-fg-sec")]
    [DataRow("TertiaryForeground", "bit-dlg-tfg", "--bit-clr-fg-ter")]
    [DataRow("PrimaryBorder", "bit-dlg-pbr", "--bit-clr-brd-pri")]
    [DataRow("SecondaryBorder", "bit-dlg-sbr", "--bit-clr-brd-sec")]
    [DataRow("TertiaryBorder", "bit-dlg-tbr", "--bit-clr-brd-ter")]
    public async Task Color_EachValue_PaintsTheActionsInThatRole(string color, string cssClass, string roleVariable)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#color-select").SelectOptionAsync(color);
        await OpenDialogAsync("btn-color-open", "color-dialog");
        await Page.Mouse.MoveAsync(2, 2);

        var root = Root("color-dialog");
        if (cssClass.Length > 0)
        {
            await Expect(root).ToHaveClassAsync(HasClass(cssClass));
        }
        else
        {
            var classes = await root.GetAttributeAsync("class");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(classes!, @"bit-dlg-(pri|sec|ter|inf|suc|wrn|swr|err|pbg|sbg|tbg|pfg|sfg|tfg|pbr|sbr|tbr)\b"), classes);
        }

        var expected = await ResolveColorAsync(roleVariable);
        await Expect(Part("color-dialog", ".bit-dlg-okb")).ToHaveCSSAsync("background-color", expected);
        await Expect(Part("color-dialog", ".bit-dlg-cnb")).ToHaveCSSAsync("border-top-color", expected);

        // The surface itself is not the color's to paint.
        await Expect(Container("color-dialog")).ToHaveCSSAsync("background-color", await ResolveColorAsync("--bit-clr-bg-pri"));
    }

    // ---------- External icons ----------

    [TestMethod]
    public async Task CloseIconNameAndCloseButtonTitle_ChangeTheGlyphAndTheAccessibleName()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-iconname-open", "iconname-dialog");

        var close = Part("iconname-dialog", ".bit-dlg-cls");
        await Expect(close).ToHaveAttributeAsync("title", "Dismiss");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Dismiss");
        await Expect(Part("iconname-dialog", ".bit-dlg-cli")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await Expect(Part("iconname-dialog", ".bit-dlg-cli")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    [DataRow("btn-iconcss-open", "iconcss-dialog", "my-close-x")]
    [DataRow("btn-iconfa-open", "iconfa-dialog", "fa-solid fa-xmark")]
    [DataRow("btn-iconbi-open", "iconbi-dialog", "bi bi-x-lg")]
    public async Task CloseIcon_ExternalIcon_RendersItsClassesInsteadOfTheBuiltInGlyph(string buttonId, string dialogId, string classes)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync(buttonId, dialogId);

        var icon = Part(dialogId, ".bit-dlg-cli");
        foreach (var cssClass in classes.Split(' '))
        {
            await Expect(icon).ToHaveClassAsync(HasClass(cssClass));
        }

        var all = await icon.GetAttributeAsync("class");
        Assert.IsFalse(all!.Contains("bit-icon--"), all);
    }

    // ---------- Size ----------

    [TestMethod]
    public async Task Width_SetsTheWidthOfTheSurface()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-width-open", "width-dialog");

        Assert.AreEqual(512, (await BoxAsync(Container("width-dialog"))).Width, 1);
    }

    [TestMethod]
    public async Task Width_OnAPhoneNarrowerThanIt_IsCappedToTheScreen()
    {
        await Page.SetViewportSizeAsync(360, 640);
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-width-open", "width-dialog");

        var area = await BoxAsync(Part("width-dialog", ".bit-dlg-doc"));
        var box = await BoxAsync(Container("width-dialog"));
        Assert.AreEqual(area.Width, box.Width, 1, "A 32rem dialog never outgrows a phone.");
        Assert.AreEqual(area.X, box.X, 1);
    }

    [TestMethod]
    public async Task MinWidthAndMaxWidth_BoundTheWidthOfTheSurface()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-minmax-open", "minmax-dialog");

        var width = (await BoxAsync(Container("minmax-dialog"))).Width;
        Assert.AreEqual(448, width, 1, "a long message grows the surface to MaxWidth");
        await Expect(Container("minmax-dialog")).ToHaveCSSAsync("min-width", "320px");
    }

    [TestMethod]
    public async Task HeightAndMaxHeight_SetTheHeightAndTheBodyScrolls()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-height-open", "height-dialog");

        Assert.AreEqual(320, (await BoxAsync(Container("height-dialog"))).Height, 1);

        var scrolls = await Part("height-dialog", ".bit-dlg-scr-cnt").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight");
        Assert.IsTrue(scrolls, "The body scrolls what the capped surface cannot show.");
    }

    [TestMethod]
    public async Task MinHeight_KeepsTheSurfaceAtLeastThatTall()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-minheight-open", "minheight-dialog");

        Assert.IsTrue((await BoxAsync(Container("minheight-dialog"))).Height >= 299);
    }

    [TestMethod]
    [DataRow("btn-fullwidth-open", "fullwidth-dialog", true, false)]
    [DataRow("btn-fullheight-open", "fullheight-dialog", false, true)]
    [DataRow("btn-fullsize-open", "fullsize-dialog", true, true)]
    public async Task FullWidthFullHeightFullSize_StretchTheSurfaceAcrossItsArea(string buttonId, string dialogId, bool wide, bool tall)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync(buttonId, dialogId);

        var area = await BoxAsync(Part(dialogId, ".bit-dlg-doc"));
        var box = await BoxAsync(Container(dialogId));

        if (wide)
        {
            await Expect(Root(dialogId)).ToHaveClassAsync(HasClass("bit-dlg-fwi"));
            Assert.AreEqual(area.Width, box.Width, 1);
        }
        else
        {
            Assert.IsTrue(box.Width < area.Width - 50);
        }

        if (tall)
        {
            await Expect(Root(dialogId)).ToHaveClassAsync(HasClass("bit-dlg-fhe"));
            Assert.AreEqual(area.Height, box.Height, 1);
        }
        else
        {
            Assert.IsTrue(box.Height < area.Height - 50);
        }
    }

    [TestMethod]
    public async Task MaxWidthVariable_OnAnAncestor_CapsTheSurface()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-maxvar-open", "maxvar-dialog");

        Assert.AreEqual(240, (await BoxAsync(Container("maxvar-dialog"))).Width, 1);
    }

    [TestMethod]
    public async Task MarginVariable_OnAnAncestor_KeepsAGutterAroundTheSurface()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-margin-open", "margin-dialog");

        var root = await BoxAsync(Root("margin-dialog"));
        var box = await BoxAsync(Container("margin-dialog"));
        Assert.AreEqual(root.X + 24, box.X, 1);
        Assert.AreEqual(root.Y + 24, box.Y, 1);
        Assert.AreEqual(root.Width - 48, box.Width, 1);
        Assert.AreEqual(root.Height - 48, box.Height, 1);
    }

    // ---------- Style, Class, Styles, Classes, CSS variables ----------

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-styled-open", "styled-dialog");

        await Expect(Root("styled-dialog")).ToHaveClassAsync(HasClass("my-root"));
        await Expect(Root("styled-dialog")).ToHaveClassAsync(HasClass("bit-dlg"));
        await Expect(Root("styled-dialog")).ToHaveCSSAsync("letter-spacing", "1px");
    }

    [TestMethod]
    [DataRow("", "root")]
    [DataRow(" .bit-dlg-doc", "document")]
    [DataRow(" .bit-dlg-ovl", "overlay")]
    [DataRow(" .bit-dlg-ctn", "container")]
    [DataRow(" .bit-dlg-hdr", "header")]
    [DataRow(" .bit-dlg-scr-cnt", "body")]
    [DataRow(" .bit-dlg-ttl", "title")]
    [DataRow(" .bit-dlg-sub", "subtitle")]
    [DataRow(" .bit-dlg-cls", "close-button")]
    [DataRow(" .bit-dlg-cli", "close-icon")]
    [DataRow(" .bit-dlg-msg", "message")]
    [DataRow(" .bit-dlg-bct", "buttons-container")]
    [DataRow(" .bit-dlg-okb", "ok-button")]
    [DataRow(" .bit-dlg-cnb", "cancel-button")]
    [DataRow(" .bit-dlg-ftr", "footer")]
    public async Task StylesAndClasses_ReachEachPartByName(string selector, string part)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-styled-open", "styled-dialog");

        var styled = Page.Locator($"#styled-dialog{selector}");
        var value = await styled.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--e2e-part')");
        Assert.AreEqual(part, value.Trim(), $"Styles.{part}");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("styled-dialog")).ToHaveCountAsync(0);

        await OpenDialogAsync("btn-classed-open", "classed-dialog");
        await Expect(Page.Locator($"#classed-dialog{selector}")).ToHaveClassAsync(HasClass($"part-{part}"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_ReskinTheDialog()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-vars-open", "vars-dialog");
        await Page.Mouse.MoveAsync(2, 2);

        var container = Container("vars-dialog");
        await Expect(container).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(container).ToHaveCSSAsync("color", "rgb(200, 210, 220)");
        await Expect(container).ToHaveCSSAsync("border-top-left-radius", "3px");
        await Expect(container).ToHaveCSSAsync("border-top-width", "4px");
        await Expect(container).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
        await Expect(container).ToHaveCSSAsync("box-shadow", "none");
        await Expect(Root("vars-dialog")).ToHaveCSSAsync("z-index", "1234");

        var overlay = Part("vars-dialog", ".bit-dlg-ovl");
        await Expect(overlay).ToHaveCSSAsync("background-color", "rgba(255, 0, 0, 0.5)");
        await Expect(overlay).ToHaveCSSAsync("backdrop-filter", "blur(2px)");

        await Expect(Part("vars-dialog", ".bit-dlg-hdr")).ToHaveCSSAsync("padding-left", "30px");

        var title = Part("vars-dialog", ".bit-dlg-ttl");
        await Expect(title).ToHaveCSSAsync("color", "rgb(250, 0, 0)");
        await Expect(title).ToHaveCSSAsync("font-size", "31px");
        await Expect(title).ToHaveCSSAsync("font-weight", "300");
        await Expect(title).ToHaveCSSAsync("text-align", "center");

        await Expect(Part("vars-dialog", ".bit-dlg-sub")).ToHaveCSSAsync("color", "rgb(0, 250, 0)");
        await Expect(Part("vars-dialog", ".bit-dlg-msg")).ToHaveCSSAsync("color", "rgb(0, 0, 250)");
        await Expect(Part("vars-dialog", ".bit-dlg-msg")).ToHaveCSSAsync("text-align", "center");

        var ok = Part("vars-dialog", ".bit-dlg-okb");
        await Expect(ok).ToHaveCSSAsync("background-color", "rgb(11, 12, 13)");
        await Expect(ok).ToHaveCSSAsync("color", "rgb(240, 241, 242)");
        await Expect(ok).ToHaveCSSAsync("border-top-left-radius", "9px");

        var cancel = Part("vars-dialog", ".bit-dlg-cnb");
        await Expect(cancel).ToHaveCSSAsync("background-color", "rgb(21, 22, 23)");
        await Expect(cancel).ToHaveCSSAsync("color", "rgb(230, 231, 232)");
        await Expect(cancel).ToHaveCSSAsync("border-top-left-radius", "7px");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColor_WinsOverTheButtonVariablesItPaints()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-varscolor-open", "varscolor-dialog");
        await Page.Mouse.MoveAsync(2, 2);

        var error = await ResolveColorAsync("--bit-clr-err");
        var ok = Part("varscolor-dialog", ".bit-dlg-okb");
        await Expect(ok).ToHaveCSSAsync("background-color", error);
        await Expect(ok).ToHaveCSSAsync("border-top-left-radius", "9px");

        var cancel = Part("varscolor-dialog", ".bit-dlg-cnb");
        await Expect(cancel).ToHaveCSSAsync("color", error);
        await Expect(cancel).ToHaveCSSAsync("background-color", "rgb(21, 22, 23)");

        // Under the pointer the role's hover fill wins over the hover variable as well.
        await ok.HoverAsync();
        await Expect(ok).ToHaveCSSAsync("background-color", await ResolveColorAsync("--bit-clr-err-hover"));
    }

    [TestMethod]
    public async Task CssVariables_ButtonStateVariables_RepaintTheActionsAtRestUnderThePointerAndWhilePressed()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-vars-open", "vars-dialog");
        await Page.Mouse.MoveAsync(2, 2);

        var ok = Part("vars-dialog", ".bit-dlg-okb");
        var cancel = Part("vars-dialog", ".bit-dlg-cnb");
        await Expect(ok).ToHaveCSSAsync("border-top-color", "rgb(31, 32, 33)");
        await Expect(cancel).ToHaveCSSAsync("border-top-color", "rgb(81, 82, 83)");

        await ok.HoverAsync();
        await Expect(ok).ToHaveCSSAsync("background-color", "rgb(41, 42, 43)");
        await Expect(ok).ToHaveCSSAsync("color", "rgb(51, 52, 53)");
        await Page.Mouse.DownAsync();
        await Expect(ok).ToHaveCSSAsync("background-color", "rgb(61, 62, 63)");
        await Expect(ok).ToHaveCSSAsync("color", "rgb(71, 72, 73)");
        await Page.Mouse.MoveAsync(2, 2);
        await Page.Mouse.UpAsync();

        await cancel.HoverAsync();
        await Expect(cancel).ToHaveCSSAsync("background-color", "rgb(91, 92, 93)");
        await Expect(cancel).ToHaveCSSAsync("color", "rgb(101, 102, 103)");
        await Page.Mouse.DownAsync();
        await Expect(cancel).ToHaveCSSAsync("background-color", "rgb(111, 112, 113)");
        await Expect(cancel).ToHaveCSSAsync("color", "rgb(121, 122, 123)");
        await Page.Mouse.MoveAsync(2, 2);
        await Page.Mouse.UpAsync();

        await Expect(Container("vars-dialog")).ToBeVisibleAsync();
    }

    // ---------- Keyboard ----------

    [TestMethod]
    [DataRow(".bit-dlg-okb", "Enter", "Ok", "OkButton")]
    [DataRow(".bit-dlg-okb", " ", "Ok", "OkButton")]
    [DataRow(".bit-dlg-cnb", "Enter", "Cancel", "CancelButton")]
    [DataRow(".bit-dlg-cnb", " ", "Cancel", "CancelButton")]
    [DataRow(".bit-dlg-cls", "Enter", "-", "CloseButton")]
    public async Task Keyboard_EnterOrSpaceOnAFocusedAction_AnswersTheDialog(string selector, string key, string result, string reason)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        await Part("basic-dialog", selector).FocusAsync();
        await Page.Keyboard.PressAsync(key == " " ? "Space" : key);

        await Expect(Root("basic-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic-result")).ToHaveTextAsync(result);
        await Expect(Page.Locator("#basic-reason")).ToHaveTextAsync(reason);
    }

    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_Dir_MirrorsTheHeaderAndTheActions()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-rtl-open", "rtl-dialog");

        await Expect(Root("rtl-dialog")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("rtl-dialog")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Part("rtl-dialog", ".bit-dlg-cls")).ToHaveAttributeAsync("aria-label", "بستن");

        var close = await BoxAsync(Part("rtl-dialog", ".bit-dlg-cls"));
        var title = await BoxAsync(Part("rtl-dialog", ".bit-dlg-ttl"));
        Assert.IsTrue(close.X < title.X, "The close button sits on the left of the title.");

        var ok = await BoxAsync(Part("rtl-dialog", ".bit-dlg-okb"));
        var cancel = await BoxAsync(Part("rtl-dialog", ".bit-dlg-cnb"));
        Assert.IsTrue(ok.X > cancel.X, "Ok comes first, on the right.");
    }

    [TestMethod]
    public async Task Ltr_Default_KeepsTheCloseButtonOnTheRightAndOkFirstOnTheLeft()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-basic-open", "basic-dialog");

        var close = await BoxAsync(Part("basic-dialog", ".bit-dlg-cls"));
        var title = await BoxAsync(Part("basic-dialog", ".bit-dlg-ttl"));
        Assert.IsTrue(close.X > title.X);

        var ok = await BoxAsync(Part("basic-dialog", ".bit-dlg-okb"));
        var cancel = await BoxAsync(Part("basic-dialog", ".bit-dlg-cnb"));
        Assert.IsTrue(ok.X < cancel.X);
    }

    [TestMethod]
    [DataRow("btn-rtlstart-open", "rtlstart-dialog", "end")]
    [DataRow("btn-rtlleft-open", "rtlleft-dialog", "start")]
    [DataRow("btn-rtlinherit-open", "rtlinherit-dialog", "end")]
    public async Task Rtl_StartFollowsTheDirectionAndLeftStaysPhysical(string buttonId, string dialogId, string horizontal)
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync(buttonId, dialogId);

        AssertPlaced(await BoxAsync(Part(dialogId, ".bit-dlg-doc")), await BoxAsync(Container(dialogId)), horizontal, "start");
    }

    // ---------- Base parameters ----------

    [TestMethod]
    public async Task BaseParameters_IdHtmlAttributesAriaLabelAndTabIndex_LandOnTheirElements()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-base-open", "base-dialog");

        var root = Root("base-dialog");
        await Expect(root).ToHaveAttributeAsync("data-e2e", "dialog-root");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Container("base-dialog")).ToHaveAttributeAsync("aria-label", "Named by AriaLabel");
        await Expect(Container("base-dialog")).Not.ToHaveAttributeAsync("aria-labelledby", new System.Text.RegularExpressions.Regex(".+"));
    }

    [TestMethod]
    [DataRow("Visible", "visible", "flex")]
    [DataRow("Hidden", "hidden", "")]
    [DataRow("Collapsed", "", "none")]
    public async Task Visibility_HidesOrCollapsesTheRoot(string visibility, string expectedVisibility, string expectedDisplay)
    {
        await OpenAsync(PagePath);
        await Page.Locator("#visibility-select").SelectOptionAsync(visibility);
        await Page.Locator("#btn-base-open").ClickAsync();
        await Expect(Page.Locator("#base-open")).ToHaveTextAsync("True");

        var root = Root("base-dialog");
        await Expect(root).ToHaveCountAsync(1);

        if (expectedVisibility.Length > 0) await Expect(root).ToHaveCSSAsync("visibility", expectedVisibility);

        if (expectedDisplay == "none")
        {
            await Expect(root).ToHaveCSSAsync("display", "none");
            await Expect(Container("base-dialog")).ToBeHiddenAsync();
        }
        else if (visibility == "Hidden")
        {
            await Expect(Container("base-dialog")).ToBeHiddenAsync();
        }
        else
        {
            await Expect(Container("base-dialog")).ToBeVisibleAsync();
        }
    }

    [TestMethod]
    public async Task Disabled_MarksTheMarkupAndRefusesEveryGesture()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-disabled-open", "disabled-dialog");

        await Expect(Root("disabled-dialog")).ToHaveClassAsync(HasClass("bit-dis"));

        foreach (var selector in new[] { ".bit-dlg-okb", ".bit-dlg-cnb", ".bit-dlg-cls" })
        {
            var button = Part("disabled-dialog", selector);
            await Expect(button).ToBeDisabledAsync();
            await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
            await button.DispatchEventAsync("click");
        }

        await ClickOverlayAsync("disabled-dialog");
        await Page.Locator("#disabled-page-close").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await NextFramesAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#disabled-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#disabled-log")).ToHaveTextAsync("");
        await Expect(Container("disabled-dialog")).ToBeVisibleAsync();

        await Page.Locator("#disabled-page-close").ClickAsync();
        await Expect(Root("disabled-dialog")).ToHaveCountAsync(0);
    }

    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_DefaultIsOpenDialog_IsInTheServerHtmlAndClosesOnItsOwn()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/dialog/prerender");
        var html = await response.TextAsync();
        StringAssert.Contains(html, "Prerendered dialog");
        StringAssert.Contains(html, "bit-dlg-ctn");

        await OpenAsync("/e2e/dialog/prerender");

        await Expect(Container("default-open-dialog")).ToBeVisibleAsync();
        await Part("default-open-dialog", ".bit-dlg-cls").ClickAsync();

        await Expect(Root("default-open-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#default-dismiss")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Unmount_WhileOpen_ReleasesWhatItHeldWithoutErrors()
    {
        await OpenAsync(PagePath);
        await OpenDialogAsync("btn-unmount-open", "unmount-dialog");

        Assert.AreEqual("hidden", await BodyOverflowAsync());

        await Page.Locator("#unmount-now").ClickAsync();

        await Expect(Page.Locator("#unmount-shown")).ToHaveTextAsync("False");
        await Expect(Root("unmount-dialog")).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
