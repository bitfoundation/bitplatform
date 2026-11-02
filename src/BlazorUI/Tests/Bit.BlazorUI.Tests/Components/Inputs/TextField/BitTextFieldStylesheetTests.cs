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
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-tfl-clr-focus, var(--bit-TextField-focus-color)))");
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

    [TestMethod]
    public void BitTextFieldShouldTakeTheCornersOfItsPartsFromTheFrame()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // The frame is the one rule that reads the variable. Every part at an end of it inherits the corners the frame is
        // drawn with instead, so a radius of several corners, or one given to the frame through Styles or Classes, reaches
        // them too - a copy of the variable would not.
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"--bit-TextField-radius\b").Count, "A part reads the radius variable instead of inheriting the frame's corners.");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tfl-fgp {"), "border-radius: var(--bit-TextField-radius, #{$shp-radius-control});");

        // One rule per end for every part, at the weight of a single class, leaving alone an end a template holds.
        StringAssert.Contains(stylesheet, ":where(.bit-tfl:not(.bit-tfl-ptm) > .bit-tfl-wrp > .bit-tfl-fgp) > :first-child {");
        StringAssert.Contains(stylesheet, ":where(.bit-tfl:not(.bit-tfl-stm) > .bit-tfl-wrp > .bit-tfl-fgp) > :last-child {");

        // The input and the overlay under it copy the physical corners of the ghost wrapper, which has the frame's
        // direction, so an input given a direction of its own is still rounded on the side the frame is.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tfl-inp {"), "border-radius: inherit;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tfl-gho {"), "border-radius: inherit;");
    }

    [TestMethod]
    public void BitTextFieldShouldCoverAnAutofilledValueWithTheFillTheFrameHas()
    {
        var stylesheet = ReadStylesheet();
        var input = SourceFiles.GetScssBlock(SourceFiles.StripScssComments(stylesheet), "\n.bit-tfl-inp {");

        // The cover is read the way the frame's fill is, the variable the hover moves first, and reaches the middle of a
        // textarea of any size: an inset shadow only reaches in from each edge as far as its spread.
        StringAssert.Contains(input, "-webkit-box-shadow: 0 0 0 100vmax var(--bit-tfl-fgp-bg, var(--bit-tfl-clr-bg, var(--bit-TextField-background, #{$clr-bg-pri}))) inset;");

        // A disabled frame hands the cover its own fill through that same variable.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {"), "--bit-tfl-fgp-bg: var(--bit-TextField-disabled-background, #{$clr-bg-dis});");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "TextField", "BitTextField.scss");

    [GeneratedRegex(@"var\(--bit-TextField-[a-z-]+, var\(--bit-tfl-")]
    private static partial Regex PublicBeforePrivate();
}
