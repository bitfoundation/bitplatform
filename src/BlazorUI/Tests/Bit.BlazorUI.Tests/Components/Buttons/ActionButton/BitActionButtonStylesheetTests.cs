using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ActionButton;

/// <summary>
/// Pins the public --bit-ActionButton-* variables against the stylesheet that reads them, which a bUnit render cannot
/// see: every variable the header documents is read with a fallback and never declared (so it keeps inheriting from
/// :root and the ancestors), and each one ranks below the parameter written on the action button itself.
/// </summary>
[TestClass]
public partial class BitActionButtonStylesheetTests
{
    [TestMethod]
    public void BitActionButtonShouldReadEveryPublicVariableItDocumentsWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");
        CollectionAssert.AreEquivalent(documented, read, "The variables the header documents are not the ones the stylesheet reads.");

        Assert.IsFalse(DeclaredVariable().IsMatch(SourceFiles.StripScssComments(stylesheet)), "A public --bit-ActionButton-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitActionButtonShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // action button an unset one stands for.
        StringAssert.Contains(stylesheet, "padding: var(--bit-acb-padding, var(--bit-ActionButton-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-sm}));");
        StringAssert.Contains(stylesheet, "min-height: var(--bit-acb-min-height, var(--bit-ActionButton-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-acb-fontsize, var(--bit-ActionButton-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-acb-ico-size, var(--bit-ActionButton-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "padding: var(--bit-acb-pad-y, var(--bit-ActionButton-padding, #{$siz-ctrl-pad-y-md}));");

        // So does an explicit Color, for every color it paints - the disabled icon and focus ring included.
        StringAssert.Contains(stylesheet, "--bit-acb-ico: var(--bit-acb-clr-ico, var(--bit-ActionButton-icon-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-acb-clr-hover, var(--bit-ActionButton-hover-color, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-acb-clr-active, var(--bit-ActionButton-active-color, #{$clr-pri-active}));");
        StringAssert.Contains(stylesheet, "var(--bit-acb-clr-focus, var(--bit-ActionButton-focus-color, #{$clr-pri-focus}))");
        StringAssert.Contains(stylesheet, "--bit-acb-ico: var(--bit-acb-clr-dis-text, var(--bit-ActionButton-disabled-color, #{$clr-pri-dis-text}));");

        // What a Color does not paint stays the variable's: the neutral text, at rest and disabled, and the backgrounds.
        StringAssert.Contains(stylesheet, "color: var(--bit-ActionButton-color, #{$clr-fg-pri});");
        StringAssert.Contains(stylesheet, "color: var(--bit-ActionButton-disabled-color, #{$clr-fg-dis});");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-ActionButton-[a-z-]+, var\(--bit-acb-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitActionButtonShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-acb {");

        // An action button can sit in the content of another one, which must not inherit the outer button's Color or
        // Size: each root starts the values those classes publish out unset, and the classes - declared further down
        // at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-acb-clr-ico", "--bit-acb-clr-hover", "--bit-acb-clr-active", "--bit-acb-clr-focus", "--bit-acb-clr-dis-text",
                                         "--bit-acb-pad-y", "--bit-acb-min-height", "--bit-acb-fontsize", "--bit-acb-ico-size", "--bit-acb-padding" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "ActionButton", "BitActionButton.scss");

    [GeneratedRegex(@"^//\s+(--bit-ActionButton-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-ActionButton-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-ActionButton-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
