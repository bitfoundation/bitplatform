using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

/// <summary>
/// Pins what a bUnit render cannot see of the Panel: the public --bit-Panel-* custom properties, which are read off
/// the stylesheet with a fallback and never declared there so they inherit, and the parts of the layout, the motion
/// and the accessibility that only exist in CSS - the edge each placement slides in from, the safe area it makes
/// room for, the open state that carries no transform, and the close button that paints its states through variables.
/// </summary>
[TestClass]
public class BitPanelStylesheetTests
{
    private static readonly string[] _publicVariables =
    [
        "--bit-Panel-background",
        "--bit-Panel-color",
        "--bit-Panel-shadow",
        "--bit-Panel-radius",
        "--bit-Panel-border-width",
        "--bit-Panel-border-color",
        "--bit-Panel-size",
        "--bit-Panel-max-size",
        "--bit-Panel-z-index",
        "--bit-Panel-overlay-background",
        "--bit-Panel-overlay-backdrop-filter",
        "--bit-Panel-padding",
        "--bit-Panel-header-font-size",
        "--bit-Panel-header-font-weight",
    ];

    [TestMethod]
    public void BitPanelStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitPanelStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        // A public variable declared by the component would stop the value set on an ancestor from inheriting.
        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-Panel-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitPanelStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-Panel-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-Panel-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read, documented);
    }

    // The ZIndex parameter writes the two layers on the root as private variables, the overlay takes the public one
    // and the panel sits one above it; while neither is set the two fall back to the layers of the library.
    [TestMethod]
    public void BitPanelShouldStackThePanelOneLayerAboveItsOverlay()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(GetRule(rules, ""), "--bit-pnl-zin-pub: calc(var(--bit-Panel-z-index) + 1);");
        StringAssert.Contains(GetRule(rules, "ovl"), "z-index: var(--bit-pnl-zin-ovl, var(--bit-Panel-z-index, #{$zindex-overlay}));");
        StringAssert.Contains(GetRule(rules, "cnt"), "z-index: var(--bit-pnl-zin-cnt, var(--bit-pnl-zin-pub, #{$zindex-callout}));");
    }

    [TestMethod]
    public void BitPanelOpenStateShouldCarryNoTransform()
    {
        var open = GetRule(GetRules(ReadStylesheet()), "opn");

        // Any transform - an identity one included - makes the open panel the containing block of its fixed
        // descendants, which lays a panel or a dialog opened from inside it out against the panel. Its visibility is
        // inherited rather than set, so a panel whose Visibility is Hidden stays hidden while it is open.
        StringAssert.Contains(open, "transform: none;");
        StringAssert.Contains(open, "visibility: inherit;");
    }

    // The panel is hidden while it is closed rather than only translated off the screen, which is what keeps its
    // content out of the tab order and out of the accessibility tree, and so is the overlay that fades out with it.
    [TestMethod]
    public void BitPanelClosedPanelAndOverlayShouldBeHidden()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(GetRule(rules, "cnt"), "visibility: hidden;");
        StringAssert.Contains(GetRule(rules, "ovl"), "visibility: hidden;");
        StringAssert.Contains(GetRule(rules, "ovl-opn"), "visibility: inherit;");
    }

    [TestMethod,
        DataRow("start", "translateX(calc(var(--bit-pnl-transform-factor) * -100%))", "width"),
        DataRow("end", "translateX(calc(var(--bit-pnl-transform-factor) * 100%))", "width"),
        DataRow("left", "translateX(-100%)", "width"),
        DataRow("right", "translateX(100%)", "width"),
        DataRow("top", "translateY(-100%)", "height"),
        DataRow("bottom", "translateY(100%)", "height")
    ]
    public void BitPanelEdgesShouldParkThePanelBeyondTheEdgeTheySlideInFrom(string edge, string transform, string axis)
    {
        var rule = GetRule(GetRules(ReadStylesheet()), edge);

        StringAssert.Contains(rule, $"transform: {transform};");

        // The size along the axis the panel slides on is the public variable's, capped so a strip of the page shows.
        StringAssert.Contains(rule, $"{axis}: var(--bit-Panel-size, fit-content);");
        StringAssert.Contains(rule, $"max-{axis}: var(--bit-pnl-max, 85%);");
    }

    // The logical pair follows the direction through the factor, which the physical pair never reads.
    [TestMethod]
    public void BitPanelPhysicalEdgesShouldNotFollowTheDirection()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var edge in new[] { "left", "right", "top", "bottom" })
        {
            Assert.IsFalse(GetRule(rules, edge).Contains("--bit-pnl-transform-factor"), $"{edge} follows the direction, so it no longer stays on the side it is named for.");
        }
    }

    [TestMethod]
    [DataRow("start", "top,bottom,left,right")]
    [DataRow("end", "top,bottom,left,right")]
    [DataRow("left", "top,bottom,left")]
    [DataRow("right", "top,bottom,right")]
    [DataRow("top", "top,left,right")]
    [DataRow("bottom", "bottom,left,right")]
    [DataRow("fsz", "top,right,bottom,left")]
    public void BitPanelEdgesShouldMakeRoomForTheSafeAreaTheyTouch(string edge, string insets)
    {
        var stylesheet = ReadStylesheet();
        var rules = GetRules(stylesheet);
        var rule = GetRule(rules, edge);

        foreach (var inset in insets.Split(','))
        {
            StringAssert.Contains(rule, $"var(--bit-pnl-sa-{inset})", $"{edge} does not make room for the {inset} inset.");

            StringAssert.Contains(GetRule(rules, ""), $"    --bit-pnl-sa-{inset}: env(safe-area-inset-{inset}, 0px);");

            // A panel laid out inside a box of the page touches no device edge.
            StringAssert.Contains(GetRule(rules, "abs"), $"    --bit-pnl-sa-{inset}: 0px;");
        }
    }

    // The inset is taken away through the variables rather than by a padding on the container, which would
    // outrank one the consumer gives the container through Classes.
    [TestMethod]
    public void BitPanelLaidOutInABoxShouldLeaveThePaddingOfTheContainerToTheConsumer()
    {
        StringAssert.DoesNotMatch(ReadStylesheet(), new Regex(@"\.bit-pnl-cnt\s*\{\s*padding"));
    }

    // A contained panel stays out of the way of the clicks its container keeps while the panel is closed, and clips
    // the panel parked beyond its edge rather than hiding or scrolling it.
    [TestMethod]
    public void BitPanelLaidOutInABoxShouldClipAndHandThePointerToItsParts()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "abs");

        StringAssert.Contains(rule, "position: absolute;");
        StringAssert.Contains(rule, "overflow: clip;");
        StringAssert.Contains(rule, "pointer-events: none;");
        StringAssert.Contains(rule, "> .bit-pnl-ovl,\n    > .bit-pnl-cnt {\n        position: absolute;\n        pointer-events: auto;");
    }

    // The direction is read off the layout as well as off the class Dir renders: the one so a panel given no
    // direction of its own on a right-to-left page still slides out towards the edge it came from, the other
    // so an explicit right-to-left still does on an engine without :dir(), which the forgiving :is() keeps the class for.
    [TestMethod]
    public void BitPanelShouldSlideTheWayThePageItIsInReads()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "    &:is(.bit-rtl, :dir(rtl)) {\n        --bit-pnl-transform-factor: -1;\n    }");
    }

    // The full-size panel takes the whole of the screen, which also lifts the cap and leaves no page edge to draw.
    [TestMethod]
    public void BitPanelFullSizeShouldLiftTheCapAndDrawNoEdge()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "fsz");

        StringAssert.Contains(rule, "width: 100%;");
        StringAssert.Contains(rule, "height: 100%;");
        StringAssert.Contains(rule, "max-width: 100%;");
        StringAssert.Contains(rule, "max-height: 100%;");
        StringAssert.Contains(rule, "border: none;");
        StringAssert.Contains(rule, "border-radius: 0;");
    }

    // A panel given a header, a footer or a close button moves the scrolling from the panel to its body, so the
    // parts around the body stay put while the content moves, and neither carries the scroll on into the page.
    [TestMethod]
    public void BitPanelSectionsShouldMoveTheScrollingToTheBody()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(GetRule(rules, "cnt"), "overflow: auto;");
        StringAssert.Contains(GetRule(rules, "cnt"), "overscroll-behavior: contain;");
        StringAssert.Contains(GetRule(rules, "sec"), "overflow: hidden;");
        StringAssert.Contains(GetRule(rules, "sec"), "flex-direction: column;");
        StringAssert.Contains(GetRule(rules, "bdy"), "overflow: hidden auto;");
        StringAssert.Contains(GetRule(rules, "bdy"), "overscroll-behavior: contain;");
        StringAssert.Contains(GetRule(rules, "hcn"), "flex-shrink: 0;");
        StringAssert.Contains(GetRule(rules, "ftr"), "flex-shrink: 0;");
    }

    [TestMethod]
    public void BitPanelSectionsShouldBeInsetByThePaddingVariable()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(GetRule(rules, "hcn"), "padding: var(--bit-Panel-padding, #{$spa-dialog}) var(--bit-Panel-padding, #{$spa-dialog}) 0;");
        StringAssert.Contains(GetRule(rules, "bdy"), "padding: var(--bit-Panel-padding, #{$spa-dialog});");
        StringAssert.Contains(GetRule(rules, "ftr"), "padding: 0 var(--bit-Panel-padding, #{$spa-dialog}) var(--bit-Panel-padding, #{$spa-dialog});");
    }

    [TestMethod]
    public void BitPanelHeaderShouldReadItsTypeOffThePublicVariables()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "hdr");

        StringAssert.Contains(rule, "font-size: var(--bit-Panel-header-font-size, #{$tg-fs-xl});");
        StringAssert.Contains(rule, "font-weight: var(--bit-Panel-header-font-weight, #{$tg-fw-semibold});");
        // A title of one line sits level with the close button beside it.
        StringAssert.Contains(rule, "min-height: $siz-ctrl-md;");
    }

    [TestMethod]
    public void BitPanelCloseButtonShouldShowItsFocusAndPaintItsStatesThroughVariables()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "cls");

        StringAssert.Contains(rule, "&:focus-visible {\n        @include focus-ring;");
        StringAssert.Contains(rule, "border-radius: $shp-radius-button;");
        StringAssert.Contains(rule, "font-size: $siz-icon-md;");
        StringAssert.Contains(rule, "margin-inline-start: auto;");

        // The states move the variables the button is painted from rather than its colors, so a close button the
        // app paints through Classes keeps that paint under the pointer and while it is pressed.
        StringAssert.Contains(rule, "color: var(--bit-pnl-cls-clr, inherit);");
        StringAssert.Contains(rule, "background-color: var(--bit-pnl-cls-bg, transparent);");
        StringAssert.Contains(rule, "&:hover:not(:disabled) {\n            --bit-pnl-cls-clr:");
        StringAssert.Contains(rule, "&:active:not(:disabled) {\n        --bit-pnl-cls-clr:");

        StringAssert.Contains(rule, "&:disabled {\n        cursor: default;\n        opacity: $opa-dis;");
    }

    // The two transitions read the motion tokens, so a preset or reduced motion re-times the slide: the opening
    // decelerates into place and the closing accelerates away, with the visibility switched at the end of the exit.
    [TestMethod]
    public void BitPanelSlideShouldReadTheMotionTokens()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(GetRule(rules, "cnt"), "transition: transform $mot-duration $mot-easing-accelerate,");
        StringAssert.Contains(GetRule(rules, "cnt"), "visibility 0s linear $mot-duration;");
        StringAssert.Contains(GetRule(rules, "opn"), "transition: transform $mot-duration $mot-easing-decelerate,");
        StringAssert.Contains(GetRule(rules, "opn"), "visibility 0s linear 0s;");
        StringAssert.Contains(GetRule(rules, "ovl"), "transition: opacity $mot-duration-short $mot-easing-accelerate, visibility 0s linear $mot-duration-short;");
        StringAssert.Contains(GetRule(rules, "ovl-opn"), "transition: opacity $mot-duration $mot-easing-decelerate, visibility 0s linear 0s;");

        Assert.IsFalse(Regex.IsMatch(rules, @"\b(ease|ease-in|ease-out|ease-in-out)\b|cubic-bezier"), "A transition hard-codes its easing instead of reading the motion token.");
    }

    // Only the overlay of a ModeFull panel dims the page; the plain one is a transparent catcher of clicks.
    [TestMethod]
    public void BitPanelOverlayShouldOnlyBeTintedInFullMode()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(GetRule(rules, "ovl").Contains("background"), "The plain overlay darkens the page it is meant to be read against.");
        StringAssert.Contains(GetRule(rules, "ovl"), "backdrop-filter: var(--bit-Panel-overlay-backdrop-filter, none);");
        StringAssert.Contains(GetRule(rules, "ovl-mfl"), "background-color: var(--bit-Panel-overlay-background, #{$clr-bg-overlay});");
    }

    // A forced-colors mode throws away the shadow and the tint of the overlay, so the rule on the side the panel
    // turns towards the page is drawn in the system text color instead.
    [TestMethod]
    public void BitPanelShouldStayASurfaceInForcedColors()
    {
        var rules = GetRules(ReadStylesheet());

        var forced = Regex.Match(rules, @"@media \(forced-colors: active\) \{.*?\n\}", RegexOptions.Singleline).Value;

        StringAssert.Contains(forced, ".bit-pnl-cnt {");
        StringAssert.Contains(forced, "--bit-pnl-edge: #{$shp-border-width} #{$shp-border-style} CanvasText;");
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n$pnl-private-properties")..];

    /// <summary>
    /// The body of the top-level rule of one class, up to the brace that closes it at the start of a line.
    /// </summary>
    private static string GetRule(string rules, string suffix)
    {
        var selector = suffix.Length == 0 ? @"\.bit-pnl" : $@"\.bit-pnl-{suffix}";

        var match = Regex.Match(rules, $@"\n{selector} \{{.*?\n\}}", RegexOptions.Singleline);

        Assert.IsTrue(match.Success, $"There is no top-level rule of {selector}.");

        return match.Value;
    }

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n$pnl-private-properties")];

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Panel", "BitPanel.scss");
}
