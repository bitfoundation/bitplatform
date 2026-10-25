using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Flag;

/// <summary>
/// Pins the contract of the public --bit-Flag-* CSS variables, which a bUnit render cannot see: every variable the
/// header documents is read with a fallback and never declared (so it inherits from :root and the ancestors), nothing
/// is read that the header does not document, the demo page documents the same set, and the shape, elevation and
/// interaction variables stay behind the parameters that ask for the feature.
/// </summary>
[TestClass]
public partial class BitFlagStylesheetTests
{
    [TestMethod]
    public void BitFlagShouldReadEveryPublicVariableItDocumentsWithAFallback()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        var body = SourceFiles.StripScssComments(stylesheet);

        foreach (var name in documented)
        {
            // The focus color alone is read without a fallback, on purpose: its absence is what hands the ring over
            // to the global --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
            var read = name.EndsWith("-focus-color", StringComparison.Ordinal) ? $"var({name})" : $"var({name}, ";

            StringAssert.Contains(body, read, $"{name} is documented but never read as it should be.");
        }
    }

    [TestMethod]
    public void BitFlagShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(SourceFiles.StripScssComments(stylesheet)).Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.IsSubsetOf(read, documented);
    }

    [TestMethod]
    public void BitFlagShouldNeverDeclareAPublicVariable()
    {
        Assert.IsFalse(DeclaredVariable().IsMatch(SourceFiles.StripScssComments(ReadStylesheet())), "A public --bit-Flag-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitFlagShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "Flag", "BitFlagDemo.razor.cs");

        var onTheDemoPage = Regex.Matches(demo, @"Name = ""(--bit-Flag-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(DocumentedVariables(ReadStylesheet()), onTheDemoPage);
    }

    [TestMethod,
        DataRow(".bit-flg-rnd", "--bit-Flag-radius"),
        DataRow(".bit-flg-brd", "--bit-Flag-border-width"),
        DataRow(".bit-flg-brd", "--bit-Flag-border-color"),
        DataRow(".bit-flg-shd", "--bit-Flag-shadow"),
        DataRow(".bit-flg-gry", "--bit-Flag-grayscale-filter"),
        DataRow(".bit-flg-clk", "--bit-Flag-hover-opacity"),
        DataRow(".bit-flg-clk", "--bit-Flag-active-opacity")]
    public void BitFlagShouldReadAFeatureVariableOnlyWhereTheParameterAsksForTheFeature(string selector, string variable)
    {
        // A global value restyles the rounded, bordered, raised, grey or clickable flags without doing the same to
        // every other flag of the page, so each variable is read by the rule of its own parameter and nowhere else.
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(RuleOf(stylesheet, selector), $"var({variable}, ");
        Assert.AreEqual(1, Regex.Matches(SourceFiles.StripScssComments(stylesheet), $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read outside {selector}.");
    }

    [TestMethod]
    public void BitFlagShouldLetTheSizeParametersWinOverTheSizeVariable()
    {
        // The variable is the default of the frame; the size classes and the inline Width and Height come after it.
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(RuleOf(stylesheet, ".bit-flg"), "--bit-flg-siz: var(--bit-Flag-size, #{$siz-icon-md});");

        foreach (var size in new[] { "sm", "md", "lg" })
        {
            Assert.IsFalse(RuleOf(stylesheet, $".bit-flg-{size}").Contains("--bit-Flag-size"), $"Size {size} reads the variable it should win over.");
        }
    }

    [TestMethod]
    public void BitFlagShouldKeepTheThemeCornerOfARoundedFlagInProportionToIt()
    {
        // The surface corner of a preset is sized for cards - Material's is 12px - and would turn a 16px flag into a
        // pill, so the default is capped by the size of the flag. A value of the page's own is taken as given.
        StringAssert.Contains(RuleOf(ReadStylesheet(), ".bit-flg-rnd"),
                              "--bit-flg-rad: var(--bit-Flag-radius, min(#{$shp-radius-surface}, calc(var(--bit-flg-siz) / 8)));");
    }

    [TestMethod]
    public void BitFlagShouldDrawTheSharedFocusRingOverItsShadow()
    {
        // The ring is the library's own, drawn with the same box-shadow the Shadow uses, so the elevation is handed
        // to the mixin as a layer to keep: a focused raised flag keeps its shadow under the ring.
        var stylesheet = ReadStylesheet();

        var focus = SourceFiles.GetScssBlock(stylesheet, "&:focus-visible {");

        StringAssert.Contains(focus, "@include focus-ring-own(var(--bit-Flag-focus-color), var(--bit-flg-elv));");
        Assert.IsFalse(focus.Contains("outline:"), "The focus ring is drawn by hand rather than with the shared mixin.");

        StringAssert.Contains(RuleOf(stylesheet, ".bit-flg"), "--bit-flg-elv: 0 0 #0000;");
        StringAssert.Contains(RuleOf(stylesheet, ".bit-flg-shd"), "--bit-flg-elv: var(--bit-Flag-shadow, #{$box-shadow-card});\n    box-shadow: var(--bit-flg-elv);");

        StringAssert.Contains(SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss"),
"@mixin focus-ring-own($color, $also: null) {");
    }

    [TestMethod]
    public void BitFlagShouldClipACroppedPictureWithAnyCornerTheFrameIsGiven()
    {
        // A --bit-Flag-radius of more than one value ("0.75rem 0") cannot be calculated with, and a calc() of it would
        // make the whole clip-path invalid - so the corner is taken whole, and a flag that is not a button leaves
        // the clipping to the overflow of its frame, which follows the inside of the border exactly.
        var crop = RuleOf(ReadStylesheet(), ".bit-flg-crp");

        StringAssert.Contains(crop, "round var(--bit-flg-rad));");
        Assert.IsFalse(crop.Contains("calc(var(--bit-flg-rad)"), "The corner of the frame is calculated with.");
        StringAssert.Contains(crop, "&:not(.bit-flg-clk) {\n        overflow: clip;");
        StringAssert.Contains(crop, "&:not(.bit-flg-clk) > .bit-flg-img {\n        clip-path: none;");
    }

    [TestMethod]
    public void BitFlagShouldPutAPageEmojiFontAheadOfThePlatformOnes()
    {
        StringAssert.Contains(RuleOf(ReadStylesheet(), ".bit-flg-emj"),
                              "font-family: var(--bit-Flag-emoji-font-family, 'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', 'Segoe UI Symbol', sans-serif);");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The block of a top-level rule, from its selector to the brace that closes it.
    private static string RuleOf(string stylesheet, string selector)
    {
        return SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");
    }

    private static string ReadStylesheet()
    {
        return SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Flag", "BitFlag.scss");
    }

    [GeneratedRegex(@"^//\s+(--bit-Flag-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Flag-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Flag-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
