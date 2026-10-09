using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

/// <summary>
/// Pins the order the text field reads its values in, which a bUnit render cannot see: a value an explicit Accent,
/// Background, Border or Size publishes is read before the public --bit-TextField-* variable that restyles the same
/// thing, and every root starts those values out unset so a nested field never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitTextFieldStylesheetTests
{
    [TestMethod]
    public void BitTextFieldShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // field an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-tfl-font-size, var(--bit-TextField-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-font-size, var(--bit-TextField-label-font-size, var(--bit-TextField-font-size, #{$tg-fs-sm})))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-min-height, var(--bit-TextField-min-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-line-height, var(--bit-TextField-line-height, #{spacing(2.125)}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-icon-font-size, var(--bit-TextField-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-btn-width, var(--bit-TextField-button-width, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-spn-size, var(--bit-TextField-spinner-size, calc(var(--bit-TextField-icon-size, #{$siz-icon-md}) + 2 * #{$siz-spinner-stroke})))");

        // So does an explicit Accent, Background and Border, for every color they paint.
        StringAssert.Contains(stylesheet, "var(--bit-tfl-clr, var(--bit-TextField-accent-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-clr, var(--bit-TextField-button-color, var(--bit-TextField-accent-color, #{$clr-pri})))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-clr-focus, var(--bit-TextField-focus-color, #{$clr-pri-focus}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-spn-clr, var(--bit-TextField-spinner-color, currentcolor))");
        // The frame at rest reads the variable its hover moves first, then the fill ranked as everywhere else.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tfl-fgp-bg, var(--bit-tfl-clr-bg, var(--bit-TextField-background, #{$clr-bg-pri})));");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-clr-brd, var(--bit-TextField-border-color, #{$clr-brd-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-tfl-clr-brd-hover, var(--bit-TextField-hover-border-color, #{$clr-brd-pri-hover}))");

        // The hovered fill and the focused frame are states no Background or Border kind has a value of its own for,
        // so their variables still come first and fall back to the fill and the frame ranked as above.
        StringAssert.Contains(stylesheet, "var(--bit-TextField-hover-background, var(--bit-tfl-clr-bg, var(--bit-TextField-background, #{$clr-bg-pri})))");
        StringAssert.Contains(stylesheet, "var(--bit-TextField-focus-border-color, var(--bit-tfl-clr-brd, var(--bit-TextField-border-color, #{$clr-brd-pri})))");

        var offenders = PublicBeforePrivate().Matches(stylesheet)
                                             .Select(m => m.Value)
                                             .Where(v => v.StartsWith("var(--bit-TextField-hover-background,") is false &&
                                                         v.StartsWith("var(--bit-TextField-focus-border-color,") is false)
                                             .ToArray();

        CollectionAssert.AreEqual(System.Array.Empty<string>(), offenders, "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitTextFieldShouldPublishItsAccentBackgroundBorderAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tfl {");

        // A field can sit in a template of another one, which must not inherit the outer field's Accent, Background,
        // Border or Size: each root starts the values those classes publish out unset, and the classes - declared
        // further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tfl-font-size", "--bit-tfl-line-height", "--bit-tfl-min-height", "--bit-tfl-icon-font-size",
                                         "--bit-tfl-btn-width", "--bit-tfl-spn-size", "--bit-tfl-clr", "--bit-tfl-clr-focus", "--bit-tfl-clr-bg", "--bit-tfl-clr-brd",
                                         "--bit-tfl-clr-brd-hover" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitTextFieldShouldGreyItsSpinnerWhenDisabledWhateverTheAccent()
    {
        var disabled = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &.bit-dis {");

        // The arc reads the Accent before its public variable, so the disabled field hands it the grey of its slot
        // instead - the state is read before the parameter, as everywhere else.
        StringAssert.Contains(disabled, "--bit-tfl-spn-clr: currentcolor;");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "TextField", "BitTextField.scss");

    [GeneratedRegex(@"var\(--bit-TextField-[a-z-]+, var\(--bit-tfl-")]
    private static partial Regex PublicBeforePrivate();
}
