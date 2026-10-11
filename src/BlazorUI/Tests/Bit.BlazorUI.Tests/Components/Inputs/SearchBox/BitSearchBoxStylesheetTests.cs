using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

/// <summary>
/// Pins the order the search box reads its values in, which a bUnit render cannot see: a value an explicit Color,
/// Background or Size publishes is read before the public --bit-SearchBox-* variable that restyles the same thing -
/// on the root and on the callout it renders outside of it - and both start those values out unset, so a nested
/// search box never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitSearchBoxStylesheetTests
{
    [TestMethod]
    public void BitSearchBoxShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // search box an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-srb-height: var(--bit-srb-siz-height, var(--bit-SearchBox-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-srb-fontsize: var(--bit-srb-siz-fontsize, var(--bit-SearchBox-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-srb-iconsize: var(--bit-srb-siz-iconsize, var(--bit-SearchBox-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "var(--bit-srb-siz-cbt-iconsize, var(--bit-SearchBox-clear-button-icon-size, ");
        StringAssert.Contains(stylesheet, "var(--bit-srb-siz-btnpad, var(--bit-SearchBox-search-button-padding, #{$siz-ctrl-pad-x-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-srb-siz-height, var(--bit-SearchBox-search-button-width, ");
        StringAssert.Contains(stylesheet, "--bit-srb-item-height: var(--bit-srb-siz-item-height, var(--bit-SearchBox-item-min-height, #{$siz-item-md}));");
        StringAssert.Contains(stylesheet, "--bit-srb-item-fontsize: var(--bit-srb-siz-item-fontsize, var(--bit-SearchBox-item-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-srb-spn-size: var(--bit-srb-siz-spn-size, var(--bit-SearchBox-spinner-size, #{$siz-icon-md}));");

        // So do an explicit Color and Background, for every color they paint.
        StringAssert.Contains(stylesheet, "var(--bit-srb-clr-bg, var(--bit-SearchBox-background, #{$clr-bg-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-srb-clr, var(--bit-SearchBox-icon-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-srb-clr-focus, var(--bit-SearchBox-focus-color)))");
        StringAssert.Contains(stylesheet, "var(--bit-srb-clr, var(--bit-SearchBox-search-button-background, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-srb-clr-txt, var(--bit-SearchBox-search-button-color, #{$clr-pri-text}))");
        StringAssert.Contains(stylesheet, "--bit-srb-dis-clr: var(--bit-srb-clr-dis-text, var(--bit-SearchBox-disabled-color, #{$clr-pri-dis-text}));");
        StringAssert.Contains(stylesheet, "--bit-srb-dis-bg: var(--bit-srb-clr-dis, var(--bit-SearchBox-disabled-background, #{$clr-pri-dis}));");
        StringAssert.Contains(stylesheet, "var(--bit-srb-clr, var(--bit-SearchBox-spinner-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-srb-clr-focus, var(--bit-SearchBox-item-selected-border-color, #{$clr-pri-focus}))");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitSearchBoxShouldPublishItsColorBackgroundAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-srb {");
        var callout = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-srb-cal {");

        // A search box can sit in a template of another one, which must not inherit the outer one's Color,
        // Background or Size: each root starts the values those classes publish out unset, and the classes - declared
        // further down at the same or a higher weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-srb-siz-height", "--bit-srb-siz-fontsize", "--bit-srb-siz-iconsize", "--bit-srb-siz-cbt-iconsize",
                                         "--bit-srb-siz-btnpad", "--bit-srb-clr", "--bit-srb-clr-txt", "--bit-srb-clr-hover", "--bit-srb-clr-active",
                                         "--bit-srb-clr-focus", "--bit-srb-clr-dis", "--bit-srb-clr-dis-text", "--bit-srb-clr-bg" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        // The callout is given the same classes, and starts out the same way wherever it happens to be rendered.
        foreach (var property in new[] { "--bit-srb-siz-item-height", "--bit-srb-siz-item-fontsize", "--bit-srb-siz-spn-size", "--bit-srb-clr", "--bit-srb-clr-focus" })
        {
            StringAssert.Contains(callout, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "SearchBox", "BitSearchBox.scss");

    [GeneratedRegex(@"var\(--bit-SearchBox-[a-z-]+, (var\(|calc\(var\()--bit-srb-")]
    private static partial Regex PublicBeforePrivate();
}
