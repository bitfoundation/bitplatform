using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitErrorBoundary on /e2e/error-boundary: a child that throws on click (or while it renders) is
/// caught and the default error UI takes its place - the title, message, illustration / IconName / IconTemplate /
/// external Icon / HideIcon, the exception details and the Copy button (clipboard), the Refresh / Home / Recover
/// buttons with their texts and Hide flags, AdditionalButtons and Footer, ErrorTemplate and ErrorContent - and the ways
/// back out: Recover (button, reference, template context), RecoverKeys, RecoverOnNavigation, Refresh and GoHome. Also
/// OnError / OnRecover with their reasons, Capture and CaptureAsync, CaughtException, the accessibility contract
/// (group named by the title, alert header, AutoFocus, HeadingLevel, a live region of the page's own, the HtmlAttributes
/// dictionary merged), BitParams, Style / Class / Styles / Classes and the CSS variables, RTL explicit and inherited,
/// Body, MaximumErrorCount across a recovery, a caught exception being no error of the host or the page (logged or
/// NoLogging), and unmounting an errored boundary.
/// </summary>
[TestClass]
public class BitErrorBoundaryE2ETests : E2ETestBase
{
    private const string Route = "/e2e/error-boundary";
    private const string DefaultTitle = "Oops, Something went wrong...";

