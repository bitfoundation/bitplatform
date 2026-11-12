using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// Pins what a bUnit render cannot see of the Modal: the public --bit-Modal-* custom properties, which are read off
/// the stylesheet with a fallback and never declared there so they inherit, and the parts of the layout and the
/// accessibility that only exist in CSS.
/// </summary>
[TestClass]
public class BitModalStylesheetTests
{
    private static readonly string[] _publicVariables =
    [
        "--bit-Modal-z-index",
        "--bit-Modal-offset",
        "--bit-Modal-max-width",
        "--bit-Modal-max-height",
        "--bit-Modal-background",
        "--bit-Modal-color",
        "--bit-Modal-radius",
        "--bit-Modal-shadow",
        "--bit-Modal-border-color",
        "--bit-Modal-border-width",
        "--bit-Modal-overlay-background",
        "--bit-Modal-overlay-backdrop-filter",
        "--bit-Modal-padding",
        "--bit-Modal-header-font-size",
        "--bit-Modal-header-font-weight",
    ];

    [TestMethod]
    public void BitModalStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitModalStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-Modal-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitModalStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-Modal-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-Modal-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        Assert.AreSequenceEqual(_publicVariables.Order().ToArray(), read);
        Assert.AreSequenceEqual(read, documented);
    }

    [TestMethod]
    public void BitModalOffsetShouldNeverPushTheSurfaceOffTheScreen()
    {
        var rules = GetRules(ReadStylesheet());

        // The offset is room inside the layer, and a cap of the consumer's own only ever narrows what that leaves.
        Assert.Contains("box-sizing: border-box;", GetRule(rules, ""));
        Assert.Contains("padding: var(--bit-Modal-offset, 0px);", GetRule(rules, ""));
        Assert.Contains("max-width: min(var(--bit-Modal-max-width, 100%), calc(100% - 2 * var(--bit-Modal-offset, 0px)));", GetRule(rules, "ctn"));
        Assert.Contains("max-height: min(var(--bit-Modal-max-height, 100%), calc(100% - 2 * var(--bit-Modal-offset, 0px)));", GetRule(rules, "ctn"));
    }

    [TestMethod,
        DataRow("tlf", "flex-end"),
        DataRow("clf", "flex-end"),
        DataRow("blf", "flex-end"),
        DataRow("trg", "flex-start"),
        DataRow("crg", "flex-start"),
        DataRow("brg", "flex-start")
    ]
    public void BitModalPhysicalPositionsShouldStayOnTheirSideInRightToLeft(string position, string justify)
    {
        var rules = GetRules(ReadStylesheet());

        var match = Regex.Match(rules, $@"[^}}]*\.bit-mdl-{position}:is\(\.bit-rtl, :dir\(rtl\)\)[^{{]*\{{([^}}]*)\}}");

        Assert.IsTrue(match.Success, $"{position} is not pinned in a right-to-left layout.");
        Assert.Contains($"justify-content: {justify};", match.Groups[1].Value);
    }

    [TestMethod]
    public void BitModalLogicalPositionsShouldFollowTheDirection()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var position in new[] { "tst", "ten", "cst", "cen", "bst", "ben" })
        {
            Assert.DoesNotContain($".bit-mdl-{position}:is(.bit-rtl, :dir(rtl))", rules, $"{position} is pinned, so it no longer follows the direction.");
        }
    }

    [TestMethod]
    public void BitModalShouldStayASurfaceInForcedColors()
    {
        var rules = GetRules(ReadStylesheet());

        var forced = Regex.Match(rules, @"@media \(forced-colors: active\) \{.*?\n\}", RegexOptions.Singleline).Value;

        Assert.Contains("border: $shp-border-width $shp-border-style CanvasText;", forced);
        Assert.Contains(".bit-mdl-cls:focus-visible", forced);
    }

    [TestMethod]
    public void BitModalCloseButtonShouldShowItsFocus()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "cls");

        Assert.Contains("&:focus-visible {\n        @include focus-ring;", rule);
        Assert.Contains("border-radius: $shp-radius-button;", rule);
        Assert.Contains("&:disabled {", rule);

        // The glyph is sized off the control icon token, so a preset that resizes control icons resizes it too.
        Assert.Contains("font-size: $siz-icon-md;", rule);

        // A header that holds nothing but the close button still puts it at the end.
        Assert.Contains("margin-inline-start: auto;", rule);
    }

    [TestMethod]
    public void BitModalStickyChromeShouldNotHideTheFocus()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "chr");

        // The header and the footer stay over the scrolling body, so the room they take is kept clear of what the
        // focus scrolls into view (WCAG 2.4.11), and only where each of them is actually rendered.
        Assert.Contains("&:has(> .bit-mdl-hcn) {\n        scroll-padding-block-start:", rule);
        Assert.Contains("&:has(> .bit-mdl-fcn) {\n        scroll-padding-block-end:", rule);
        Assert.Contains("var(--bit-Modal-padding, #{$spa-dialog}) + #{$siz-ctrl-md}", rule);
    }

    [TestMethod]
    public void BitModalFooterShouldLayOutItsActionsTheWayTheThemeDoes()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "fcn");

        // Never a literal row / flex-end in a dialog footer: Cupertino stacks its actions full width.
        Assert.Contains("flex-direction: $layout-dialog-actions-direction;", rule);
        Assert.Contains("justify-content: $layout-dialog-actions-justify;", rule);
        Assert.Contains("align-items: $layout-dialog-actions-align;", rule);
    }

    [TestMethod]
    public void BitModalRefusalShouldStillBeAnsweredUnderReducedMotion()
    {
        var rules = GetRules(ReadStylesheet());

        var reduced = Regex.Match(rules, @"@media \(prefers-reduced-motion: reduce\) \{.*?\n\}", RegexOptions.Singleline).Value;

        // The pulse collapses to nothing with the motion tokens, so a ring on the untouched duration answers instead -
        // unless the Modal was asked to animate regardless.
        Assert.Contains("animation-name: bit-mdl-refuse-ring-a;", reduced);
        Assert.Contains("animation-name: bit-mdl-refuse-ring-b;", reduced);
        Assert.Contains("animation-duration: $mot-duration-long-full;", reduced);
        Assert.IsFalse(Regex.IsMatch(reduced, @"\d+m?s\b"), "The ring hard-codes a duration instead of reading the motion token.");
        Assert.Contains(":not(.bit-fam *)", reduced);
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n.bit-mdl {")..];

    /// <summary>
    /// The body of the top-level rule of one class, up to the brace that closes it at the start of a line.
    /// </summary>
    private static string GetRule(string rules, string suffix)
    {
        var selector = suffix.Length == 0 ? @"\.bit-mdl" : $@"\.bit-mdl-{suffix}";

        return Regex.Match(rules, $@"\n{selector} \{{.*?\n\}}", RegexOptions.Singleline).Value;
    }

    [TestMethod]
    public void BitModalDragHandleShouldStartNoTextSelection()
    {
        // The class the drag-drop script puts on the handle of a draggable Modal or Dialog: a press on it is a move,
        // never a selection - nor, in Firefox, a drag of text an earlier selection reached.
        var handle = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-mdl-nta {");

        Assert.Contains("touch-action: none;", handle);
        Assert.Contains("-webkit-user-select: none;", handle);
        Assert.Contains("\n    user-select: none;", handle);
    }

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-mdl {")];

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Modal", "BitModal.scss");
}
