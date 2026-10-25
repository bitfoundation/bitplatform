using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

/// <summary>
/// Pins the order the file upload reads its public --bit-FileUpload-* variables in, which a bUnit render cannot see:
/// an explicit Color or Size wins over the variable that restyles what it sets, and the variable only restyles the
/// default an unset one stands for.
/// </summary>
[TestClass]
public class BitFileUploadStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "FileUpload", "BitFileUpload.scss");

    [TestMethod]
    public void BitFileUploadShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // file upload an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-upl-lbl-height, var(--bit-FileUpload-label-min-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-lbl-fontsize, var(--bit-FileUpload-label-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-lbl-padding, var(--bit-FileUpload-label-padding, ");
        StringAssert.Contains(stylesheet, "var(--bit-upl-ico-fontsize, var(--bit-FileUpload-label-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-itm-fontsize, var(--bit-FileUpload-item-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-prv-size, var(--bit-FileUpload-preview-size, #{spacing(5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-btn-size, var(--bit-FileUpload-action-size, #{$siz-ctrl-md}))");

        // So does an explicit Color, for every color it paints - the drag state and the tints included. The browse
        // button's rest rules read the variables its hover and press states set first, so the states move the
        // variables rather than outranking an app's class with paint of their own.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-upl-lbl-sbg, var(--bit-upl-clr, var(--bit-FileUpload-fill-label-background, var(--bit-FileUpload-label-background, #{$clr-pri}))));");
        StringAssert.Contains(stylesheet, "color: var(--bit-upl-lbl-sfg, var(--bit-upl-clr-txt, var(--bit-FileUpload-fill-label-color, var(--bit-FileUpload-label-color, #{$clr-pri-text}))));");
        StringAssert.Contains(stylesheet, "--bit-upl-lbl-sbg: var(--bit-upl-clr-hover, var(--bit-FileUpload-fill-label-hover-background, var(--bit-FileUpload-label-hover-background, #{$clr-pri-hover})));");
        StringAssert.Contains(stylesheet, "var(--bit-upl-clr-hover, var(--bit-FileUpload-outline-label-hover-background, var(--bit-FileUpload-label-hover-background, #{$clr-pri-hover})))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-clr-hover, var(--bit-FileUpload-drop-background, #{$clr-pri-hover}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-clr-tint-8, var(--bit-FileUpload-drop-area-hover-background, #{translucent($clr-pri, 8%)}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-clr-tint-10, var(--bit-FileUpload-action-hover-background, #{translucent($clr-pri, 10%)}))");
        StringAssert.Contains(stylesheet, "var(--bit-upl-clr, var(--bit-FileUpload-progress-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-upl-clr-focus, var(--bit-FileUpload-focus-color)))");

        // What a Color does not paint - the transparent fill of Outline and Text - stays the variable's alone.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-upl-lbl-sbg, var(--bit-FileUpload-outline-label-background, var(--bit-FileUpload-label-background, transparent)));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-upl-lbl-sbg, var(--bit-FileUpload-text-label-background, var(--bit-FileUpload-label-background, transparent)));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-FileUpload-[a-z-]+, var\(--bit-upl-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-upl-fil"),
        DataRow("outline", ".bit-upl-otl"),
        DataRow("text", ".bit-upl-txt")]
    public void BitFileUploadShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of every shared browse button color, so a theme can set one look
        // apart from the other two on :root.
        foreach (var name in new[] { "label-color", "label-background", "label-border-color", "label-hover-color", "label-hover-background",
                                     "label-active-background", "disabled-background", "disabled-border-color" })
        {
            StringAssert.Contains(block, $"var(--bit-FileUpload-{variant}-{name}, var(--bit-FileUpload-{name}, ",
                                  $"{selector} does not read --bit-FileUpload-{variant}-{name} ahead of --bit-FileUpload-{name}.");
        }

        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-FileUpload-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // The ShowDropArea panel, drawn the same in every variant, reads the disabled colors its variant resolved - on the
        // label itself, never on the root, so a public variable set on Styles.Label still reaches it.
        StringAssert.DoesNotMatch(SourceFiles.GetScssDeclarations(stylesheet, $"\n{selector} {{"), new Regex(@"--bit-upl-dis-(bg|brd)\s*:"),
                                  $"{selector} resolves the disabled colors on the root, out of reach of the label's own style.");
        var label = SourceFiles.GetScssDeclarations(block, ".bit-upl-lbl {");
        StringAssert.Contains(label, "--bit-upl-dis-bg: ");
        StringAssert.Contains(label, "--bit-upl-dis-brd: ");

        var dropArea = SourceFiles.GetScssBlock(stylesheet, "\n.bit-upl.bit-upl-dra {");
        StringAssert.Contains(dropArea, "border-color: var(--bit-upl-dis-brd, #{$clr-brd-dis});");
        StringAssert.Contains(dropArea, "background-color: var(--bit-upl-dis-bg, #{$clr-bg-dis});");
    }

    [TestMethod]
    public void BitFileUploadShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-upl {");

        // A file upload can sit in a template of another one, which must not inherit the outer one's Color or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at
        // the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-upl-clr", "--bit-upl-clr-txt", "--bit-upl-clr-hover", "--bit-upl-clr-active", "--bit-upl-clr-focus",
                                         "--bit-upl-clr-tint-8", "--bit-upl-clr-tint-10", "--bit-upl-lbl-height", "--bit-upl-lbl-fontsize",
                                         "--bit-upl-lbl-padding", "--bit-upl-bab-height", "--bit-upl-bab-padding", "--bit-upl-itm-fontsize",
                                         "--bit-upl-fs-fontsize", "--bit-upl-prv-size", "--bit-upl-btn-size", "--bit-upl-ico-fontsize" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-upl {", StringComparison.Ordinal) < stylesheet.IndexOf(".bit-upl-#{$role} {", StringComparison.Ordinal), "The role classes come before the root that resets them.");
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-upl {", StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-upl-md {", StringComparison.Ordinal), "The size classes come before the root that resets them.");

        // The tints are published by the role classes, so they are unset along with the rest of an unset Color.
        var roles = SourceFiles.GetScssBlock(stylesheet, ".bit-upl-#{$role} {");
        StringAssert.Contains(roles, "--bit-upl-clr-tint-8: #{translucent(role($tokens, main), 8%)};");
        StringAssert.Contains(roles, "--bit-upl-clr-tint-10: #{translucent(role($tokens, main), 10%)};");
    }
}