    [TestMethod]
    public async Task Prerender_AQuietBoundaryRendersItsContent_AndNothingOfItsOwn()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Route}")).TextAsync();

        StringAssert.Contains(html, "id=\"basic-content\"");
        StringAssert.Contains(html, "id=\"basic-throw\"");
        Assert.IsFalse(html.Contains("class=\"bit-erb", StringComparison.Ordinal), "A boundary that caught nothing rendered an error UI.");

        await OpenAsync(Route);

        await Expect(Page.Locator("#basic-content")).ToBeVisibleAsync();
        await Expect(Page.Locator(".bit-erb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Basic_AChildThatThrows_IsReplacedByTheDefaultErrorUi_AndRecoverBringsItBack()
    {
        await OpenAsync(Route);

        await Page.Locator("#basic-throw").ClickAsync();

        var root = Page.Locator("#erb-basic");
        await Expect(root).ToBeVisibleAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-erb"));
        await Expect(Page.Locator("#basic-content")).ToHaveCountAsync(0);

        var title = root.Locator(".bit-erb-ttl");
        await Expect(title).ToHaveTextAsync(DefaultTitle);
        Assert.AreEqual("H3", await title.EvaluateAsync<string>("el => el.tagName"));
        await Expect(root.Locator(".bit-erb-svg")).ToBeVisibleAsync();
        await Expect(root.Locator(".bit-erb-hdr")).ToHaveAttributeAsync("role", "alert");

        // The root is a group named by the title.
        await Expect(root).ToHaveAttributeAsync("role", "group");
        await Expect(root).ToHaveAttributeAsync("aria-labelledby", (await title.GetAttributeAsync("id"))!);
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = DefaultTitle })).ToHaveCountAsync(1);

        var buttons = root.Locator(".bit-erb-ftr button");
        await Expect(buttons).ToHaveTextAsync(["Refresh", "Home", "Recover"]);

        // The error UI fills the room it is given and centers its parts.
        await Expect(root).ToHaveCSSAsync("align-items", "center");
        await Expect(root).ToHaveCSSAsync("text-align", "center");

        await buttons.Nth(2).ClickAsync();

        await Expect(Page.Locator("#basic-content")).ToBeVisibleAsync();
        await Expect(root).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());

        // And it catches the next one as well.
        await Page.Locator("#basic-throw").ClickAsync();
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TitleAndMessage_NameAndDescribeTheErrorUi_AndAnEmptyTitleDropsTheHeading()
    {
        await OpenAsync(Route);

        await Page.Locator("#title-throw").ClickAsync();
        var titled = Page.Locator("#erb-title");
        await Expect(titled.Locator(".bit-erb-ttl")).ToHaveTextAsync("This report could not be built");
        var message = titled.Locator(".bit-erb-msg");
        await Expect(message).ToHaveTextAsync("Try again in a minute.");
        await Expect(titled).ToHaveAttributeAsync("aria-describedby", (await message.GetAttributeAsync("id"))!);
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "This report could not be built" })).ToHaveCountAsync(1);

        await Page.Locator("#no-title-throw").ClickAsync();
        var untitled = Page.Locator("#erb-no-title");
        await Expect(untitled.Locator(".bit-erb-msg")).ToHaveTextAsync("Only a message.");
        await Expect(untitled.Locator(".bit-erb-ttl")).ToHaveCountAsync(0);
        await Expect(untitled).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(untitled.Locator(".bit-erb-hdr")).ToHaveAttributeAsync("role", "alert");

        // With nothing to say in a header, the root itself is the alert.
        await Page.Locator("#bare-throw").ClickAsync();
        var bare = Page.Locator("#erb-bare");
        await Expect(bare).ToHaveAttributeAsync("role", "alert");
        await Expect(bare.Locator(".bit-erb-hdr")).ToHaveCountAsync(0);
        await Expect(bare.Locator(".bit-erb-ftr")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Icon_IconNameIconTemplateExternalIconAndHideIcon_ReplaceTheIllustration()
    {
        await OpenAsync(Route);

        await Page.Locator("#icon-name-throw").ClickAsync();
        await Expect(Page.Locator("#erb-icon-name i.bit-erb-ico")).ToHaveClassAsync(HasClass("bit-icon--ErrorBadge"));
        await Expect(Page.Locator("#erb-icon-name i.bit-erb-ico")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#erb-icon-name .bit-erb-svg")).ToHaveCountAsync(0);

        await Page.Locator("#icon-template-throw").ClickAsync();
        await Expect(Page.Locator("#erb-icon-template .bit-erb-hdr #custom-icon")).ToBeVisibleAsync();
        await Expect(Page.Locator("#erb-icon-template .bit-erb-svg")).ToHaveCountAsync(0);

        await Page.Locator("#hide-icon-throw").ClickAsync();
        await Expect(Page.Locator("#erb-hide-icon .bit-erb-ttl")).ToBeVisibleAsync();
        await Expect(Page.Locator("#erb-hide-icon .bit-erb-svg")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#erb-hide-icon .bit-erb-ico")).ToHaveCountAsync(0);

        // An external icon wins over IconName.
        await Page.Locator("#external-icon-throw").ClickAsync();
        var external = Page.Locator("#erb-external-icon i.bit-erb-ico");
        await Expect(external).ToHaveClassAsync(HasClass("my-icon-lib"));
        await Expect(external).ToHaveClassAsync(HasClass("my-icon-warning"));
        await Expect(external).Not.ToHaveClassAsync(HasClass("bit-icon--ErrorBadge"));
    }

    [TestMethod]
    public async Task Details_ShowExceptionRendersAFocusableNamedRegion_AndCopyPutsItOnTheClipboard()
    {
        // Reading the clipboard back takes a permission Playwright can only grant Chromium; the copy itself is a write in
        // answer to a click, which every engine allows.
        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await OpenAsync(Route);

        await Page.Locator("#details-throw").ClickAsync();

        var details = Page.Locator("#erb-details .bit-erb-exp");
        await Expect(details).ToContainTextAsync("System.InvalidOperationException: Boom");
        await Expect(details).ToHaveAttributeAsync("tabindex", "0");
        await Expect(details).ToHaveAttributeAsync("role", "region");
        await Expect(details).ToHaveAttributeAsync("aria-label", "Exception details");
        await details.FocusAsync();
        await Expect(details).ToBeFocusedAsync();

        var copy = Page.Locator("#erb-details .bit-erb-ftr button", new() { HasText = "Copy details" });
        await Expect(copy).ToHaveAttributeAsync("aria-live", "polite");
        await copy.ClickAsync();

        await Expect(Page.Locator("#erb-details .bit-erb-ftr button", new() { HasText = "Copied" })).ToBeVisibleAsync();
        if (IsChromium)
        {
            var clipboard = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
            StringAssert.Contains(clipboard, "System.InvalidOperationException: Boom");
        }

        // The message is up for a moment only.
        await Expect(Page.Locator("#erb-details .bit-erb-ftr button", new() { HasText = "Copy details" })).ToBeVisibleAsync();

        await Page.Locator("#details-custom-throw").ClickAsync();
        await Expect(Page.Locator("#erb-details-custom .bit-erb-exp")).ToHaveAttributeAsync("aria-label", "Server response");
        await Page.Locator("#erb-details-custom .bit-erb-ftr button", new() { HasText = "Copy it" }).ClickAsync();
        await Expect(Page.Locator("#erb-details-custom .bit-erb-ftr button", new() { HasText = "Done" })).ToBeVisibleAsync();

        await Page.Locator("#details-unlabeled-throw").ClickAsync();
        var unlabeled = Page.Locator("#erb-details-unlabeled .bit-erb-exp");
        await Expect(unlabeled).ToBeVisibleAsync();
        await Expect(unlabeled).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(unlabeled).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
    }

    [TestMethod]
    public async Task Actions_TheTextsAreTheParameters_AndHomeAndRefreshReloadThePage()
    {
        // Firefox reports blazor.web.js's own send on the old page's socket, cut off by the forced reload, as an uncaught
        // error of the page. That one is let through and checked for at the end; nothing else may appear.
        AllowPageErrors = IsFirefox;

        await OpenAsync(Route);

        await Page.Locator("#actions-throw").ClickAsync();
        var buttons = Page.Locator("#erb-actions .bit-erb-ftr button");
        await Expect(buttons).ToHaveTextAsync(["Reload", "Back home", "Try again"]);

        await buttons.Nth(2).ClickAsync();
        await Expect(Page.Locator("#actions-throw")).ToBeVisibleAsync();

        // Home goes to HomeUrl with a full load.
        await Page.EvaluateAsync("() => window.__loadMarker = true");
        await Page.Locator("#actions-throw").ClickAsync();
        await Page.RunAndWaitForNavigationAsync(() => Page.Locator("#erb-actions .bit-erb-ftr button", new() { HasText = "Back home" }).ClickAsync());
        await Expect(Page).ToHaveURLAsync(new Regex(@"/e2e/error-boundary\?home=1$"));
        await WaitForStatusAsync("Ready");
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__loadMarker === true"), "Home did not load the page anew.");
        await Expect(Page.Locator("#actions-throw")).ToBeVisibleAsync();

        // Refresh reloads the page in place.
        await Page.EvaluateAsync("() => window.__loadMarker = true");
        await Page.Locator("#actions-throw").ClickAsync();
        await Page.RunAndWaitForNavigationAsync(() => Page.Locator("#erb-actions .bit-erb-ftr button", new() { HasText = "Reload" }).ClickAsync());
        await WaitForStatusAsync("Ready");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/e2e/error-boundary\?home=1$"));
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__loadMarker === true"), "Refresh did not reload the page.");
        await Expect(Page.Locator("#actions-throw")).ToBeVisibleAsync();
        await Expect(Page.Locator("#erb-actions")).ToHaveCountAsync(0);

        Assert.IsTrue(PageErrors.All(e => e.Contains("WebSocket is not in the OPEN state", StringComparison.Ordinal)), string.Join(Environment.NewLine, PageErrors));
    }

    [TestMethod]
    public async Task Footer_AdditionalButtonsAppendToTheRow_WhileFooterReplacesIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#additional-throw").ClickAsync();
        var additional = Page.Locator("#erb-additional .bit-erb-ftr button");
        await Expect(additional).ToHaveTextAsync(["Recover", "Report"]);
        await Page.Locator("#additional-report").ClickAsync();
        await Expect(Page.Locator("#report-count")).ToHaveTextAsync("1");

        await Page.Locator("#footer-throw").ClickAsync();
        var footer = Page.Locator("#erb-footer .bit-erb-ftr button");
        await Expect(footer).ToHaveTextAsync(["Retry"]);
        await Expect(Page.Locator("#erb-footer .bit-erb-ftr")).ToHaveCSSAsync("display", "flex");

        await Page.Locator("#footer-retry").ClickAsync();
        await Expect(Page.Locator("#footer-throw")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Templates_ErrorTemplateWinsOverErrorContent_AndItsContextRecovers()
    {
        await OpenAsync(Route);

        await Page.Locator("#template-throw").ClickAsync();
        await Expect(Page.Locator("#template-message")).ToHaveTextAsync(new Regex("^Boom \\d+$"));
        await Expect(Page.Locator("#erb-template")).ToHaveCountAsync(0);

        await Page.Locator("#template-recover").ClickAsync();
        await Expect(Page.Locator("#template-throw")).ToBeVisibleAsync();
        await Expect(Page.Locator("#template-ui")).ToHaveCountAsync(0);

        await Page.Locator("#content-throw").ClickAsync();
        await Expect(Page.Locator("#content-ui")).ToHaveTextAsync(new Regex("^ErrorContent: Boom \\d+$"));

        await Page.Locator("#both-throw").ClickAsync();
        await Expect(Page.Locator("#both-template")).ToBeVisibleAsync();
        await Expect(Page.Locator("#both-content")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Events_OnErrorReportsTheException_AndOnRecoverTheManualWayOut()
    {
        await OpenAsync(Route);

        await Page.Locator("#events-throw").ClickAsync();

        await Expect(Page.Locator("#error-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-error")).ToHaveTextAsync(new Regex("^Boom \\d+$"));
        await Expect(Page.Locator("#recover-count")).ToHaveTextAsync("0");

        await Page.Locator("#erb-events .bit-erb-ftr button", new() { HasText = "Recover" }).ClickAsync();

        await Expect(Page.Locator("#recover-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-reason")).ToHaveTextAsync("Manual");

        await Page.Locator("#events-throw").ClickAsync();
        await Expect(Page.Locator("#error-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Lifecycle_AnExceptionThrownWhileRendering_IsCaught_AndRecoverThroughTheReferenceRendersAgain()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#render-content")).ToHaveTextAsync("Rendered");

        await Page.Locator("#btn-render-throw").ClickAsync();
        await Expect(Page.Locator("#erb-render .bit-erb-ttl")).ToHaveTextAsync(DefaultTitle);
        await Expect(Page.Locator("#render-content")).ToHaveCountAsync(0);

        await Page.Locator("#btn-render-read").ClickAsync();
        await Expect(Page.Locator("#render-caught")).ToHaveTextAsync("Thrown while rendering");

        // Fixed underneath, the boundary still shows the error until it is recovered.
        await Page.Locator("#btn-render-fix").ClickAsync();
        await Expect(Page.Locator("#erb-render")).ToBeVisibleAsync();

        await Page.Locator("#btn-render-recover").ClickAsync();
        await Expect(Page.Locator("#render-content")).ToHaveTextAsync("Rendered");

        await Page.Locator("#btn-render-read").ClickAsync();
        await Expect(Page.Locator("#render-caught")).ToHaveTextAsync("-");
    }

    [TestMethod]
    public async Task Capture_AnExceptionHandedOverThroughTheReferenceOrTheCascade_IsShown()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-capture").ClickAsync();

        await Expect(Page.Locator("#erb-capture .bit-erb-exp")).ToContainTextAsync("Captured through the reference");
        await Expect(Page.Locator("#captured-message")).ToHaveTextAsync("Captured through the reference");

        await Page.Locator("#erb-capture .bit-erb-ftr button", new() { HasText = "Recover" }).ClickAsync();
        await Expect(Page.Locator("#btn-capture-async")).ToBeVisibleAsync();

        // Thrown in a task nothing awaits, and handed over from off the renderer's thread.
        await Page.Locator("#btn-capture-async").ClickAsync();

        await Expect(Page.Locator("#erb-capture .bit-erb-exp")).ToContainTextAsync("Failed in the background");
        await Expect(Page.Locator("#captured-message")).ToHaveTextAsync("Failed in the background");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task RecoverKeys_AChangedKeyRecoversTheBoundary_AndAnUnchangedOneDoesNot()
    {
        await OpenAsync(Route);

        await Page.Locator("#keys-throw").ClickAsync();
        await Expect(Page.Locator("#erb-keys")).ToBeVisibleAsync();

        await Page.Locator("#btn-record-same").ClickAsync();
        await Expect(Page.Locator("#erb-keys")).ToBeVisibleAsync();
        await Expect(Page.Locator("#recover-count")).ToHaveTextAsync("0");

        await Page.Locator("#btn-record-next").ClickAsync();

        await Expect(Page.Locator("#keys-throw")).ToHaveTextAsync("Throw (record 2)");
        await Expect(Page.Locator("#erb-keys")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#last-reason")).ToHaveTextAsync("Keys");
    }

    [TestMethod]
    public async Task RecoverOnNavigation_ANavigationRecoversOnlyTheBoundaryThatAskedForIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#nav-throw").ClickAsync();
        await Page.Locator("#no-nav-throw").ClickAsync();
        await Expect(Page.Locator("#erb-nav")).ToBeVisibleAsync();
        await Expect(Page.Locator("#erb-no-nav")).ToBeVisibleAsync();

        await Page.Locator("#btn-navigate").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?n="));
        await Expect(Page.Locator("#nav-throw")).ToBeVisibleAsync();
        await Expect(Page.Locator("#last-reason")).ToHaveTextAsync("Navigation");
        await Expect(Page.Locator("#erb-no-nav")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Accessibility_AutoFocusMovesTheFocusToTheGroup_HeadingLevelAndAPageLiveRegionAreHonored()
    {
        await OpenAsync(Route);

        await Page.Locator("#autofocus-throw").ClickAsync();

        var root = Page.Locator("#erb-autofocus");
        await Expect(root).ToBeFocusedAsync();
        await Expect(root).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(root).ToHaveClassAsync(HasClass("bit-erb-anc"));
        Assert.AreEqual("H2", await root.Locator(".bit-erb-ttl").EvaluateAsync<string>("el => el.tagName"));

        // The focus announces the titled group, so the header is no alert on top of it.
        await Expect(root.Locator(".bit-erb-hdr")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        // Tab goes on to the first button of the footer.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(root.Locator(".bit-erb-ftr button").First).ToBeFocusedAsync();

        await Page.Locator("#live-throw").ClickAsync();
        await Expect(Page.Locator("#erb-live")).ToHaveAttributeAsync("role", "status");
        await Expect(Page.Locator("#erb-live .bit-erb-hdr")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        // A dictionary of attributes is merged with what the boundary writes itself, and an aria-live in it makes the
        // root the live region.
        await Page.Locator("#splat-throw").ClickAsync();
        var splat = Page.Locator("#erb-splat");
        await Expect(splat).ToHaveClassAsync(HasClass("splat-class"));
        await Expect(splat).ToHaveClassAsync(HasClass("bit-erb"));
        await Expect(splat).ToHaveCSSAsync("margin-left", "4px");
        await Expect(splat).ToHaveAttributeAsync("data-testid", "erb-splat-attrs");
        await Expect(splat).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(splat).ToHaveAttributeAsync("role", "group");
        await Expect(splat.Locator(".bit-erb-hdr")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Page.Locator("#heading-clamp-throw").ClickAsync();
        Assert.AreEqual("H6", await Page.Locator("#erb-heading-clamp .bit-erb-ttl").EvaluateAsync<string>("el => el.tagName"));
    }

    [TestMethod]
    public async Task Params_BitParamsSetsTheDefaultsOfEveryBoundary_AndAnOwnParameterWins()
    {
        await OpenAsync(Route);

        await Page.Locator("#params-1-throw").ClickAsync();
        var cascaded = Page.Locator("#erb-params-1");
        await Expect(cascaded.Locator(".bit-erb-ttl")).ToHaveTextAsync("Cascaded title");
        await Expect(cascaded.Locator(".bit-erb-msg")).ToHaveTextAsync("Cascaded message");
        await Expect(cascaded).ToHaveClassAsync(HasClass("cascaded-class"));
        await Expect(cascaded.Locator(".bit-erb-ftr button")).ToHaveTextAsync(["Refresh", "Cascaded recover"]);

        await Page.Locator("#params-2-throw").ClickAsync();
        var own = Page.Locator("#erb-params-2");
        await Expect(own.Locator(".bit-erb-ttl")).ToHaveTextAsync("Its own title");
        await Expect(own.Locator(".bit-erb-msg")).ToHaveTextAsync("Cascaded message");
        await Expect(own.Locator(".bit-erb-ftr button")).ToHaveTextAsync(["Refresh", "Cascaded recover"]);
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot_AndStylesAndClassesReachEveryPart()
    {
        await OpenAsync(Route);

        await Page.Locator("#styles-throw").ClickAsync();

        var root = Page.Locator("#erb-styles");
        await Expect(root).ToHaveClassAsync(HasClass("custom-root-class"));
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root).ToHaveCSSAsync("outline-style", "dashed");
        await Expect(root).ToHaveCSSAsync("padding-top", "9px");

        await Expect(root.Locator(".bit-erb-hdr")).ToHaveClassAsync(HasClass("c-header"));
        await Expect(root.Locator(".bit-erb-hdr")).ToHaveCSSAsync("margin-top", "2px");
        await Expect(root.Locator(".bit-erb-svg")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(root.Locator(".bit-erb-svg")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(root.Locator(".bit-erb-ttl")).ToHaveClassAsync(HasClass("c-title"));
        await Expect(root.Locator(".bit-erb-ttl")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(root.Locator(".bit-erb-msg")).ToHaveClassAsync(HasClass("c-message"));
        await Expect(root.Locator(".bit-erb-msg")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(root.Locator(".bit-erb-exp")).ToHaveClassAsync(HasClass("c-exception"));
        await Expect(root.Locator(".bit-erb-exp")).ToHaveCSSAsync("max-height", "50px");
        await Expect(root.Locator(".bit-erb-ftr")).ToHaveClassAsync(HasClass("c-footer"));
        await Expect(root.Locator(".bit-erb-ftr")).ToHaveCSSAsync("margin-bottom", "3px");

        var buttons = root.Locator(".bit-erb-ftr button");
        await Expect(buttons.Nth(0)).ToHaveClassAsync(HasClass("c-refresh"));
        await Expect(buttons.Nth(0)).ToHaveCSSAsync("border-top-left-radius", "4px");
        await Expect(buttons.Nth(1)).ToHaveClassAsync(HasClass("c-home"));
        await Expect(buttons.Nth(1)).ToHaveCSSAsync("border-top-left-radius", "5px");
        await Expect(buttons.Nth(2)).ToHaveClassAsync(HasClass("c-recover"));
        await Expect(buttons.Nth(2)).ToHaveCSSAsync("border-top-left-radius", "6px");
        await Expect(buttons.Nth(3)).ToHaveClassAsync(HasClass("c-copy"));
        await Expect(buttons.Nth(3)).ToHaveCSSAsync("border-top-left-radius", "7px");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheErrorUi()
    {
        await OpenAsync(Route);

        await Page.Locator("#vars-throw").ClickAsync();

        var root = Page.Locator("#erb-vars");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(255, 240, 240)");
        await Expect(root).ToHaveCSSAsync("text-align", "start");
        await Expect(root).ToHaveCSSAsync("align-items", "start");
        await Expect(root).ToHaveCSSAsync("padding-top", "13px");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "8px");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(root.Locator(".bit-erb-ttl")).ToHaveCSSAsync("color", "rgb(128, 0, 0)");
        await Expect(root.Locator(".bit-erb-msg")).ToHaveCSSAsync("color", "rgb(0, 0, 128)");
    }

    [TestMethod]
    public async Task RtlAndBody_TheErrorUiReadsRightToLeft_AndBodyWinsOverChildContent()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#body-content")).ToBeVisibleAsync();
        await Expect(Page.Locator("#child-content")).ToHaveCountAsync(0);

        await Page.Locator("#rtl-throw").ClickAsync();

        var root = Page.Locator("#erb-rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");

        // The footer row reads right to left: its first button is the rightmost one.
        var first = await root.Locator(".bit-erb-ftr button").First.BoundingBoxAsync();
        var last = await root.Locator(".bit-erb-ftr button").Last.BoundingBoxAsync();
        Assert.IsTrue(first!.X > last!.X, $"first button at {first.X}, last at {last.X}");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_LaysTheFooterOutRightToLeft()
    {
        await OpenAsync(Route);

        await Page.Locator("#rtl-inherited-throw").ClickAsync();

        var root = Page.Locator("#erb-rtl-inherited");
        await Expect(root).ToBeVisibleAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rtl"));

        var first = await root.Locator(".bit-erb-ftr button").First.BoundingBoxAsync();
        var last = await root.Locator(".bit-erb-ftr button").Last.BoundingBoxAsync();
        Assert.IsTrue(first!.X > last!.X, $"first button at {first.X}, last at {last.X}");
    }

    [TestMethod]
    public async Task MaximumErrorCount_IsCountedPerError_SoARecoveredBoundaryCatchesAgain()
    {
        await OpenAsync(Route);

        await Page.Locator("#maximum-throw").ClickAsync();
        await Expect(Page.Locator("#erb-maximum")).ToBeVisibleAsync();

        await Page.Locator("#erb-maximum .bit-erb-ftr button", new() { HasText = "Recover" }).ClickAsync();
        await Page.Locator("#maximum-throw").ClickAsync();

        await Expect(Page.Locator("#erb-maximum")).ToBeVisibleAsync();
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Logging_ACaughtExceptionIsNoErrorOfTheHostOrThePage_LoggedOrNot()
    {
        await OpenAsync(Route);

        // The boundary logs what it caught as a warning (IErrorBoundaryLogger), so neither boundary leaves an error in the
        // host's log or the browser's console: a caught exception is a handled one.
        await Page.Locator("#logged-throw").ClickAsync();
        await Expect(Page.Locator("#erb-logged")).ToBeVisibleAsync();

        await Page.Locator("#unlogged-throw").ClickAsync();
        await Expect(Page.Locator("#erb-unlogged")).ToBeVisibleAsync();

        var errors = await GetServerErrorsAsync();
        Assert.IsFalse(errors.Any(e => e.Contains("Boom", StringComparison.Ordinal)), string.Join(Environment.NewLine, errors));
        Assert.AreEqual(0, ConsoleErrors.Count, string.Join(Environment.NewLine, ConsoleErrors));
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Runtime_AnErroredBoundary_UnmountsWithoutAnError()
    {
        await OpenAsync(Route);

        await Page.Locator("#runtime-throw").ClickAsync();
        await Expect(Page.Locator("#erb-runtime")).ToBeVisibleAsync();

        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(Page.Locator("#runtime-mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#erb-runtime")).ToHaveCountAsync(0);

        await Page.Locator("#btn-mount").ClickAsync();
        await Expect(Page.Locator("#runtime-throw")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
