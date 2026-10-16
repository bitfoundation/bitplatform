using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitCallout on a desktop viewport (/e2e/callout and /e2e/callout/placement): the callout relocated to the body
/// and placed against its anchor, an element given by id or reference, or a point (OpenAt), measured with bounding
/// boxes on every side and every alignment, flipping near each edge of the viewport and held there by NoFlip, the
/// automatic Direction, Gap, AlignmentOffset, CollisionPadding, MaxWindowWidth, the arrow on every side with its size
/// and corner padding; opening by click, keyboard, binding, hover with its delays and the methods; closing by an
/// outside click (through the overlay and without it), Escape, a scroll, AutoClose, Tab out and the content, and the
/// NoDismiss* parameters holding it open; nested callouts peeled one by one; AutoFocus, TrapFocus, Modal and the ARIA
/// wiring; the sizing parameters, Header / Footer and a scroll container wired by hand; LazyRender; the events; the
/// BitParams cascade; the surface parameters, Style / Class / Styles / Classes and the public CSS variables reaching
/// the relocated callout; RTL explicit and inherited; Escape inside a modal; Visibility, HtmlAttributes, prerendering,
/// re-rendering and unmounting while open.
/// </summary>
[TestClass]
public class BitCalloutE2ETests : E2ETestBase
{
    private const string Url = "/e2e/callout";
    private const string PlacementUrl = "/e2e/callout/placement";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    // A callout is found by the element the page put in it, since it is relocated to the body while it is open and its
    // own id is generated: the direct child of the callout, or of the body a callout with a header or footer lays out.
    private ILocator Cal(string bodyId) => Page.Locator($".bit-clo-cal:has(> #{bodyId}), .bit-clo-cal:has(> .bit-clo-bdy > #{bodyId})");
    private ILocator El(string id) => Page.Locator($"#{id}");

