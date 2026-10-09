using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

/// <summary>
/// Pins the order the dropdown reads its public --bit-Dropdown-* variables in, which a bUnit render cannot see: an
/// explicit Color, Size or Transparent wins over the variable that restyles what it sets - in the field and in the
/// callout, which is rendered outside the root and resolves the same mixin - and the variable only restyles the
/// default an unset one stands for.
/// </summary>
[TestClass]
public class BitDropdownStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Dropdown", "BitDropdown.scss");

    [TestMethod]
    public void BitDropdownShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var vars = SourceFiles.GetScssBlock(stylesheet, "@mixin drp-vars {");

        // An explicit Size publishes the --bit-drp-sz-* values, so they are read before the variable, which only
        // restyles the medium dropdown an unset one stands for.
        StringAssert.Contains(vars, "--bit-drp-fs: var(--bit-drp-sz-fs, var(--bit-Dropdown-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(vars, "--bit-drp-h: var(--bit-drp-sz-h, var(--bit-Dropdown-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(vars, "--bit-drp-itm-h: var(--bit-drp-sz-itm-h, var(--bit-Dropdown-item-height, #{$siz-item-md}));");
        StringAssert.Contains(vars, "--bit-drp-itm-fs: var(--bit-drp-sz-itm-fs, var(--bit-Dropdown-item-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(vars, "--bit-drp-ico: var(--bit-drp-sz-ico, var(--bit-Dropdown-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-drp-sz-fs, var(--bit-Dropdown-label-font-size, var(--bit-drp-fs)));");

        // So does an explicit Color, whose role class publishes the --bit-drp-role-* values.
        StringAssert.Contains(vars, "--bit-drp-clr: var(--bit-drp-role-main, var(--bit-Dropdown-accent-color, #{$clr-pri}));");
        StringAssert.Contains(vars, "--bit-drp-clr-hover: var(--bit-drp-role-hover, var(--bit-Dropdown-accent-hover-color, var(--bit-Dropdown-accent-color, #{$clr-pri-hover})));");
        StringAssert.Contains(vars, "--bit-drp-clr-text: var(--bit-drp-role-on, var(--bit-Dropdown-accent-text-color, #{$clr-pri-text}));");
        StringAssert.Contains(vars, "--bit-drp-clr-focus: var(--bit-drp-role-focus, var(--bit-Dropdown-focus-color, #{$clr-pri-focus}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-drp-role-main, var(--bit-Dropdown-header-color, var(--bit-drp-clr)));");

        // And Transparent, over the background of the field.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-drp-bg, var(--bit-Dropdown-background, #{$clr-bg-pri}));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Dropdown-[a-z-]+, var\(--bit-drp-(sz-|role-|bg\b)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitDropdownShouldPublishItsColorSizeAndTransparentOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var reset = SourceFiles.GetScssBlock(stylesheet, "@mixin drp-reset {");

        // A dropdown can sit in a template of another one, and its callout in another one's callout before it is
        // moved to the body, so the root and the callout each start the values the Color, Size and Transparent
        // classes publish out unset; the classes - declared further down at the same weight - still win on the
        // element that carries them.
        foreach (var property in new[] { "--bit-drp-sz-fs", "--bit-drp-sz-h", "--bit-drp-sz-lbl-lh", "--bit-drp-sz-itm-h", "--bit-drp-sz-itm-fs",
                                         "--bit-drp-sz-ico", "--bit-drp-sz-sel", "--bit-drp-role-main", "--bit-drp-role-on", "--bit-drp-role-hover",
                                         "--bit-drp-role-focus", "--bit-drp-bg" })
        {
            StringAssert.Contains(reset, $"{property}: initial;");
        }

        foreach (var start in new[] { "\n.bit-drp {", "\n.bit-drp-cal {" })
        {
            var declarations = SourceFiles.GetScssDeclarations(stylesheet, start);

            StringAssert.Contains(declarations, "@include drp-reset;");
            StringAssert.Contains(declarations, "@include drp-vars;");
            Assert.IsFalse(declarations.Contains("@include drp-size-medium;"), $"{start.Trim()} publishes the medium size whether or not it was asked for.");

            Assert.IsTrue(stylesheet.IndexOf(start, StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-drp-md {", StringComparison.Ordinal), "The size classes come before the element that resets them.");
            Assert.IsTrue(stylesheet.IndexOf(start, StringComparison.Ordinal) < stylesheet.IndexOf(".bit-drp-#{$role} {", StringComparison.Ordinal), "The role classes come before the element that resets them.");
        }
    }
}
