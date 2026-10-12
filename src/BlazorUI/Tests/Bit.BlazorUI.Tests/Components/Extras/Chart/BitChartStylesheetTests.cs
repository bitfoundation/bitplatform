using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

/// <summary>
/// Pins what a bUnit render cannot see of the chart: its public --bit-Chart-* variables. They are read in two places -
/// the stylesheet, for the HTML chrome, and the defaults of the options, for what is drawn inside the SVG - and the
/// header comment of the stylesheet is where all of them are documented.
/// </summary>
[TestClass]
public class BitChartStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Chart-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex InterpolatedVariableRead = new(@"var\(\s*(--bit-Chart-[a-zA-Z0-9-]+)\{", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Chart-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Chart-[a-zA-Z0-9-]+)", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitChartShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var sources = Directory.GetFiles(ChartFolder(), "*.cs", SearchOption.AllDirectories)
                               .Select(SourceFiles.ReadFullPath)
                               .Append(stylesheet)
                               .ToArray();

        // The palette is read as --bit-Chart-series-color-{n}, and documented as one row for the ten of them.
        var read = sources.SelectMany(s => PublicVariableRead.Matches(s).Select(m => m.Groups[1].Value))
                          .Concat(sources.SelectMany(s => InterpolatedVariableRead.Matches(s).Select(m => m.Groups[1].Value)))
                          .Select(Normalize)
                          .ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => Normalize(m.Groups[1].Value)).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Chart-* variables the chart reads and the ones the stylesheet's header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitChartShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every chart below it.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitChartShouldFallBackToTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "--bit-cht-clr-focus: var(--bit-Chart-focus-color);");
        // The keyboard position is a stroke, so it cannot be the global ring: it reads the rings' own chain, with the
        // primary focus color as its default.
        StringAssert.Contains(stylesheet, ".bit-cht-focus-ring {\n    stroke: var(--bit-cht-clr-focus, #{$clr-pri-focus});");
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-Chart-tooltip-radius, #{$shp-radius-popup});");
        StringAssert.Contains(stylesheet, "box-shadow: var(--bit-Chart-tooltip-shadow, #{$box-shadow-tooltip});");
        StringAssert.Contains(stylesheet, "font-family: var(--bit-Chart-font-family, #{$tg-font-family});");
    }

    [TestMethod]
    public void BitChartShouldNotHardCodeAnEasingCurve()
    {
        var stylesheet = ReadStylesheet();

        Assert.DoesNotContain("ease-out", stylesheet);
        StringAssert.Contains(stylesheet, "var(--bit-cht-ease, #{$mot-easing-decelerate})");
    }

    [TestMethod]
    public void BitChartShouldKeepItsSwatchesInForcedColors()
    {
        var stylesheet = ReadStylesheet();
        var forced = SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active) {");

        StringAssert.Contains(forced, "forced-color-adjust: none;");
        StringAssert.Contains(forced, "border: 1px solid CanvasText;");
    }

    [TestMethod]
    public void BitChartShouldPlaceThePartsBesideThePlotByTheirSide()
    {
        var stylesheet = ReadStylesheet();

        // The row around the plot follows the reading direction, so Start and End need no right-to-left rule of their
        // own; only Left and Right, which keep their side, are moved back in one (#13714).
        Assert.DoesNotContain("row-reverse", stylesheet);
        StringAssert.Contains(stylesheet, ".bit-cht-#{$part}:where(.bit-cht-lft, .bit-cht-sta) {\n        order: -$step;");
        StringAssert.Contains(stylesheet, ".bit-cht-#{$part}:where(.bit-cht-rgt, .bit-cht-end) {\n        order: $step;");
        StringAssert.Contains(stylesheet, ".bit-cht-#{$part}:where(.bit-cht-lft):where(:dir(rtl), .bit-cht.bit-rtl > .bit-cht-mid > *) {\n        order: $step;");
        StringAssert.Contains(stylesheet, ".bit-cht-#{$part}:where(.bit-cht-rgt):where(:dir(rtl), .bit-cht.bit-rtl > .bit-cht-mid > *) {\n        order: -$step;");

        // A title or subtitle on the left is turned over: Left always, Start reading left to right, End reading right
        // to left.
        StringAssert.Contains(stylesheet, ".bit-cht-ttl-v:where(.bit-cht-lft, .bit-cht-sta) {\n    rotate: 180deg;");
        StringAssert.Contains(stylesheet, ".bit-cht-ttl-v:where(.bit-cht-sta):where(:dir(rtl), .bit-cht.bit-rtl > .bit-cht-mid > *) {\n    rotate: none;");
        StringAssert.Contains(stylesheet, ".bit-cht-ttl-v:where(.bit-cht-end):where(:dir(rtl), .bit-cht.bit-rtl > .bit-cht-mid > *) {\n    rotate: 180deg;");

        // The side is read through :where() alone, so these rules weigh what the part's own class weighs and a single
        // class an app hands the part through Classes still moves or turns it.
        var sideCompound = new Regex(@"\.bit-cht-[a-z#{}$]+\.bit-cht-(lft|rgt|sta|end)\b");
        CollectionAssert.AreEqual(Array.Empty<string>(), sideCompound.Matches(stylesheet).Select(m => m.Value).ToArray());
    }

    private static string Normalize(string name)
        => name.StartsWith("--bit-Chart-series-color-", StringComparison.Ordinal) ? "--bit-Chart-series-color-N" : name;

    private static string ChartFolder() => SourceFiles.GetDirectory("Bit.BlazorUI.Extras", "Components", "Chart");

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Chart", "BitChart.scss");
}
