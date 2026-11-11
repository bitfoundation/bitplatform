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
        Assert.Contains("--bit-grd-cols: max(1, var(--bit-Grid-columns, 12));", root);
        Assert.Contains("--bit-grd-cgap: var(--bit-Grid-horizontal-spacing, var(--bit-Grid-spacing, #{spacing(0.5)}));", root);
        Assert.Contains("--bit-grd-rgap: var(--bit-Grid-vertical-spacing, var(--bit-Grid-spacing, #{spacing(0.5)}));", root);

        // What the component writes into an inline style starts out unset on every root, so a grid nested in another
        // one never takes the outer one's minimum item width for its own.
        Assert.Contains("--bit-grd-mnw: initial;", root);
    }

    [TestMethod]
    public void BitGridShouldOnlyWriteTheColumnCountInlineWhenItWasAskedFor()
    {
        // A grid left at the default follows the public --bit-Grid-columns variable the root rule falls back to; one
        // given a count writes it under the same private name, which wins on its element over the root rule.
        var defaulted = RenderComponent<BitGrid>();

        Assert.DoesNotContain("--bit-grd-cols", defaulted.Find(".bit-grd").GetAttribute("style")!);

        var counted = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 6);
        });

        Assert.Contains("--bit-grd-cols:6", counted.Find(".bit-grd").GetAttribute("style")!);

        // Both sides clamp a count below one the same way, since every width is divided by it.
        var clamped = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 0);
        });

        Assert.Contains("--bit-grd-cols:1", clamped.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldOnlyWriteTheSpacingInlineWhenItWasAskedFor()
    {
        var defaulted = RenderComponent<BitGrid>();

        var style = defaulted.Find(".bit-grd").GetAttribute("style")!;

        Assert.DoesNotContain("--bit-grd-cgap", style);
        Assert.DoesNotContain("--bit-grd-rgap", style);

        var spaced = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "1rem");
            parameters.Add(p => p.VerticalSpacing, "2rem");
        });

        style = spaced.Find(".bit-grd").GetAttribute("style")!;

        // The spacing of one axis outranks the spacing of both, exactly as the public variables do on the root rule.
        Assert.Contains("--bit-grd-cgap:1rem", style);
        Assert.Contains("--bit-grd-rgap:2rem", style);
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
        Assert.Contains($"{property}-xs: var({property});", root);

        for (var i = 1; i < Breakpoints.Length; i++)
        {
            Assert.Contains($"{property}-{Breakpoints[i]}: var({property}-{Breakpoints[i - 1]});", root);
        }
    }

    [TestMethod]
    public void BitGridShouldHandTheGapOfTheBreakpointBeingPaintedToTheItems()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-grd {");

        // The width of every item is worked out from the room the gaps of its row take up, so the container and its
        // items read the one property the media queries point at whichever breakpoint currently applies.
        Assert.Contains("--bit-grd-cgap-now: var(--bit-grd-cgap-xs);", root);
        Assert.Contains("--bit-grd-rgap-now: var(--bit-grd-rgap-xs);", root);
        Assert.Contains("row-gap: var(--bit-grd-rgap-now);", root);
        Assert.Contains("column-gap: var(--bit-grd-cgap-now);", root);

        Assert.Contains("--bit-grd-cgap-now: var(--bit-grd-cgap-#{$name});", stylesheet);
        Assert.Contains("--bit-grd-rgap-now: var(--bit-grd-rgap-#{$name});", stylesheet);

        var item = SourceFiles.GetScssBlock(stylesheet, "\n.bit-grd-itm {");

        // The base of the span chain is inherited from the grid, so an item that sets no span of its own follows the
        // Span of its container, and one rendered outside a grid stays at a single column.
        Assert.Contains("--bit-grd-xs: var(--bit-grd-span, 1);", item);

        // The offset and the order are per item, so their bases are declared rather than defaulted through a var()
        // fallback, which keeps an enclosing item's from reaching a nested grid.
        Assert.Contains("--bit-grd-off: 0;", item);
        Assert.Contains("--bit-grd-ord: 0;", item);
    }

    [TestMethod]
    public void BitGridShouldWriteTheSpanItsItemsInheritOnEveryRoot()
    {
        // The default span is always written, so a grid nested in another one never inherits the outer one's Span.
        var defaulted = RenderComponent<BitGrid>();

        Assert.Contains("--bit-grd-span:1", defaulted.Find(".bit-grd").GetAttribute("style")!);

        var spanned = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Span, 3);
        });

        Assert.Contains("--bit-grd-span:3", spanned.Find(".bit-grd").GetAttribute("style")!);
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
        Assert.HasCount(4, documented, $"The header documents {string.Join(", ", documented)}.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}", code, $"{name} is documented but never read.");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Grid", "BitGrid.scss");
}
