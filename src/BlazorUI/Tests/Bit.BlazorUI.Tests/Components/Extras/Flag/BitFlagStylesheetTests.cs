using System.IO;
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

        foreach (var name in documented)
        {
            StringAssert.Contains(RulesOf(stylesheet), $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitFlagShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(RulesOf(stylesheet)).Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.IsSubsetOf(read, documented);
    }

    [TestMethod]
    public void BitFlagShouldNeverDeclareAPublicVariable()
    {
        Assert.IsFalse(DeclaredVariable().IsMatch(RulesOf(ReadStylesheet())), "A public --bit-Flag-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitFlagShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "Flag", "BitFlagDemo.razor.cs");

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
        Assert.AreEqual(1, Regex.Matches(RulesOf(stylesheet), $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read outside {selector}.");
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
    public void BitFlagShouldKeepTheFocusRingOffTheShadow()
    {
        // The box-shadow of the frame is its Shadow; a ring drawn with it would take the elevation away from a
        // focused flag. So the ring is an outline, which the forced-colors palette keeps in the system color.
        var focus = ReadStylesheet();
        focus = focus[focus.IndexOf("&:focus-visible {", System.StringComparison.Ordinal)..];
        focus = focus[..focus.IndexOf("\n    }", System.StringComparison.Ordinal)];

        StringAssert.Contains(focus, "outline: $shp-focus-ring-width solid var(--bit-Flag-focus-color, #{$clr-pri-focus});");
        StringAssert.Contains(focus, "@media (forced-colors: active) {\n            outline-color: Highlight;");
        Assert.IsFalse(focus.Contains("box-shadow"), "The focus ring is drawn with the box-shadow the Shadow uses.");
        Assert.IsFalse(focus.Contains("focus-ring;"), "The focus ring is drawn with the box-shadow the Shadow uses.");
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
        var start = stylesheet.IndexOf($"\n{selector} {{", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector} has no rule of its own.");

        return stylesheet[start..stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal)];
    }

    // The header comment is where the variables are documented, so only what is not a comment is searched.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet()
    {
        return ReadFile("Bit.BlazorUI.Extras", "Components", "Flag", "BitFlag.scss");
    }

    // The path is relative to the BlazorUI folder; the test project copies each file it reads to the same path under
    // the output directory.
    private static string ReadFile(params string[] segments)
    {
        var path = Path.Combine([System.AppContext.BaseDirectory, .. segments]);

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Flag-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Flag-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Flag-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
