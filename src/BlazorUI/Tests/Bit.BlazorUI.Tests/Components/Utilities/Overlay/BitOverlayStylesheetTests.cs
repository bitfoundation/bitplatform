using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

/// <summary>
/// Pins what a bUnit render cannot see of the Overlay: its public --bit-Overlay-* variables, which the stylesheet reads
/// and the header comment of that stylesheet documents, and the layout rules the parameters stand for.
/// </summary>
[TestClass]
public class BitOverlayStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Overlay-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Overlay-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Overlay-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitOverlayShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        Assert.AreSequenceEqual(documented.Order().ToArray(), read.Order().ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The --bit-Overlay-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitOverlayShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every Overlay below
        // it: a declaration on the Overlay would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitOverlayShouldFallBackToTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("z-index: var(--bit-Overlay-z-index, #{$zindex-overlay});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ovl {"));
        Assert.Contains("background-color: var(--bit-Overlay-background, #{$clr-bg-overlay});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ovl-mfl {"));
        Assert.Contains("var(--bit-Overlay-transition-duration, #{$mot-duration-short})", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ovl {"));
    }

    [TestMethod]
    public void BitOverlayAbsolutePositionShouldTakeTheCornersOfItsContainer()
    {
        var absolute = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-ovl-abs {");

        Assert.Contains("border-radius: inherit;", absolute);
        Assert.Contains("z-index: unset;", absolute);
    }

    [TestMethod]
    public void BitOverlayShouldPlaceItsContentForEveryPosition()
    {
        // Every class the Position parameter maps to has a rule of its own.
        var stylesheet = ReadStylesheet();

        foreach (var cls in new[] { "tlf", "tcr", "trg", "tst", "ten", "clf", "ctr", "crg", "cst", "cen", "blf", "bcr", "brg", "bst", "ben" })
        {
            Assert.Contains($".bit-ovl-{cls}", stylesheet);
        }

        // The physical ones are pinned back to their own side in a right-to-left layout.
        Assert.Contains(".bit-ovl-tlf:is(.bit-rtl, :dir(rtl))", stylesheet);
        Assert.Contains(".bit-ovl-brg:is(.bit-rtl, :dir(rtl))", stylesheet);
    }

    [TestMethod]
    public void BitOverlayShouldNeverHardCodeMotion()
    {
        // The fade reads the motion tokens, which is what collapses it under reduced motion.
        var stylesheet = ReadStylesheet();

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\b(ease|ease-in|ease-out|ease-in-out|cubic-bezier)\b"));
        Assert.Contains("$mot-duration-short", stylesheet);
    }

    [TestMethod]
    public void BitOverlayShouldHandTheSelectabilityBackToTheChildrenOfItsContent()
    {
        // The layer is user-select:none, and the content box is display:contents - which Firefox skips when it resolves
        // a child's user-select:auto, so the children are handed it themselves, at no weight a consumer's rule loses to.
        var stylesheet = ReadStylesheet();

        Assert.Contains("user-select: none;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ovl {"));
        Assert.Contains("user-select: text;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ovl-cnt {"));
        Assert.Contains("user-select: text;", SourceFiles.GetScssBlock(stylesheet, "\n:where(.bit-ovl-cnt) > * {"));
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Overlay", "BitOverlay.scss");
}
