using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read, documented);
    }

    [TestMethod]
    public void BitModalOffsetShouldNeverPushTheSurfaceOffTheScreen()
    {
        var rules = GetRules(ReadStylesheet());

        // The offset is room inside the layer, and a cap of the consumer's own only ever narrows what that leaves.
        StringAssert.Contains(GetRule(rules, ""), "box-sizing: border-box;");
        StringAssert.Contains(GetRule(rules, ""), "padding: var(--bit-Modal-offset, 0px);");
        StringAssert.Contains(GetRule(rules, "ctn"), "max-width: min(var(--bit-Modal-max-width, 100%), calc(100% - 2 * var(--bit-Modal-offset, 0px)));");
        StringAssert.Contains(GetRule(rules, "ctn"), "max-height: min(var(--bit-Modal-max-height, 100%), calc(100% - 2 * var(--bit-Modal-offset, 0px)));");
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

        var match = Regex.Match(rules, $@"[^}}]*\.bit-mdl-{position}:dir\(rtl\)[^{{]*\{{([^}}]*)\}}");

        Assert.IsTrue(match.Success, $"{position} is not pinned in a right-to-left layout.");
        StringAssert.Contains(match.Groups[1].Value, $"justify-content: {justify};");
    }

    [TestMethod]
    public void BitModalLogicalPositionsShouldFollowTheDirection()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var position in new[] { "tst", "ten", "cst", "cen", "bst", "ben" })
        {
            Assert.IsFalse(rules.Contains($".bit-mdl-{position}:dir(rtl)"), $"{position} is pinned, so it no longer follows the direction.");
        }
    }

    [TestMethod]
    public void BitModalShouldStayASurfaceInForcedColors()
    {
        var rules = GetRules(ReadStylesheet());

        var forced = Regex.Match(rules, @"@media \(forced-colors: active\) \{.*?\n\}", RegexOptions.Singleline).Value;

        StringAssert.Contains(forced, "border: $shp-border-width $shp-border-style CanvasText;");
        StringAssert.Contains(forced, ".bit-mdl-cls:focus-visible");
    }

    [TestMethod]
    public void BitModalCloseButtonShouldShowItsFocus()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "cls");

        StringAssert.Contains(rule, "&:focus-visible {\n        @include focus-ring;");
        StringAssert.Contains(rule, "border-radius: $shp-radius-button;");
        StringAssert.Contains(rule, "&:disabled {");

        // The glyph is sized off the control icon token, so a preset that resizes control icons resizes it too.
        StringAssert.Contains(rule, "font-size: $siz-icon-md;");

        // A header that holds nothing but the close button still puts it at the end.
        StringAssert.Contains(rule, "margin-inline-start: auto;");
    }

    [TestMethod]
    public void BitModalFooterShouldLayOutItsActionsTheWayTheThemeDoes()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "fcn");

        // Never a literal row / flex-end in a dialog footer: Cupertino stacks its actions full width.
        StringAssert.Contains(rule, "flex-direction: $layout-dialog-actions-direction;");
        StringAssert.Contains(rule, "justify-content: $layout-dialog-actions-justify;");
        StringAssert.Contains(rule, "align-items: $layout-dialog-actions-align;");
    }

    [TestMethod]
    public void BitModalRefusalShouldStillBeAnsweredUnderReducedMotion()
    {
        var rules = GetRules(ReadStylesheet());

        var reduced = Regex.Match(rules, @"@media \(prefers-reduced-motion: reduce\) \{.*?\n\}", RegexOptions.Singleline).Value;

        // The pulse collapses to nothing with the motion tokens, so a ring on the untouched duration answers instead -
        // unless the Modal was asked to animate regardless.
        StringAssert.Contains(reduced, "animation-name: bit-mdl-refuse-ring-a;");
        StringAssert.Contains(reduced, "animation-name: bit-mdl-refuse-ring-b;");
        StringAssert.Contains(reduced, "var(--bit-mot-duration-long-full, 300ms)");
        StringAssert.Contains(reduced, ":not(.bit-fam *)");
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

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-mdl {")];

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Surfaces", "Modal", "BitModal.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
