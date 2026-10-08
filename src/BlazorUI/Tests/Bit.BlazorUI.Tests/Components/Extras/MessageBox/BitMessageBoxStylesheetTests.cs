using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MessageBox;

/// <summary>
/// Pins the public --bit-MessageBox-* variables and the theme decisions of the stylesheet a bUnit render cannot see:
/// every variable the header documents is read with a fallback, none of them is ever declared (so they keep
/// inheriting from :root), nothing is read that the header does not document, and the inset, the title type, the
/// alignment and the width come from the dialog tokens of the theme.
/// </summary>
[TestClass]
public partial class BitMessageBoxStylesheetTests
{
    [TestMethod]
    public void BitMessageBoxShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(15, documented.Length, "The stylesheet does not document the fifteen public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-MessageBox-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitMessageBoxShouldFollowTheDialogTokensOfTheTheme()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb {");

        StringAssert.Contains(root, "var(--bit-msb-pad, var(--bit-MessageBox-padding, #{$spa-dialog}))");
        StringAssert.Contains(root, "var(--bit-msb-ttl-fontsize, var(--bit-MessageBox-title-font-size, #{$tg-dialog-title-font-size}))");
        StringAssert.Contains(root, "var(--bit-MessageBox-text-align, #{$layout-dialog-text-align})");

        // The dialog's ceiling is the default only in a modal; inline, the box fills its container as it always has.
        StringAssert.Contains(root, "var(--bit-MessageBox-max-width, 100%)");
        StringAssert.Contains(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-mdl-ctn > .bit-msb {"), "var(--bit-MessageBox-max-width, #{$siz-dialog-max-width})");

        // The floor never passes the ceiling, which a narrow preset (Cupertino's 270px alert) would otherwise do.
        StringAssert.Contains(root, "min-width: min(");

        var title = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb-ttl {");

        StringAssert.Contains(title, "var(--bit-MessageBox-title-font-weight, #{$tg-dialog-title-font-weight})");
        StringAssert.Contains(title, "text-align: var(--bit-msb-text-align);");

        // Text colors come from the foreground tokens, which the forced-colors palette maps; a role color does not.
        Assert.IsFalse(ReadStylesheet().Contains("$clr-ter"), "The text is painted in a role color rather than a foreground token.");

        StringAssert.Contains(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb-ftr {"), "justify-content: var(--bit-MessageBox-actions-justify, #{$layout-dialog-actions-justify});");
    }

    [TestMethod]
    public void BitMessageBoxShouldFitTheModalItIsShownIn()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb {");

        // The room a modal leaves its content is the screen less its accent border and its offset; a box as tall as
        // the screen makes the modal scroll it by those few pixels on top of the body scrolling inside it.
        StringAssert.Contains(root, "var(--bit-Modal-border-width, #{spacing(0.5)})");
        StringAssert.Contains(root, "(2 * var(--bit-Modal-offset, 0px))");
        StringAssert.Contains(root, "max-height: var(--bit-msb-max-height);");

        // The container shrinks inside the root's padding by layout rather than by subtracting the padding, which a
        // two-value --bit-MessageBox-padding would turn into an invalid calc().
        StringAssert.Contains(root, "flex-direction: column;");
        StringAssert.Contains(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb-con {"), "min-height: 0;");
        Assert.IsFalse(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb-con {").Contains("--bit-MessageBox-padding"), "The container's height is derived from the public padding.");
    }

    [TestMethod]
    public void BitMessageBoxShouldBalanceACenteredTitleAgainstTheCloseButton()
    {
        var query = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@container style(--bit-msb-text-align: center) {");

        StringAssert.Contains(query, ".bit-msb-hdr:has(> .bit-btn) > .bit-msb-ttl:first-child {");
        StringAssert.Contains(query, "padding-inline-start: calc(var(--bit-msb-cls-size, #{$siz-ctrl-md}) + #{spacing(2)});");

        // The spacer is collapsed where a title fills the header, or a centered title would share the room with it.
        StringAssert.Contains(ReadStylesheet(), ".bit-msb-ttl ~ .bit-msb-spc {");
    }

    [TestMethod]
    public void BitMessageBoxShouldKeepSmallerThanMediumThanLarge()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-msb-sm {"), "--bit-msb-ttl-fontsize: calc(#{$tg-dialog-title-font-size} * 0.8);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-msb-lg {"), "--bit-msb-ttl-fontsize: calc(#{$tg-dialog-title-font-size} * 1.2);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-msb-sm {"), "--bit-msb-pad: calc(#{$spa-dialog} * 0.667);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-msb-lg {"), "--bit-msb-pad: calc(#{$spa-dialog} * 1.333);");
    }

    [TestMethod]
    public void BitMessageBoxShouldPrintAllOfItsBody()
    {
        var print = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media print {");

        StringAssert.Contains(print, "max-height: none !important;");
        StringAssert.Contains(print, "overflow: visible !important;");
    }

    [TestMethod]
    public void BitMessageBoxShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size or Color publishes these, so they are read before the variable, which only restyles the
        // medium message box (and the text-colored icon) an unset one stands for.
        StringAssert.Contains(stylesheet, "padding: var(--bit-msb-pad, var(--bit-MessageBox-padding, #{$spa-dialog}));");
        StringAssert.Contains(stylesheet, "min-width: min(var(--bit-msb-min-width, var(--bit-MessageBox-min-width, #{spacing(40)})), var(--bit-msb-max-width));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-msb-ico-fontsize, var(--bit-MessageBox-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-msb-fontsize, var(--bit-MessageBox-body-font-size, #{$tg-fs-md}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-msb-clr, var(--bit-MessageBox-icon-color, currentcolor));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-MessageBox-[a-z-]+, var\(--bit-msb-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitMessageBoxShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-msb {");

        // A message box can sit in the body of another one, which must not inherit the outer box's Color or Size: each
        // root starts the values those classes publish out unset, and the classes - declared further down at the same
        // weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-msb-pad", "--bit-msb-min-width", "--bit-msb-fontsize", "--bit-msb-ttl-fontsize",
                                         "--bit-msb-ico-fontsize", "--bit-msb-cls-size", "--bit-msb-clr" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitMessageBoxShouldKeepItsIconInForcedColors()
    {
        StringAssert.Contains(SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {"), "color: CanvasText;");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "MessageBox", "BitMessageBox.scss");

    [GeneratedRegex(@"^//\s+(--bit-MessageBox-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-MessageBox-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-MessageBox-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
