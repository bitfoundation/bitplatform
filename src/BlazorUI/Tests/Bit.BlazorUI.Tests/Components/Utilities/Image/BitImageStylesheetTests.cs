using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Image;

/// <summary>
/// Pins the public --bit-Image-* variables against the stylesheet that reads them, which a bUnit render cannot
/// see: every variable the header documents is read somewhere with a fallback, none of them is ever declared (so
/// they keep inheriting from :root and the ancestors), nothing is read that the header does not document, and the
/// shape and elevation variables stay behind the parameters that ask for the feature.
/// </summary>
[TestClass]
public partial class BitImageStylesheetTests
{
    [TestMethod]
    public void BitImageShouldReadEveryPublicVariableItDocuments()
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
    public void BitImageShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitImageShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Image-* variable is declared, which stops it inheriting.");
    }

    [TestMethod,
        DataRow(".bit-img-rnd", "--bit-Image-radius"),
        DataRow(".bit-img-brd", "--bit-Image-border-width"),
        DataRow(".bit-img-brd", "--bit-Image-border-color"),
        DataRow(".bit-img-shd", "--bit-Image-shadow")]
    public void BitImageShouldReadTheShapeVariablesOnlyWhereTheParameterAsksForTheFeature(string selector, string variable)
    {
        // A global value restyles the rounded, bordered or raised images without rounding, bordering or raising
        // every other image of the page, so each variable is read by the rule of its own parameter and nowhere else.
        var stylesheet = ReadStylesheet();

        var block = RuleOf(stylesheet, selector);

        StringAssert.Contains(block, $"var({variable}, ");
        Assert.AreEqual(1, Regex.Matches(RulesOf(stylesheet), $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read outside {selector}.");
    }

    [TestMethod]
    public void BitImageShouldCollapseTheFadeUnderReducedMotionUnlessAnimationIsForced()
    {
        var stylesheet = ReadStylesheet();

        // The fade reads a private token rather than the public variable, so the preference still wins over a
        // pace set through the variable - and ForceAnimation is the only way back out of it.
        StringAssert.Contains(stylesheet, "animation-duration: var(--bit-img-fade-duration);");
        StringAssert.Contains(stylesheet, "@media (prefers-reduced-motion: reduce) {\n    .bit-img {\n        --bit-img-fade-duration: 0.01ms;");
        StringAssert.Contains(stylesheet, ".bit-img.bit-fam,\n.bit-fam .bit-img {");
    }

    [TestMethod]
    public void BitImageShouldHideTheTextAlternativeOnlyVisually()
    {
        // display:none or visibility:hidden would take the stand-in out of the accessibility tree along with
        // the image it stands in for, which is the one thing it is there to avoid.
        var block = RuleOf(ReadStylesheet(), ".bit-img-alt");

        StringAssert.Contains(block, "clip-path: inset(50%);");
        StringAssert.Contains(block, "position: absolute;");
        Assert.IsFalse(block.Contains("display: none"));
        Assert.IsFalse(block.Contains("visibility: hidden"));
    }

    [TestMethod]
    public void BitImageShouldKeepAFluidFrameInsideItsContainer()
    {
        var block = RuleOf(ReadStylesheet(), ".bit-img-flu");

        StringAssert.Contains(block, "max-width: 100%;");
        StringAssert.Contains(block, ".bit-img-img {\n        max-width: 100%;");
    }

    [TestMethod]
    public void BitImageShouldDrawTheFocusRingInTheFocusColor()
    {
        StringAssert.Contains(ReadStylesheet(), "&:has(.bit-img-img:focus-visible) {\n        @include focus-ring(var(--bit-Image-focus-color, #{$clr-pri-focus}));");
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

    // The header comment is where the variables are documented, so only what follows it is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "Image", "BitImage.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Image-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Image-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Image-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
