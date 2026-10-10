using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Shimmer;

/// <summary>
/// Pins the public CSS variables of the shimmer, which a bUnit render cannot see: each one is read with a fallback and
/// never declared, is listed in the header of the stylesheet and in the table of the demo page, and ranks below the
/// parameter written on the shimmer itself.
/// </summary>
[TestClass]
public class BitShimmerStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Shimmer-background",
        "--bit-Shimmer-color",
        "--bit-Shimmer-height",
        "--bit-Shimmer-circle-size",
        "--bit-Shimmer-radius",
        "--bit-Shimmer-gap",
        "--bit-Shimmer-last-line-width",
        "--bit-Shimmer-animation-duration",
        "--bit-Shimmer-animation-delay",
    ];

    [TestMethod]
    public void BitShimmerShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Progress", "Shimmer", "BitShimmer.scss");

        var read = Regex.Matches(stylesheet, @"var\((--bit-Shimmer-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitShimmerShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Progress", "Shimmer", "BitShimmerDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-Shimmer-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    [TestMethod]
    public void BitShimmerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Progress", "Shimmer", "BitShimmer.scss");

        // The parameters publish the private properties inline, so they are read first and the variable only
        // restyles what the shimmer did not set.
        StringAssert.Contains(stylesheet, "var(--bit-smr-hgt, var(--bit-smr-lnh, var(--bit-Shimmer-height, ");
        StringAssert.Contains(stylesheet, "var(--bit-smr-hgt, var(--bit-smr-crs, var(--bit-Shimmer-circle-size, ");
        StringAssert.Contains(stylesheet, "var(--bit-smr-gap, var(--bit-Shimmer-gap, ");
        StringAssert.Contains(stylesheet, "var(--bit-smr-llw, var(--bit-Shimmer-last-line-width, 60%))");

        // A Square or a Pill is a choice rather than a default, so it outranks the variable as well.
        StringAssert.Contains(stylesheet, "var(--bit-smr-rad, var(--bit-smr-shp, var(--bit-Shimmer-radius, ");

        // So is an explicit Size, Color or Background, whose classes publish these only while they are set.
        StringAssert.Contains(stylesheet, "var(--bit-smr-wrp-bg, var(--bit-Shimmer-background, ");
        StringAssert.Contains(stylesheet, "var(--bit-smr-bg-clr, var(--bit-Shimmer-color, ");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Shimmer-[a-z-]+, var\(--bit-smr-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitShimmerShouldResetTheInheritedSizingOnEveryNestedShimmer()
    {
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Progress", "Shimmer", "BitShimmer.scss");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-smr {");

        // A Template is built out of shimmers of its own, which must not inherit the corner of the outer shape.
        foreach (var property in new[] { "--bit-smr-hgt", "--bit-smr-gap", "--bit-smr-llw", "--bit-smr-rad", "--bit-smr-shp", "--bit-smr-dly", "--bit-smr-lnh", "--bit-smr-crs", "--bit-smr-bg-clr", "--bit-smr-wrp-bg" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }
}
