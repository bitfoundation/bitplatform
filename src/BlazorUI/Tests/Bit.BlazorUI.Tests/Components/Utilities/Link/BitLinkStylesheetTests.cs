using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Link;

/// <summary>
/// Pins the public --bit-Link-* variables and the accessibility rules of the stylesheet, which a bUnit render cannot
/// see: every variable the header documents is read with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), nothing is read that the header does not document, and the states,
/// the colors and the forced-colors mode behave as documented.
/// </summary>
[TestClass]
public partial class BitLinkStylesheetTests
{
    [TestMethod]
    public void BitLinkShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitLinkShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitLinkShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Link-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitLinkShouldLetTheVariablesWinOverTheColorAndTheSize()
    {
        var root = Block(ReadStylesheet(), "\n.bit-lnk {");

        // The Color and the Size classes only set the private fallbacks the public variables are read over.
        StringAssert.Contains(root, "color: var(--bit-Link-color, var(--bit-lnk-clr));");
        StringAssert.Contains(root, "font-size: var(--bit-Link-font-size, var(--bit-lnk-fs, inherit));");
    }

    [TestMethod,
        DataRow("sm", "xs"),
        DataRow("md", "sm"),
        DataRow("lg", "md")]
    public void BitLinkShouldSizeFromTheTypeRamp(string size, string step)
    {
        var block = Block(ReadStylesheet(), $"\n.bit-lnk-{size} {{");

        StringAssert.Contains(block, $"--bit-lnk-fs: #{{$tg-fs-{step}}};");
    }

    [TestMethod]
    public void BitLinkShouldInheritTheTypeOfTheSentenceItSitsIn()
    {
        var root = Block(ReadStylesheet(), "\n.bit-lnk {");

        // A link rendered as a button is talked out of everything the browser gives a button of its own.
        foreach (var property in new[] { "line-height", "text-align", "word-spacing", "letter-spacing", "text-transform" })
        {
            StringAssert.Contains(root, $"{property}: inherit;", $"The {property} of a button link is not inherited.");
        }

        StringAssert.Contains(root, "font-weight: var(--bit-Link-font-weight, inherit);");
    }

    [TestMethod]
    public void BitLinkShouldDrawTheUnderlineThroughItsLonghands()
    {
        var rules = RulesOf(ReadStylesheet());

        // The shorthand resets the color, the thickness and the offset of the underline every time a state turns it on.
        Assert.IsFalse(Regex.IsMatch(rules, @"(^|\s)text-decoration:\s"), "The text-decoration shorthand is used.");
        StringAssert.Contains(rules, "text-decoration-color: var(--bit-Link-underline-color, currentColor);");
        StringAssert.Contains(rules, "text-decoration-thickness: var(--bit-Link-underline-thickness, auto);");
        StringAssert.Contains(rules, "text-underline-offset: var(--bit-Link-underline-offset, auto);");
    }

    [TestMethod]
    public void BitLinkShouldUnderlineOnHoverOnlyUnderAPointerThatHovers()
    {
        var stylesheet = ReadStylesheet();

        var hover = Block(stylesheet, "\n    @media (hover: hover) {");

        StringAssert.Contains(hover, "&:hover {");
        StringAssert.Contains(hover, "text-decoration-line: var(--bit-lnk-deco-hover, underline);");
    }

    [TestMethod]
    public void BitLinkShouldChangeOnTheKeyboardFocusOnly()
    {
        var rules = RulesOf(ReadStylesheet());

        // A link left in its hover color after a mouse click reads as stuck, so only :focus-visible is styled.
        Assert.IsFalse(Regex.IsMatch(rules, @"&:focus\b(?!-visible)"), "A bare :focus is styled.");
        StringAssert.Contains(rules, "@include focus-ring(var(--bit-Link-focus-color, var(--bit-lnk-clr-focus)));");
    }

    [TestMethod]
    public void BitLinkShouldStyleVisitedAheadOfTheInteractiveStates()
    {
        var root = Block(ReadStylesheet(), "\n.bit-lnk {");

        var visited = root.IndexOf("&:visited {", System.StringComparison.Ordinal);
        var hover = root.IndexOf("&:hover {", System.StringComparison.Ordinal);
        var active = root.IndexOf("&:active {", System.StringComparison.Ordinal);

        Assert.IsTrue(visited >= 0 && visited < hover && visited < active, "A visited link would no longer change under the pointer.");
        StringAssert.Contains(root, "color: var(--bit-Link-visited-color, var(--bit-Link-color, var(--bit-lnk-clr)));");
    }

    [TestMethod]
    public void BitLinkShouldKeepADisabledLinkInTheDisabledColor()
    {
        var disabled = Block(ReadStylesheet(), "\n    &.bit-dis {");

        StringAssert.Contains(disabled, "color: var(--bit-Link-disabled-color, var(--bit-lnk-clr-dis));");
        StringAssert.Contains(disabled, "@include focus-ring(var(--bit-Link-disabled-color, var(--bit-lnk-clr-dis)));");
        Assert.IsFalse(disabled.Contains("--bit-Link-color"), "A disabled link reads the enabled color variable.");
        Assert.IsFalse(disabled.Contains("--bit-Link-hover-color"), "A disabled link reads the hover color variable.");
    }

    [TestMethod]
    public void BitLinkShouldPaintAccentColorsInTheirReadableForeground()
    {
        var roles = Block(ReadStylesheet(), "\n    .bit-lnk-#{$role} {");

        // A secondary or a warning main is picked to fill a surface and falls under the contrast of body text,
        // while the primary main is the brand's own link color and a surface role is a page color already.
        StringAssert.Contains(roles, "@if role($tokens, kind) == semantic and $role != pri {");
        StringAssert.Contains(roles, "--bit-lnk-clr: #{role($tokens, fg)};");
        StringAssert.Contains(roles, "--bit-lnk-clr: #{role($tokens, main)};");
        StringAssert.Contains(roles, "--bit-lnk-clr-dis: #{role($tokens, dis-text)};");
    }

    [TestMethod]
    public void BitLinkShouldReadAsALinkAndAsDisabledInForcedColors()
    {
        var forced = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, "color: LinkText;");
        StringAssert.Contains(forced, ".bit-lnk.bit-dis,");
        StringAssert.Contains(forced, "color: GrayText;");
    }

    [TestMethod]
    public void BitLinkShouldKeepTheIconGapLogical()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(Block(stylesheet, "\n.bit-lnk-sic {"), "margin-inline-end: var(--bit-Link-icon-gap, ");
        StringAssert.Contains(Block(stylesheet, "\n.bit-lnk-eic {"), "margin-inline-start: var(--bit-Link-icon-gap, ");
    }

    private static string Block(string stylesheet, string opening)
    {
        var start = stylesheet.IndexOf(opening, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"No rule opens with {opening.Trim()}.");

        var indent = opening[1..].Length - opening[1..].TrimStart().Length;
        var end = stylesheet.IndexOf("\n" + new string(' ', indent) + "}", start + opening.Length, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The header comment is where the variables are documented, so only what follows it is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "Link", "BitLink.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Link-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Link-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Link-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
