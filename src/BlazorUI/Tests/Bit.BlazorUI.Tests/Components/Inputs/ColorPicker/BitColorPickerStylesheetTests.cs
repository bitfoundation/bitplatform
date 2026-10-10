using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

/// <summary>
/// Pins the order the ColorPicker's stylesheet reads its sizes in, which a bUnit render cannot see: what an explicit
/// Size publishes is read before the public --bit-ColorPicker-* variable restyling it, and the medium size an unset one
/// stands for comes last.
/// </summary>
[TestClass]
public class BitColorPickerStylesheetTests
{
    private static readonly string[] SizeValues =
    [
        "--bit-clp-width-sz", "--bit-clp-sat-height-sz", "--bit-clp-track-height-sz", "--bit-clp-thumb-size-sz", "--bit-clp-button-size-sz",
        "--bit-clp-swatch-size-sz", "--bit-clp-preview-size-sz", "--bit-clp-fs-sz", "--bit-clp-fs-sub-sz", "--bit-clp-icon-size-sz",
    ];

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "ColorPicker", "BitColorPicker.scss");

    [TestMethod]
    public void BitColorPickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium size
        // an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-clp-width-sz, var(--bit-ColorPicker-width, #{spacing(33.5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-fs-sz, var(--bit-ColorPicker-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-fs-sub-sz, var(--bit-ColorPicker-caption-font-size, #{$tg-fs-xs}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-sat-height-sz, var(--bit-ColorPicker-saturation-height, #{spacing(29.5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-thumb-size-sz, var(--bit-ColorPicker-thumb-size, #{spacing(2.5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-track-height-sz, var(--bit-ColorPicker-track-height, #{spacing(2.5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-button-size-sz, var(--bit-ColorPicker-button-size, #{spacing(3)}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-icon-size-sz, var(--bit-ColorPicker-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-preview-size-sz, var(--bit-ColorPicker-preview-size, #{spacing(6)}))");
        StringAssert.Contains(stylesheet, "var(--bit-clp-swatch-size-sz, var(--bit-ColorPicker-swatch-size, #{spacing(3)}))");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-ColorPicker-[a-z-]+, var\(--bit-clp-[a-z-]+-sz"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitColorPickerShouldPublishItsSizeOnlyWhereItIsSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-clp {");

        // A picker can sit inside something that holds another one, which must not hand it its Size: each root starts
        // the values the size classes publish out unset, and the classes - declared further down at the same weight -
        // still win on the root that carries them.
        foreach (var property in SizeValues)
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-clp {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-clp-sm {", System.StringComparison.Ordinal),
                      "The size classes come before the root that resets what they publish.");
    }
}
