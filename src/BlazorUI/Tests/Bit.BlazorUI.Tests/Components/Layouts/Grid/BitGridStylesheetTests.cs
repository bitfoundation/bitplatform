using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Grid;

/// <summary>
/// Pins the contract between what the grid writes inline and the stylesheet that lays its items out, which a bUnit
/// render cannot see: the root declares the column count and the gaps of every grid from the public --bit-Grid-*
/// variables (read with a fallback and never declared, so they inherit), the inline declarations of a Columns, Spacing,
/// HorizontalSpacing or VerticalSpacing parameter win over those on the root that carries them, and every per
/// breakpoint property chains to the breakpoint below it so the component only ever declares what it was given.
/// </summary>
[TestClass]
public class BitGridStylesheetTests : BunitTestContext
{
    private static readonly string[] Breakpoints = ["xs", "sm", "md", "lg", "xl", "xxl"];

    [TestMethod]
    public void BitGridShouldDeclareItsLayoutFromThePublicVariablesOnEveryRoot()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-grd {");

        // Clamped the way the Columns parameter is, since every width is divided by the column count.
        StringAssert.Contains(root, "--bit-grd-cols: max(1, var(--bit-Grid-columns, 12));");
        StringAssert.Contains(root, "--bit-grd-cgap: var(--bit-Grid-horizontal-spacing, var(--bit-Grid-spacing, #{spacing(0.5)}));");
        StringAssert.Contains(root, "--bit-grd-rgap: var(--bit-Grid-vertical-spacing, var(--bit-Grid-spacing, #{spacing(0.5)}));");

        // What the component writes into an inline style starts out unset on every root, so a grid nested in another
        // one never takes the outer one's minimum item width for its own.
        StringAssert.Contains(root, "--bit-grd-mnw: initial;");
    }

    [TestMethod]
    public void BitGridShouldOnlyWriteTheColumnCountInlineWhenItWasAskedFor()
    {
        // A grid left at the default follows the public --bit-Grid-columns variable the root rule falls back to; one
        // given a count writes it under the same private name, which wins on its element over the root rule.
        var defaulted = RenderComponent<BitGrid>();

        Assert.IsFalse(defaulted.Find(".bit-grd").GetAttribute("style")!.Contains("--bit-grd-cols"));

        var counted = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 6);
        });

        StringAssert.Contains(counted.Find(".bit-grd").GetAttribute("style")!, "--bit-grd-cols:6");

        // Both sides clamp a count below one the same way, since every width is divided by it.
        var clamped = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 0);
        });

        StringAssert.Contains(clamped.Find(".bit-grd").GetAttribute("style")!, "--bit-grd-cols:1");
    }

    [TestMethod]
    public void BitGridShouldOnlyWriteTheSpacingInlineWhenItWasAskedFor()
    {
        var defaulted = RenderComponent<BitGrid>();

        var style = defaulted.Find(".bit-grd").GetAttribute("style")!;

        Assert.IsFalse(style.Contains("--bit-grd-cgap"));
        Assert.IsFalse(style.Contains("--bit-grd-rgap"));

        var spaced = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "1rem");
            parameters.Add(p => p.VerticalSpacing, "2rem");
        });

        style = spaced.Find(".bit-grd").GetAttribute("style")!;

        // The spacing of one axis outranks the spacing of both, exactly as the public variables do on the root rule.
        StringAssert.Contains(style, "--bit-grd-cgap:1rem");
        StringAssert.Contains(style, "--bit-grd-rgap:2rem");
    }

    [TestMethod,
        DataRow("--bit-grd-cols"),
        DataRow("--bit-grd-cgap"),
        DataRow("--bit-grd-rgap")]
    public void BitGridShouldChainEveryBreakpointToTheOneBelowIt(string property)
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-grd {");

        // Mobile first: a value set for one breakpoint keeps applying to every wider one until one of those replaces
        // it, and the component only declares the breakpoints it was given.
        StringAssert.Contains(root, $"{property}-xs: var({property});");

        for (var i = 1; i < Breakpoints.Length; i++)
        {
            StringAssert.Contains(root, $"{property}-{Breakpoints[i]}: var({property}-{Breakpoints[i - 1]});");
        }
    }

    [TestMethod]
    public void BitGridShouldHandTheGapOfTheBreakpointBeingPaintedToTheItems()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-grd {");

        // The width of every item is worked out from the room the gaps of its row take up, so the container and its
        // items read the one property the media queries point at whichever breakpoint currently applies.
        StringAssert.Contains(root, "--bit-grd-cgap-now: var(--bit-grd-cgap-xs);");
        StringAssert.Contains(root, "--bit-grd-rgap-now: var(--bit-grd-rgap-xs);");
        StringAssert.Contains(root, "row-gap: var(--bit-grd-rgap-now);");
        StringAssert.Contains(root, "column-gap: var(--bit-grd-cgap-now);");

        StringAssert.Contains(stylesheet, "--bit-grd-cgap-now: var(--bit-grd-cgap-#{$name});");
        StringAssert.Contains(stylesheet, "--bit-grd-rgap-now: var(--bit-grd-rgap-#{$name});");

        var item = SourceFiles.GetScssBlock(stylesheet, "\n.bit-grd-itm {");

        // The base of the span chain is inherited from the grid, so an item that sets no span of its own follows the
        // Span of its container, and one rendered outside a grid stays at a single column.
        StringAssert.Contains(item, "--bit-grd-xs: var(--bit-grd-span, 1);");

        // The offset and the order are per item, so their bases are declared rather than defaulted through a var()
        // fallback, which keeps an enclosing item's from reaching a nested grid.
        StringAssert.Contains(item, "--bit-grd-off: 0;");
        StringAssert.Contains(item, "--bit-grd-ord: 0;");
    }

    [TestMethod]
    public void BitGridShouldWriteTheSpanItsItemsInheritOnEveryRoot()
    {
        // The default span is always written, so a grid nested in another one never inherits the outer one's Span.
        var defaulted = RenderComponent<BitGrid>();

        StringAssert.Contains(defaulted.Find(".bit-grd").GetAttribute("style")!, "--bit-grd-span:1");

        var spanned = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Span, 3);
        });

        StringAssert.Contains(spanned.Find(".bit-grd").GetAttribute("style")!, "--bit-grd-span:3");
    }

    [TestMethod]
    public void BitGridShouldNeverDeclareAPublicVariable()
    {
        var code = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(code, @"--bit-Grid-[a-z-]+\s*:"), "A public --bit-Grid-* variable is declared by the stylesheet.");
    }

    [TestMethod]
    public void BitGridShouldReadEveryPublicVariableItsHeaderDocuments()
    {
        var stylesheet = ReadStylesheet();
        var code = SourceFiles.StripScssComments(stylesheet);

        var documented = Regex.Matches(stylesheet, @"^\s*\*\s+(?<name>--bit-Grid-[a-z-]+)\s", RegexOptions.Multiline)
                              .Select(m => m.Groups["name"].Value)
                              .Distinct()
                              .ToArray();

        // Guards the guard: a header that documents nothing would let every variable through in silence.
        Assert.AreEqual(4, documented.Length, $"The header documents {string.Join(", ", documented)}.");

        foreach (var name in documented)
        {
            StringAssert.Contains(code, $"var({name}", $"{name} is documented but never read.");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Grid", "BitGrid.scss");
}
