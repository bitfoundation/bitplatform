using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

/// <summary>
/// Pins the order the file input reads its public --bit-FileInput-* variables in, which a bUnit render cannot see:
/// an explicit Color or Size wins over the variable that restyles what it sets, and the variable only restyles the
/// default an unset one stands for.
/// </summary>
[TestClass]
public class BitFileInputStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "FileInput", "BitFileInput.scss");

    [TestMethod]
    public void BitFileInputShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // The role slots resolved on the root read an explicit Color first, then the variable, then the primary role.
        StringAssert.Contains(stylesheet, "--bit-fin-c: var(--bit-fin-clr, var(--bit-FileInput-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-fin-c-txt: var(--bit-fin-clr-txt, var(--bit-FileInput-text-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-fin-c-hover: var(--bit-fin-clr-hover, var(--bit-FileInput-hover-color, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "--bit-fin-c-active: var(--bit-fin-clr-active, var(--bit-FileInput-active-color, #{$clr-pri-active}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-fin-clr-focus, var(--bit-FileInput-focus-color)))");

        // The drop indicator's own variables come after an explicit Color too.
        StringAssert.Contains(stylesheet, "var(--bit-fin-clr-txt, var(--bit-FileInput-drop-color, var(--bit-fin-c-txt)))");
        StringAssert.Contains(stylesheet, "var(--bit-fin-clr-hover, var(--bit-FileInput-drop-background, var(--bit-fin-c-hover)))");

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium file
        // input an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-fin-lbl-height, var(--bit-FileInput-label-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-fin-lbl-fontsize, var(--bit-FileInput-label-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-fin-dzn-height, var(--bit-FileInput-drop-zone-height, #{spacing(11)}))");
        StringAssert.Contains(stylesheet, "var(--bit-fin-prv-size, var(--bit-FileInput-preview-size, #{spacing(5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-fin-rbt-size, var(--bit-FileInput-remove-button-size, #{$siz-ctrl-md}))");

        // A public variable never comes before a value a parameter publishes; it may come before the role slots
        // (--bit-fin-c*), which are already resolved with the Color ahead of it.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-FileInput-[a-z-]+, var\(--bit-fin-(?!c\b)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-fin-fil", "disabled-background"),
        DataRow("fill", ".bit-fin-fil", "disabled-border-color"),
        DataRow("outline", ".bit-fin-otl", "disabled-background"),
        DataRow("outline", ".bit-fin-otl", "disabled-border-color"),
        DataRow("text", ".bit-fin-txt", "disabled-background")]
    public void BitFileInputShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string name)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of the shared disabled colors, so a theme can set one look apart
        // from the others on :root. The role colors stay shared: every variant derives its look from the same role.
        StringAssert.Contains(block, $"var(--bit-FileInput-{variant}-{name}, var(--bit-FileInput-{name}, ",
                              $"{selector} does not read --bit-FileInput-{variant}-{name} ahead of --bit-FileInput-{name}.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-FileInput-(?!{variant}-|text-color\b)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // The remove buttons, which every variant draws alike, read the disabled background their variant resolved.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-fin-rbt {"), "background-color: var(--bit-fin-dis-bg, #{$clr-bg-dis});");
    }

    [TestMethod]
    public void BitFileInputShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-fin {");

        // A file input can sit in a template of another one, which must not inherit the outer one's Color or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at
        // the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-fin-clr", "--bit-fin-clr-txt", "--bit-fin-clr-hover", "--bit-fin-clr-active", "--bit-fin-clr-focus",
                                         "--bit-fin-lbl-height", "--bit-fin-lbl-fontsize", "--bit-fin-lbl-padding", "--bit-fin-itm-fontsize",
                                         "--bit-fin-fs-fontsize", "--bit-fin-prv-size", "--bit-fin-dzn-height", "--bit-fin-dzn-padding",
                                         "--bit-fin-dzn-ico-size", "--bit-fin-rbt-size", "--bit-fin-ico-fontsize" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-fin {", StringComparison.Ordinal) < stylesheet.IndexOf(".bit-fin-#{$role} {", StringComparison.Ordinal), "The role classes come before the root that resets them.");
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-fin {", StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-fin-md {", StringComparison.Ordinal), "The size classes come before the root that resets them.");
    }
}
