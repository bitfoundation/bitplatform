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
                               .Select(File.ReadAllText)
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

        StringAssert.Contains(stylesheet, "--bit-cht-clr-focus: var(--bit-Chart-focus-color, #{$clr-pri-focus});");
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
        var forced = stylesheet[stylesheet.IndexOf("@media (forced-colors: active)", StringComparison.Ordinal)..];

        StringAssert.Contains(forced, "forced-color-adjust: none;");
        StringAssert.Contains(forced, "border: 1px solid CanvasText;");
    }

    private static string Normalize(string name)
        => name.StartsWith("--bit-Chart-series-color-", StringComparison.Ordinal) ? "--bit-Chart-series-color-N" : name;

    private static string ChartFolder() => SourceFiles.GetDirectory("Bit.BlazorUI.Extras", "Components", "Chart");

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Chart", "BitChart.scss");
}
