using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PhoneInput;

/// <summary>
/// Pins the order the phone input reads its values in, which a bUnit render cannot see: a value an explicit Size,
/// Color, Background or Border publishes is read before the public --bit-PhoneInput-* variable that restyles the same
/// thing - on the root and on the callout it renders outside of it - and both start those values out unset, so a
/// nested field never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitPhoneInputStylesheetTests
{
    [TestMethod]
    public void BitPhoneInputShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // field an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-phi-fs: var(--bit-phi-sz-fs, var(--bit-PhoneInput-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-phi-lbl-fs: var(--bit-phi-sz-fs, var(--bit-PhoneInput-label-font-size, var(--bit-PhoneInput-font-size, #{$tg-fs-sm})));");
        StringAssert.Contains(stylesheet, "--bit-phi-h: var(--bit-phi-sz-h, var(--bit-PhoneInput-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-inp-pad: var(--bit-phi-sz-pad, var(--bit-PhoneInput-padding, 0 #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-drp-pad: var(--bit-phi-sz-pad, var(--bit-PhoneInput-dropdown-padding, 0 #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-flg: var(--bit-phi-sz-icon, var(--bit-PhoneInput-flag-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-itm-h: var(--bit-phi-sz-itm-h, var(--bit-PhoneInput-item-height, #{$siz-item-md}));");

        // So do an explicit Background, Border and Color - the Border kind before the main color of the Color role.
        StringAssert.Contains(stylesheet, "--bit-phi-bg: var(--bit-phi-kind-bg, var(--bit-PhoneInput-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "--bit-phi-brd: var(--bit-phi-kind-brd, var(--bit-phi-role-main, var(--bit-PhoneInput-border-color, #{$clr-brd-pri})));");
        StringAssert.Contains(stylesheet, "--bit-phi-brd-hover: var(--bit-phi-kind-brd-hover, var(--bit-phi-role-main, var(--bit-PhoneInput-hover-border-color, #{$clr-brd-pri-hover})));");
        StringAssert.Contains(stylesheet, "--bit-phi-focus: var(--bit-phi-role-focus, var(--bit-PhoneInput-focus-color));");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitPhoneInputShouldPublishItsSizeColorBackgroundAndBorderOnlyWhereTheyAreSet()
    {
        var resets = SourceFiles.GetScssBlock(ReadStylesheet(), "@mixin phi-resets {");

        // The root and the callout both start the values the Size, Color, Background and Border classes publish out
        // unset, and those classes - declared further down at the same weight - still win where they are carried.
        foreach (var property in new[] { "--bit-phi-sz-fs", "--bit-phi-sz-h", "--bit-phi-sz-pad", "--bit-phi-sz-icon", "--bit-phi-sz-itm-h",
                                         "--bit-phi-role-main", "--bit-phi-role-focus", "--bit-phi-kind-bg", "--bit-phi-kind-brd", "--bit-phi-kind-brd-hover" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-phi {"), "@include phi-resets;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-phi-cal {"), "@include phi-resets;");
    }

    [TestMethod]
    public void BitPhoneInputShouldDrawBothLabelsAtTheSameSize()
    {
        var stylesheet = ReadStylesheet();

        // The label above the field and the one heading the responsive panel are the same Label, so both read the size
        // phi-vars resolves - an explicit Size, then the label and font variables - which the callout includes too.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-phi-lbl {"), "font-size: var(--bit-phi-lbl-fs);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-phi-rlb {"), "font-size: var(--bit-phi-lbl-fs);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-phi-cal {"), "@include phi-vars;");
    }

    [TestMethod]
    public void BitPhoneInputShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();
        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(44, documented.Length, "The header does not document the forty-four public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}", $"{name} is documented but never read.");
        }

        // Every read has a fallback, so an unset variable leaves the default in place - except the focus color,
        // which is read inside the role's own fallback chain and has no default of its own there.
        foreach (var name in documented.Where(n => n != "--bit-PhoneInput-focus-color"))
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"var\({Regex.Escape(name)}\)"), $"{name} is read somewhere without a fallback.");
        }
    }

    [TestMethod]
    public void BitPhoneInputShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();
        var documented = DocumentedVariables(stylesheet);

        foreach (var name in ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct())
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitPhoneInputShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-PhoneInput-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitPhoneInputShouldWriteTheNumberLeftToRightInARightToLeftField()
    {
        var stylesheet = ReadStylesheet();

        // An input of type tel with no dir of its own is left to right by definition, so :dir() never finds it right
        // to left: the direction is read off the group it sits in, whatever made that right to left - and the box
        // itself keeps to the right hand side of the field.
        var rule = SourceFiles.GetScssBlock(stylesheet, "\n.bit-phi-fgp:dir(rtl) > .bit-phi-inp {");
        StringAssert.Contains(rule, "direction: ltr;");
        StringAssert.Contains(rule, "text-align: right;");

        // An engine without :dir() is left with the dir attribute an explicit Dir renders, through child combinators
        // only, so a nested field reading the other way is never reached.
        var fallback = SourceFiles.GetScssBlock(stylesheet, "\n@supports not selector(:dir(rtl)) {");
        StringAssert.Contains(fallback, ".bit-phi[dir=\"rtl\"] > .bit-phi-fgp > .bit-phi-inp {");
        StringAssert.Contains(fallback, "direction: ltr;");
        StringAssert.Contains(fallback, "text-align: right;");

        // The responsive panel follows the class the callout carries for an explicit Dir, or the direction it is laid out in.
        StringAssert.Contains(stylesheet, "\n.bit-phi-res:is(.bit-rtl, :dir(rtl)) {");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "PhoneInput", "BitPhoneInput.scss");

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    [GeneratedRegex(@"var\(--bit-PhoneInput-[a-z-]+, (0 )?var\(--bit-phi-(sz-[a-z-]+|role-[a-z]+|kind-[a-z-]+|fs|h|pad-x|icon)\b")]
    private static partial Regex PublicBeforePrivate();

    [GeneratedRegex(@"^//\s+(--bit-PhoneInput-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-PhoneInput-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-PhoneInput-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
