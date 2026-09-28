using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        StringAssert.Contains(GetRule(rules, "htc"), "text-align: var(--bit-dlg-text-align);");
        StringAssert.Contains(GetRule(rules, "msg"), "text-align: var(--bit-dlg-text-align);");
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

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Surfaces", "Dialog", "BitDialog.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
