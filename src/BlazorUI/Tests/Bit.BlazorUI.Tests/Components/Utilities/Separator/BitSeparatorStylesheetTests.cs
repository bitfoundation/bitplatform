using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Separator;

/// <summary>
/// Pins the public --bit-Separator-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from
/// :root and the ancestors), nothing is read that the header does not document, a parameter wins over the variable
/// standing for the same thing, nested separators do not inherit each other's line, and the disabled state holds in a
/// forced-colors mode.
/// </summary>
[TestClass]
public partial class BitSeparatorStylesheetTests
{
    [TestMethod]
    public void BitSeparatorShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Separator-* variable is declared, which stops it inheriting.");
    }

    [TestMethod,
        DataRow("--bit-spr-brd"),
        DataRow("--bit-spr-siz"),
        DataRow("--bit-spr-stl"),
        DataRow("--bit-spr-bg"),
        DataRow("--bit-spr-ofs"),
        DataRow("--bit-spr-ins"),
        DataRow("--bit-spr-img"),
        DataRow("--bit-spr-dis-brd"),
        DataRow("--bit-spr-dis-fg")]
    public void BitSeparatorShouldStartEveryPrivateVariableUnset(string name)
    {
        // A separator nested in the content of another would otherwise inherit its line.
        StringAssert.Contains(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr {"), $"{name}: initial;");
    }

    [TestMethod,
        DataRow("var(--bit-spr-stl, var(--bit-Separator-line-style, solid))"),
        DataRow("var(--bit-spr-brd, var(--bit-Separator-color, #{$clr-brd-sec}))"),
        DataRow("var(--bit-spr-siz, var(--bit-Separator-thickness, #{$siz-divider}))"),
        DataRow("var(--bit-spr-ins, var(--bit-Separator-inset, 0))"),
        DataRow("var(--bit-spr-ofs, var(--bit-Separator-content-offset, 0))"),
        DataRow("var(--bit-spr-bg, var(--bit-Separator-content-background, transparent))"),
        DataRow("var(--bit-spr-img, var(--bit-Separator-line-background, none))")]
    public void BitSeparatorShouldLetTheParameterWinOverItsVariable(string chain)
    {
        // The parameter classes and the inline lengths set the private variable, which is read ahead of the public one.
        StringAssert.Contains(ReadStylesheet(), chain);
    }

    [TestMethod]
    public void BitSeparatorShouldGiveSolidAClassOfItsOwn()
    {
        // An explicit Solid has to beat a dashed line-style variable set on an ancestor, so it is not the unset default.
        StringAssert.Contains(ReadStylesheet(), ".bit-spr-sld {\n    --bit-spr-stl: solid;");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepADisabledSeparatorInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {");
        StringAssert.Contains(block, "--bit-spr-img: none;");
        StringAssert.Contains(block, "--bit-spr-dis-brd: #{$clr-brd-dis};");
        StringAssert.Contains(block, "--bit-spr-dis-fg: #{$clr-fg-dis};");

        // The disabled colors come first in each chain, ahead of the parameters and the public variables.
        StringAssert.Contains(stylesheet, "var(--bit-spr-dis-brd, var(--bit-spr-brd, ");
        StringAssert.Contains(stylesheet, "color: var(--bit-spr-dis-fg, var(--bit-Separator-content-color, ");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepTheDisabledStateInForcedColors()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(block, "border-color: GrayText;");
        StringAssert.Contains(block, "color: GrayText;");
    }

    [TestMethod]
    public void BitSeparatorShouldDrawTheLineBackgroundAsTheBorderImageOfEachSegment()
    {
        var stylesheet = ReadStylesheet();

        // A border image replaces the paint of the border alone, so without one every line style is drawn as before. It
        // comes after the border shorthand, which would reset it.
        var segments = SourceFiles.GetScssDeclarations(stylesheet, "\n    &::before,\n    &::after {");
        var border = segments.IndexOf("border: 0 var(--bit-spr-stl, ", StringComparison.Ordinal);
        var image = segments.IndexOf("border-image-source: var(--bit-spr-img, var(--bit-Separator-line-background, none));", StringComparison.Ordinal);

        Assert.IsTrue(border >= 0, "The segments are not drawn as a border.");
        Assert.IsTrue(image > border, "The line background is not the border image of the segments, set after the border.");
        StringAssert.Contains(segments, "border-image-slice: 1;");
    }

    [TestMethod,
        DataRow(".bit-spr-dsh"), DataRow(".bit-spr-dot"), DataRow(".bit-spr-dbl"),
        DataRow(".bit-spr-bpr"), DataRow(".bit-spr-bsr"), DataRow(".bit-spr-btr"), DataRow(".bit-spr-brr"),
        DataRow(".bit-spr-pri"), DataRow(".bit-spr-sec"), DataRow(".bit-spr-ter"), DataRow(".bit-spr-inf"),
        DataRow(".bit-spr-suc"), DataRow(".bit-spr-wrn"), DataRow(".bit-spr-swr"), DataRow(".bit-spr-err"),
        DataRow(".bit-spr-pbg"), DataRow(".bit-spr-sbg"), DataRow(".bit-spr-tbg"),
        DataRow(".bit-spr-pfg"), DataRow(".bit-spr-sfg"), DataRow(".bit-spr-tfg"),
        DataRow(".bit-spr-pbr"), DataRow(".bit-spr-sbr"), DataRow(".bit-spr-tbr")]
    public void BitSeparatorShouldLetTheLineParametersWinOverTheLineBackground(string parameterClass)
    {
        // The line background is drawn in place of the color and the style of the line, so a parameter picking either
        // takes it away, as every parameter wins over the variable standing for the same thing.
        var rules = SourceFiles.GetScssRules(ReadStylesheet());

        Assert.IsTrue(rules.Any(r => r.Ancestors.Count == 0 &&
                                     SourceFiles.SplitScssSelectorList(r.Header).Select(s => s.Trim()).Contains(parameterClass) &&
                                     r.Declarations.Contains("--bit-spr-img: none")),
                      $"{parameterClass} leaves the line background in place of the line it asks for.");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepASolidLineStyleOverTheLineBackground()
    {
        // A solid line is what a line background is drawn as anyway, so an explicit Solid leaves it.
        Assert.IsFalse(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr-sld {").Contains("--bit-spr-img"));
    }

    [TestMethod]
    public void BitSeparatorShouldFadeEachSegmentTowardItsOuterEnd()
    {
        var stylesheet = ReadStylesheet();

        // Every segment reads the mask a fade gives it, and none without one.
        var segments = SourceFiles.GetScssDeclarations(stylesheet, "\n    &::before,\n    &::after {");
        StringAssert.Contains(segments, "-webkit-mask-image: var(--bit-spr-fdm, none);");
        StringAssert.Contains(segments, "mask-image: var(--bit-spr-fdm, none);");

        var fade = SourceFiles.GetScssRules(stylesheet)
                              .Where(r => r.Ancestors.FirstOrDefault() == ".bit-spr-fad" || r.Header == ".bit-spr-fad")
                              .SelectMany(r => SourceFiles.ResolveScssSelectors([.. r.Ancestors, r.Header])
                                                          .Select(selector => (Selector: selector, Mask: r.Declarations.FirstOrDefault(d => d.StartsWith("--bit-spr-fdm:", StringComparison.Ordinal)))))
                              .Where(r => r.Mask is not null)
                              .ToDictionary(r => r.Selector, r => r.Mask);

        // The two segments around the content mirror each other, transparent at their outer ends.
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to right, transparent, #000)", fade[".bit-spr-fad.bit-spr-hrz::before"]);
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to left, transparent, #000)", fade[".bit-spr-fad.bit-spr-hrz::after"]);
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to bottom, transparent, #000)", fade[".bit-spr-fad.bit-spr-vrt::before"]);
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to top, transparent, #000)", fade[".bit-spr-fad.bit-spr-vrt::after"]);

        // The outer ends follow the reading direction, and a direction Dir does not name is read too.
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to left, transparent, #000)", fade[".bit-spr-fad.bit-spr-hrz:is(.bit-rtl, :dir(rtl))::before"]);
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to right, transparent, #000)", fade[".bit-spr-fad.bit-spr-hrz:is(.bit-rtl, :dir(rtl))::after"]);

        // Without content, the one segment fades toward both of its ends.
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to right, transparent, #000, transparent)", fade[".bit-spr-fad.bit-spr-hrz:not(:has(> .bit-spr-cnt))::before"]);
        Assert.AreEqual("--bit-spr-fdm: linear-gradient(to bottom, transparent, #000, transparent)", fade[".bit-spr-fad.bit-spr-vrt:not(:has(> .bit-spr-cnt))::before"]);
    }

    [TestMethod]
    public void BitSeparatorShouldDrawTheLineInFullAndInTheSystemColorInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        // A forced-colors mode leaves a border image alone, so the line background is taken away for the border to be
        // drawn in the system color.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n@media (forced-colors: active) {"), "border-image-source: none;");

        // A faded end is the line at a fraction of its contrast, which neither a forced-colors mode nor a reader asking
        // for more contrast wants. The rule comes after the one reading the mask, at the same specificity.
        var contrast = SourceFiles.GetScssBlock(stylesheet, "\n@media (forced-colors: active), (prefers-contrast: more) {");
        StringAssert.Contains(contrast, "-webkit-mask-image: none;");
        StringAssert.Contains(contrast, "mask-image: none;");
        Assert.IsTrue(stylesheet.IndexOf(contrast, StringComparison.Ordinal) > stylesheet.IndexOf("mask-image: var(--bit-spr-fdm, none);", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitSeparatorShouldInsetTheLineLogically()
    {
        var stylesheet = ReadStylesheet();

        // Logical padding takes one length for both ends or two for the start and the end, in the reading direction.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-hrz {"), "padding-inline: var(--bit-spr-ins, ");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-vrt {"), "padding-block: var(--bit-spr-ins, ");
    }

    [TestMethod]
    public void BitSeparatorShouldLetTheContentShrinkAndWrap()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr-cnt {");

        StringAssert.Contains(block, "min-width: 0;");
        StringAssert.Contains(block, "overflow-wrap: break-word;");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepAVerticalSeparatorAtLeastALineOfTextTall()
    {
        var stylesheet = ReadStylesheet();

        // Between runs of text, or sized to the content it does not have, the line would otherwise collapse to nothing.
        // The minimum sits on the line rather than on the border-box root, whose Inset padding would otherwise eat it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    &:not(:has(> .bit-spr-cnt))::before {"), "min-height: 1em;");
        Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-vrt {").Contains("\n    min-height:"), "The minimum is on the root, where an Inset eats it.");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepTheContentGapAtBothEnds()
    {
        var stylesheet = ReadStylesheet();

        // Start- and end-aligned content keeps the same gap from the edge as from the line, however it is painted and
        // whatever its offset, so upgrading leaves an existing aligned label where it was.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-hrz {"), "padding: 0 var(--bit-Separator-content-gap, #{spacing(1.5)});");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-vrt {"), "padding: var(--bit-Separator-content-gap, #{spacing(1.5)}) 0;");
        Assert.IsFalse(stylesheet.Contains("padding-inline-start"), "The content is held flush with its edge.");
        Assert.IsFalse(stylesheet.Contains("padding-top"), "The content is held flush with its edge.");
    }

    [TestMethod]
    public void BitSeparatorShouldLetTheContentInheritTheFontOfItsSurroundings()
    {
        // The size and the weight inherit, so the family does too: a label inside a heading or a monospace panel
        // matches the text around it.
        Assert.IsFalse(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr-cnt {").Contains("font-family"), "The content forces a font family.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Utilities", "Separator", "BitSeparator.scss");

    [GeneratedRegex(@"^//\s+(--bit-Separator-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Separator-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Separator-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
