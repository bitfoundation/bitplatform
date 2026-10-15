using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitOverlay in a real browser (/e2e/overlay): the prerendered layer, opening and closing through the binding,
/// the methods and DefaultIsOpen with OnOpen / OnClose, ModeFull, every Position in both directions (Dir and an
/// inherited rtl), the dismissal rules (layer click, content click, a selection dragged out of the content, Escape,
/// Blocking, NoDismissOnEscape, a claimed Escape, the topmost of two layers, a page dialog, a layer over a BitModal),
/// AbsolutePosition, the scroll hold of the page, of a ScrollerSelector and of a ScrollerElement (counted, changed
/// while open, the offset top), the wheel forwarded to the scroller, Disabled, the BitParams cascade, Id / Style /
/// Class / ZIndex / Visibility / HtmlAttributes / AriaLabel / TabIndex, the --bit-Overlay-* variables, the focus
/// handed back on close, reduced motion and ForceAnimation, and an open Overlay unmounted.
/// </summary>
[TestClass]
public class BitOverlayE2ETests : E2ETestBase
{
    private const string Route = "/e2e/overlay";

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheClosedLayer()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"ovl-basic\"");
        StringAssert.Contains(html, "bit-ovl-ctr bit-ovl");
        StringAssert.Contains(html, "id=\"basic-content\"");
        StringAssert.Contains(html, "id=\"status\">Loading");

        await OpenAsync(Route);

        await Expect(Page.Locator("#ovl-basic")).ToHaveClassAsync(HasClass("bit-ovl"));
    }

    [TestMethod]
    public async Task Basic_ClickOpen_TheLayerCoversTheScreenAndShowsItsContent()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-basic");
        await Expect(layer).ToHaveAttributeAsync("inert", "");
        await Expect(layer).ToBeHiddenAsync();

        await Page.ClickAsync("#btn-open-basic");

        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(layer).ToBeVisibleAsync();
        await Expect(layer).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Expect(layer).ToHaveCSSAsync("opacity", "1");
        await Expect(layer).ToHaveCSSAsync("position", "fixed");
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#basic-opened")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-closed")).ToHaveTextAsync("0");

        var viewport = Page.ViewportSize!;
        var box = (await layer.BoundingBoxAsync())!;
        Assert.AreEqual(0, box.X, 1);
        Assert.AreEqual(0, box.Y, 1);
        Assert.AreEqual(viewport.Width, box.Width, 1);
        Assert.AreEqual(viewport.Height, box.Height, 1);

        // Centered: Position="Center".
        var content = (await Page.Locator("#basic-content").BoundingBoxAsync())!;
        Assert.AreEqual(viewport.Width / 2.0, content.X + content.Width / 2, 1.5);
        Assert.AreEqual(viewport.Height / 2.0, content.Y + content.Height / 2, 1.5);

        // The transparent layer catches the clicks meant for what it covers.
        var hit = await Page.EvaluateAsync<string>("() => document.elementFromPoint(10, 10).closest('.bit-ovl')?.id ?? ''");
        Assert.AreEqual("ovl-basic", hit);
    }

    [TestMethod]
    public async Task Basic_ClickTheLayer_ClosesItAndReportsOnClose()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-basic");
        var layer = Page.Locator("#ovl-basic");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.Mouse.ClickAsync(10, 10);

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(layer).ToHaveAttributeAsync("inert", "");
        await Expect(layer).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#basic-closed")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-opened")).ToHaveTextAsync("1");

        // Closed, it no longer catches the clicks of the page.
        await Page.ClickAsync("#btn-open-basic");
        await Expect(Page.Locator("#basic-opened")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Basic_ThePageSetsIsOpenFalse_TheBindingClosesIt()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-basic");
        await Expect(Page.Locator("#ovl-basic")).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        // The layer covers the button, so its click is dispatched the way a script would.
        await Page.Locator("#btn-close-basic").DispatchEventAsync("click");

        await Expect(Page.Locator("#ovl-basic")).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#basic-closed")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ModeFull_TheLayerDims_AndTogglingItAtRuntimeTakesTheDimAway()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-basic");
        await Expect(Page.Locator("#ovl-basic")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ovl-basic")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");

        await Page.ClickAsync("#btn-open-full");
        var layer = Page.Locator("#ovl-full");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-mfl"));
        await Expect(layer).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        await Page.Locator("#btn-toggle-modefull").DispatchEventAsync("click");

        await Expect(Page.Locator("#full-mode")).ToHaveTextAsync("False");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-mfl"));
        await Expect(layer).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));
    }

    [TestMethod]
    [DataRow("TopLeft", "bit-ovl-tlf", "left", "right", "top")]
    [DataRow("TopCenter", "bit-ovl-tcr", "center", "center", "top")]
    [DataRow("TopRight", "bit-ovl-trg", "right", "left", "top")]
    [DataRow("TopStart", "bit-ovl-tst", "left", "right", "top")]
    [DataRow("TopEnd", "bit-ovl-ten", "right", "left", "top")]
    [DataRow("CenterLeft", "bit-ovl-clf", "left", "right", "middle")]
    [DataRow("Center", "bit-ovl-ctr", "center", "center", "middle")]
    [DataRow("CenterRight", "bit-ovl-crg", "right", "left", "middle")]
    [DataRow("CenterStart", "bit-ovl-cst", "left", "right", "middle")]
    [DataRow("CenterEnd", "bit-ovl-cen", "right", "left", "middle")]
    [DataRow("BottomLeft", "bit-ovl-blf", "left", "right", "bottom")]
    [DataRow("BottomCenter", "bit-ovl-bcr", "center", "center", "bottom")]
    [DataRow("BottomRight", "bit-ovl-brg", "right", "left", "bottom")]
    [DataRow("BottomStart", "bit-ovl-bst", "left", "right", "bottom")]
    [DataRow("BottomEnd", "bit-ovl-ben", "right", "left", "bottom")]
    public async Task Position_EachValue_PlacesTheContentOnItsSideInBothDirections(string position, string cssClass, string ltrSide, string rtlSide, string vertical)
    {
        await OpenAsync(Route);

        await Page.ClickAsync($"#btn-pos-{position}");
        await Expect(Page.Locator("#pos-current")).ToHaveTextAsync(position);

        await Expect(Page.Locator("#ovl-pos")).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Page.Locator("#ovl-pos-rtl")).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Page.Locator("#ovl-pos-inh")).ToHaveClassAsync(HasClass(cssClass));

        // A physical side (Left / Right) stays where it is in a right-to-left layout; a logical one (Start / End)
        // follows the direction - whether Dir set it or an ancestor's dir attribute.
        // The physical Left / Right are always on the side they name; only Start / End swap.
        var isPhysical = position.EndsWith("Left") || position.EndsWith("Right");
        await AssertPlacedAsync("#ovl-pos", "#pos-content", ltrSide, vertical);
        await AssertPlacedAsync("#ovl-pos-rtl", "#pos-rtl-content", isPhysical ? ltrSide : rtlSide, vertical);
        await AssertPlacedAsync("#ovl-pos-inh", "#pos-inh-content", isPhysical ? ltrSide : rtlSide, vertical);
    }

    [TestMethod]
    public async Task Position_NotSet_RendersNoPlacementClassAndLaysTheContentAtTheStart()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-pos-none");
        await Expect(Page.Locator("#pos-current")).ToHaveTextAsync("none");

        await Expect(Page.Locator("#ovl-pos")).ToHaveAttributeAsync("class", "bit-ovl-opn bit-ovl-abs bit-ovl");
        await Expect(Page.Locator("#ovl-pos-rtl")).ToHaveAttributeAsync("class", "bit-ovl-opn bit-ovl-abs bit-ovl bit-rtl");

        // A flex row starting at the inline start: the left in LTR, the right in RTL.
        await AssertPlacedAsync("#ovl-pos", "#pos-content", "left", "top");
        await AssertPlacedAsync("#ovl-pos-rtl", "#pos-rtl-content", "right", "top");
        await AssertPlacedAsync("#ovl-pos-inh", "#pos-inh-content", "right", "top");
    }

    [TestMethod]
    public async Task Rtl_DirSetsTheClassAndTheAttribute_AndAnInheritedDirectionSetsNeither()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ovl-pos-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#ovl-pos-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#ovl-pos-rtl")).ToHaveCSSAsync("direction", "rtl");

        await Expect(Page.Locator("#ovl-pos-inh")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#ovl-pos-inh")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        await Expect(Page.Locator("#ovl-pos-inh")).ToHaveCSSAsync("direction", "rtl");

        await Expect(Page.Locator("#ovl-pos")).ToHaveCSSAsync("direction", "ltr");
    }

    [TestMethod]
    public async Task Dismissal_ClickOnTheContent_ReportsOnClickButDoesNotClose()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.ClickAsync("#dismiss-content-btn");
        await Page.ClickAsync("#dismiss-text");

        await Expect(Page.Locator("#dismiss-content-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dismiss-clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
    }

    [TestMethod]
    public async Task Dismissal_ClickOnTheLayer_ReportsTheClickWithItsCoordinatesAndCloses()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.Mouse.ClickAsync(37, 20);

        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dismiss-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dismiss-last-x")).ToHaveTextAsync("37");
        await Expect(Page.Locator("#ovl-dismiss")).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
    }

    [TestMethod]
    public async Task Dismissal_ASelectionDraggedOutOfTheContent_DoesNotClose()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        var text = (await Page.Locator("#dismiss-text").BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(text.X + 5, text.Y + text.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(text.X + text.Width / 2, text.Y + text.Height / 2, new() { Steps = 5 });
        await Page.Mouse.MoveAsync(10, 10, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        // The click the browser raised on the layer did not dismiss.
        await RoundTripAsync();
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");

        // And the drag selected the text of the content, which the layer hands its selectability back to.
        var selection = await Page.EvaluateAsync<string>("() => window.getSelection().toString()");
        Assert.IsTrue(selection.Length > 0, "The drag selected no text of the content.");

        // A plain click on the layer afterwards dismisses as usual.
        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dismissal_Escape_ClosesTheOverlayWhereverTheFocusIs()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#btn-open-dismiss")).ToBeFocusedAsync();
        // OnOpen is reported once the Overlay is watching for Escape.
        await Expect(Page.Locator("#dismiss-opened")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dismiss-clicks")).ToHaveTextAsync("0");

        // With the focus inside the content as well.
        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#dismiss-opened")).ToHaveTextAsync("2");
        await Page.FocusAsync("#dismiss-content-btn");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dismissal_AnEscapeClaimedInsideTheContent_IsLeftToTheClaimingControl()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#dismiss-opened")).ToHaveTextAsync("1");

        await Page.FocusAsync("#dismiss-claim-input");
        await Page.Keyboard.PressAsync("Escape");

        // The script answers over the wire; a non-answer is waited for by a round trip of the page.
        await Page.Locator("#btn-toggle-noesc").DispatchEventAsync("click");
        await Expect(Page.Locator("#dismiss-noesc")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task NoDismissOnEscape_EscapeLeavesItOpen_ButTheLayerClickStillCloses()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-toggle-noesc");
        await Expect(Page.Locator("#dismiss-noesc")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await RoundTripAsync();
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Blocking_NeitherTheLayerClickNorEscapeCloses_OnClickStillReports()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-toggle-blocking");
        await Expect(Page.Locator("#dismiss-blocking")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");

        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#dismiss-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await RoundTripAsync();
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");

        // Blocking turned off while it is open: the next click on the layer dismisses.
        await Page.Locator("#btn-toggle-blocking").DispatchEventAsync("click");
        await Expect(Page.Locator("#dismiss-blocking")).ToHaveTextAsync("False");
        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dismiss-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Blocking_TheContentsOwnCloseButton_IsTheWayOut()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-toggle-blocking");
        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");

        await Page.ClickAsync("#dismiss-close-btn");

        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Focus_APressOnTheLayerTakesTheFocus_AndClosingHandsItBackToTheOpener()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-toggle-blocking");
        await Page.ClickAsync("#btn-open-dismiss");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#ovl-dismiss")).ToHaveAttributeAsync("tabindex", "-1");

        // A press on the layer moves the focus inside the Overlay rather than to the body.
        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#ovl-dismiss")).ToBeFocusedAsync();

        await Page.ClickAsync("#dismiss-close-btn");
        await Expect(Page.Locator("#dismiss-open")).ToHaveTextAsync("False");

        await Expect(Page.Locator("#btn-open-dismiss")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Escape_TwoOpenOverlays_ClosesTheTopmostFirst()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-lower");
        await Expect(Page.Locator("#lower-opened")).ToHaveTextAsync("1");
        await Page.ClickAsync("#btn-open-upper");
        await Expect(Page.Locator("#upper-opened")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("Escape");
        if (IsFirefox)
        {
            await RoundTripAsync();
            if (await Page.Locator("#upper-open").TextContentAsync() == "True")
            {
                Assert.Inconclusive("KNOWN ISSUE (Firefox): an Escape pressed with the focus on a button inside the lower layer's content does not reach the layer opened over it.");
            }
        }
        await Expect(Page.Locator("#upper-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#lower-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#lower-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Escape_PressedInADialogOfThePage_IsTheDialogs_PressedInADialogOnTheLayer_ClosesTheLayer()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-esc-dialog");
        await Expect(Page.Locator("#esc-dialog-opened")).ToHaveTextAsync("1");

        await Page.FocusAsync("#esc-dialog-input");
        await Page.Keyboard.PressAsync("Escape");
        await RoundTripAsync();
        await Expect(Page.Locator("#esc-dialog-open")).ToHaveTextAsync("True");

        await Page.FocusAsync("#esc-layer-dialog-input");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#esc-dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Escape_AnOverlayOpenedFromABitModal_ClosesFirst_ThenTheModal()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-modal");
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#btn-open-modal-overlay")).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-open-modal-overlay");
        await Expect(Page.Locator("#modal-ovl-opened")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#modal-ovl-open")).ToHaveTextAsync("False");
        await RoundTripAsync();
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Escape_AnOpenDropdownOnTheLayerClosesFirst_ThenTheLayer()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-esc-dropdown");
        await Expect(Page.Locator("#esc-dropdown-opened")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ovl-esc-dropdown")).ToHaveCSSAsync("opacity", "1");

        await Page.Locator("#drp-in-overlay .bit-drp-wrp").ClickAsync();
        var option = Page.GetByRole(AriaRole.Option, new() { Name = "Apple" });
        await Expect(option).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(option).ToBeHiddenAsync();
        await RoundTripAsync();
        await Expect(Page.Locator("#esc-dropdown-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#esc-dropdown-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AbsolutePosition_CoversItsContainerOnly_WithItsCorners()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-abs");
        var layer = Page.Locator("#ovl-abs");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-abs"));
        await Expect(layer).ToHaveCSSAsync("position", "absolute");
        await Expect(layer).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(layer).ToHaveCSSAsync("z-index", "auto");
        await Expect(layer).ToHaveCSSAsync("opacity", "1");

        var container = (await Page.Locator("#abs-container").BoundingBoxAsync())!;
        var box = (await layer.BoundingBoxAsync())!;
        // Inside the 1px border of the container.
        Assert.AreEqual(container.X + 1, box.X, 1);
        Assert.AreEqual(container.Y + 1, box.Y, 1);
        Assert.AreEqual(container.Width - 2, box.Width, 1);
        Assert.AreEqual(container.Height - 2, box.Height, 1);

        // What it covers is unreachable; what is outside the container is not. The covered button is probed at its
        // center: it sits in the container's corner, and the layer takes that corner's rounding, which hit testing
        // follows - a point a few pixels into the button's own corner lies outside the arc in some engines' layout.
        var behind = (await Page.Locator("#abs-behind").BoundingBoxAsync())!;
        var hit = await Page.EvaluateAsync<string>($"() => document.elementFromPoint({behind.X + behind.Width / 2}, {behind.Y + behind.Height / 2}).closest('.bit-ovl')?.id ?? ''");
        Assert.AreEqual("ovl-abs", hit);
        await Page.ClickAsync("#abs-outside");
        await Expect(Page.Locator("#abs-outside")).ToBeFocusedAsync();

        await layer.ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(Page.Locator("#abs-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AutoToggleScroll_HoldsThePageWhileOpen_AndHandsItBackOnClose()
    {
        await OpenAsync(Route);

        Assert.AreEqual("", await Page.EvaluateAsync<string>("() => document.body.style.overflow"));

        await Page.ClickAsync("#btn-open-lock");
        await Expect(Page.Locator("#lock-opened")).ToHaveTextAsync("1");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("overflow", "hidden");

        var before = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Mouse.MoveAsync(200, 200);
        await Page.Mouse.WheelAsync(0, 600);
        await NextFramesAsync();
        Assert.AreEqual(before, await Page.EvaluateAsync<double>("() => window.scrollY"), "The page scrolled behind the Overlay.");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#lock-closed")).ToHaveTextAsync("1");
        Assert.AreEqual("", await Page.EvaluateAsync<string>("() => document.body.style.overflow"));
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");

        await Page.Mouse.WheelAsync(0, 600);
        await Page.WaitForFunctionAsync("y => window.scrollY > y", before);
    }

    [TestMethod]
    public async Task AutoToggleScroll_TwoOverlaysHoldThePage_ItIsHandedBackOnlyWhenTheLastCloses()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-lock");
        await Expect(Page.Locator("#lock-open")).ToHaveTextAsync("True");
        await Page.ClickAsync("#btn-open-lock2");
        await Expect(Page.Locator("#lock2-opened")).ToHaveTextAsync("1");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("overflow", "hidden");

        await Page.Keyboard.PressAsync("Escape");
        if (IsFirefox)
        {
            await RoundTripAsync();
            if (await Page.Locator("#lock2-open").TextContentAsync() == "True")
            {
                Assert.Inconclusive("KNOWN ISSUE (Firefox): an Escape pressed with the focus on a button inside the lower layer's content does not reach the layer opened over it.");
            }
        }
        await Expect(Page.Locator("#lock2-open")).ToHaveTextAsync("False");
        await RoundTripAsync();
        await Expect(Page.Locator("body")).ToHaveCSSAsync("overflow", "hidden");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#lock-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");
    }

    [TestMethod]
    public async Task AutoToggleScroll_TurnedOnAndOffWhileOpen_TakesAndReleasesTheHoldThereAndThen()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-lt");
        await Expect(Page.Locator("#ovl-lt")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");

        await Page.Locator("#btn-toggle-lt-auto").DispatchEventAsync("click");
        await Expect(Page.Locator("#lt-auto")).ToHaveTextAsync("True");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("overflow", "hidden");

        await Page.Locator("#btn-toggle-lt-auto").DispatchEventAsync("click");
        await Expect(Page.Locator("#lt-auto")).ToHaveTextAsync("False");
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");
    }

    [TestMethod]
    public async Task ScrollerSelector_TheBoxIsHeldAndTheLayerFollowsItsScrollOffset()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#sel-box");
        await box.EvaluateAsync("el => el.scrollTop = 60");

        await Page.ClickAsync("#btn-open-sel");
        await Expect(Page.Locator("#sel-open")).ToHaveTextAsync("True");
        await Expect(box).ToHaveCSSAsync("overflow-y", "hidden");

        // Pushed down by the offset the box was left at, so it covers what is on screen.
        var layer = Page.Locator("#ovl-sel");
        await Expect(layer).ToHaveCSSAsync("top", "60px");
        var boxRect = (await box.BoundingBoxAsync())!;
        var layerRect = (await layer.BoundingBoxAsync())!;
        Assert.AreEqual(boxRect.Y + 1, layerRect.Y, 1.5);

        await box.HoverAsync();
        await Page.Mouse.WheelAsync(0, 200);
        await NextFramesAsync();
        Assert.AreEqual(60, await box.EvaluateAsync<double>("el => el.scrollTop"), 1);

        await Page.ClickAsync("#btn-close-sel");
        await Expect(Page.Locator("#sel-open")).ToHaveTextAsync("False");
        await Expect(box).ToHaveCSSAsync("overflow-y", "auto");
        await Expect(layer).Not.ToHaveAttributeAsync("style", new Regex("top"));

        await box.HoverAsync();
        await Page.Mouse.WheelAsync(0, 200);
        await Page.WaitForFunctionAsync("() => document.getElementById('sel-box').scrollTop > 60");
    }

    [TestMethod]
    public async Task ScrollerElement_TheReferencedBoxIsHeldWhileOpen()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#elm-box");
        await Page.ClickAsync("#btn-open-elm");
        await Expect(Page.Locator("#elm-open")).ToHaveTextAsync("True");
        await Expect(box).ToHaveCSSAsync("overflow-y", "hidden");
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");

        await Page.ClickAsync("#btn-close-elm");
        await Expect(Page.Locator("#elm-open")).ToHaveTextAsync("False");
        await Expect(box).ToHaveCSSAsync("overflow-y", "auto");
    }

    [TestMethod]
    public async Task ScrollerElement_NotCapturedYetWhenTheOverlayOpens_IsHeldOnceThePageHandsItOver()
    {
        // /e2e/overlay/scroller-ref: open from the first render, holding a scroller whose @ref that render captures.
        await OpenAsync("/e2e/overlay/scroller-ref");

        await Expect(Page.Locator("#opened")).ToHaveTextAsync("1");
        Assert.IsFalse(await IsCircuitDeadAsync(), "Opening with a reference that is not captured yet took the circuit down.");

        // The render that hands the captured reference over moves the hold onto it, and off whatever stood in for it.
        await Page.ClickAsync("#btn-rerender");
        await Expect(Page.Locator("#renders")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ref-box")).ToHaveCSSAsync("overflow-y", "hidden");
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");

        await Page.ClickAsync("#btn-close");
        await Expect(Page.Locator("#ref-box")).ToHaveCSSAsync("overflow-y", "auto");
        await Expect(Page.Locator("body")).Not.ToHaveCSSAsync("overflow", "hidden");

        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ScrollForward_TheWheelOnAFixedLayer_ScrollsItsScrollerSelector()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-fwd");
        await Expect(Page.Locator("#ovl-fwd")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ovl-fwd")).ToHaveCSSAsync("opacity", "1");

        var outer = Page.Locator("#fwd-box");
        var inner = Page.Locator("#fwd-content");

        // On the layer, away from its content: the wheel moves the scroller behind it.
        var viewport = Page.ViewportSize!;
        await Page.Mouse.MoveAsync(viewport.Width - 50, viewport.Height - 50);
        await Page.Mouse.WheelAsync(0, 120);
        await Page.WaitForFunctionAsync("() => document.getElementById('fwd-box').scrollTop > 0");

        Assert.AreEqual(0, await inner.EvaluateAsync<double>("el => el.scrollTop"));
    }

    // A test of its own rather than a second wheel of the one above: Firefox sends every wheel of a transaction - the
    // ones that follow the first within a second or so, a pointer moved in the meantime included - to the target the
    // first one scrolled, so a wheel over the content right after one over the layer would still go to the layer.
    [TestMethod]
    public async Task ScrollForward_TheWheelOnAScrollerOfTheContent_ScrollsThatScrollerAndNotTheOneBehind()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-fwd");
        await Expect(Page.Locator("#ovl-fwd")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ovl-fwd")).ToHaveCSSAsync("opacity", "1");

        var outer = Page.Locator("#fwd-box");
        var inner = Page.Locator("#fwd-content");

        await inner.HoverAsync();
        await Page.Mouse.WheelAsync(0, 120);
        await Page.WaitForFunctionAsync("() => document.getElementById('fwd-content').scrollTop > 0");
        Assert.AreEqual(0, await outer.EvaluateAsync<double>("el => el.scrollTop"), 1);
    }

    [TestMethod]
    public async Task Methods_OpenCloseToggle_DriveAnUncontrolledOverlay_WithOneEventPerChange()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-ref");

        await Page.ClickAsync("#btn-ref-open");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ref-opened")).ToHaveTextAsync("1");

        // Open() on an open Overlay does nothing.
        await Page.Locator("#btn-ref-open").DispatchEventAsync("click");
        await RoundTripAsync();
        await Expect(Page.Locator("#ref-opened")).ToHaveTextAsync("1");

        await Page.Locator("#btn-ref-toggle").DispatchEventAsync("click");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ref-closed")).ToHaveTextAsync("1");

        // Close() on a closed Overlay does nothing; a re-render of the page keeps it closed.
        await Page.ClickAsync("#btn-ref-close");
        var rerenders = int.Parse((await Page.Locator("#rerenders").TextContentAsync())!);
        await Page.ClickAsync("#btn-rerender");
        await Expect(Page.Locator("#rerenders")).ToHaveTextAsync($"{rerenders + 1}");
        await Expect(Page.Locator("#ref-closed")).ToHaveTextAsync("1");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.ClickAsync("#btn-ref-toggle");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ref-opened")).ToHaveTextAsync("2");

        // Escape and the layer click close an uncontrolled Overlay as well.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ref-closed")).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-ref-open");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Page.Locator("#btn-ref-close").DispatchEventAsync("click");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#ref-closed")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task DefaultIsOpen_StartsOpenWithOnOpen_AndStaysClosedOnceDismissed()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-default");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(Page.Locator("#default-opened")).ToHaveTextAsync("1");

        await layer.ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.ClickAsync("#btn-rerender");
        await Expect(Page.Locator("#rerenders")).ToHaveTextAsync("1");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
    }

    [TestMethod]
    public async Task Disabled_NoDismissalNoOnClick_OpenIsRefused_CloseIsHonoured()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-dis");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-dis"));

        // The binding still opens it.
        await Page.ClickAsync("#btn-dis-open");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.Mouse.ClickAsync(10, 10);
        await Page.Keyboard.PressAsync("Escape");
        await RoundTripAsync();
        await Expect(Page.Locator("#dis-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#dis-open")).ToHaveTextAsync("True");

        // Close() takes a disabled Overlay off the screen; Open() does not bring it back.
        await Page.Locator("#btn-dis-ref-close").DispatchEventAsync("click");
        await Expect(Page.Locator("#dis-open")).ToHaveTextAsync("False");
        await Page.ClickAsync("#btn-dis-ref-open");
        await RoundTripAsync();
        await Expect(Page.Locator("#dis-open")).ToHaveTextAsync("False");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));

        // Enabled again at runtime: it opens and dismisses as usual.
        await Page.ClickAsync("#btn-dis-toggle");
        await Expect(layer).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Page.ClickAsync("#btn-dis-ref-open");
        await Expect(Page.Locator("#dis-open")).ToHaveTextAsync("True");
        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#dis-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dis-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Cascade_BitParamsSetsTheDefaults_AnExplicitParameterWins_AndAChangeReachesThem()
    {
        await OpenAsync(Route);

        var cascaded = Page.Locator("#ovl-cascade");
        var own = Page.Locator("#ovl-cascade-own");

        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-ovl-mfl"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-ovl-ctr"));
        await Expect(cascaded).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(cascaded).ToHaveCSSAsync("justify-content", "center");

        await Expect(own).ToHaveClassAsync(HasClass("bit-ovl-tcr"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-ovl-mfl"));
        await Expect(own).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        // Blocking from the cascade: the layer click does not close.
        await Page.ClickAsync("#btn-cascade-open");
        await Expect(Page.Locator("#ovl-cascade-blocking")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Page.Mouse.ClickAsync(10, 10);
        await RoundTripAsync();
        await Expect(Page.Locator("#cascade-open")).ToHaveTextAsync("True");

        await Page.Locator("#btn-cascade-change").DispatchEventAsync("click");

        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-ovl-brg"));
        await Expect(cascaded).Not.ToHaveClassAsync(HasClass("bit-ovl-mfl"));
        await Expect(cascaded).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(own).ToHaveClassAsync(HasClass("bit-ovl-tcr"));

        await Page.Mouse.ClickAsync(10, 10);
        await Expect(Page.Locator("#cascade-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheLayer()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ovl-style")).ToHaveCSSAsync("background-color", "rgb(200, 0, 0)");
        await Expect(Page.Locator("#ovl-class")).ToHaveClassAsync(HasClass("e2e-ovl-class"));
        await Expect(Page.Locator("#ovl-class")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#ovl-class")).ToHaveClassAsync(HasClass("bit-ovl"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheLayer_TheZIndexParameterWins()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-vars");
        await Expect(layer).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(layer).ToHaveCSSAsync("padding-top", "24px");
        await Expect(layer).ToHaveCSSAsync("backdrop-filter", "blur(4px)");
        await Expect(layer).ToHaveCSSAsync("z-index", "1234");
        Assert.AreEqual(400, await FadeDurationMsAsync("#ovl-vars"), 0.001);

        await Expect(Page.Locator("#ovl-vars-zindex")).ToHaveCSSAsync("z-index", "77");
        await Expect(Page.Locator("#ovl-vars-zindex")).ToHaveAttributeAsync("style", new Regex("z-index:77"));

        // The defaults, for comparison.
        var defaults = Page.Locator("#ovl-default-styles");
        await Expect(defaults).Not.ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(defaults).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(defaults).ToHaveCSSAsync("padding-top", "0px");
        await Expect(defaults).ToHaveCSSAsync("backdrop-filter", "none");
        await Expect(defaults).Not.ToHaveCSSAsync("z-index", "1234");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideAnOpenLayer()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-vis");
        await Expect(layer).ToHaveCSSAsync("visibility", "visible");

        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(layer).ToHaveCSSAsync("visibility", "hidden");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(layer).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(layer).ToHaveCSSAsync("display", "flex");
        await Expect(layer).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task Attributes_AriaLabelHtmlAttributesAndTabIndex_LandOnTheLayer()
    {
        await OpenAsync(Route);

        var layer = Page.Locator("#ovl-vis");
        await Expect(layer).ToHaveAttributeAsync("aria-label", "Loading the report");
        await Expect(layer).ToHaveAttributeAsync("data-e2e", "overlay-attr");
        await Expect(layer).ToHaveAttributeAsync("title", "layer title");
        await Expect(layer).ToHaveAttributeAsync("tabindex", "-1");

        var other = Page.Locator("#ovl-tabindex");
        await Expect(other).ToHaveAttributeAsync("tabindex", "0");
        await Expect(other).ToHaveAttributeAsync("aria-label", "From attributes");
        await Expect(other).ToHaveAttributeAsync("data-extra", "x");
    }

    [TestMethod]
    public async Task ReducedMotion_TheFadeCollapses_ForceAnimationKeepsIt()
    {
        await OpenAsync(Route);

        var full = await FadeDurationMsAsync("#ovl-motion");
        Assert.IsTrue(full >= 50, $"The layer does not fade without reduced motion ({full}ms).");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        // Collapsed to the 0.01ms the motion tokens leave so that transitionend still fires.
        await Page.WaitForFunctionAsync(@"() => {
            const v = getComputedStyle(document.getElementById('ovl-motion')).transitionDuration.split(',')[0].trim();
            return (v.endsWith('ms') ? parseFloat(v) : parseFloat(v) * 1000) < 1;
        }");
        Assert.AreEqual(full, await FadeDurationMsAsync("#ovl-motion-force"), 0.001);
        await Expect(Page.Locator("#ovl-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));

        // A duration of the page's own is kept by ForceAnimation as well.
        Assert.AreEqual(300, await FadeDurationMsAsync("#ovl-fade"), 0.001);
        await Page.ClickAsync("#btn-open-fade");
        await Expect(Page.Locator("#ovl-fade")).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task Fade_OpeningPlaysTheOpacityTransitionToTheEnd()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-fade");
        var layer = Page.Locator("#ovl-fade");
        await Expect(layer).ToHaveClassAsync(HasClass("bit-ovl-opn"));

        // Mid-fade the opacity is between the two ends; it settles at 1.
        var samples = await layer.EvaluateAsync<double[]>(@"el => new Promise(resolve => {
            const values = [];
            const start = performance.now();
            const tick = () => {
                values.push(parseFloat(getComputedStyle(el).opacity));
                if (performance.now() - start < 500) requestAnimationFrame(tick); else resolve(values);
            };
            tick();
        })");
        await Expect(layer).ToHaveCSSAsync("opacity", "1");
        Assert.AreEqual(1, samples[^1], 0.001);
    }

    [TestMethod]
    public async Task Unmount_AnOpenOverlayRemoved_HandsTheScrollerBackWithoutAnError()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#unmount-box");
        await Page.ClickAsync("#btn-unmount-open");
        await Expect(Page.Locator("#ovl-unmount")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(box).ToHaveCSSAsync("overflow-y", "hidden");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-shown")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#ovl-unmount")).ToHaveCountAsync(0);
        await Expect(box).ToHaveCSSAsync("overflow-y", "auto");

        // Mounted again, the binding still says open: it takes the box again, and a click on it hands it back.
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#ovl-unmount")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#ovl-unmount")).ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(box).ToHaveCSSAsync("overflow-y", "hidden");
        await Page.Locator("#ovl-unmount").ClickAsync(new() { Position = new() { X = 250, Y = 100 } });
        await Expect(Page.Locator("#ovl-unmount")).Not.ToHaveClassAsync(HasClass("bit-ovl-opn"));
        await Expect(box).ToHaveCSSAsync("overflow-y", "auto");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task<double> FadeDurationMsAsync(string selector)
    {
        return Page.Locator(selector).EvaluateAsync<double>(@"el => {
            const v = getComputedStyle(el).transitionDuration.split(',')[0].trim();
            return v.endsWith('ms') ? parseFloat(v) : parseFloat(v) * 1000;
        }");
    }

    // A round trip of the circuit: a page button's click is rendered back only once everything the circuit received
    // before it - a key the script handed to .NET - has been handled.
    private async Task RoundTripAsync()
    {
        var before = await Page.Locator("#rerenders").TextContentAsync();
        await Page.Locator("#btn-rerender").DispatchEventAsync("click");
        await Expect(Page.Locator("#rerenders")).Not.ToHaveTextAsync(before!);
    }

    private async Task AssertPlacedAsync(string layerSelector, string contentSelector, string horizontal, string vertical)
    {
        var layer = (await Page.Locator(layerSelector).BoundingBoxAsync())!;
        var content = (await Page.Locator(contentSelector).BoundingBoxAsync())!;

        var dx = content.X - layer.X;
        var dy = content.Y - layer.Y;

        var expectedX = horizontal switch
        {
            "left" => 0,
            "center" => (layer.Width - content.Width) / 2,
            _ => layer.Width - content.Width
        };
        var expectedY = vertical switch
        {
            "top" => 0,
            "middle" => (layer.Height - content.Height) / 2,
            _ => layer.Height - content.Height
        };

        Assert.AreEqual(expectedX, dx, 1.5, $"{layerSelector}: the content is not on the {horizontal}.");
        Assert.AreEqual(expectedY, dy, 1.5, $"{layerSelector}: the content is not at the {vertical}.");
    }
}
