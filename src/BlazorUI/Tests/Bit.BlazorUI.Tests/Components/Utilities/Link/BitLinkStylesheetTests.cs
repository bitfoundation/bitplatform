using System;
using System.Linq;
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

        Assert.IsNotEmpty(documented, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            // The focus color alone is read without a fallback, on purpose: its absence is what hands the ring over
            // to the global --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
            var read = name.EndsWith("-focus-color", StringComparison.Ordinal) ? $"var({name})" : $"var({name}, ";

            Assert.Contains(read, stylesheet, $"{name} is documented but never read as it should be.");
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
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitLinkShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Link-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitLinkShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lnk {");

        // An explicit Color or Size publishes these, so they are read before the variable, which only restyles the
        // default an unset one stands for: the primary role, and the font size of the text around the link.
        // (The states set the state color the rules at rest read first, rather than painting the color themselves.)
        Assert.Contains("color: var(--bit-lnk-sfg, var(--bit-lnk-clr, var(--bit-Link-color, #{$clr-pri})));", root);
        Assert.Contains("--bit-lnk-sfg: var(--bit-lnk-clr-hover, var(--bit-Link-hover-color, #{$clr-pri-hover}));", root);
        Assert.Contains("--bit-lnk-sfg: var(--bit-lnk-clr-active, var(--bit-Link-active-color, #{$clr-pri-active}));", root);
        Assert.Contains("font-size: var(--bit-lnk-fs, var(--bit-Link-font-size, inherit));", root);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Link-[a-z-]+, var\(--bit-lnk-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitLinkShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lnk {");

        // A link never inherits another one's Color or Size from an ancestor carrying those classes: each root starts
        // the values they publish out unset, and the classes - declared further down at the same weight - still win on
        // the root that carries them.
        foreach (var property in new[] { "--bit-lnk-clr", "--bit-lnk-clr-hover", "--bit-lnk-clr-active", "--bit-lnk-clr-focus", "--bit-lnk-clr-dis", "--bit-lnk-fs" })
        {
            Assert.Contains($"{property}: initial;", root);
        }

        var rootAt = stylesheet.IndexOf("\n.bit-lnk {", System.StringComparison.Ordinal);
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-lnk-md {", System.StringComparison.Ordinal), rootAt, "The size classes are declared ahead of the root that resets them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n    .bit-lnk-#{$role} {", System.StringComparison.Ordinal), rootAt, "The color classes are declared ahead of the root that resets them.");
    }

    [TestMethod,
        DataRow("sm", "xs"),
        DataRow("md", "sm"),
        DataRow("lg", "md")]
    public void BitLinkShouldSizeFromTheTypeRamp(string size, string step)
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n.bit-lnk-{size} {{");

        Assert.Contains($"--bit-lnk-fs: #{{$tg-fs-{step}}};", block);
    }

    [TestMethod]
    public void BitLinkShouldInheritTheTypeOfTheSentenceItSitsIn()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-lnk {");

        // A link rendered as a button is talked out of everything the browser gives a button of its own.
        foreach (var property in new[] { "line-height", "text-align", "word-spacing", "letter-spacing", "text-transform" })
        {
            Assert.Contains($"{property}: inherit;", root, $"The {property} of a button link is not inherited.");
        }

        Assert.Contains("font-weight: var(--bit-Link-font-weight, inherit);", root);
    }

    [TestMethod]
    public void BitLinkShouldDrawTheUnderlineThroughItsLonghands()
    {
        var rules = SourceFiles.StripScssComments(ReadStylesheet());

        // The shorthand resets the color, the thickness and the offset of the underline every time a state turns it on.
        Assert.IsFalse(Regex.IsMatch(rules, @"(^|\s)text-decoration:\s"), "The text-decoration shorthand is used.");
        Assert.Contains("text-decoration-color: var(--bit-Link-underline-color, currentColor);", rules);
        Assert.Contains("text-decoration-thickness: var(--bit-Link-underline-thickness, auto);", rules);
        Assert.Contains("text-underline-offset: var(--bit-Link-underline-offset, auto);", rules);
    }

    [TestMethod]
    public void BitLinkShouldUnderlineOnHoverOnlyUnderAPointerThatHovers()
    {
        var stylesheet = ReadStylesheet();

        var hover = SourceFiles.GetScssBlock(stylesheet, "\n    @media (hover: hover) {");

        Assert.Contains("&:hover {", hover);
        Assert.Contains("text-decoration-line: var(--bit-lnk-deco-hover, underline);", hover);
    }

    [TestMethod]
    public void BitLinkShouldChangeOnTheKeyboardFocusOnly()
    {
        var rules = SourceFiles.StripScssComments(ReadStylesheet());

        // A link left in its hover color after a mouse click reads as stuck, so only :focus-visible is styled.
        Assert.IsFalse(Regex.IsMatch(rules, @"&:focus\b(?!-visible)"), "A bare :focus is styled.");
        Assert.Contains("@include focus-ring-own(var(--bit-lnk-clr-focus, var(--bit-Link-focus-color)));", rules);
    }

    [TestMethod]
    public void BitLinkShouldStyleVisitedAheadOfTheInteractiveStates()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-lnk {");

        var visited = root.IndexOf("&:visited {", System.StringComparison.Ordinal);
        var hover = root.IndexOf("&:hover {", System.StringComparison.Ordinal);
        var active = root.IndexOf("&:active {", System.StringComparison.Ordinal);

        Assert.IsTrue(visited >= 0 && visited < hover && visited < active, "A visited link would no longer change under the pointer.");
        Assert.Contains("color: var(--bit-lnk-sfg, var(--bit-lnk-clr, var(--bit-Link-visited-color, var(--bit-Link-color, #{$clr-pri}))));", root);
    }

    [TestMethod]
    public void BitLinkShouldDrawTheCurrentLinkWithoutOutrankingItsStates()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lnk {");

        // The page a current link points at is visited by definition, so the current color covers :visited too,
        // and :where() keeps it below the pointer, the press, the focus, NoColor and the disabled state.
        var current = root.IndexOf("&:where(.bit-lnk-cur),", System.StringComparison.Ordinal);
        var hover = root.IndexOf("&:hover {", System.StringComparison.Ordinal);

        Assert.IsTrue(current >= 0 && current < hover, "The current link is not styled ahead of the interactive states.");
        Assert.Contains("&:where(.bit-lnk-cur):visited {", root);
        Assert.Contains("color: var(--bit-lnk-sfg, var(--bit-lnk-clr, var(--bit-Link-current-color, var(--bit-Link-color, #{$clr-pri}))));", root);

        // Color alone must not be what tells the current link apart, and NoUnderline still takes the underline off.
        Assert.Contains("--bit-lnk-deco: underline;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-lnk-cur {"));
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-lnk-nun {", System.StringComparison.Ordinal), stylesheet.IndexOf("\n.bit-lnk-cur {", System.StringComparison.Ordinal), "NoUnderline is declared ahead of the current link, which would win over it.");
    }

    [TestMethod]
    public void BitLinkShouldKeepADisabledLinkInTheDisabledColor()
    {
        var disabled = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &.bit-dis {");

        Assert.Contains("color: var(--bit-lnk-clr-dis, var(--bit-Link-disabled-color, #{$clr-pri-dis-text}));", disabled);
        Assert.Contains("@include focus-ring(var(--bit-lnk-clr-dis, var(--bit-Link-disabled-color, #{$clr-pri-dis-text})));", disabled);
        Assert.DoesNotContain("--bit-Link-color", disabled, "A disabled link reads the enabled color variable.");
        Assert.DoesNotContain("--bit-Link-hover-color", disabled, "A disabled link reads the hover color variable.");
    }

    [TestMethod]
    public void BitLinkShouldPaintAccentColorsInTheirReadableForeground()
    {
        var roles = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-lnk-#{$role} {");

        // A secondary or a warning main is picked to fill a surface and falls under the contrast of body text,
        // while the primary main is the brand's own link color and a foreground role is a text color already.
        Assert.Contains("@if role($tokens, kind) == semantic and $role != pri {", roles);
        Assert.Contains("--bit-lnk-clr: #{role($tokens, fg)};", roles);
        Assert.Contains("--bit-lnk-clr: #{role($tokens, main)};", roles);

        // A background or border main is the page's own color or a hairline's, invisible or far under 4.5:1 as text,
        // so those roles read their foreground - the body text - with that text color's own states.
        Assert.Contains("@else if role($tokens, kind) == surface and $role != pfg and $role != sfg and $role != tfg {\n            --bit-lnk-clr: #{role($tokens, fg)};\n            --bit-lnk-clr-hover: #{$clr-fg-pri-hover};\n            --bit-lnk-clr-active: #{$clr-fg-pri-active};", roles);
        Assert.Contains("--bit-lnk-clr-dis: #{role($tokens, dis-text)};", roles);
    }

    [TestMethod]
    public void BitLinkShouldReadAsALinkAndAsDisabledInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        Assert.Contains("color: LinkText;", forced);
        Assert.Contains(".bit-lnk.bit-dis,", forced);
        Assert.Contains("color: GrayText;", forced);
    }

    [TestMethod]
    public void BitLinkShouldKeepTheIconGapLogical()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("margin-inline-end: var(--bit-Link-icon-gap, ", SourceFiles.GetScssBlock(stylesheet, "\n.bit-lnk-sic {"));
        Assert.Contains("margin-inline-start: var(--bit-Link-icon-gap, ", SourceFiles.GetScssBlock(stylesheet, "\n.bit-lnk-eic {"));
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Utilities", "Link", "BitLink.scss");

    [GeneratedRegex(@"^//\s+(--bit-Link-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Link-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Link-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
