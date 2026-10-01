using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Text;

/// <summary>
/// Pins the parts of the text that live in its stylesheet, which a bUnit render cannot see: the public CSS variables
/// (each read with a fallback and never declared, listed in the header of the stylesheet and in the table of the demo
/// page, and ranked below the parameter that does the same job) and the focus behavior of a visually hidden text.
/// </summary>
[TestClass]
public class BitTextStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Text-color",
        "--bit-Text-font-family",
        "--bit-Text-heading-font-family",
        "--bit-Text-monospace-font-family",
        "--bit-Text-gutter",
        "--bit-Text-decoration-color",
        "--bit-Text-decoration-thickness",
        "--bit-Text-underline-offset",
        "--bit-Text-disabled-opacity",
    ];

    [TestMethod]
    public void BitTextShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(stylesheet, @"var\((--bit-Text-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            Assert.IsTrue(Regex.IsMatch(stylesheet, $@"var\({variable}, [^)]"), $"{variable} is read without a fallback.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitTextShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Utilities", "Text", "BitTextDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-Text-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    // The variables restyle the defaults; a parameter is a choice made on the text itself. The color and the family are
    // read on the root, and the classes of Color, Foreground, Gradient, Monospace and the inherit variant all set the
    // same property with the same specificity further down the file, so they win by order alone.
    [TestMethod,
        DataRow("color: var(--bit-Text-color", ".bit-txt-#{$role} {"),
        DataRow("color: var(--bit-Text-color", "\n.bit-txt-rfg {"),
        DataRow("color: var(--bit-Text-color", "\n.bit-txt-grd {"),
        DataRow("font-family: var(--bit-Text-font-family", "\n.bit-txt-inherit {"),
        DataRow("font-family: var(--bit-Text-font-family", "\n.bit-txt-mno {"),
        DataRow("font-family: var(--bit-Text-heading-font-family", "\n.bit-txt-mno {")]
    public void BitTextShouldLetAParameterWinOverItsPublicVariable(string variableRule, string parameterRule)
    {
        var stylesheet = ReadStylesheet();

        var root = stylesheet.IndexOf("\n.bit-txt {", StringComparison.Ordinal);
        var variable = stylesheet.IndexOf(variableRule, StringComparison.Ordinal);
        var parameter = stylesheet.IndexOf(parameterRule, StringComparison.Ordinal);

        Assert.IsTrue(root >= 0 && variable > root, $"\"{variableRule}\" is not read on the root.");
        Assert.IsTrue(parameter > variable, $"\"{parameterRule.Trim()}\" is declared before the root and loses to its variable.");
    }

    // A visually hidden text that takes the focus - a skip link - has to be drawn while it holds it (WCAG 2.4.7), so
    // the hiding rules are scoped away from the focused state rather than applying unconditionally.
    [TestMethod]
    public void BitTextShouldRevealAVisuallyHiddenTextWhileItHoldsTheFocus()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "\n.bit-txt-vhd:not(:focus):not(:focus-within) {");
        Assert.IsFalse(stylesheet.Contains("\n.bit-txt-vhd {", StringComparison.Ordinal), "The visually hidden rules apply to a focused text as well.");
    }

    // A forced-colors mode paints every text in CanvasText, so the dimming alone would be all that says a text is
    // disabled; GrayText is the system color for it, as on a disabled label or link. The rule sits inside the
    // disabled one, which outranks the forced-colors rules of the gradient classes by specificity.
    [TestMethod]
    public void BitTextShouldPaintADisabledTextInGrayTextInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        var disabled = stylesheet[stylesheet.IndexOf("\n.bit-txt.bit-dis {", StringComparison.Ordinal)..];
        disabled = disabled[..disabled.IndexOf("\n}", StringComparison.Ordinal)];

        StringAssert.Contains(disabled, "@media (forced-colors: active) {");
        StringAssert.Contains(disabled, "color: GrayText;");
    }

    [TestMethod]
    public void BitTextShouldDrawTheSharedFocusRing()
    {
        var stylesheet = ReadStylesheet();

        var root = stylesheet[stylesheet.IndexOf("\n.bit-txt {", StringComparison.Ordinal)..];
        root = root[..root.IndexOf("\n}", StringComparison.Ordinal)];

        StringAssert.Contains(root, "&:focus-visible {");
        StringAssert.Contains(root, "@include focus-ring;");
    }



    private static string ReadStylesheet() => ReadFile("Bit.BlazorUI", "Components", "Utilities", "Text", "BitText.scss");

    private static string ReadFile(params string[] segments) => ReadFileFrom(segments);

    private static string ReadFileFrom(string[] segments, [CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine([Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..", .. segments]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
