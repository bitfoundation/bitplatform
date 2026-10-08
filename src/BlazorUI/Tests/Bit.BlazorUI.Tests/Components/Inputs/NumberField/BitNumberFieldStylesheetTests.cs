using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

/// <summary>
/// Pins the order the number field reads its values in, which a bUnit render cannot see: a value an explicit Accent,
/// Background, Border or Size publishes is read before the public --bit-NumberField-* variable that restyles the same
/// thing, and every root starts those values out unset so a nested field never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitNumberFieldStylesheetTests
{
    [TestMethod]
    public void BitNumberFieldShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // field an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-nfl-height, var(--bit-NumberField-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-fontsize, var(--bit-NumberField-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-label-fontsize, var(--bit-NumberField-label-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-icon-size, var(--bit-NumberField-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-btn-width, var(--bit-NumberField-button-width, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-stack-width, var(--bit-NumberField-button-width, #{spacing(3)}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-btn-icon-size, var(--bit-NumberField-button-icon-size, #{$tg-fs-xs}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-pad-x, var(--bit-NumberField-padding-inline, #{spacing(1)}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-spn-size, var(--bit-NumberField-spinner-size, calc(var(--bit-NumberField-icon-size, #{$siz-icon-md}) + 2 * #{$siz-spinner-stroke})))");

        // So do an explicit Accent, Background and Border, for every color they paint.
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr-focus, var(--bit-NumberField-focus-color, #{$clr-pri-focus}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr, var(--bit-NumberField-icon-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr, var(--bit-NumberField-affix-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr, var(--bit-NumberField-loading-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-spn-clr, var(--bit-NumberField-spinner-color, currentcolor))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr-bg, var(--bit-NumberField-background, #{$clr-bg-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr-brd, var(--bit-NumberField-border-color, #{$clr-brd-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-nfl-clr-brd-hover, var(--bit-NumberField-hover-border-color, #{$clr-brd-pri-hover}))");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitNumberFieldShouldPublishItsAccentBackgroundBorderAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-nfl {");

        // A field can sit in a template of another one, which must not inherit the outer field's Accent, Background,
        // Border or Size: each root starts the values those classes publish out unset, and the classes - declared
        // further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-nfl-height", "--bit-nfl-fontsize", "--bit-nfl-label-fontsize", "--bit-nfl-icon-size", "--bit-nfl-btn-width",
                                         "--bit-nfl-stack-width", "--bit-nfl-btn-icon-size", "--bit-nfl-pad-x", "--bit-nfl-spn-size", "--bit-nfl-clr",
                                         "--bit-nfl-clr-focus", "--bit-nfl-clr-bg", "--bit-nfl-clr-brd", "--bit-nfl-clr-brd-hover" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        // The arc of the spinner reads the Accent before its public variable, so the disabled field hands it the grey
        // of its slot instead - the state is read before the parameter, as everywhere else.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {"), "--bit-nfl-spn-clr: currentcolor;");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "NumberField", "BitNumberField.scss");

    [GeneratedRegex(@"var\(--bit-NumberField-[a-z-]+, (var\(|calc\(var\()--bit-nfl-")]
    private static partial Regex PublicBeforePrivate();
}
