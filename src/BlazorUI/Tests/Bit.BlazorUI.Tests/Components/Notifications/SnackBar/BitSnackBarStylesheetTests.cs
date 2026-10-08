using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.SnackBar;

/// <summary>
/// Pins what a bUnit render cannot see of the snack bar: the public --bit-SnackBar-* custom properties, which are
/// read off the stylesheet with a fallback and never declared there so they inherit, and the parts of the layout
/// and the colors that only exist in CSS.
/// </summary>
[TestClass]
public class BitSnackBarStylesheetTests
{
    private static readonly string[] _publicVariables =
    [
        "--bit-SnackBar-z-index",
        "--bit-SnackBar-offset",
        "--bit-SnackBar-gap",
        "--bit-SnackBar-min-width",
        "--bit-SnackBar-max-width",
        "--bit-SnackBar-background",
        "--bit-SnackBar-color",
        "--bit-SnackBar-border-color",
        "--bit-SnackBar-border-width",
        "--bit-SnackBar-radius",
        "--bit-SnackBar-shadow",
        "--bit-SnackBar-padding",
        "--bit-SnackBar-title-font-size",
        "--bit-SnackBar-title-font-weight",
        "--bit-SnackBar-body-font-size",
        "--bit-SnackBar-icon-color",
        "--bit-SnackBar-icon-size",
        "--bit-SnackBar-progress-color",
        "--bit-SnackBar-progress-height",
    ];

