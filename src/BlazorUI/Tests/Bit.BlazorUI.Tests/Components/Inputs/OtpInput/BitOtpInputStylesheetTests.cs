using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

/// <summary>
/// Pins the order the otp input reads its values in, which a bUnit render cannot see: a value an explicit Accent or
/// Size publishes is read before the public --bit-OtpInput-* variable that restyles the same thing, and every root
/// starts those values out unset so a nested instance never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitOtpInputStylesheetTests
{
    [TestMethod]
    public void BitOtpInputShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // box an unset one stands for.
        StringAssert.Contains(stylesheet, "width: var(--bit-otp-size, var(--bit-OtpInput-input-width, var(--bit-OtpInput-input-size, #{$siz-ctrl-md})));");
        StringAssert.Contains(stylesheet, "height: var(--bit-otp-size, var(--bit-OtpInput-input-height, var(--bit-OtpInput-input-size, #{$siz-ctrl-md})));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-otp-fontsize, var(--bit-OtpInput-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-otp-dsc-fontsize, var(--bit-OtpInput-description-font-size, #{$tg-fs-xs}));");

        // So does an explicit Accent, for every color it paints - the focused rule through the variable the box is
        // painted from at rest, so an app's class on the box keeps its paint while it is focused.
        StringAssert.Contains(stylesheet, "--bit-otp-inp-sbr: var(--bit-otp-clr, var(--bit-OtpInput-focus-border-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-otp-clr-focus, var(--bit-OtpInput-outline-focus-color, var(--bit-OtpInput-focus-color))))");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-otp-clr-focus, var(--bit-OtpInput-fill-focus-color, var(--bit-OtpInput-focus-color))))");
        StringAssert.Contains(stylesheet, "focus-underline-ring(var(--bit-otp-clr-focus, var(--bit-OtpInput-text-focus-color, var(--bit-OtpInput-focus-color, #{$clr-pri-focus}))))");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-otp-clr, var(--bit-OtpInput-loader-color, #{$clr-pri}));");

        // The colors of the boxes are the Variant's recipe, which the public background and border variables re-skin
        // in every variant - only the values of the Accent and the Size are pinned here.
        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-otp-fil"),
        DataRow("outline", ".bit-otp-otl"),
        DataRow("text", ".bit-otp-txt")]
    public void BitOtpInputShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant resolves the colors of its boxes from a tier of its own ahead of every shared variable, so a theme
        // can set one look apart from the other two on :root - and the box reads only what its variant resolved.
        foreach (var name in new[] { "background", "hover-background", "filled-background", "border-color", "hover-border-color", "filled-border-color" })
        {
            StringAssert.Contains(block, $"var(--bit-OtpInput-{variant}-{name}, var(--bit-OtpInput-{name}, ",
                                  $"{selector} does not read --bit-OtpInput-{variant}-{name} ahead of --bit-OtpInput-{name}.");
        }

        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-OtpInput-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // So do the focus rings, which the variants draw apart (a ring, or an underline for Text).
        StringAssert.Contains(stylesheet, $"var(--bit-OtpInput-{variant}-focus-color, var(--bit-OtpInput-focus-color");
        StringAssert.Contains(stylesheet, $"var(--bit-OtpInput-{variant}-invalid-focus-color, var(--bit-OtpInput-invalid-focus-color, #{{$clr-err-focus}})");
    }

    [TestMethod]
    public void BitOtpInputShouldPublishItsAccentAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-otp {");

        // Each root starts the values the Accent and Size classes publish out unset, so nothing is inherited from an
        // outer instance, and the classes - declared further down at the same weight - still win on the root that
        // carries them.
        foreach (var property in new[] { "--bit-otp-size", "--bit-otp-fontsize", "--bit-otp-dsc-fontsize", "--bit-otp-clr", "--bit-otp-clr-focus" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "OtpInput", "BitOtpInput.scss");

    [GeneratedRegex(@"var\(--bit-OtpInput-[a-z-]+, (var\(--bit-OtpInput-[a-z-]+, )*var\(--bit-otp-(size|fontsize|dsc-fontsize|clr|clr-focus)\b")]
    private static partial Regex PublicBeforePrivate();
}
