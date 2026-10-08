using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Header;

/// <summary>
/// Pins what a bUnit render cannot see of the header: that a parameter written on it ranks above the public
/// --bit-Header-* variable restyling the same thing, and that a header nested in another one does not inherit its choice.
/// </summary>
[TestClass]
public class BitHeaderStylesheetTests
{
    [TestMethod]
    public void BitHeaderShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color or Size publishes these only while it is set, so they are read before the variable, which
        // only restyles the default an unset one stands for: the plain page surface and the medium spacing.
        StringAssert.Contains(stylesheet, "$hdr-bg: var(--bit-hdr-bg, var(--bit-Header-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "$hdr-on: var(--bit-hdr-on, var(--bit-Header-color, #{$clr-fg-pri}));");
        StringAssert.Contains(stylesheet, "$hdr-fg: var(--bit-hdr-fg, var(--bit-Header-color, #{$clr-fg-pri}));");
        StringAssert.Contains(stylesheet, "$hdr-brd: var(--bit-hdr-brd, var(--bit-Header-border-color, #{$clr-brd-pri}));");
        StringAssert.Contains(stylesheet, "$hdr-bg-dis: var(--bit-hdr-bg-dis, var(--bit-Header-disabled-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "$hdr-fg-dis: var(--bit-hdr-fg-dis, var(--bit-Header-disabled-color, #{$clr-fg-dis}));");
        StringAssert.Contains(stylesheet, "$hdr-pad-y: var(--bit-hdr-pad-y, var(--bit-Header-padding-block, #{spacing(1)}));");
        StringAssert.Contains(stylesheet, "$hdr-pad-x: var(--bit-hdr-pad-x, var(--bit-Header-padding-inline, #{spacing(3)}));");

        // The size classes publish their paddings bare, so they win over the variables.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-sm {"), "--bit-hdr-pad-y: #{spacing(0.5)};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-lg {"), "--bit-hdr-pad-x: #{spacing(4)};");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Header-[a-z-]+, var\(--bit-hdr-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitHeaderShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-hdr {");

        // A header can sit in the content of another one (a toolbar inside a card inside the page header), which must
        // not inherit the outer header's Color or Size: each root starts the values those classes publish out unset,
        // and the classes - declared further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-hdr-bg", "--bit-hdr-on", "--bit-hdr-fg", "--bit-hdr-brd", "--bit-hdr-bg-dis", "--bit-hdr-fg-dis", "--bit-hdr-pad-y", "--bit-hdr-pad-x" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Layouts", "Header", "BitHeader.scss");
}
