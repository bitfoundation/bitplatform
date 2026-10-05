using System.Linq;
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
        var body = SourceFiles.StripScssComments(ReadStylesheet());

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
        Assert.AreEqual(1, Regex.Matches(SourceFiles.StripScssComments(stylesheet), $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read outside {selector}.");
    }

    [TestMethod,
        DataRow("--bit-Image-hover-overlay"),
        DataRow("--bit-Image-active-overlay")]
    public void BitImageShouldTintOnlyAClickableImage(string variable)
    {
        // The tint is the feedback of a control, so it is laid over the frame of an image with a click handler and
        // nowhere else - read once, by a rule of the clickable frame's own pseudo-element.
        var rules = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.AreEqual(1, Regex.Matches(rules, $@"var\({Regex.Escape(variable)}[,)]").Count, $"{variable} is read more than once.");
        Assert.IsTrue(Regex.IsMatch(rules, $@"(\.bit-img-clk|&)[^{{}}]*::after \{{\s*background-color: var\({Regex.Escape(variable)}, "),
                      $"{variable} is not read by the clickable frame's tint.");
    }

    [TestMethod]
    public void BitImageShouldCrossFadeThePlaceholderAtThePaceOfTheImage()
    {
        // The placeholder of an image that fades in fades out over the same duration, so a reduced motion
        // preference collapses both halves of the cross-fade together.
        var block = RuleOf(ReadStylesheet(), ".bit-img-plc");

        StringAssert.Contains(block, "&.bit-img-pfo {");
        StringAssert.Contains(block, "animation-name: bit-img-fade-out;");
        StringAssert.Contains(block, FadeDuration);
    }

    [TestMethod]
    public void BitImageShouldLeaveReducedMotionToTheMotionTokens()
    {
        // The fade reads the theme's motion token like every other component, so the library-wide mechanism -
        // the tokens collapsing under the preference, .bit-fam and an app's :root restoring them - is the only
        // one: the image keeps no reduced motion query, and no ForceAnimation restore, of its own.
        var stylesheet = ReadStylesheet();

        Assert.AreEqual(2, Regex.Matches(stylesheet, Regex.Escape(FadeDuration)).Count);
        Assert.IsFalse(stylesheet.Contains("prefers-reduced-motion"), "The image collapses its own motion.");
        Assert.IsFalse(stylesheet.Contains("bit-fam"), "The image restores its own motion.");
        Assert.IsFalse(stylesheet.Contains("--bit-img-fade-duration"), "The fade reads a private token of its own.");
    }

    [TestMethod]
    public void BitImageShouldGiveTheFrameBackgroundNoSpecificity()
    {
        // A page's own single-class rule on the frame wins over the default background wherever the two
        // stylesheets are linked, the way it did before the frame had a background of its own.
        var block = RuleOf(ReadStylesheet(), ":where(.bit-img)");

        StringAssert.Contains(block, "background-color: var(--bit-Image-background, transparent);");
        Assert.AreEqual(1, Regex.Matches(SourceFiles.StripScssComments(ReadStylesheet()), @"var\(--bit-Image-background[,)]").Count);
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
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(RuleOf(stylesheet, ".bit-img-flu"), "max-width: 100%;");

        // The image is scaled with the frame only where it is left to its default fit - the class is never
        // rendered beside an explicit ImageFit, whose own rules set neither max-width nor, for the centered
        // fits, object-fit, and so could not undo it.
        var image = RuleOf(stylesheet, ".bit-img-img.bit-img-fli");

        StringAssert.Contains(image, "max-width: 100%;");

        // A width clamped under a fixed Height would otherwise squeeze the image out of its shape.
        StringAssert.Contains(image, "object-fit: contain;");

        // The maximized frame's cover comes later with the same specificity, so it still wins over it.
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-img-img.bit-img-fli {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-img-max {", System.StringComparison.Ordinal));
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
        var rules = SourceFiles.StripScssComments(stylesheet[..stylesheet.IndexOf("@keyframes", System.StringComparison.Ordinal)]) +
                    SourceFiles.StripScssComments(stylesheet[(stylesheet.IndexOf(".bit-img-img {", System.StringComparison.Ordinal))..]);

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
        var focus = SourceFiles.GetScssBlock(stylesheet, "&:has(.bit-img-img:focus-visible) {");

        Assert.IsFalse(focus.Contains("box-shadow"), "The focus ring is drawn with the box-shadow the elevation uses.");
        Assert.IsFalse(focus.Contains("focus-ring("), "The focus ring is drawn with the box-shadow the elevation uses.");
        StringAssert.Contains(focus, "@media (forced-colors: active) {\n            outline-color: Highlight;");

        // The native ring of the image is dropped only where the frame can draw its own.
        StringAssert.Contains(stylesheet, "@supports selector(:has(a)) {\n        .bit-img-img:focus-visible {\n            outline: none;");
    }

    private const string FadeDuration = "animation-duration: var(--bit-Image-fade-duration, #{$mot-duration-long});";

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The block of a top-level rule, from its selector to the brace that closes it.
    private static string RuleOf(string stylesheet, string selector)
    {
        return SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Image", "BitImage.scss");

    [GeneratedRegex(@"^//\s+(--bit-Image-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Image-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Image-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
