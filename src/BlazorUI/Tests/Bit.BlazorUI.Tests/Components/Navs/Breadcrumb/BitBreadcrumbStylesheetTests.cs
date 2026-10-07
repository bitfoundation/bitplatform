using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

/// <summary>
/// Pins the order the stylesheet reads the public --bit-Breadcrumb-* variables in, which a bUnit render cannot see: a
/// parameter written on the breadcrumb wins over the variable that restyles what it sets, and the values the Color and
/// Size classes publish start out unset on every root and callout.
/// </summary>
[TestClass]
public class BitBreadcrumbStylesheetTests
{
    [TestMethod]
    public void BitBreadcrumbShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // breadcrumb an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-brc-fs: var(--bit-brc-sz-fs, var(--bit-Breadcrumb-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-brc-h: var(--bit-brc-sz-h, var(--bit-Breadcrumb-item-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-brc-div-sz: var(--bit-brc-sz-div, var(--bit-Breadcrumb-divider-size, #{$tg-fs-md}));");
        StringAssert.Contains(stylesheet, "--bit-brc-ofi-h: var(--bit-brc-sz-ofi-h, var(--bit-Breadcrumb-overflow-item-height, #{$siz-item-md}));");
        StringAssert.Contains(stylesheet, "--bit-brc-ofi-fs: var(--bit-brc-sz-ofi-fs, var(--bit-Breadcrumb-overflow-font-size, #{$tg-fs-sm}));");

        // So does an explicit Color, for every color of the trail it paints; an unset one is the neutral trail.
        StringAssert.Contains(stylesheet, "--bit-brc-clr: var(--bit-brc-role-main, var(--bit-Breadcrumb-color, #{$clr-fg-pri}));");
        StringAssert.Contains(stylesheet, "--bit-brc-sel-clr: var(--bit-brc-role-main, var(--bit-Breadcrumb-selected-color, ");
        StringAssert.Contains(stylesheet, "--bit-brc-div-clr: var(--bit-brc-role-main, var(--bit-Breadcrumb-divider-color, #{$clr-fg-sec}));");
        StringAssert.Contains(stylesheet, "--bit-brc-fcs-clr: var(--bit-brc-role-focus, var(--bit-Breadcrumb-focus-color, #{$clr-pri-focus}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-brc-role-main, var(--bit-Breadcrumb-hover-color, ");

        // The hover color still falls back to the color the item has at rest (--bit-brc-itm-clr, --bit-brc-div-clr),
        // which is no parameter's own value, so only the size and role values are banned behind a public variable.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Breadcrumb-[a-z-]+, var\(--bit-brc-(sz|role)-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitBreadcrumbShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var vars = SourceFiles.GetScssBlock(stylesheet, "\n@mixin brc-vars {");

        // A breadcrumb never inherits another one's Color or Size from an ancestor carrying those classes: the root and
        // the callout - both include brc-vars - start the values they publish out unset, and the classes, declared
        // further down at the same weight, still win on the element that carries them.
        foreach (var property in new[] { "--bit-brc-sz-fs", "--bit-brc-sz-h", "--bit-brc-sz-div", "--bit-brc-sz-obt", "--bit-brc-sz-ofi-h",
                                         "--bit-brc-sz-ofi-fs", "--bit-brc-sz-ofi-lh", "--bit-brc-role-main", "--bit-brc-role-focus",
                                         "--bit-brc-itm-max-width" })
        {
            StringAssert.Contains(vars, $"{property}: initial;");
        }

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-brc {"), "@include brc-vars;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-brc-cal {"), "@include brc-vars;");

        var callout = stylesheet.IndexOf("\n.bit-brc-cal {", System.StringComparison.Ordinal);
        Assert.IsTrue(callout < stylesheet.IndexOf("\n    .bit-brc-#{$role} {", System.StringComparison.Ordinal), "The color classes are declared ahead of the rules that reset them.");
        Assert.IsTrue(callout < stylesheet.IndexOf("\n.bit-brc-md {", System.StringComparison.Ordinal), "The size classes are declared ahead of the rules that reset them.");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "Breadcrumb", "BitBreadcrumb.scss");
}
