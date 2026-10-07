using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pagination;

/// <summary>
/// Pins the contract of the public --bit-Pagination-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit from :root, an ancestor or the Style of an instance), and the
/// parameters the instance asks for itself win over a value inherited from an ancestor.
/// </summary>
[TestClass]
public class BitPaginationStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Pagination-gap",
        "--bit-Pagination-color",
        "--bit-Pagination-font-size",
        "--bit-Pagination-button-size",
        "--bit-Pagination-button-radius",
        "--bit-Pagination-button-border-width",
        "--bit-Pagination-button-color",
        "--bit-Pagination-button-background",
        "--bit-Pagination-button-border-color",
        "--bit-Pagination-button-hover-color",
        "--bit-Pagination-button-hover-background",
        "--bit-Pagination-button-active-color",
        "--bit-Pagination-button-active-background",
        "--bit-Pagination-selected-color",
        "--bit-Pagination-selected-background",
        "--bit-Pagination-selected-font-weight",
        "--bit-Pagination-focus-color",
        "--bit-Pagination-input-color",
        "--bit-Pagination-input-background",
        "--bit-Pagination-input-border-color",
    ];

    [TestMethod]
    public void BitPaginationShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.IsTrue(stylesheet.Contains($"var({variable}, "), $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        // Every public variable the stylesheet reads is one the list above (and the demo page) documents.
        var read = Regex.Matches(stylesheet, @"var\((--bit-Pagination-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitPaginationShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        // The demo page's CSS variables table is the only source of these names the site and the MCP server have,
        // so a variable the stylesheet reads but the table leaves out is one nobody gets to know about.
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Navs", "Pagination", "BitPaginationDemo.razor.cs");

        var documented = Regex.Matches(demo, @"Name = ""(--bit-Pagination-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, documented);
    }

    [TestMethod]
    public void BitPaginationShouldLetRoundedWinOverAnInheritedRadius()
    {
        var stylesheet = ReadStylesheet();

        // The radius is resolved into a private variable on the root, which the Rounded class declared after it
        // replaces on the same element, so a radius inherited from an ancestor does not undo the parameter.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn {"), "--bit-pgn-radius: var(--bit-Pagination-button-radius, #{$shp-radius-control});");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn-rnd {"), "--bit-pgn-radius: #{$shp-radius-full};");

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-pgn {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-pgn-rnd {", System.StringComparison.Ordinal));

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn-btn {"), "border-radius: var(--bit-pgn-radius);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn-elp {"), "border-radius: var(--bit-pgn-radius);");
    }

    [TestMethod]
    public void BitPaginationShouldSizeEveryControlFromOneResolvedVariable()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn {"), "--bit-pgn-btn-size: var(--bit-pgn-size-btn, var(--bit-Pagination-button-size, #{$siz-ctrl-md}));");

        foreach (var size in new[] { "sm", "md", "lg" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-pgn-{size} {{");

            StringAssert.Contains(block, $"--bit-pgn-size-btn: #{{$siz-ctrl-{size}}};");
            StringAssert.Contains(block, $"--bit-pgn-pad-x: #{{$siz-ctrl-pad-x-{size}}};");
        }
    }

    [TestMethod]
    public void BitPaginationShouldNotTellTheCurrentPageApartByColorAlone()
    {
        var stylesheet = ReadStylesheet();

        // Under the Fill variant every page is filled and the current one only a shade darker, so its weight is the
        // cue that does not rest on color, and forced colors repaint it with the system selection colors.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn-sel {"), "font-weight: var(--bit-Pagination-selected-font-weight, #{$tg-fw-semibold});");
        StringAssert.Contains(stylesheet, ".bit-pgn-btn.bit-pgn-sel {\n        color: HighlightText;");
    }

    [TestMethod]
    public void BitPaginationShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // pagination an unset one stands for.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-pgn-font-size, var(--bit-Pagination-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-pgn-btn-size: var(--bit-pgn-size-btn, var(--bit-Pagination-button-size, #{$siz-ctrl-md}));");

        // So does an explicit Color, for every color it paints; what it leaves alone (the transparent fill of Outline
        // and Text) stays the variable's alone.
        StringAssert.Contains(stylesheet, "--bit-pgn-clr-btn-txt: var(--bit-pgn-clr-txt, var(--bit-Pagination-button-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-pgn-clr-btn-bg: var(--bit-pgn-clr, var(--bit-Pagination-button-background, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-pgn-clr-btn-bg: var(--bit-Pagination-button-background, transparent);");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-pgn-clr-btn-bg-hover, var(--bit-Pagination-button-hover-background, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-pgn-clr-btn-sel-bg, var(--bit-Pagination-selected-background, #{$clr-pri-dark}));");
        StringAssert.Contains(stylesheet, "@include focus-ring(var(--bit-pgn-clr-fcs, var(--bit-Pagination-focus-color, #{$clr-pri-focus})));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Pagination-[a-z-]+, var\(--bit-pgn-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitPaginationShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pgn {");

        // A pagination never inherits another one's Color or Size from an ancestor carrying those classes: each root
        // starts the values they publish out unset, and the classes - declared further down at the same weight - still
        // win on the root that carries them.
        foreach (var property in new[] { "--bit-pgn-clr-fcs", "--bit-pgn-clr-txt", "--bit-pgn-clr", "--bit-pgn-clr-hover", "--bit-pgn-clr-active",
                                         "--bit-pgn-clr-sel", "--bit-pgn-clr-sel-hover", "--bit-pgn-clr-sel-active", "--bit-pgn-clr-dis-bg",
                                         "--bit-pgn-clr-dis-text", "--bit-pgn-size-btn", "--bit-pgn-font-size", "--bit-pgn-pad-x" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        // While Color is unset, a disabled control still takes the disabled colors of the primary role it stands for.
        StringAssert.Contains(root, "--bit-pgn-clr-dis: var(--bit-pgn-clr-dis-bg, #{$clr-pri-dis});");
        StringAssert.Contains(root, "--bit-pgn-clr-txt-dis: var(--bit-pgn-clr-dis-text, #{$clr-pri-dis-text});");

        var rootAt = stylesheet.IndexOf("\n.bit-pgn {", System.StringComparison.Ordinal);
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n    .bit-pgn-#{$role} {", System.StringComparison.Ordinal), "The color classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n.bit-pgn-md {", System.StringComparison.Ordinal), "The size classes are declared ahead of the root that resets them.");
    }

    private static string ReadStylesheet()
    {
        return SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "Pagination", "BitPagination.scss");
    }
}
