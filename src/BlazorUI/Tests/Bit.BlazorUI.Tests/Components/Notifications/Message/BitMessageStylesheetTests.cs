using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

/// <summary>
/// Pins what a bUnit render cannot see of the message: the color its unfilled variants put on the page as text, which
/// only exists in CSS.
/// </summary>
[TestClass]
public class BitMessageStylesheetTests
{
    [TestMethod,
        DataRow("\n.bit-msg-otl {", "outline"),
        DataRow("\n.bit-msg-txt {", "text")]
    public void BitMessageUnfilledVariantsShouldWriteTheirTextInTheRoleForeground(string variant, string name)
    {
        var stylesheet = ReadStylesheet();

        // The two variants drop the fill and leave the text on the page's own surface, so it reads the role's fg - the
        // shade meant to be read there, which for a background or border role is its on color (BitColorRoleMapsTests).
        Assert.Contains($"--bit-msg-fg: var(--bit-msg-clr-txt, var(--bit-Message-{name}-color, var(--bit-Message-color, #{{$clr-inf-fg}})));", SourceFiles.GetScssDeclarations(stylesheet, variant));
        Assert.Contains("--bit-msg-clr-txt: #{role($tokens, fg)};", SourceFiles.GetScssBlock(stylesheet, "\n    .bit-msg-#{$role} {"));
    }

    [TestMethod]
    public void BitMessageUnfilledVariantsShouldNeverWriteTheirTextInTheRoleMain()
    {
        var roles = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-msg-#{$role} {");

        // A role's main is picked to be a fill; a background role's main as text on the page is the page itself.
        Assert.DoesNotContain("--bit-msg-clr-txt: #{role($tokens, main)};", roles, "The unfilled text is the role's main.");
    }

    [TestMethod,
        DataRow("\n.bit-msg-otl {", "outline"),
        DataRow("\n.bit-msg-txt {", "text")]
    public void BitMessageTintedVariantShouldWashTheSurfaceWithTheRoleTint(string variant, string name)
    {
        var stylesheet = ReadStylesheet();

        // The tint is a faint wash of the role, picked to keep the fg text above it readable. Only the two unfilled
        // variants render it, so each writes it itself, reading its own background ahead of the shared one.
        var tinted = SourceFiles.GetScssDeclarations(SourceFiles.GetScssBlock(stylesheet, variant), "&.bit-msg-tnt {");
        Assert.Contains($"--bit-msg-bg: var(--bit-msg-clr-tint, var(--bit-Message-{name}-background, var(--bit-Message-background, #{{$clr-inf-tint}})));", tinted);
        Assert.Contains("--bit-msg-clr-tint: #{role($tokens, tint)};", SourceFiles.GetScssBlock(stylesheet, "\n    .bit-msg-#{$role} {"));
    }

    [TestMethod]
    public void BitMessageShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // message an unset one stands for.
        Assert.Contains("font-size: var(--bit-msg-fontsize, var(--bit-Message-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("font-size: var(--bit-msg-ico-fontsize, var(--bit-Message-icon-size, #{$siz-icon-md}));", stylesheet);
        Assert.Contains("height: var(--bit-msg-track-size, var(--bit-Message-progress-height, #{$siz-track-md}));", stylesheet);

        // So does an explicit Color, for every color its role paints; the transparent background of Outline and Text is
        // the variable's alone.
        var fill = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-msg-fil {");
        Assert.Contains("--bit-msg-fg: var(--bit-msg-clr, var(--bit-Message-fill-color, var(--bit-Message-color, #{$clr-inf-text})));", fill);
        Assert.Contains("--bit-msg-bg: var(--bit-msg-clr-bg, var(--bit-Message-fill-background, var(--bit-Message-background, #{$clr-inf})));", fill);
        Assert.Contains("--bit-msg-brd: var(--bit-msg-clr-bg, var(--bit-Message-fill-border-color, var(--bit-Message-border-color, #{$clr-inf})));", fill);
        Assert.Contains("--bit-msg-bg: var(--bit-Message-outline-background, var(--bit-Message-background, transparent));", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-msg-otl {"));
        Assert.Contains("--bit-msg-focus: var(--bit-msg-clr-focus, var(--bit-Message-focus-color, #{$clr-inf-focus}));", stylesheet);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Message-[a-z-]+, var\(--bit-msg-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-msg-fil"),
        DataRow("outline", ".bit-msg-otl"),
        DataRow("text", ".bit-msg-txt")]
    public void BitMessageShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of every shared color variable, so a theme can set one look apart
        // from the other two on :root - a neutral hairline around the outlined messages without a border on every filled one.
        foreach (var name in new[] { "color", "background", "border-color" })
        {
            Assert.Contains($"var(--bit-Message-{variant}-{name}, var(--bit-Message-{name}, ", block, $"{selector} does not read --bit-Message-{variant}-{name} ahead of --bit-Message-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-Message-{variant}-[a-z-]+, )var\(--bit-Message-(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Message-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod]
    public void BitMessageShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msg {");

        // A message can sit in the content of another one, which must not inherit the outer message's Color or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at the
        // same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-msg-clr", "--bit-msg-clr-bg", "--bit-msg-clr-txt", "--bit-msg-clr-focus", "--bit-msg-clr-tint",
                                         "--bit-msg-margin", "--bit-msg-fontsize", "--bit-msg-ico-margin", "--bit-msg-ico-fontsize",
                                         "--bit-msg-btn-size", "--bit-msg-btn-icosize", "--bit-msg-act-maxheight", "--bit-msg-track-size" })
        {
            Assert.Contains($"{property}: initial;", root);
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Notifications", "Message", "BitMessage.scss");
}