    // What a callout is placed against: the container its Anchor is rendered in, which is a line box around the
    // trigger and so can stand a few pixels above it.
    private ILocator AnchorOf(string triggerId) => Page.Locator($".bit-clo-acn:has(> #{triggerId})");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheAnchorsAndTheClosedCallouts()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"clo-basic\"");
        StringAssert.Contains(html, "class=\"bit-clo-acn");
        StringAssert.Contains(html, "id=\"clo-basic-trigger\"");
        StringAssert.Contains(html, "id=\"clo-basic-body\"");
        StringAssert.Contains(html, "id=\"eager-clo-input\"");
        Assert.IsFalse(html.Contains("id=\"lazy-clo-input\""), "A LazyRender callout prerenders its content.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_ClickTheAnchor_OpensTheCalloutBelowIt_AndAClickOutsideClosesIt()
    {
        await OpenAsync(Url);

        var trigger = El("clo-basic-trigger");
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Cal("clo-basic-body")).ToBeHiddenAsync();

        var callout = await OpenCalloutAsync("clo-basic-trigger", "clo-basic-body");

        await Expect(El("clo-basic")).ToHaveClassAsync(HasClass("bit-clo-opn"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-clo-ocl"));
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(trigger).ToHaveAttributeAsync("aria-controls", (await callout.GetAttributeAsync("id"))!);
        await Expect(callout).ToHaveCSSAsync("position", "fixed");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        await Expect(trigger).ToBeFocusedAsync();

        var anchor = await BoxAsync(trigger);
        var box = await BoxAsync(callout);
        Near(anchor.Y + anchor.Height + 1, box.Y, "top below the anchor");
        Near(anchor.X, box.X, "left aligned with the anchor");

        // The overlay takes the click: the button behind it never hears it.
        await Page.Mouse.ClickAsync((float)(await BoxAsync(El("clo-basic-behind"))).X + 5, (float)(await BoxAsync(El("clo-basic-behind"))).Y + 5);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("behind-clicks")).ToHaveTextAsync("0");
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(El("clo-basic")).Not.ToHaveClassAsync(HasClass("bit-clo-opn"));
    }

    [TestMethod]
    public async Task Overlay_CoversTheViewportWhileTheCalloutIsOpen()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-basic-trigger", "clo-basic-body");
        var overlay = (await PartAsync(callout, "overlay"));

        await Expect(overlay).ToHaveCSSAsync("display", "block");
        var box = await BoxAsync(overlay);
        var viewport = Page.ViewportSize!;
        Near(0, box.X, "overlay left");
        Near(0, box.Y, "overlay top");
        Near(viewport.Width, box.Width, "overlay width");
        Near(viewport.Height, box.Height, "overlay height");
        await Expect(overlay).ToHaveCSSAsync("background-color", Transparent);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(overlay).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task NoOverlay_ThePageKeepsItsClicks_AndTheClickStillClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-nooverlay-trigger", "clo-nooverlay-body");
        await Expect((await PartAsync(callout, "overlay"))).ToHaveCountAsync(0);

        await El("clo-nooverlay-behind").ClickAsync();

        await Expect(El("nooverlay-behind-clicks")).ToHaveTextAsync("1");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Keyboard_EnterOnTheTriggerOpensIt_AndEscapeClosesItBackToTheTrigger()
    {
        await OpenAsync(Url);

        await El("clo-basic-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Cal("clo-basic-body")).ToBeVisibleAsync();
        await Expect(El("clo-basic")).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Cal("clo-basic-body")).ToBeHiddenAsync();
        await Expect(El("clo-basic-trigger")).ToBeFocusedAsync();
        await Expect(El("clo-basic")).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
    }

    [TestMethod]
    public async Task Disabled_TheAnchorOpensNothing()
    {
        await OpenAsync(Url);

        await Expect(El("clo-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(El("clo-disabled")).ToHaveCSSAsync("pointer-events", "none");

        await El("clo-disabled-trigger").DispatchEventAsync("click");
        var box = await BoxAsync(AnchorOf("clo-disabled-trigger"));
        await Page.Mouse.ClickAsync((float)(box.X + 5), (float)(box.Y + 5));
        await NextFramesAsync();

        await Expect(Cal("clo-disabled-body")).ToBeHiddenAsync();
        await Expect(El("clo-disabled")).Not.ToHaveClassAsync(HasClass("bit-clo-opn"));
    }

    [TestMethod]
    public async Task Disabled_SetWhileOpen_ClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-disrt-trigger", "clo-disrt-body");

        await El("btn-disrt").ClickAsync();

        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("clo-disrt")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(El("disrt-dismissed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task AnchorId_ToggleOpensTheCalloutAgainstThatElement_AndEscapeHandsTheFocusBack()
    {
        await OpenAsync(Url);

        await El("ext-anchor").ClickAsync();
        var callout = Cal("ext-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);

        var anchor = await BoxAsync(El("ext-anchor"));
        var box = await BoxAsync(callout);
        Near(anchor.Y + anchor.Height + 1, box.Y, "top below the external anchor");
        Near(anchor.X, box.X, "left aligned with the external anchor");

        await El("ext-inner").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("ext-dismissed")).ToHaveTextAsync("1");
        await Expect(El("ext-anchor")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AnchorEl_PlacesTheCalloutAgainstTheElementReference()
    {
        await OpenAsync(Url);

        await El("extel-anchor").ClickAsync();
        var callout = Cal("extel-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);

        var anchor = await BoxAsync(El("extel-anchor"));
        var box = await BoxAsync(callout);
        Near(anchor.Y + anchor.Height + 1, box.Y, "top below the element");
        Near(anchor.X, box.X, "left aligned with the element");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Binding_TwoWay_ThePageOpensIt_AndItsContentAndAClickOutsideCloseIt()
    {
        await OpenAsync(Url);

        await El("btn-bind-open").ClickAsync();
        var callout = Cal("clo-bind-body");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(El("clo-bind")).ToHaveClassAsync(HasClass("bit-clo-opn"));
        await Expect(El("bind-value")).ToHaveTextAsync("True");

        await El("btn-bind-done").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("bind-value")).ToHaveTextAsync("False");

        await El("clo-bind-trigger").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(El("bind-value")).ToHaveTextAsync("True");

        await Page.Mouse.ClickAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("bind-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task OpenOnHover_ThePointerOpensIt_KeepsItWhileOverTheCallout_AndClosesItOnceItLeaves()
    {
        await OpenAsync(Url);

        await El("clo-hover-trigger").HoverAsync();
        var callout = Cal("clo-hover-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);

        // The overlay of a hover callout lets the pointer through.
        await Expect((await PartAsync(callout, "overlay"))).ToHaveCSSAsync("pointer-events", "none");

        await El("clo-hover-body").HoverAsync();
        await Page.WaitForTimeoutAsync(400);
        await Expect(callout).ToBeVisibleAsync();

        await El("hover-far").HoverAsync();
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task HoverDelays_HoldTheOpeningAndTheClosing()
    {
        await OpenAsync(Url);

        var callout = Cal("clo-hover-delay-body");
        var watch = Stopwatch.StartNew();
        await El("clo-hover-delay-trigger").HoverAsync();
        await Expect(callout).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 550, $"Opened after {watch.ElapsedMilliseconds}ms.");

        await El("hover-far").HoverAsync();
        watch.Restart();
        Assert.IsTrue(await callout.IsVisibleAsync(), "Closed without waiting for HoverCloseDelay.");
        await Expect(callout).ToBeHiddenAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 450, $"Closed after {watch.ElapsedMilliseconds}ms.");
    }

    [TestMethod]
    public async Task AutoClose_AClickInsideRunsTheItemAndClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-autoclose-trigger", "clo-autoclose-body");

        await El("ac-rename").ClickAsync();

        await Expect(El("auto-action")).ToHaveTextAsync("Renamed");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Scroll_ThePageScrollingUnderTheCallout_ClosesIt()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-scroll-trigger", "clo-scroll-body");

        await Page.EvaluateAsync("() => window.scrollBy(0, 120)");

        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task NoDismissOnScroll_TheCalloutFollowsItsAnchorThroughAScroll()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-noscroll-trigger", "clo-noscroll-body");
        var before = await BoxAsync(callout);

        await Page.EvaluateAsync("() => window.scrollBy(0, 80)");

        await PollAsync(async () => (await BoxAsync(callout)).Y, y => Math.Abs(y - before.Y) > 40, "the callout did not follow its anchor");
        await Expect(callout).ToBeVisibleAsync();
        var anchor = await BoxAsync(AnchorOf("clo-noscroll-trigger"));
        await PollAsync(async () => (await BoxAsync(callout)).Y, y => Math.Abs(y - (anchor.Y + anchor.Height + 1)) < 1.5, "the callout is not below its anchor after the scroll");
    }

    [TestMethod]
    public async Task NoDismissOnEscapeAndOnOutsideClick_LeaveTheContentToCloseIt()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-nodismiss-trigger", "clo-nodismiss-body");

        await Page.Keyboard.PressAsync("Escape");
        await NextFramesAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(El("clo-nodismiss")).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));

        // The overlay is still there, and takes the click without closing anything.
        var outside = await BoxAsync(El("nodismiss-outside"));
        await Page.Mouse.ClickAsync((float)(outside.X + 5), (float)(outside.Y + 5));
        await NextFramesAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(El("nodismiss-outside-clicks")).ToHaveTextAsync("0");

        await El("btn-nodismiss-close").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task OpenAt_ARightClickOpensTheCalloutAtThePoint_AndTheNextOneMovesIt()
    {
        await OpenAsync(Url);

        // At the top of the viewport, so a callout opened at a point inside it has the room to open below it.
        await El("ctx-area").EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");
        var area = await BoxAsync(El("ctx-area"));
        await El("ctx-area").ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 40, Y = 30 } });

        var callout = Cal("ctx-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);
        var box = await BoxAsync(callout);
        Near(area.X + 40, box.X, "left at the point");
        Near(area.Y + 30 + 1, box.Y, "top at the point");

        await El("ctx-area").ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 300, Y = 60 }, Force = true });
        await PollAsync(async () => (await BoxAsync(callout)).X, x => Math.Abs(x - (area.X + 300)) < 1.5, "the callout did not move to the new point");
        Near(area.Y + 60 + 1, (await BoxAsync(callout)).Y, "top at the new point");
        await Expect(El("ctx-opens")).ToHaveTextAsync("1");

        await El("ctx-copy").ClickAsync();
        await Expect(El("ctx-action")).ToHaveTextAsync("Copied");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task OpenAt_Coordinates_OpensTheCalloutAtThatPointOfTheViewport()
    {
        await OpenAsync(Url);

        await El("btn-openat").ClickAsync();

        var callout = Cal("point-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);
        var box = await BoxAsync(callout);
        Near(120, box.X, "left at x");
        Near(141, box.Y, "top at y");
    }

    [TestMethod]
    public async Task Background_EveryColorKind_PaintsTheSurface()
    {
        await OpenAsync(Url);

        var expected = new (string Name, string Class)[]
        {
            ("primary", "bit-clo-bpg"),
            ("secondary", "bit-clo-bsg"),
            ("tertiary", "bit-clo-btg"),
            ("transparent", "bit-clo-brg"),
        };

        var fills = new List<string>();
        foreach (var (name, cssClass) in expected)
        {
            var callout = await OpenCalloutAsync($"clo-bg-{name}-trigger", $"clo-bg-{name}-body");
            await Expect(callout).ToHaveClassAsync(HasClass(cssClass));
            fills.Add(await StyleAsync(callout, "background-color"));
            await CloseByEscapeAsync(callout);
        }

        Assert.AreEqual(4, fills.Distinct().Count(), string.Join(" | ", fills));
        Assert.AreEqual(Transparent, fills[3]);

        var unset = await OpenCalloutAsync("clo-unset-trigger", "clo-unset-body");
        Assert.AreEqual(fills[0], await StyleAsync(unset, "background-color"));
        await Expect(unset).Not.ToHaveClassAsync(new Regex(@"bit-clo-b[pst]g|bit-clo-brg"));
    }

    [TestMethod]
    public async Task Border_EveryColorKind_DrawsAnOutline_AndUnsetDrawsNone()
    {
        await OpenAsync(Url);

        var expected = new (string Name, string Class)[]
        {
            ("primary", "bit-clo-bpr"),
            ("secondary", "bit-clo-bsr"),
            ("tertiary", "bit-clo-btr"),
            ("transparent", "bit-clo-brr"),
        };

        var colors = new List<string>();
        foreach (var (name, cssClass) in expected)
        {
            var callout = await OpenCalloutAsync($"clo-br-{name}-trigger", $"clo-br-{name}-body");
            await Expect(callout).ToHaveClassAsync(HasClass("bit-clo-brd"));
            await Expect(callout).ToHaveClassAsync(HasClass(cssClass));
            await Expect(callout).ToHaveCSSAsync("border-top-width", "1px");
            colors.Add(await StyleAsync(callout, "border-top-color"));
            await CloseByEscapeAsync(callout);
        }

        Assert.AreEqual(4, colors.Distinct().Count(), string.Join(" | ", colors));
        Assert.AreEqual(Transparent, colors[3]);

        var unset = await OpenCalloutAsync("clo-unset-trigger", "clo-unset-body");
        await Expect(unset).ToHaveCSSAsync("border-top-width", "0px");
    }

    [TestMethod]
    public async Task NoShadow_DropsTheElevation()
    {
        await OpenAsync(Url);

        var unset = await OpenCalloutAsync("clo-unset-trigger", "clo-unset-body");
        Assert.AreNotEqual("none", await StyleAsync(unset, "box-shadow"));
        await CloseByEscapeAsync(unset);

        var flat = await OpenCalloutAsync("clo-noshadow-trigger", "clo-noshadow-body");
        await Expect(flat).ToHaveClassAsync(HasClass("bit-clo-nsh"));
        await Expect(flat).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task ShowArrow_PointsAtTheAnchorFromTheEdgeFacingIt()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-arrow-trigger", "clo-arrow-body");
        var arrow = (await PartAsync(callout, "arrow"));

        await Expect(arrow).ToBeVisibleAsync();
        await Expect(arrow).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        Assert.AreEqual(12, await arrow.EvaluateAsync<double>("el => el.offsetWidth"));
        Assert.AreEqual(await StyleAsync(callout, "background-color"), await StyleAsync(arrow, "background-color"));

        var anchor = await BoxAsync(AnchorOf("clo-arrow-trigger"));
        var box = await BoxAsync(callout);
        var beak = await BoxAsync(arrow);
        Near(anchor.Y + anchor.Height + 9, box.Y, "a Gap of 8 between the anchor and the callout");
        Near(anchor.X + anchor.Width / 2, beak.X + beak.Width / 2, "the beak centered on the anchor");
        Near(box.Y, beak.Y + beak.Height / 2, "the beak on the top edge");

        await CloseByEscapeAsync(callout);
        await Expect(arrow).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task ArrowSize_SizesTheBeak()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-arrowsize-trigger", "clo-arrowsize-body");
        var arrow = (await PartAsync(callout, "arrow"));

        await Expect(arrow).ToBeVisibleAsync();
        Assert.AreEqual(20, await arrow.EvaluateAsync<double>("el => el.offsetWidth"));
        Assert.AreEqual(20, await arrow.EvaluateAsync<double>("el => el.offsetHeight"));
    }

    [TestMethod]
    public async Task ArrowPadding_KeepsTheBeakAwayFromTheCorners()
    {
        await OpenAsync(Url);

        var padded = await OpenCalloutAsync("clo-arrowpad-trigger", "clo-arrowpad-body");
        var paddedBeak = await BoxAsync((await PartAsync(padded, "arrow")));
        Near(64, paddedBeak.X + paddedBeak.Width / 2 - (await BoxAsync(padded)).X, "64px from the corner");
        await CloseByEscapeAsync(padded);

        var plain = await OpenCalloutAsync("clo-arrowdef-trigger", "clo-arrowdef-body");
        var plainBeak = await BoxAsync((await PartAsync(plain, "arrow")));
        Near(16, plainBeak.X + plainBeak.Width / 2 - (await BoxAsync(plain)).X, "16px from the corner by default");
    }

    [TestMethod]
    public async Task Sizing_WidthMinWidthMaxWidthAndMaxHeight_SizeTheCallout()
    {
        await OpenAsync(Url);

        var wide = await OpenCalloutAsync("clo-width-trigger", "clo-width-body");
        await Expect(wide).ToHaveClassAsync(HasClass("bit-clo-wid"));
        Near(320, (await BoxAsync(wide)).Width, "Width 20rem");
        await CloseByEscapeAsync(wide);

        var capped = await OpenCalloutAsync("clo-maxwidth-trigger", "clo-maxwidth-body");
        var cappedBox = await BoxAsync(capped);
        Assert.IsTrue(cappedBox.Width <= 256.5, $"MaxWidth 16rem, got {cappedBox.Width}");
        Assert.IsTrue((await BoxAsync(El("clo-maxwidth-body"))).Height > 30, "The long line does not wrap.");
        await CloseByEscapeAsync(capped);

        var tall = await OpenCalloutAsync("clo-maxheight-trigger", "clo-maxheight-body");
        var tallBox = await BoxAsync(tall);
        Assert.IsTrue(tallBox.Height <= 192.5, $"MaxHeight 12rem, got {tallBox.Height}");
        Assert.IsTrue(tallBox.Width >= 223.5, $"MinWidth 14rem, got {tallBox.Width}");
        Assert.IsTrue(await tall.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));
        await Expect(tall).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task SetCalloutWidthAndFixedCalloutWidth_SizeTheCalloutByItsAnchor()
    {
        await OpenAsync(Url);

        var widened = await OpenCalloutAsync("clo-setwidth-trigger", "clo-setwidth-body");
        Near((await BoxAsync(AnchorOf("clo-setwidth-trigger"))).Width, (await BoxAsync(widened)).Width, "at least as wide as the anchor");
        await CloseByEscapeAsync(widened);

        var held = await OpenCalloutAsync("clo-fixedwidth-trigger", "clo-fixedwidth-body");
        var anchorWidth = (await BoxAsync(AnchorOf("clo-fixedwidth-trigger"))).Width;
        Assert.IsTrue((await BoxAsync(held)).Width <= anchorWidth + 0.5, "wider than its anchor");
        Assert.IsTrue((await BoxAsync(El("clo-fixedwidth-body"))).Height > 30, "The long line does not wrap.");
    }

    [TestMethod]
    public async Task HeaderAndFooter_StayPutWhileTheBodyScrolls_AndTheHeaderNamesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-sections-trigger", "clo-sections-body");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-clo-sec"));

        var header = callout.Locator("> .bit-clo-hdr");
        var body = callout.Locator("> .bit-clo-bdy");
        var footer = callout.Locator("> .bit-clo-ftr");
        await Expect(header.Locator("#clo-sections-header")).ToBeVisibleAsync();
        await Expect(footer.Locator("#clo-sections-footer")).ToBeVisibleAsync();
        await Expect(callout).ToHaveAttributeAsync("aria-labelledby", (await header.GetAttributeAsync("id"))!);

        Assert.IsTrue(await body.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "The body does not scroll.");
        var headerBefore = await BoxAsync(header);
        var footerBefore = await BoxAsync(footer);

        await body.EvaluateAsync("el => el.scrollTop = 300");
        await NextFramesAsync();

        Assert.IsTrue(await body.EvaluateAsync<double>("el => el.scrollTop") > 0);
        Near(headerBefore.Y, (await BoxAsync(header)).Y, "the header moved");
        Near(footerBefore.Y, (await BoxAsync(footer)).Y, "the footer moved");
        Assert.IsTrue((await BoxAsync(callout)).Y + (await BoxAsync(callout)).Height <= Page.ViewportSize!.Height + 0.5, "The callout runs off the screen.");
    }

    [TestMethod]
    public async Task ScrollContainerHeaderAndFooterIds_WireUpTheConsumersOwnParts()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-manual-trigger", "clo-manual-body");

        var maxHeight = await El("e2e-scroller").EvaluateAsync<string>("el => el.style.maxHeight");
        Assert.IsTrue(maxHeight.EndsWith("px", StringComparison.Ordinal), $"The scroll container is not capped: '{maxHeight}'.");
        Assert.IsTrue(await El("e2e-scroller").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"));
        var box = await BoxAsync(callout);
        Assert.IsTrue(box.Y + box.Height <= Page.ViewportSize!.Height + 0.5, "The callout runs off the screen.");
        await Expect(El("e2e-scroller-header")).ToBeVisibleAsync();
        await Expect(El("e2e-scroller-footer")).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task AutoFocus_MovesTheFocusIn_ToTheDataAutofocusElementOrTheFirstFocusable()
    {
        await OpenAsync(Url);

        await OpenCalloutAsync("clo-autofocus-trigger", "clo-autofocus-body");
        await Expect(El("af-input")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(El("clo-autofocus-trigger")).ToBeFocusedAsync();

        await OpenCalloutAsync("clo-autofocus2-trigger", "clo-autofocus2-body");
        await Expect(El("af2-first")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TrapFocus_KeepsTabInside_AndReportsAModalDialog()
    {
        await OpenAsync(Url);

        await Expect(El("clo-trap-trigger")).ToHaveAttributeAsync("aria-haspopup", "dialog");
        var callout = await OpenCalloutAsync("clo-trap-trigger", "clo-trap-body");

        await Expect(callout).ToHaveAttributeAsync("role", "dialog");
        await Expect(callout).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(callout).ToHaveAttributeAsync("aria-labelledby", (await callout.Locator("> .bit-clo-hdr").GetAttributeAsync("id"))!);
        await Expect(El("trap-name")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("trap-email")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("trap-submit")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("trap-name")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(El("trap-submit")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("clo-trap-trigger")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Modal_DimsThePage_HoldsItStill_AndTrapsTheFocus()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-modal-trigger", "clo-modal-body");
        var overlay = (await PartAsync(callout, "overlay"));

        await Expect(overlay).ToHaveClassAsync(HasClass("bit-clo-ovm"));
        await Expect(overlay).ToHaveCSSAsync("background-color", new Regex(@"^(?!rgba\(0, 0, 0, 0\)).+"));
        await Expect(Page.Locator("body")).ToHaveCSSAsync("overflow", "hidden");
        await Expect(callout).ToHaveAttributeAsync("role", "dialog");
        await Expect(callout).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(callout).ToHaveAttributeAsync("aria-labelledby", "clo-modal-title");
        await Expect(callout).ToHaveAttributeAsync("aria-describedby", "clo-modal-text");
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Heads up" })).ToHaveAccessibleDescriptionAsync("The page behind is dimmed and holds still.");
        await Expect(El("clo-modal-ok")).ToBeFocusedAsync();

        await El("clo-modal-ok").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");
    }

    [TestMethod]
    public async Task RoleAndAriaLabel_SayWhatTheCalloutIs()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-role-trigger", "clo-role-body");

        await Expect(callout).ToHaveAttributeAsync("role", "status");
        await Expect(callout).ToHaveAttributeAsync("aria-label", "Sync status");
        await Expect(El("clo-role-trigger")).Not.ToHaveAttributeAsync("aria-haspopup", new Regex(".*"));

        // Without a role of its own a callout is the plain group of content it is.
        await Page.Keyboard.PressAsync("Escape");
        var plain = await OpenCalloutAsync("clo-basic-trigger", "clo-basic-body");
        await Expect(plain).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(plain).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task TabOut_TabGoesIntoTheCallout_AndPastItsEndMovesOnAndClosesIt()
    {
        await OpenAsync(Url);

        await El("clo-tabout-trigger").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        var callout = Cal("clo-tabout-body");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(El("clo-tabout-trigger")).ToBeFocusedAsync();
        // The trigger hands Tab to the callout once it reports it as expanded, which the render after the open does.
        await Expect(El("clo-tabout-trigger")).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("tabout-one")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(El("tabout-two")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(El("clo-tabout-trigger")).ToBeFocusedAsync();
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");

        await Expect(El("after-tabout")).ToBeFocusedAsync();
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Nesting_InnerCalloutsStayOpenOverTheOuter_AndEscapePeelsTheInnermost()
    {
        await OpenAsync(Url);

        var outer = await OpenCalloutAsync("clo-outer-trigger", "clo-outer-body");
        var mid = await OpenCalloutAsync("clo-mid-trigger", "clo-mid-body");
        await Expect(outer).ToBeVisibleAsync();

        // Placed at the end of its anchor, inside the outer callout.
        var midAnchor = await BoxAsync(AnchorOf("clo-mid-trigger"));
        var midBox = await BoxAsync(mid);
        Near(midAnchor.X + midAnchor.Width + 9, midBox.X, "the nested callout at the end of its anchor");
        await Expect(mid).ToHaveAttributeAsync("data-bit-cal-pos", "right");

        var inner = await OpenCalloutAsync("clo-inner-trigger", "clo-inner-body");
        await Expect(mid).ToBeVisibleAsync();
        await Expect(outer).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(inner).ToBeHiddenAsync();
        await Expect(mid).ToBeVisibleAsync();
        await Expect(outer).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(mid).ToBeHiddenAsync();
        await Expect(outer).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(outer).ToBeHiddenAsync();

        await Expect(El("inner-dismissed")).ToHaveTextAsync("1");
        await Expect(El("mid-dismissed")).ToHaveTextAsync("1");
        await Expect(El("outer-dismissed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Nesting_AClickOnTheOuterCallout_ClosesOnlyTheOnesAboveIt()
    {
        await OpenAsync(Url);

        var outer = await OpenCalloutAsync("clo-outer-trigger", "clo-outer-body");
        var mid = await OpenCalloutAsync("clo-mid-trigger", "clo-mid-body");

        var label = await BoxAsync(Page.Locator("#clo-outer-body > div").First);
        await Page.Mouse.ClickAsync((float)(label.X + 5), (float)(label.Y + label.Height / 2));

        await Expect(mid).ToBeHiddenAsync();
        await Expect(outer).ToBeVisibleAsync();
        await Expect(El("outer-dismissed")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Nesting_AClickOutsideAllOfThem_ClosesThemAll()
    {
        await OpenAsync(Url);

        var outer = await OpenCalloutAsync("clo-outer-trigger", "clo-outer-body");
        var mid = await OpenCalloutAsync("clo-mid-trigger", "clo-mid-body");

        await Page.Mouse.ClickAsync(5, Page.ViewportSize!.Height - 5);

        await Expect(mid).ToBeHiddenAsync();
        await Expect(outer).ToBeHiddenAsync();
        await Expect(El("outer-dismissed")).ToHaveTextAsync("1");
        await Expect(El("mid-dismissed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task LazyRender_TheContentArrivesWithTheFirstOpening_AndStays()
    {
        await OpenAsync(Url);

        await Expect(El("lazy-clo-input")).ToHaveCountAsync(0);
        await Expect(El("eager-clo-input")).ToHaveCountAsync(1);

        var callout = await OpenCalloutAsync("clo-lazy-trigger", "clo-lazy-body");
        await El("lazy-clo-input").FillAsync("kept");
        await CloseByEscapeAsync(callout);
        await Expect(El("lazy-clo-input")).ToHaveCountAsync(1);

        await OpenCalloutAsync("clo-lazy-trigger", "clo-lazy-body");
        await Expect(El("lazy-clo-input")).ToHaveValueAsync("kept");
    }

    [TestMethod]
    public async Task Events_OnToggleOnOpenAndOnDismiss_FireForEveryWayInAndOut()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-events-trigger", "clo-events-body");
        await Expect(El("ev-toggle")).ToHaveTextAsync("1");
        await Expect(El("ev-last-toggle")).ToHaveTextAsync("True");
        await Expect(El("ev-open")).ToHaveTextAsync("1");
        await Expect(El("ev-dismiss")).ToHaveTextAsync("0");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("ev-toggle")).ToHaveTextAsync("2");
        await Expect(El("ev-last-toggle")).ToHaveTextAsync("False");
        await Expect(El("ev-dismiss")).ToHaveTextAsync("1");

        await OpenCalloutAsync("clo-events-trigger", "clo-events-body");
        await Page.Mouse.ClickAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(El("ev-toggle")).ToHaveTextAsync("4");
        await Expect(El("ev-open")).ToHaveTextAsync("2");
        await Expect(El("ev-dismiss")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Methods_OpenCloseAndToggle_DriveTheCallout()
    {
        await OpenAsync(Url);

        var callout = Cal("clo-m-body");

        await El("btn-m-open").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await El("btn-m-open").ClickAsync();
        await El("btn-m-close").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await El("btn-m-close").ClickAsync();
        await El("btn-m-toggle").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await El("btn-m-toggle").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();

        await Expect(El("m-toggles")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Methods_Reposition_LaysTheCalloutOutAgainAgainstAMovedAnchor()
    {
        await OpenAsync(Url);

        await El("btn-m-open").ClickAsync();
        var callout = Cal("clo-m-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);
        var before = await BoxAsync(callout);

        await El("btn-m-move").ClickAsync();
        await PollAsync(async () => (await BoxAsync(AnchorOf("clo-m-trigger"))).X, x => x > before.X + 60, "the anchor did not move");
        await NextFramesAsync();
        Near(before.X, (await BoxAsync(callout)).X, "the callout moved before Reposition");

        await El("btn-m-reposition").ClickAsync();
        var anchor = await BoxAsync(AnchorOf("clo-m-trigger"));
        await PollAsync(async () => (await BoxAsync(callout)).X, x => Math.Abs(x - anchor.X) < 1.5, "Reposition did not follow the anchor");
        Near(anchor.Y + anchor.Height + 1, (await BoxAsync(callout)).Y, "below the moved anchor");
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndTheCalloutsOwnValuesWin()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-par-trigger", "clo-par-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "above");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-clo-bpr"));
        await Expect((await PartAsync(callout, "arrow"))).ToBeVisibleAsync();
        var anchor = await BoxAsync(AnchorOf("clo-par-trigger"));
        var box = await BoxAsync(callout);
        Near(anchor.Y - 11, box.Y + box.Height, "a cascaded Gap of 10 above the anchor");

        // NoOverlay came from the cascade too, so the page keeps its clicks.
        await Expect((await PartAsync(callout, "overlay"))).ToHaveCountAsync(0);

        var own = await OpenCalloutAsync("clo-par-own-trigger", "clo-par-own-body");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(own).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        await Expect(own).ToHaveClassAsync(HasClass("bit-clo-bpr"));
        await Expect((await PartAsync(own, "arrow"))).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstance_ReachTheRelocatedCalloutAndItsArrow()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-vars-trigger", "clo-vars-body");

        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(30, 41, 59)");
        await Expect(callout).ToHaveCSSAsync("color", "rgb(248, 250, 252)");
        await Expect(callout).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(callout).ToHaveCSSAsync("padding-top", "16px");

        var arrow = (await PartAsync(callout, "arrow"));
        await Expect(arrow).ToHaveCSSAsync("background-color", "rgb(30, 41, 59)");
        Assert.AreEqual(16, await arrow.EvaluateAsync<double>("el => el.offsetWidth"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheRelocatedCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-anc-trigger", "clo-anc-body");

        await Expect(callout).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(callout).ToHaveCSSAsync("border-top-color", "rgb(124, 58, 237)");
        await Expect(callout).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRootAroundTheAnchor()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator(".bit-clo:has(#clo-style-trigger)")).ToHaveCSSAsync("margin-top", "5px");
        var classed = Page.Locator(".bit-clo:has(#clo-class-trigger)");
        await Expect(classed).ToHaveClassAsync(HasClass("e2e-clo-class"));
        await Expect(classed).ToHaveCSSAsync("outline-style", "dotted");

        // The root's class does not travel with the relocated callout.
        var callout = await OpenCalloutAsync("clo-class-trigger", "clo-class-body");
        await Expect(callout).Not.ToHaveClassAsync(HasClass("e2e-clo-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart_AndOpenedOnlyWhileOpen()
    {
        await OpenAsync(Url);

        var anchor = Page.Locator(".bit-clo-acn:has(#clo-styles-trigger)");
        await Expect(anchor).ToHaveCSSAsync("background-color", "rgb(46, 139, 87)");

        var callout = await OpenCalloutAsync("clo-styles-trigger", "clo-styles-body");

        await Expect(anchor).ToHaveCSSAsync("background-color", "rgb(4, 203, 91)");
        await Expect(callout).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(callout).ToHaveCSSAsync("border-top-color", "rgb(4, 203, 91)");
        await Expect((await PartAsync(callout, "arrow"))).ToHaveCSSAsync("outline-color", "rgb(7, 8, 9)");
        await Expect((await PartAsync(callout, "overlay"))).ToHaveCSSAsync("background-color", "rgba(255, 0, 0, 0.2)");

        await CloseByEscapeAsync(callout);
        await Expect(anchor).ToHaveCSSAsync("background-color", "rgb(46, 139, 87)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart_AndOpenedOnlyWhileOpen()
    {
        await OpenAsync(Url);

        var root = Page.Locator(".bit-clo:has(#clo-classes-trigger)");
        var anchor = Page.Locator(".bit-clo-acn:has(#clo-classes-trigger)");
        await Expect(root).ToHaveClassAsync(HasClass("e2e-clo-root"));
        await Expect(root).Not.ToHaveClassAsync(HasClass("e2e-clo-opened"));
        await Expect(anchor).ToHaveClassAsync(HasClass("e2e-clo-anchor"));
        await Expect(anchor).ToHaveCSSAsync("padding-top", "3px");

        var callout = await OpenCalloutAsync("clo-classes-trigger", "clo-classes-body");

        await Expect(root).ToHaveClassAsync(HasClass("e2e-clo-opened"));
        await Expect(callout).ToHaveClassAsync(HasClass("e2e-clo-content"));
        await Expect(callout).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect((await PartAsync(callout, "arrow"))).ToHaveClassAsync(HasClass("e2e-clo-arrow"));
        await Expect((await PartAsync(callout, "arrow"))).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect((await PartAsync(callout, "overlay"))).ToHaveClassAsync(HasClass("e2e-clo-overlay"));
        await Expect((await PartAsync(callout, "overlay"))).ToHaveCSSAsync("background-color", "rgba(0, 0, 255, 0.25)");
    }

    [TestMethod]
    public async Task Rtl_DirRtl_LinesTheCalloutUpWithTheAnchorsRightEdge()
    {
        await OpenAsync(Url);

        await Expect(El("clo-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(El("clo-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        var callout = await OpenCalloutAsync("clo-rtl-trigger", "clo-rtl-body");
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-rtl"));

        var anchor = await BoxAsync(AnchorOf("clo-rtl-trigger"));
        var box = await BoxAsync(callout);
        Near(anchor.X + anchor.Width, box.X + box.Width, "right edges");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_TravelsWithTheRelocatedCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-rtl-inh-trigger", "clo-rtl-inh-body");

        await Expect(callout).ToHaveCSSAsync("direction", "rtl");
        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        var anchor = await BoxAsync(AnchorOf("clo-rtl-inh-trigger"));
        var box = await BoxAsync(callout);
        Near(anchor.X + anchor.Width, box.X + box.Width, "right edges");
    }

    [TestMethod]
    public async Task InsideAModal_EscapeClosesTheCalloutFirst_AndTheModalOnTheNextPress()
    {
        await OpenAsync(Url);

        await El("btn-open-modal").ClickAsync();
        await Expect(El("clo-in-modal-trigger")).ToBeVisibleAsync();

        var callout = await OpenCalloutAsync("clo-in-modal-trigger", "clo-in-modal-body");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await NextFramesAsync();
        await Expect(El("modal-open")).ToHaveTextAsync("True");
        await Expect(El("clo-in-modal-trigger")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(El("modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(El("clo-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(El("clo-vis-hidden-trigger")).ToBeHiddenAsync();
        Assert.IsTrue((await BoxAsync(El("clo-vis-hidden"))).Height > 0);

        await Expect(El("clo-vis-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(El("clo-vis-collapsed-trigger")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(El("clo-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(El("clo-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedWhileOpen_UpdateTheOpenCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-rr-trigger", "clo-rr-body");
        await Expect(El("clo-rr-body")).ToHaveTextAsync("Before");

        await El("btn-rr").ClickAsync();

        await Expect(El("clo-rr-body")).ToHaveTextAsync("After");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-clo-btg"));
        await Expect(callout).ToBeVisibleAsync();
        await PollAsync(async () => (await BoxAsync(callout)).Width, w => w >= 299.5, "MinWidth was not applied");
    }

    [TestMethod]
    public async Task Unmount_WhileOpen_TakesTheRelocatedCalloutAwayWithoutAnError()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("clo-um-trigger", "clo-um-body");
        await Expect((await PartAsync(callout, "arrow"))).ToBeVisibleAsync();
        var arrowId = (await callout.GetAttributeAsync("id"))!.Replace("-content", "-arrow");

        await El("btn-unmount").ClickAsync();

        await Expect(El("clo-um")).ToHaveCountAsync(0);
        await Expect(El("clo-um-body")).ToHaveCountAsync(0);
        await Expect(El(arrowId)).ToHaveCountAsync(0);

        await El("btn-unmount").ClickAsync();
        await OpenCalloutAsync("clo-um-trigger", "clo-um-body");
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    [DataRow("Top", "above")]
    [DataRow("Bottom", "below")]
    [DataRow("Start", "left")]
    [DataRow("End", "right")]
    [DataRow("Left", "left")]
    [DataRow("Right", "right")]
    public async Task Placement_EverySide_PutsTheCalloutThere(string side, string expected)
    {
        await OpenAsync($"{PlacementUrl}?side={side}");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", expected);
        AssertSide(expected, await BoxAsync(AnchorOf("pl-trigger")), await BoxAsync(callout), 1);
    }

    [TestMethod]
    [DataRow("Bottom", "near-bottom", "above")]
    [DataRow("Top", "near-top", "below")]
    [DataRow("Start", "near-left", "right")]
    [DataRow("End", "near-right", "left")]
    [DataRow("Right", "near-right", "left")]
    [DataRow("Left", "near-left", "right")]
    public async Task Placement_WithoutRoomOnTheSideAskedFor_FlipsToTheOppositeOne(string side, string pos, string expected)
    {
        await OpenAsync($"{PlacementUrl}?side={side}&pos={pos}");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", expected);
        AssertSide(expected, await BoxAsync(AnchorOf("pl-trigger")), await BoxAsync(callout), 1);
        AssertWithinViewport(await BoxAsync(callout));
    }

    [TestMethod]
    public async Task NoFlip_KeepsTheSideAskedFor_AndStillKeepsTheCalloutOnTheScreen()
    {
        await OpenAsync($"{PlacementUrl}?side=Bottom&pos=near-bottom&noflip=true");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        var box = await BoxAsync(callout);
        AssertWithinViewport(box);
        Assert.IsTrue(box.Y < (await BoxAsync(AnchorOf("pl-trigger"))).Y + 32, "The forced callout is not slid back onto the screen over its anchor.");
    }

    [TestMethod]
    [DataRow("center", "below")]
    [DataRow("near-bottom", "above")]
    [DataRow("near-top", "below")]
    public async Task Direction_TopAndBottomByDefault_PicksTheVerticalSideWithRoom(string pos, string expected)
    {
        await OpenAsync($"{PlacementUrl}?pos={pos}");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", expected);
        AssertSide(expected, await BoxAsync(AnchorOf("pl-trigger")), await BoxAsync(callout), 1);
    }

    [TestMethod]
    public async Task Direction_All_GoesBesideTheAnchor_WhenNeitherVerticalSideHasRoom()
    {
        await OpenAsync($"{PlacementUrl}?pos=middle&h=500&direction=All");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        var anchor = await BoxAsync(AnchorOf("pl-trigger"));
        Near(anchor.X + anchor.Width + 1, (await BoxAsync(callout)).X, "beside the anchor");

        await OpenAsync($"{PlacementUrl}?pos=middle&h=500");
        var vertical = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(vertical).ToHaveAttributeAsync("data-bit-cal-pos", "below");
    }

    [TestMethod]
    [DataRow("Start")]
    [DataRow("Center")]
    [DataRow("End")]
    [DataRow("Left")]
    [DataRow("Right")]
    public async Task Alignment_LinesACalloutBelowTheAnchorUpAcrossIt(string align)
    {
        await OpenAsync($"{PlacementUrl}?side=Bottom&align={align}&aw=300&w=100");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-align", align.ToLowerInvariant());
        var anchor = await BoxAsync(AnchorOf("pl-trigger"));
        var box = await BoxAsync(callout);

        switch (align)
        {
            case "Start" or "Left": Near(anchor.X, box.X, "left edges"); break;
            case "Center": Near(anchor.X + anchor.Width / 2, box.X + box.Width / 2, "centers"); break;
            default: Near(anchor.X + anchor.Width, box.X + box.Width, "right edges"); break;
        }
    }

    [TestMethod]
    [DataRow("Top")]
    [DataRow("Bottom")]
    [DataRow("Center")]
    public async Task Alignment_LinesACalloutBesideTheAnchorUpAlongIt(string align)
    {
        await OpenAsync($"{PlacementUrl}?side=End&align={align}&h=120");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        var anchor = await BoxAsync(AnchorOf("pl-trigger"));
        var box = await BoxAsync(callout);

        switch (align)
        {
            case "Top": Near(anchor.Y, box.Y, "top edges"); break;
            case "Center": Near(anchor.Y + anchor.Height / 2, box.Y + box.Height / 2, "centers"); break;
            default: Near(anchor.Y + anchor.Height, box.Y + box.Height, "bottom edges"); break;
        }
    }

    [TestMethod]
    public async Task AlignmentOffset_SlidesTheCalloutInwardsFromTheEdgeItIsAlignedWith()
    {
        await OpenAsync($"{PlacementUrl}?side=Bottom&align=Start&offset=20&aw=300&w=100");
        var start = await OpenCalloutAsync("pl-trigger", "pl-body");
        Near((await BoxAsync(AnchorOf("pl-trigger"))).X + 20, (await BoxAsync(start)).X, "start + 20");

        await OpenAsync($"{PlacementUrl}?side=Bottom&align=End&offset=20&aw=300&w=100");
        var end = await OpenCalloutAsync("pl-trigger", "pl-body");
        var anchor = await BoxAsync(AnchorOf("pl-trigger"));
        var box = await BoxAsync(end);
        Near(anchor.X + anchor.Width - 20, box.X + box.Width, "end - 20");
    }

    [TestMethod]
    [DataRow("Bottom", "below")]
    [DataRow("Top", "above")]
    [DataRow("End", "right")]
    [DataRow("Start", "left")]
    public async Task Gap_SpacesTheCalloutFromTheAnchorOnEverySide(string side, string expected)
    {
        await OpenAsync($"{PlacementUrl}?side={side}&gap=16");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        AssertSide(expected, await BoxAsync(AnchorOf("pl-trigger")), await BoxAsync(callout), 17);
    }

    [TestMethod]
    public async Task CollisionPadding_KeepsTheCalloutAwayFromTheScreenEdges()
    {
        var width = 1280.0;

        await OpenAsync($"{PlacementUrl}?side=Bottom&pos=near-right&w=300");
        var plain = await OpenCalloutAsync("pl-trigger", "pl-body");
        width = Page.ViewportSize!.Width;
        var plainBox = await BoxAsync(plain);
        Near(width - 3, plainBox.X + plainBox.Width, "slid back onto the screen");

        await OpenAsync($"{PlacementUrl}?side=Bottom&pos=near-right&w=300&padding=24");
        var padded = await OpenCalloutAsync("pl-trigger", "pl-body");
        var paddedBox = await BoxAsync(padded);
        Near(width - 24 - 3, paddedBox.X + paddedBox.Width, "kept 24px off the edge");
    }

    [TestMethod]
    public async Task MaxWindowWidth_LetsTheCalloutHangOffAWindowNarrowerThanIt()
    {
        await OpenAsync($"{PlacementUrl}?side=Bottom&pos=near-right&w=300&maxww=5000&aw=60");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        var box = await BoxAsync(callout);
        var anchor = await BoxAsync(AnchorOf("pl-trigger"));

        Near(anchor.X, box.X, "left on the anchor");
        Assert.IsTrue(box.X + box.Width > Page.ViewportSize!.Width, "The callout was slid back onto the screen.");
    }

    [TestMethod]
    [DataRow("Start", "right")]
    [DataRow("End", "left")]
    [DataRow("Left", "left")]
    [DataRow("Right", "right")]
    public async Task Rtl_StartAndEndFollowTheReadingDirection_LeftAndRightStayPut(string side, string expected)
    {
        await OpenAsync($"{PlacementUrl}?side={side}&rtl=true");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", expected);
        AssertSide(expected, await BoxAsync(AnchorOf("pl-trigger")), await BoxAsync(callout), 1);
    }

    [TestMethod]
    public async Task Rtl_AStartAlignedCalloutBelowTheAnchor_LinesUpWithItsRightEdge()
    {
        await OpenAsync($"{PlacementUrl}?side=Bottom&rtl=true&aw=300&w=100");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        var anchor = await BoxAsync(AnchorOf("pl-trigger"));
        var box = await BoxAsync(callout);
        Near(anchor.X + anchor.Width, box.X + box.Width, "right edges");
    }

    [TestMethod]
    [DataRow("Top", "above")]
    [DataRow("Bottom", "below")]
    [DataRow("End", "right")]
    [DataRow("Start", "left")]
    public async Task ShowArrow_SitsOnTheEdgeFacingTheAnchor_OnEverySide(string side, string expected)
    {
        // A callout wider than its anchor, so the beak is free to center on the anchor rather than kept off a corner.
        await OpenAsync($"{PlacementUrl}?side={side}&arrow=true&gap=8&w=300");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        var arrow = (await PartAsync(callout, "arrow"));
        await Expect(arrow).ToHaveAttributeAsync("data-bit-cal-pos", expected);

        var anchor = await BoxAsync(AnchorOf("pl-trigger"));
        var box = await BoxAsync(callout);
        var beak = await BoxAsync(arrow);
        var beakX = beak.X + beak.Width / 2;
        var beakY = beak.Y + beak.Height / 2;

        switch (expected)
        {
            case "above":
                Near(box.Y + box.Height, beakY, "on the bottom edge");
                Near(anchor.X + anchor.Width / 2, beakX, "centered on the anchor");
                break;
            case "below":
                Near(box.Y, beakY, "on the top edge");
                Near(anchor.X + anchor.Width / 2, beakX, "centered on the anchor");
                break;
            case "right":
                Near(box.X, beakX, "on the left edge");
                Near(anchor.Y + anchor.Height / 2, beakY, "level with the anchor");
                break;
            default:
                Near(box.X + box.Width, beakX, "on the right edge");
                Near(anchor.Y + anchor.Height / 2, beakY, "level with the anchor");
                break;
        }
    }

    [TestMethod]
    public async Task DefaultIsOpen_OpensAndPlacesTheCalloutOnTheFirstRender()
    {
        await OpenAsync($"{PlacementUrl}?open=true");

        var callout = Cal("pl-body");
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);
        await Expect(El("pl")).ToHaveClassAsync(HasClass("bit-clo-opn"));
        AssertSide("below", await BoxAsync(AnchorOf("pl-trigger")), await BoxAsync(callout), 1);
    }

    [TestMethod]
    public async Task AnchorThatGrows_TheOpenCalloutFollowsIt()
    {
        await OpenAsync($"{PlacementUrl}?side=Bottom&align=End&sticky=true");

        var callout = await OpenCalloutAsync("pl-trigger", "pl-body");
        var before = await BoxAsync(AnchorOf("pl-trigger"));
        Near(before.X + before.Width, (await BoxAsync(callout)).X + (await BoxAsync(callout)).Width, "right edges before");

        await El("pl-grow").ClickAsync();

        await PollAsync(async () => (await BoxAsync(AnchorOf("pl-trigger"))).Width, w => w > before.Width + 100, "the anchor did not grow");
        var after = await BoxAsync(AnchorOf("pl-trigger"));
        await PollAsync(async () => { var b = await BoxAsync(callout); return b.X + b.Width; }, r => Math.Abs(r - (after.X + after.Width)) < 1.5, "the callout did not follow its anchor");
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ResponsiveMode_OnAWideScreen_IsAnOrdinaryCallout()
    {
        await OpenAsync("/e2e/callout/responsive?mode=Panel");

        var callout = await OpenCalloutAsync("res-trigger", "res-body");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-clo-res"));
        await Expect((await PartAsync(callout, "arrow"))).ToBeVisibleAsync();
        var anchor = await BoxAsync(AnchorOf("res-trigger"));
        var box = await BoxAsync(callout);
        Near(anchor.Y + anchor.Height + 9, box.Y, "below the anchor");
        Assert.IsTrue(box.Height < Page.ViewportSize!.Height / 2, "A wide screen shows the panel.");
    }

    private async Task<ILocator> OpenCalloutAsync(string triggerId, string bodyId)
    {
        await El(triggerId).ClickAsync();

        var callout = Cal(bodyId);
        await Expect(callout).ToBeVisibleAsync();
        await SettleAsync(callout);

        return callout;
    }

    private async Task CloseByEscapeAsync(ILocator callout)
    {
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
    }

    // The overlay and the arrow are siblings of the callout with ids built from the same component id.
    private async Task<ILocator> PartAsync(ILocator callout, string part)
    {
        var id = (await callout.GetAttributeAsync("id"))!.Replace("-content", $"-{part}");

        return Page.Locator($"[id='{id}']");
    }

    private Task SettleAsync(ILocator callout)
    {
        return PollAsync(() => callout.EvaluateAsync<bool>("el => el.getAnimations({ subtree: false }).length === 0 && getComputedStyle(el).opacity === '1'"), done => done, "the callout never settled");
    }

    private static async Task<LayoutBox> BoxAsync(ILocator locator)
    {
        var box = await locator.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        return new(box[0], box[1], box[2], box[3]);
    }

    private static Task<string> StyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
    }

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance, $"{what}: expected {expected}, got {actual}.");
    }

    private static void AssertSide(string side, LayoutBox anchor, LayoutBox callout, double distance)
    {
        switch (side)
        {
            case "below": Near(anchor.Y + anchor.Height + distance, callout.Y, "top below the anchor"); break;
            case "above": Near(anchor.Y - distance, callout.Y + callout.Height, "bottom above the anchor"); break;
            case "right": Near(anchor.X + anchor.Width + distance, callout.X, "left after the anchor"); break;
            default: Near(anchor.X - distance, callout.X + callout.Width, "right before the anchor"); break;
        }
    }

    private void AssertWithinViewport(LayoutBox box)
    {
        var viewport = Page.ViewportSize!;
        Assert.IsTrue(box.X >= -0.5 && box.Y >= -0.5 && box.X + box.Width <= viewport.Width + 0.5 && box.Y + box.Height <= viewport.Height + 0.5,
                      $"The callout ({box}) is off the {viewport.Width}x{viewport.Height} screen.");
    }

    private async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> done, string message)
    {
        var started = DateTime.UtcNow;
        T value;
        do
        {
            value = await read();
            if (done(value)) return value;
            await NextFramesAsync();
        } while (DateTime.UtcNow - started < TimeSpan.FromMilliseconds(DefaultTimeout));

        Assert.Fail($"{message} (last value: {value})");
        return value;
    }

    private sealed record LayoutBox(double X, double Y, double Width, double Height);
}
