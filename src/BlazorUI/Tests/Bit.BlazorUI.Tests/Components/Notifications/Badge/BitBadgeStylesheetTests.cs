using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Badge;

/// <summary>
/// Pins the public --bit-Badge-* variables against the stylesheet that reads them, which a bUnit render cannot see:
/// every variable the header documents is read somewhere with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), and nothing is read that the header does not document.
/// </summary>
[TestClass]
public partial class BitBadgeStylesheetTests
{
    [TestMethod]
    public void BitBadgeShouldReadEveryPublicVariableItDocuments()
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
    public void BitBadgeShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitBadgeShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Badge-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitBadgeShouldKeepADisabledBadgeInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {", "The disabled badge has no rule of its own.");

        // The disabled colors are handed over from the disabled tokens alone, never through the public color variables...
        Assert.Contains("--bit-bdg-dis-txt: var(--bit-bdg-clr-dis-text, #{$clr-pri-dis-text});", block);
        Assert.Contains("--bit-bdg-dis-bg: var(--bit-bdg-clr-bg-dis);", block);
        Assert.Contains("--bit-bdg-dis-brd: var(--bit-bdg-clr-brd-dis);", block);
        Assert.DoesNotContain("--bit-Badge-", block);

        // ...and the badge reads them ahead of the colors the variants resolve out of the Color and those variables,
        // so a re-tinted badge that is disabled still reads as disabled, while the rule painting it keeps the weight
        // of a single class a Classes.Badge class can compete with.
        // The hover and press of a clickable badge only move a variable read between the two, so a Classes.Badge
        // class painting the badge keeps that paint under the pointer.
        Assert.Contains("color: var(--bit-bdg-dis-txt, var(--bit-bdg-cnt-sfg, var(--bit-bdg-cnt-clr-txt)));", stylesheet);
        Assert.Contains("background-color: var(--bit-bdg-dis-bg, var(--bit-bdg-cnt-sbg, var(--bit-bdg-cnt-clr-bg)));", stylesheet);
        Assert.Contains("border-color: var(--bit-bdg-dis-brd, var(--bit-bdg-cnt-sbr, var(--bit-bdg-cnt-clr-brd)));", stylesheet);
        Assert.DoesNotContain("\n.bit-bdg.bit-dis .bit-bdg-ctn {", stylesheet, "The disabled colors are set by a heavier rule of their own.");
    }

    [TestMethod]
    public void BitBadgeShouldHandTheDotToTheBadgeWithoutOutweighingAClassOfItsOwn()
    {
        var stylesheet = ReadStylesheet();

        // A dot is a circle of its own size whatever the public height, padding and radius say, read first by the one
        // rule painting the badge rather than set by a heavier rule, so a Classes.Badge class can still re-size it.
        Assert.Contains("height: var(--bit-bdg-dot-size, var(--bit-bdg-height, var(--bit-Badge-height, ", stylesheet);
        Assert.Contains("padding: var(--bit-bdg-dot-padding, var(--bit-bdg-one-padding, var(--bit-bdg-padding, var(--bit-Badge-padding, ", stylesheet);
        Assert.Contains("border-radius: var(--bit-bdg-dot-radius, var(--bit-bdg-radius, var(--bit-Badge-radius, ", stylesheet);
        Assert.DoesNotContain("\n.bit-bdg-dot .bit-bdg-ctn {", stylesheet, "The dot is set by a heavier rule of its own.");
    }

