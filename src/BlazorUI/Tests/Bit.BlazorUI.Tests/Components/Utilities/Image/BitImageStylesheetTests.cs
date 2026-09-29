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
        DataRow(".bit-img-shd", "--bit-Image-shadow"),
        DataRow(".bit-img-shd", "--bit-Image-hover-shadow")]
    public void BitImageShouldReadTheShapeVariablesOnlyWhereTheParameterAsksForTheFeature(string selector, string variable)
    {
        // A global value restyles the rounded, bordered or raised images without rounding, bordering or raising
        // every other image of the page, so each variable is read by the rule of its own parameter and nowhere else.
        var stylesheet = ReadStylesheet();

        var block = RuleOf(stylesheet, selector);

        StringAssert.Contains(block, $"var({variable}, ");
        Assert.AreEqual(1, Regex.Matches(RulesOf(stylesheet), $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read outside {selector}.");
    }

    [TestMethod,
        DataRow("--bit-Image-hover-overlay"),
        DataRow("--bit-Image-active-overlay")]
    public void BitImageShouldTintOnlyAClickableImage(string variable)
    {
        // The tint is the feedback of a control, so it is laid over the frame of an image with a click handler and
        // nowhere else - read once, by a rule of the clickable frame's own pseudo-element.
        var rules = RulesOf(ReadStylesheet());

        Assert.AreEqual(1, Regex.Matches(rules, $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read more than once.");
        Assert.IsTrue(Regex.IsMatch(rules, $@"(\.bit-img-clk|&)[^{{}}]*::after \{{\s*background-color: var\({Regex.Escape(variable)}, "),
                      $"{variable} is not read by the clickable frame's tint.");
    }

    [TestMethod]
    public void BitImageShouldCrossFadeThePlaceholderAtThePaceOfTheImage()
    {
        // The placeholder of an image that fades in fades out over the same private duration, so a reduced motion
        // preference collapses both halves of the cross-fade together.
        var block = RuleOf(ReadStylesheet(), ".bit-img-plc");

        StringAssert.Contains(block, "&.bit-img-pfo {");
        StringAssert.Contains(block, "animation-name: bit-img-fade-out;");
        StringAssert.Contains(block, "animation-duration: var(--bit-img-fade-duration);");
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

        // A width clamped under a fixed Height would otherwise squeeze the image out of its shape.
        StringAssert.Contains(block, "object-fit: contain;");

        // The fits and the maximized frame's cover come later, so an explicit choice still wins over it.
        var stylesheet = ReadStylesheet();
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-img-flu {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-img-max {", System.StringComparison.Ordinal));
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-img-flu {", System.StringComparison.Ordinal) < stylesheet.IndexOf("&.bit-img-cvr {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitImageShouldFillASizedFrameWithItsTemplates()
    {
        // A skeleton given a 100% height holds exactly the room the image will take only if the wrapper around
        // it takes the frame's; in a frame sized by its content the percentages resolve to nothing.
        var block = RuleOf(ReadStylesheet(), ".bit-img-tpl");

        StringAssert.Contains(block, "width: 100%;");
        StringAssert.Contains(block, "height: 100%;");
        StringAssert.Contains(block, "box-sizing: border-box;");
    }

    [TestMethod]
    public void BitImageShouldHideAnImageByItsStateAlone()
    {
        // Whether the image is on screen is the hidden class's call (display, or visibility for a lazy one) and
        // the fade's; an opacity on the image or on a fit would be a second switch for a custom class to trip.
        var stylesheet = ReadStylesheet();
        var rules = RulesOf(stylesheet[..stylesheet.IndexOf("@keyframes", System.StringComparison.Ordinal)]) +
                    RulesOf(stylesheet[(stylesheet.IndexOf(".bit-img-img {", System.StringComparison.Ordinal))..]);

        Assert.IsFalse(Regex.IsMatch(rules, @"^\s*opacity: [01];", RegexOptions.Multiline), "An opacity decides whether the image is visible.");
    }

    [TestMethod]
    public void BitImageShouldDrawTheFocusRingInTheFocusColor()
    {
        StringAssert.Contains(ReadStylesheet(), "&:has(.bit-img-img:focus-visible) {\n        outline: $shp-focus-ring-width solid var(--bit-Image-focus-color, #{$clr-pri-focus});");
    }

    [TestMethod]
    public void BitImageShouldKeepTheFocusRingOffTheElevation()
    {
        // The box-shadow of the frame is its elevation, which a Shadow and the hover lift of a clickable image both
        // write; a ring drawn with it would take the lift away from a focused image, and be taken away itself by
        // the hover rule, whose selector is the heavier one. So the ring is an outline, which the forced-colors
        // palette keeps.
        var stylesheet = ReadStylesheet();
        var focus = stylesheet[stylesheet.IndexOf("&:has(.bit-img-img:focus-visible) {", System.StringComparison.Ordinal)..];
        focus = focus[..focus.IndexOf("\n    }", System.StringComparison.Ordinal)];

        Assert.IsFalse(focus.Contains("box-shadow"), "The focus ring is drawn with the box-shadow the elevation uses.");
        Assert.IsFalse(focus.Contains("focus-ring("), "The focus ring is drawn with the box-shadow the elevation uses.");
        StringAssert.Contains(focus, "@media (forced-colors: active) {\n            outline-color: Highlight;");

        // The native ring of the image is dropped only where the frame can draw its own.
        StringAssert.Contains(stylesheet, "@supports selector(:has(a)) {\n        .bit-img-img:focus-visible {\n            outline: none;");
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
