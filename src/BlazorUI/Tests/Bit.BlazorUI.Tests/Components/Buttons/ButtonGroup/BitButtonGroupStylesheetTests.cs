using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ButtonGroup;

/// <summary>
/// Pins the public --bit-ButtonGroup-* variables against the stylesheet that reads them, which a bUnit render cannot
/// see: every variable the header documents is read with a fallback and never declared (so it keeps inheriting from
/// :root and the ancestors), and each one ranks below the parameter written on the group itself.
/// </summary>
[TestClass]
public partial class BitButtonGroupStylesheetTests
{
    [TestMethod]
    public void BitButtonGroupShouldReadEveryPublicVariableItDocumentsWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");
        CollectionAssert.AreEquivalent(documented, read, "The variables the header documents are not the ones the stylesheet reads.");

        Assert.IsFalse(DeclaredVariable().IsMatch(SourceFiles.StripScssComments(stylesheet)), "A public --bit-ButtonGroup-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitButtonGroupShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these on the group, so the buttons read them before the variable, which only
        // restyles the medium group an unset one stands for.
        StringAssert.Contains(stylesheet, "padding: var(--bit-btg-itm-padding, var(--bit-ButtonGroup-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "min-height: var(--bit-btg-itm-min-height, var(--bit-ButtonGroup-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-btg-itm-fs: var(--bit-btg-itm-fontsize, var(--bit-ButtonGroup-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-btg-itm-ics: var(--bit-btg-itm-iconsize, var(--bit-ButtonGroup-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-btg-itm-bdg-fs, var(--bit-ButtonGroup-badge-font-size, #{$tg-fs-xs}));");
        StringAssert.Contains(stylesheet, "padding: var(--bit-btg-itm-pad-y, var(--bit-ButtonGroup-padding, #{$siz-ctrl-pad-y-md}));");

        // Rounded is a choice as well, so its full radius outranks the public radius, on the group and on the detached
        // buttons alike.
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-btg-rad, var(--bit-ButtonGroup-radius, #{$shp-radius-button}));");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-rnd {"), "--bit-btg-rad: #{$shp-radius-full};");

        // So does an explicit Color, for every color it paints - read on the button itself, where a variable set on the
        // Style of a single button arrives, with the default of the variant after it.
        StringAssert.Contains(stylesheet, "color: var(--bit-btg-itm-clr-txt, var(--bit-ButtonGroup-color, var(--bit-btg-dfl-txt)));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btg-itm-clr-bg, var(--bit-ButtonGroup-background, var(--bit-btg-dfl-bg)));");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-btg-clr-outer, var(--bit-ButtonGroup-border-color, var(--bit-btg-dfl-outer)));");
        StringAssert.Contains(stylesheet, "--bit-btg-sep: var(--bit-btg-itm-clr-brd, var(--bit-ButtonGroup-separator-color, var(--bit-ButtonGroup-border-color, var(--bit-btg-dfl-brd))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btg-itm-clr-bg-hover, var(--bit-ButtonGroup-hover-background, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btg-clr-dark, var(--bit-ButtonGroup-selected-background, #{$clr-pri-dark}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btg-itm-clr-bg-dis, var(--bit-ButtonGroup-disabled-background, var(--bit-btg-dfl-bg-dis)));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-btg-clr-focus, var(--bit-ButtonGroup-focus-color)))");

        // The default of each variant is the primary role, and what a Color does not paint - the transparent background
        // of Outline - has no value of the Color's in front of the variable.
        var fill = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-fil {");
        StringAssert.Contains(fill, "--bit-btg-itm-clr-bg: var(--bit-btg-clr);");
        StringAssert.Contains(fill, "--bit-btg-dfl-bg: #{$clr-pri};");
        var outline = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-otl {");
        StringAssert.Contains(outline, "--bit-btg-itm-clr-bg: initial;");
        StringAssert.Contains(outline, "--bit-btg-dfl-bg: transparent;");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-ButtonGroup-[a-z-]+, var\(--bit-btg-(?!dfl-)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitButtonGroupShouldPublishItsColorSizeAndRoundedOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-btg {");

        // A group can sit in the template of another one's button, which must not inherit the outer group's Color, Size
        // or Rounded: each root starts the values those classes publish out unset, and the classes - declared further
        // down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-btg-clr", "--bit-btg-clr-txt", "--bit-btg-clr-brd", "--bit-btg-clr-hover", "--bit-btg-clr-active",
                                         "--bit-btg-clr-focus", "--bit-btg-clr-dark", "--bit-btg-clr-dark-hover", "--bit-btg-clr-dark-active",
                                         "--bit-btg-clr-dis", "--bit-btg-clr-dis-text", "--bit-btg-itm-min-height", "--bit-btg-itm-pad-y",
                                         "--bit-btg-itm-padding", "--bit-btg-itm-fontsize", "--bit-btg-itm-bdg-fs", "--bit-btg-itm-iconsize", "--bit-btg-rad" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "ButtonGroup", "BitButtonGroup.scss");

    [GeneratedRegex(@"^//\s+(--bit-ButtonGroup-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-ButtonGroup-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-ButtonGroup-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