    [TestMethod]
    public void BitBadgeShouldDropTheSidePaddingOfASingleCharacterTheWayTheDotDoes()
    {
        var stylesheet = ReadStylesheet();

        // A single character takes no side padding, so the minimum width - the height - is its width in any font. It is
        // read next after the dot's by the one rule painting the badge, like the dot's, rather than set by a rule of its own.
        Assert.Contains("--bit-bdg-one-padding: 0;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-one {"));
        Assert.Contains("min-width: var(--bit-bdg-dot-size, var(--bit-bdg-height, var(--bit-Badge-height, ", stylesheet);
        Assert.DoesNotContain("\n.bit-bdg-one .bit-bdg-ctn {", stylesheet, "The single character is set by a heavier rule of its own.");
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitBadgeShouldReadItsHeightAndDotFromTheTheme(string size)
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-bdg-{size} {{");

        // The height and the dot are design-system decisions, so a preset re-sizes every badge through the theme.
        Assert.Contains($"--bit-bdg-height: #{{$siz-badge-{size}}};", block);
        Assert.Contains($"--bit-bdg-dotsize: #{{$siz-badge-dot-{size}}};", block);
    }

    [TestMethod]
    public void BitBadgeInTheFlowShouldTakeThePointerBackWithoutOutweighingTheDisabledState()
    {
        var stylesheet = ReadStylesheet();

        // The rule undoing the overlay insets of a standalone badge weighs as much as three classes, which would outweigh
        // the disabled button and link (.bit-bdg-clk:disabled, [aria-disabled="true"]) and hand them the pointer back.
        var insets = SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg > .bit-bdg-stl > .bit-bdg-ctn {");
        Assert.DoesNotContain("pointer-events", insets, "The heavy standalone rule sets the pointer events.");

        // The pointer is taken back by a rule of the weight of .bit-bdg-ctn alone, which the disabled one outweighs.
        var pointer = SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-ctn:where(.bit-bdg-stl > *) {");
        Assert.Contains("pointer-events: auto;", pointer);

        var disabled = SourceFiles.GetScssBlock(stylesheet, "\n    &:disabled,\n    &[aria-disabled=\"true\"] {");
        Assert.Contains("pointer-events: none;", disabled);
    }

    [TestMethod]
    public void BitBadgeShouldKeepItsOwnLookAwayFromABadgeNestedInItsChildContent()
    {
        var rules = SourceFiles.StripScssComments(ReadStylesheet()).Split('\n');

        // A badge can sit in the child content of another one, so every rule that reaches the badge from a class of
        // the root does it through child combinators: a descendant selector would hand the outer badge's corner,
        // ring, pulse or order to the inner one too.
        var nested = rules.Where(line => NestedBadgeSelector().IsMatch(line) && line.StartsWith('.') is false).ToArray();

        Assert.IsNotEmpty(nested, "No rule reaches the badge from a class of the root.");

        foreach (var line in nested)
        {
            Assert.Contains("> .bit-bdg-wrp > ", line, $"'{line.Trim()}' reaches the badge through a descendant selector.");
        }

        Assert.DoesNotContain(line => line.StartsWith('.') && DescendantBadgeSelector().IsMatch(line), rules, "A top-level rule reaches the badge through a descendant selector.");

        // The offsets are handed over through custom properties set on the root's style, which inherit just as well,
        // so each root starts them out unset.
        var stylesheet = ReadStylesheet();
        Assert.Contains("--bit-bdg-ofs-x: initial;", stylesheet);
        Assert.Contains("--bit-bdg-ofs-y: initial;", stylesheet);
    }

    [TestMethod]
    public void BitBadgeShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size or Shape publishes these, so they are read before the variable, which only restyles the
        // default an unset one stands for.
        Assert.Contains("var(--bit-bdg-height, var(--bit-Badge-height, #{$siz-badge-md}))", stylesheet);
        Assert.Contains("var(--bit-bdg-padding, var(--bit-Badge-padding, ", stylesheet);
        Assert.Contains("var(--bit-bdg-fontsize, var(--bit-Badge-font-size, #{$tg-fs-sm}))", stylesheet);
        Assert.Contains("var(--bit-bdg-radius, var(--bit-Badge-radius, #{$shp-radius-full}))", stylesheet);
        Assert.Contains("var(--bit-bdg-dotsize, var(--bit-Badge-dot-size, #{$siz-badge-dot-md}))", stylesheet);

        // So does an explicit Color, for every color it paints.
        Assert.Contains("--bit-bdg-cnt-clr-bg: var(--bit-bdg-clr, var(--bit-Badge-fill-background, var(--bit-Badge-background, #{$clr-pri})));", stylesheet);
        Assert.Contains("--bit-bdg-cnt-clr-txt: var(--bit-bdg-clr-txt, var(--bit-Badge-fill-color, var(--bit-Badge-color, #{$clr-pri-text})));", stylesheet);
        Assert.Contains("focus-ring-own(var(--bit-bdg-clr-fcs, var(--bit-Badge-focus-color)))", stylesheet);
        Assert.Contains("var(--bit-bdg-clr, var(--bit-Badge-pulse-color, #{$clr-pri}))", stylesheet);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Badge-[a-z-]+, var\(--bit-bdg-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-bdg-fil"),
        DataRow("outline", ".bit-bdg-otl"),
        DataRow("text", ".bit-bdg-txt")]
    public void BitBadgeShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of every shared color variable, so a theme can set one look apart
        // from the other two on :root - an outline badge's neutral border without a ring around every filled one.
        foreach (var name in new[] { "color", "background", "border-color", "hover-background", "active-background" })
        {
            Assert.Contains($"var(--bit-Badge-{variant}-{name}, var(--bit-Badge-{name}, ", block, $"{selector} does not read --bit-Badge-{variant}-{name} ahead of --bit-Badge-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-Badge-{variant}-[a-z-]+, )var\(--bit-Badge-(?:hover-|active-)?(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Badge-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod,
        DataRow(".bit-bdg-otl", "outline"),
        DataRow(".bit-bdg-txt", "text")]
    public void BitBadgeShouldDrawTheCountOfAnUnfilledVariantInTheRoleTextColor(string selector, string variant)
    {
        var stylesheet = ReadStylesheet();

        // The count of an Outline or Text badge sits on the page rather than on the role's fill, so it is drawn in the
        // role's text color: a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white.
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");
        Assert.Contains($"--bit-bdg-cnt-clr-txt: var(--bit-bdg-clr-fg, var(--bit-Badge-{variant}-color, var(--bit-Badge-color, #{{$clr-pri}})));", block);

        // A clickable one is hovered and pressed on a wash of the role - its tint, the surface that text color is solved to
        // be read on - rather than on its light tier, a pastel which in a dark palette is as light as the text.
        Assert.Contains($"--bit-bdg-cnt-clr-bg-hover: var(--bit-bdg-clr-tint, var(--bit-Badge-{variant}-hover-background, ", block);
        Assert.Contains("#{$clr-pri-tint})))));", block);
        Assert.Contains($"--bit-bdg-cnt-clr-bg-active: var(--bit-bdg-clr-tint-active, var(--bit-Badge-{variant}-active-background, ", block);
        Assert.Contains("#{translucent($clr-pri, 16%)})))))));", block);
        Assert.DoesNotMatchRegex(new Regex("clr-[a-z-]*light"), block);

        // Over that wash the count is drawn in the role's fg, the primary role's too: its main, which the count keeps at
        // rest, drops under 4.5:1 on its own tint. Both states hand it over through a variable, never as a paint.
        Assert.Contains($"--bit-bdg-cnt-clr-txt-wash: var(--bit-bdg-clr-wash-fg, var(--bit-Badge-{variant}-color, var(--bit-Badge-color, #{{$clr-pri-fg}})));", block);
        var clickable = SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-clk {");
        Assert.Contains("--bit-bdg-cnt-sfg: var(--bit-bdg-cnt-clr-txt-wash);", SourceFiles.GetScssDeclarations(clickable, "&:hover {"));
        Assert.Contains("--bit-bdg-cnt-sfg: var(--bit-bdg-cnt-clr-txt-wash);", SourceFiles.GetScssDeclarations(clickable, "&:active {"));
        Assert.Contains("--bit-bdg-cnt-sfg: initial;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-ctn {"));
        Assert.DoesNotContain("--bit-bdg-cnt-clr-txt-wash", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-fil {"), "A Fill badge keeps the count it has at rest.");

        var roles = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-bdg-#{$role} {");
        Assert.Contains("--bit-bdg-clr-fg: #{role-text($role, $tokens)};", roles);
        Assert.Contains("--bit-bdg-clr-wash-fg: #{role($tokens, fg)};", roles);
        Assert.Contains("--bit-bdg-clr-tint: #{role-wash($tokens)};", roles);
        Assert.Contains("--bit-bdg-clr-tint-active: #{role-wash($tokens, 16%)};", roles);
    }

    [TestMethod]
    public void BitBadgeShouldPublishItsColorSizeAndShapeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-bdg {");

        // A badge can sit in the child content of another one, which must not inherit the outer badge's Color, Size or
        // Shape: each root starts the values those classes publish out unset, and the classes - declared further down
        // at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-bdg-clr", "--bit-bdg-clr-fg", "--bit-bdg-clr-wash-fg", "--bit-bdg-clr-txt", "--bit-bdg-clr-dis", "--bit-bdg-clr-dis-text",
                                         "--bit-bdg-clr-hover", "--bit-bdg-clr-active", "--bit-bdg-clr-tint", "--bit-bdg-clr-tint-active", "--bit-bdg-clr-fcs",
                                         // Only Outline and Text declare it, so a Fill badge in another one's child content does not inherit it.
                                         "--bit-bdg-cnt-clr-txt-wash",
                                         "--bit-bdg-height", "--bit-bdg-padding", "--bit-bdg-fontsize", "--bit-bdg-dotsize", "--bit-bdg-radius" })
        {
            Assert.Contains($"{property}: initial;", root);
        }
    }

    [TestMethod]
    public void BitBadgeShouldStopThePulseUnderReducedMotionUnlessAnimationIsForced()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-pls {");

        // An endless attention cue has to be stoppable (WCAG 2.2.2), so reduced motion stops it outright rather than
        // slowing it the way it slows a loader, while ForceAnimation - on the root or on an ancestor - opts back in.
        Assert.Contains("@media (prefers-reduced-motion: reduce)", block);
        Assert.Contains("&:not(.bit-fam):not(.bit-fam *) > .bit-bdg-wrp > .bit-bdg-ctn::after {", block);
        Assert.Contains("animation: none;", block);
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Notifications", "Badge", "BitBadge.scss");

    [GeneratedRegex(@"^//\s+(--bit-Badge-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Badge-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Badge-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    // A selector, rather than a declaration, that ends on the badge or on the clickable badge.
    [GeneratedRegex(@"\.bit-bdg-(ctn|clk)\b[^;]*\{\s*$")]
    private static partial Regex NestedBadgeSelector();

    // A top-level selector that reaches the badge from an ancestor class without a child combinator.
    [GeneratedRegex(@"\.bit-bdg[a-z-]*\s+\.bit-bdg-(wrp|stl|ctn|clk)\b")]
    private static partial Regex DescendantBadgeSelector();
}
