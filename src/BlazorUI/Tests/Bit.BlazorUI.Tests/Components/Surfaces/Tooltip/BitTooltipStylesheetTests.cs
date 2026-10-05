using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Tooltip;

/// <summary>
/// Pins the public CSS variables of the tooltip, which a bUnit render cannot see: each one is read with a fallback and
/// never declared, is listed in the header of the stylesheet and in the table of the demo page, and ranks below the
/// parameter written on the tooltip itself.
/// </summary>
[TestClass]
public class BitTooltipStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Tooltip-background",
        "--bit-Tooltip-color",
        "--bit-Tooltip-padding",
        "--bit-Tooltip-font-size",
        "--bit-Tooltip-font-weight",
        "--bit-Tooltip-line-height",
        "--bit-Tooltip-text-align",
        "--bit-Tooltip-radius",
        "--bit-Tooltip-shadow",
        "--bit-Tooltip-max-width",
        "--bit-Tooltip-offset",
        "--bit-Tooltip-arrow-size",
        "--bit-Tooltip-z-index",
    ];

    [TestMethod]
    public void BitTooltipShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(stylesheet, @"var\((--bit-Tooltip-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitTooltipShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Surfaces", "Tooltip", "BitTooltipDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-Tooltip-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    [TestMethod]
    public void BitTooltipShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-ttp {");

        // The variables only seed the private properties on the root, which the inline style of MaxWidth, Offset,
        // ArrowSize and ZIndex and the later Size and Color classes then override on that same element.
        foreach (var (property, variable) in new[]
                 {
                     ("--bit-ttp-offset", "--bit-Tooltip-offset"),
                     ("--bit-ttp-arrow-size", "--bit-Tooltip-arrow-size"),
                     ("--bit-ttp-max-width", "--bit-Tooltip-max-width"),
                     ("--bit-ttp-zindex", "--bit-Tooltip-z-index"),
                     ("--bit-ttp-padding", "--bit-Tooltip-padding"),
                     ("--bit-ttp-fontsize", "--bit-Tooltip-font-size"),
                     ("--bit-ttp-clr-bg", "--bit-Tooltip-background"),
                     ("--bit-ttp-clr-fg", "--bit-Tooltip-color"),
                 })
        {
            StringAssert.Contains(root, $"{property}: var({variable}, ");
        }

        var sizes = stylesheet[stylesheet.IndexOf("\n.bit-ttp-sm {", System.StringComparison.Ordinal)..];
        var colors = stylesheet[stylesheet.IndexOf("@each $role, $tokens in $bit-color-roles", System.StringComparison.Ordinal)..];

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-ttp {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-ttp-sm {", System.StringComparison.Ordinal));
        StringAssert.Contains(sizes, "--bit-ttp-fontsize: #{$tg-fs-xs};");
        StringAssert.Contains(colors, "--bit-ttp-clr-bg: #{role($tokens, main)};");
    }

    [TestMethod]
    public void BitTooltipShouldNotInheritTheTextOfWhereItIsPut()
    {
        var stylesheet = ReadStylesheet();

        var surface = SourceFiles.GetScssBlock(stylesheet, "\n.bit-ttp-ctn {");

        // A tooltip in a nowrap cell, a heading or a BitLayout would otherwise take its wrapping, its type and its
        // 1.75rem line from there.
        foreach (var declaration in new[]
                 {
                     "font-family: $tg-font-family;",
                     "font-style: normal;",
                     "line-height: var(--bit-Tooltip-line-height, #{$tg-caption1-line-height});",
                     "white-space: normal;",
                     "letter-spacing: normal;",
                     "text-transform: none;",
                 })
        {
            StringAssert.Contains(surface, declaration);
        }
    }

    [TestMethod]
    public void BitTooltipArrowShouldShareTheElevationOfTheSurfaceWithoutASeam()
    {
        var stylesheet = ReadStylesheet();

        var arrow = SourceFiles.GetScssBlock(stylesheet, "\n.bit-ttp-arw {\n    position: absolute;");

        // The arrow casts the shadow of the surface (none in Material, the outline of a dark theme), and is drawn
        // above the surface so that outline is not painted across its base - cut to its outer half, so it is not
        // painted over the content either.
        StringAssert.Contains(arrow, "box-shadow: var(--bit-Tooltip-shadow, #{$box-shadow-tooltip});");
        StringAssert.Contains(arrow, "z-index: calc(var(--bit-ttp-zindex) + 1);");

        Assert.AreEqual(12, Regex.Matches(stylesheet, @"clip-path: \$arrow-clip-(br|tl|tr|bl);").Count);
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Tooltip", "BitTooltip.scss");
}
