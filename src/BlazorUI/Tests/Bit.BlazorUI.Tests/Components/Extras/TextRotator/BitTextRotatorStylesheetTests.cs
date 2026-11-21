using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextRotator;

/// <summary>
/// Pins the contract between the text rotator and its stylesheet: the component only moves the classes of its items,
/// the stylesheet draws everything they mean, and a bUnit render cannot see the stylesheet.
/// </summary>
[TestClass]
public class BitTextRotatorStylesheetTests
{
    private static readonly Regex VariableDeclaration = new(@"^\s*(--bit-trt-[a-z-]+)\s*:", RegexOptions.Multiline);
    private static readonly Regex VariableReference = new(@"var\((--bit-trt-[a-z-]+)");

    [TestMethod,
        DataRow("\n.bit-trt-itm {"),
        DataRow("\n.bit-trt-cur {"),
        DataRow("\n.bit-trt-ent {"),
        DataRow("\n.bit-trt-prv {"),
        DataRow("\n.bit-trt-srt {")]
    public void EveryClassTheComponentWritesIsStyled(string rule)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(SourceFiles.GetScssBlock(ReadStylesheet(), rule)), $"{rule.Trim()} has no rule.");
    }

    // The items are laid over one another in one cell, which is what keeps the rotator as wide as its widest item.
    [TestMethod]
    public void TheItemsShareOneCellOfAGrid()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt {"), "display: inline-grid;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt-itm {"), "grid-area: 1 / 1;");
    }

    // The item going out is hidden like any other one and only its exit shows it, so where the animation does not run
    // it never lingers over the item that took its place.
    [TestMethod]
    public void TheItemGoingOutIsOnlyShownByItsExit()
    {
        var stylesheet = ReadStylesheet();

        Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt-prv {").Contains("visibility"));

        var exit = SourceFiles.GetScssBlock(stylesheet, "@keyframes bit-trt-ext");

        StringAssert.Contains(SourceFiles.GetScssBlock(exit, "from {"), "visibility: visible;");
        StringAssert.Contains(SourceFiles.GetScssBlock(exit, "to {"), "visibility: hidden;");
    }

    // A variable read without a default would be invalid at computed-value time, which drops the whole animation.
    [TestMethod]
    public void EveryVariableTheStylesheetReadsHasADefaultOnTheRoot()
    {
        var stylesheet = ReadStylesheet();
        var declared = VariableDeclaration.Matches(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt {")).Select(m => m.Groups[1].Value).ToArray();
        var read = VariableReference.Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.IsNotEmpty(read);
        foreach (var variable in read)
        {
            CollectionAssert.Contains(declared, variable, $"{variable} is read by the stylesheet but has no default on .bit-trt.");
        }
    }

    [TestMethod]
    public void TheDurationFollowsTheMotionFactorAndTheMoveReadsTheMotionTokens()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-trt {");

        StringAssert.Contains(root, "--bit-trt-dur: #{mot-scaled-duration(var(--bit-TextRotator-duration, #{$mot-duration-long}))};");
        StringAssert.Contains(root, "--bit-trt-eas: var(--bit-TextRotator-easing, #{$mot-easing});");
        StringAssert.Contains(root, "--bit-trt-dst: var(--bit-TextRotator-distance, 0.5em);");
    }

    // Reduced motion shows the first item alone and still, which no duration can say, so it is a query of its own -
    // one ForceAnimation opts out of, on the rotator and on an ancestor of it alike.
    [TestMethod]
    public void ReducedMotionShowsTheFirstItemAloneUnlessAnimationIsForced()
    {
        var reducedMotion = SourceFiles.GetScssBlock(ReadStylesheet(), "@media (prefers-reduced-motion: reduce)");

        var items = SourceFiles.GetScssBlock(reducedMotion, ".bit-trt:where(:not(.bit-fam):not(.bit-fam *)) > .bit-trt-itm {");
        StringAssert.Contains(items, "animation: none;");
        StringAssert.Contains(items, "visibility: hidden;");

        var first = SourceFiles.GetScssBlock(reducedMotion, ".bit-trt:where(:not(.bit-fam):not(.bit-fam *)) > .bit-trt-itm:first-child {");
        StringAssert.Contains(first, "opacity: 1;");
        StringAssert.Contains(first, "visibility: visible;");
    }

    [TestMethod]
    public void TheScreenReaderTextIsVisuallyHiddenInsideAPositionedRoot()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt-srt {"), "@include visually-hidden;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt {"), "position: relative;");
    }

    [TestMethod]
    public void ADisabledRotatorIsDimmedAndGrayInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-trt.bit-dis {"), "opacity: var(--bit-TextRotator-disabled-opacity, #{$opa-dis});");
        StringAssert.Contains(SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active)"), ".bit-trt.bit-dis {"), "color: GrayText;");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "TextRotator", "BitTextRotator.scss").Replace("\r\n", "\n");
}
