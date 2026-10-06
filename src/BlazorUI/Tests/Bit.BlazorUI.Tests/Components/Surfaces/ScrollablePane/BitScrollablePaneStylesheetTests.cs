using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.ScrollablePane;

/// <summary>
/// Pins the public CSS variables of the pane, which a bUnit render cannot see: they are read off the root with a
/// fallback and never declared, so a value set on an ancestor or on the Style of one pane is the one that applies.
/// </summary>
[TestClass]
public class BitScrollablePaneStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-ScrollablePane-scrollbar-size",
        "--bit-ScrollablePane-scrollbar-thumb-color",
        "--bit-ScrollablePane-scrollbar-thumb-hover-color",
        "--bit-ScrollablePane-scrollbar-thumb-active-color",
        "--bit-ScrollablePane-scrollbar-thumb-radius",
        "--bit-ScrollablePane-scrollbar-track-color",
        "--bit-ScrollablePane-fade-size",
        "--bit-ScrollablePane-focus-color",
    ];

    [TestMethod]
    public void BitScrollablePaneShouldReadEveryPublicVariableWithAFallback()
    {
        var stylesheet = ReadStylesheet();

        // The focus color is the one read without a fallback, on purpose: its absence is what hands the ring over to
        // the global --bit-shd-focus-ring (see BitScrollablePaneFocusRingShouldBeTheGlobalOneUnlessAColorIsSet).
        foreach (var variable in PublicVariables.Where(v => v != "--bit-ScrollablePane-focus-color"))
        {
            StringAssert.Contains(stylesheet, $"var({variable}, ", $"{variable} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitScrollablePaneFocusRingShouldBeTheGlobalOneUnlessAColorIsSet()
    {
        var ring = SourceFiles.GetScssBlock(ReadStylesheet(), "&:focus-visible {\n        @include focus-ring;", "The focus ring is not the default one of the library.");

        // An unset focus color leaves --bit-scp-fcr invalid, which is what makes box-shadow fall back to the global
        // composite - so an app that re-shapes --bit-shd-focus-ring re-shapes this ring too.
        StringAssert.Contains(ring, "var(--bit-ScrollablePane-focus-color);");
        StringAssert.Contains(ring, "box-shadow: var(--bit-scp-fcr, var(--bit-shd-focus-ring,");
    }

    [TestMethod]
    public void BitScrollablePaneShouldReadNoPublicVariableItDoesNotDocument()
    {
        var read = Regex.Matches(ReadStylesheet(), @"var\((--bit-ScrollablePane-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.AreEquivalent(PublicVariables, read.ToArray());
    }

    [TestMethod]
    public void BitScrollablePaneShouldNeverDeclareAPublicVariable()
    {
        // A declaration would stop the value an ancestor set from inheriting down to the pane.
        var declaration = new Regex(@"^\s*--bit-ScrollablePane-[a-z-]+\s*:", RegexOptions.Multiline);

        Assert.IsFalse(declaration.IsMatch(ReadStylesheet()), declaration.Match(ReadStylesheet()).Value);
    }

    [TestMethod]
    public void BitScrollablePaneShouldResolveThePublicVariablesOnTheRoot()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-scp {");

        // The private names are what the rest of the file reads - and what a pane styled before the public names
        // existed still sets - so they are declared once, on the root, from the public ones.
        StringAssert.Contains(root, "--bit-scp-sbs: var(--bit-ScrollablePane-scrollbar-size, ");
        StringAssert.Contains(root, "--bit-scp-sbc: var(--bit-ScrollablePane-scrollbar-thumb-color, ");
        StringAssert.Contains(root, "--bit-scp-sbch: var(--bit-ScrollablePane-scrollbar-thumb-hover-color, ");
        StringAssert.Contains(root, "--bit-scp-sbca: var(--bit-ScrollablePane-scrollbar-thumb-active-color, ");
        StringAssert.Contains(root, "--bit-scp-sbr: var(--bit-ScrollablePane-scrollbar-thumb-radius, ");
        StringAssert.Contains(root, "--bit-scp-sbt: var(--bit-ScrollablePane-scrollbar-track-color, ");
        StringAssert.Contains(root, "--bit-scp-fsz: var(--bit-ScrollablePane-fade-size, ");
    }

    [TestMethod]
    public void BitScrollablePaneModernScrollbarShouldBeRepaintedInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        // A negated guard is false in every engine that does not know the feature, which would drop the Modern bar
        // from all of them; the forced colors rules are an override that follows the rendering instead.
        Assert.IsFalse(stylesheet.Contains("not all and (forced-colors", System.StringComparison.Ordinal), "The Modern rendering is behind a negated forced-colors query.");

        var opening = "@media (forced-colors: active) {\n    .bit-scp-mod,";
        var forced = SourceFiles.GetScssBlock(stylesheet, opening, "The Modern rendering has no forced-colors override.");

        Assert.IsTrue(stylesheet.IndexOf(opening, System.StringComparison.Ordinal) > stylesheet.IndexOf("scrollbar-color: transparent transparent;", System.StringComparison.Ordinal),
                      "The forced-colors override does not come after the rendering it overrides.");

        // The idle state is matched at its own specificity, so an auto hiding bar is repainted and never hidden.
        StringAssert.Contains(forced, ".bit-scp-mod.bit-scp-ahs[data-bit-scp-idle]:not([data-bit-scp-drag]) {");
        StringAssert.Contains(forced, "background: Canvas;");
        StringAssert.Contains(forced, "background-color: CanvasText;");
    }

    [TestMethod]
    public void BitScrollablePaneFadeShouldBeSetAsideForTheFocusRing()
    {
        var stylesheet = ReadStylesheet();
        var start = stylesheet.IndexOf(".bit-scp-fad {", System.StringComparison.Ordinal);
        var fade = stylesheet[start..];

        // The mask clips the ring painted outside the border box, so a focused pane must drop it.
        StringAssert.Contains(fade, "&:focus-visible {\n            mask-image: none;");
    }

    [TestMethod]
    public void BitScrollablePaneFadeShouldBeSetAsideForMoreContrastAndForcedColors()
    {
        var stylesheet = ReadStylesheet();
        var media = stylesheet.IndexOf("@media (forced-colors: active), (prefers-contrast: more) {", System.StringComparison.Ordinal);

        Assert.IsTrue(media >= 0, "The fade is not guarded against forced colors and more contrast.");

        // A faded band draws content below its contrast, which both preferences ask not to be shown.
        StringAssert.Contains(stylesheet[media..], ".bit-scp-fad {\n            mask-image: none;");
    }

    [TestMethod]
    public void BitScrollablePaneShouldNeverPrintTheFadeAndExpandOnPrint()
    {
        var stylesheet = ReadStylesheet();
        var media = stylesheet.IndexOf("@media print {", System.StringComparison.Ordinal);

        Assert.IsTrue(media >= 0, "The pane has no print rules.");

        var print = stylesheet[media..];

        // The mask it drops is declared at the same specificity, so the print block has to come after it.
        Assert.IsTrue(media > stylesheet.IndexOf("mask-image: linear-gradient", System.StringComparison.Ordinal));
        StringAssert.Contains(print, ".bit-scp-fad {\n        mask-image: none;");

        // The sizes and the overflow are inline styles, which only !important outranks.
        StringAssert.Contains(print, ".bit-scp-eop {\n        height: auto !important;\n        max-height: none !important;\n        overflow: visible !important;");
    }

    // The header documents the variables in comments; only the rules are what the browser reads.
    private static string ReadStylesheet() => SourceFiles.StripScssComments(SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "ScrollablePane", "BitScrollablePane.scss"));
}
