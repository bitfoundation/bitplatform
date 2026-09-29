using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        StringAssert.Contains(GetRule(rules, variant), "--bit-snb-clr: var(--bit-snb-clr-txt);");
    }

    [TestMethod]
    public void BitSnackBarUnfilledVariantsShouldReadBackgroundAndBorderRolesWithTheirOnColor()
    {
        var rules = GetRules(ReadStylesheet());

        // The fg of a background or border role is that surface or border color itself, invisible as text on the
        // page surface the unfilled variants sit on; only the foreground roles are read as they are.
        StringAssert.Contains(rules, "@if role($tokens, kind) == surface and $role != pfg and $role != sfg and $role != tfg {\n            --bit-snb-clr-txt: #{role($tokens, on)};");
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

    [TestMethod]
    public void BitSnackBarItemsShouldSpanTheStackOnAPhoneUnlessAMinWidthIsGiven()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "@include xs {\n    .bit-snb {\n        --bit-snb-min-w: 100%;");
        StringAssert.Contains(GetRule(rules, "itm"), "min-width: min(100%, var(--bit-SnackBar-min-width, var(--bit-snb-min-w, 0px)));");
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

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Notifications", "SnackBar", "BitSnackBar.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
