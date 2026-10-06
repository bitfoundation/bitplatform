using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

/// <summary>
/// Pins the public --bit-Dialog-* custom properties and the global dialog tokens the stylesheet reads, which a
/// bUnit render cannot see: the public ones are read with a fallback and never declared, so they inherit from
/// :root, an ancestor or the Style of an instance, and the list at the head of the stylesheet names exactly the
/// ones it reads.
/// </summary>
[TestClass]
public class BitDialogStylesheetTests
{
    // Read off the stylesheet.
    private static readonly string[] _publicVariables =
    [
        "--bit-Dialog-z-index",
        "--bit-Dialog-margin",
        "--bit-Dialog-overlay-background",
        "--bit-Dialog-overlay-backdrop-filter",
        "--bit-Dialog-background",
        "--bit-Dialog-color",
        "--bit-Dialog-border-width",
        "--bit-Dialog-border-color",
        "--bit-Dialog-radius",
        "--bit-Dialog-shadow",
        "--bit-Dialog-padding",
        "--bit-Dialog-text-align",
        "--bit-Dialog-title-color",
        "--bit-Dialog-title-font-size",
        "--bit-Dialog-title-font-weight",
        "--bit-Dialog-subtitle-color",
        "--bit-Dialog-message-color",
    ];

    // Read off the style the C# side writes on the surface, since it only applies where no size was given.
    private const string MaxWidthVariable = "--bit-Dialog-max-width";

    [TestMethod]
    public void BitDialogStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitDialogStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-Dialog-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitDialogStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-Dialog-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-Dialog-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read.Append(MaxWidthVariable).Order().ToArray(), documented);
    }

    [TestMethod]
    public void BitDialogTitleShouldTakeTheDialogTypographyTokens()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "ttl");

        StringAssert.Contains(rule, "font-size: var(--bit-Dialog-title-font-size, #{$tg-dialog-title-font-size});");
        StringAssert.Contains(rule, "font-weight: var(--bit-Dialog-title-font-weight, #{$tg-dialog-title-font-weight});");
        Assert.IsFalse(rule.Contains("$tg-fs-xl"), "The title is sized off the type ramp rather than the dialog title token every preset retunes.");
    }

    [TestMethod]
    public void BitDialogTextShouldFollowTheDialogTextAlignToken()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "--bit-dlg-text-align: var(--bit-Dialog-text-align, #{$layout-dialog-text-align});");

        // Set on the title itself, since an application's element rule for h2 wins over an inherited alignment.
        foreach (var text in new[] { "ttl", "sub", "msg" })
        {
            StringAssert.Contains(GetRule(rules, text), "text-align: var(--bit-dlg-text-align);", $"The {text} does not follow the alignment.");
        }

        // The wrapper also holds a HeaderTemplate, whose content is the consumer's to lay out.
        Assert.IsFalse(GetRule(rules, "htc").Contains("text-align"), "A HeaderTemplate is re-aligned along with the title.");
    }

    [TestMethod]
    public void BitDialogCentredTitleShouldBeCentredOnTheSurfaceBesideTheCloseButton()
    {
        var rules = GetRules(ReadStylesheet());

        // The title and subtitle are given the close button's width back on their start side while centred, so
        // they are centred on the surface rather than on the room the button leaves beside them.
        var query = Regex.Match(rules, @"@container style\(--bit-dlg-text-align: center\) \{.*?\n\}", RegexOptions.Singleline).Value;

        StringAssert.Contains(query, ".bit-dlg-htc:not(:last-child) > .bit-dlg-ttl");
        StringAssert.Contains(query, ".bit-dlg-htc:not(:last-child) > .bit-dlg-sub");
        StringAssert.Contains(query, "padding-inline-start: $siz-ctrl-md;");
    }

    [TestMethod]
    public void BitDialogAbsolutePositionShouldNotReadThePublicZIndex()
    {
        // A value set on :root to restack the full-screen Dialogs must not lift every absolutely positioned one
        // out of the stacking of the area it is laid out in.
        var rule = GetRule(GetRules(ReadStylesheet()), "abs");

        StringAssert.Contains(rule, "z-index: auto;");
        Assert.IsFalse(rule.Contains("--bit-Dialog-z-index"), "An absolutely positioned Dialog reads the public z-index.");
    }

    [TestMethod]
    public void BitDialogTitleShouldSetWhatAnApplicationsHeadingRuleCouldReach()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "ttl");

        foreach (var declaration in new[] { "margin: 0;", "padding: 0;", "font-style: normal;", "font-family: inherit;",
                                            "letter-spacing: normal;", "text-transform: none;", "text-decoration: none;" })
        {
            StringAssert.Contains(rule, declaration);
        }
    }

    [TestMethod]
    public void BitDialogBandsShouldTakeTheSurfacesBackgroundAndCorners()
    {
        var rules = GetRules(ReadStylesheet());

        // A surface given a background or a radius of its own is not left with bands of the default in it.
        foreach (var band in new[] { "hdr", "bct", "ftr" })
        {
            var rule = GetRule(rules, band);

            StringAssert.Contains(rule, "background-color: var(--bit-dlg-bg);", $"The {band} band keeps a background of its own.");
            StringAssert.Contains(rule, "radius: var(--bit-dlg-radius);", $"The {band} band keeps corners of its own.");
        }
    }

    [TestMethod]
    public void BitDialogBusyOkButtonShouldStayUndimmedAndIgnoreHover()
    {
        var rules = GetRules(ReadStylesheet());

        // The spinner is the only sign the work is running, so the busy button is not dimmed like a disabled one.
        StringAssert.Contains(GetRule(rules, "bct"), "&[aria-busy=\"true\"] {\n            cursor: progress;\n        }");
        Assert.IsFalse(rules.Contains("&[aria-disabled=\"true\"] {"), "The busy Ok button is dimmed along with the spinner it holds.");
        StringAssert.Contains(GetRule(rules, "okb"), "&:hover:not(:disabled):not([aria-disabled=\"true\"])");
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n.bit-dlg {")..];

    /// <summary>
    /// The body of the top-level rule of one class, up to the brace that closes it at the start of a line.
    /// </summary>
    private static string GetRule(string rules, string suffix) => Regex.Match(rules, $@"\n\.bit-dlg-{suffix} \{{.*?\n\}}", RegexOptions.Singleline).Value;

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-dlg {")];

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Dialog", "BitDialog.scss");
}