    [TestMethod]
    public void BitSnackBarStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitSnackBarStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-SnackBar-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitSnackBarStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-SnackBar-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-SnackBar-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read, documented);
    }

    [TestMethod,
        DataRow("otl"),
        DataRow("txt")
    ]
    public void BitSnackBarUnfilledVariantsShouldReadTheRoleForegroundAsText(string variant)
    {
        var rules = GetRules(ReadStylesheet());

        // A role's main color is picked to be a fill; on the page surface the text takes the shade meant to be read.
        StringAssert.Contains(rules, "--bit-snb-clr-txt: #{role($tokens, fg)};");
        StringAssert.Contains(GetRule(rules, variant), "--bit-snb-clr: var(--bit-snb-clr-txt, var(--bit-SnackBar-color, #{$clr-inf-fg}));");
    }

    [TestMethod]
    public void BitSnackBarUnfilledVariantsShouldReadEveryRoleThroughTheSharedForeground()
    {
        var rules = GetRules(ReadStylesheet());

        // The shared map already gives a background or border role its on color as fg (BitColorRoleMapsTests), so the
        // snack bar reads the same slot as every other component instead of special-casing those roles itself.
        Assert.IsFalse(rules.Contains("--bit-snb-clr-txt: #{role($tokens, on)};"), "The snack bar special-cases the text of a role.");
    }

    [TestMethod,
        DataRow("tst", "top: var(--bit-snb-off-top);"),
        DataRow("tcn", "top: var(--bit-snb-off-top);"),
        DataRow("ten", "top: var(--bit-snb-off-top);"),
        DataRow("bst", "bottom: var(--bit-snb-off-bottom);"),
        DataRow("bcn", "bottom: var(--bit-snb-off-bottom);"),
        DataRow("ben", "bottom: var(--bit-snb-off-bottom);")
    ]
    public void BitSnackBarPositionsShouldClearTheSafeArea(string position, string declaration)
    {
        var rules = GetRules(ReadStylesheet());
        var rule = GetRule(rules, position);

        StringAssert.Contains(rule, declaration);
        StringAssert.Contains(rules, "--bit-snb-off-top: calc(var(--bit-snb-off-block) + env(safe-area-inset-top, 0px));");
        StringAssert.Contains(rules, "--bit-snb-off-bottom: calc(var(--bit-snb-off-block) + env(safe-area-inset-bottom, 0px));");

        // The sides clear the safe area too - the notch of a phone in landscape is on one of them - and a centered
        // stack is centered in what they leave rather than on the whole screen.
        StringAssert.Contains(rule, "left: var(--bit-snb-off-left);");
        StringAssert.Contains(rule, "right: var(--bit-snb-off-right);");
        Assert.IsFalse(rule.Contains("translateX"), $"{position} is centered on the whole screen.");
        StringAssert.Contains(rules, "--bit-snb-off-left: calc(var(--bit-snb-off-inline) + env(safe-area-inset-left, 0px));");
        StringAssert.Contains(rules, "--bit-snb-off-right: calc(var(--bit-snb-off-inline) + env(safe-area-inset-right, 0px));");
        StringAssert.Contains(rules, "max-width: calc(100% - var(--bit-snb-off-left) - var(--bit-snb-off-right));");
    }

    [TestMethod,
        DataRow("tlf", "top: var(--bit-snb-off-top);", "left: var(--bit-snb-off-left);"),
        DataRow("trg", "top: var(--bit-snb-off-top);", "right: var(--bit-snb-off-right);"),
        DataRow("blf", "bottom: var(--bit-snb-off-bottom);", "left: var(--bit-snb-off-left);"),
        DataRow("brg", "bottom: var(--bit-snb-off-bottom);", "right: var(--bit-snb-off-right);"),
        DataRow("clf", "bottom: var(--bit-snb-off-bottom);", "left: var(--bit-snb-off-left);"),
        DataRow("crg", "bottom: var(--bit-snb-off-bottom);", "right: var(--bit-snb-off-right);"),
        DataRow("cst", "bottom: var(--bit-snb-off-bottom);", "left: var(--bit-snb-off-left);"),
        DataRow("cen", "bottom: var(--bit-snb-off-bottom);", "right: var(--bit-snb-off-right);"),
        DataRow("ctr", "bottom: var(--bit-snb-off-bottom);", "left: var(--bit-snb-off-left);")
    ]
    public void BitSnackBarCornerAndCenteredPositionsShouldClearTheSafeArea(string position, string blockDeclaration, string inlineDeclaration)
    {
        var rule = GetRule(GetRules(ReadStylesheet()), position);

        StringAssert.Contains(rule, blockDeclaration);
        StringAssert.Contains(rule, inlineDeclaration);

        // Pinned by the raw offsets, a stack ignores the notch; centered by a translate, it is centered on the
        // whole screen rather than on the part of it that is safe to draw on.
        Assert.IsFalse(rule.Contains("var(--bit-snb-off-block)"), $"{position} ignores the safe area of the block axis.");
        Assert.IsFalse(rule.Contains("var(--bit-snb-off-inline)"), $"{position} ignores the safe area of the inline axis.");
        Assert.IsFalse(rule.Contains("translate"), $"{position} is centered on the whole screen.");
    }

    [TestMethod]
    public void BitSnackBarItemsShouldSpanTheStackOnAPhoneUnlessAMinWidthIsGiven()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "@include xs {\n    .bit-snb {\n        --bit-snb-min-w: 100%;");
        StringAssert.Contains(GetRule(rules, "itm"), "min-width: min(100%, var(--bit-SnackBar-min-width, var(--bit-snb-min-w, 0px)));");
    }

    [TestMethod]
    public void BitSnackBarShouldLetAParameterWinOverItsPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium item
        // an unset one stands for.
        StringAssert.Contains(rules, "padding: var(--bit-snb-pad, var(--bit-SnackBar-padding, #{spacing(1.25)}));");
        StringAssert.Contains(rules, "font-size: var(--bit-snb-ico-fs, var(--bit-SnackBar-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(rules, "font-size: var(--bit-snb-ttl-fs, var(--bit-SnackBar-title-font-size, #{$tg-fs-md}));");
        StringAssert.Contains(rules, "font-size: var(--bit-snb-bdy-fs, var(--bit-SnackBar-body-font-size, #{$tg-fs-sm}));");

        // So does the explicit Color of an item, for every color its role paints; the page surface the unfilled
        // variants float on is the variable's alone.
        var fill = GetRule(rules, "fil");
        StringAssert.Contains(fill, "--bit-snb-clr: var(--bit-snb-fg, var(--bit-SnackBar-color, #{$clr-inf-text}));");
        StringAssert.Contains(fill, "--bit-snb-clr-bg: var(--bit-snb-bg, var(--bit-SnackBar-background, #{$clr-inf}));");
        StringAssert.Contains(fill, "--bit-snb-clr-brd: var(--bit-snb-bg, var(--bit-SnackBar-border-color, #{$clr-inf}));");
        StringAssert.Contains(GetRule(rules, "otl"), "--bit-snb-clr-bg: var(--bit-SnackBar-background, #{$clr-bg-pri});");

        // The two left in front are fed by no parameter of these: the phone-width floor comes from a media query, and
        // the border width from the Variant, which always publishes a class.
        var publicFirst = Regex.Matches(rules, @"var\(--bit-SnackBar-[a-z-]+, var\(--bit-snb-([a-z-]+)").Select(m => m.Groups[1].Value).Order().ToArray();
        CollectionAssert.AreEqual(new[] { "brd-w", "min-w" }, publicFirst);
    }

    [TestMethod]
    public void BitSnackBarShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var item = GetRule(GetRules(ReadStylesheet()), "itm");

        // A snack bar can be rendered in the template of an item of another one, whose items must not inherit the outer
        // item's Color or Size: each item starts the values those classes publish out unset, and the classes - declared
        // further down at the same weight - still win on the item that carries them.
        foreach (var property in new[] { "--bit-snb-bg", "--bit-snb-fg", "--bit-snb-clr-txt", "--bit-snb-gap", "--bit-snb-pad", "--bit-snb-hdr-gap",
                                         "--bit-snb-ttl-fs", "--bit-snb-bdy-fs", "--bit-snb-ico-fs", "--bit-snb-cbt-size" })
        {
            StringAssert.Contains(item, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitSnackBarCountdownBarShouldFollowAPageLevelRightToLeftDirection()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, ".bit-snb-prb:dir(rtl) {\n    transform-origin: right center;");
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n.bit-snb {")..];

    /// <summary>
    /// The body of the top-level rule of one class, up to the brace that closes it at the start of a line.
    /// </summary>
    private static string GetRule(string rules, string suffix) => Regex.Match(rules, $@"\n\.bit-snb-{suffix} \{{.*?\n\}}", RegexOptions.Singleline).Value;

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-snb {")];

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Notifications", "SnackBar", "BitSnackBar.scss");
}
