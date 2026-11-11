using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

/// <summary>
/// Pins what a bUnit render cannot see of the nav: its public --bit-Nav-* variables, which the stylesheet reads and
/// the header comment of that stylesheet documents, and the few layout rules the rendered markup relies on.
/// </summary>
[TestClass]
public class BitNavStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Nav-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Nav-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Nav-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitNavShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        Assert.AreSequenceEqual(documented.Order().ToArray(), read.Order().ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The --bit-Nav-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitNavShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every nav below
        // it: a declaration on the nav would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitNavShouldLetAGroupHeaderGrowWithItsDescription()
    {
        var header = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-gcb {");

        // A header with a description stacks two lines, which a fixed height would let spill over the rule under it.
        Assert.Contains("min-height: var(--bit-nav-header-min-height, var(--bit-Nav-header-min-height", header);
        Assert.IsFalse(Regex.IsMatch(header, @"(?<![-\w])height\s*:"), "The group header has a fixed height.");
    }

    [TestMethod]
    public void BitNavShouldPutNoRoomBetweenTheChevronAndTheContentOfAnItem()
    {
        // A childless sibling makes up for the chevron with IndentPadding alone, so a gap here would push the text of
        // a parent out of line with it.
        var row = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-mct {");

        Assert.DoesNotContain("gap", row, "The row of an item puts a gap between its chevron and its content.");
    }

    [TestMethod]
    public void BitNavShouldGiveAPressedItemFeedbackOnEveryPointer()
    {
        var item = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-ict {");

        var hover = item.IndexOf("@media (hover: hover)", StringComparison.Ordinal);
        var active = item.IndexOf("&:active {", StringComparison.Ordinal);

        // The hover is kept to the pointers that can hover, the pressed state is not: it is outside that media query.
        Assert.IsGreaterThan(0, active, "The item has no pressed state.");
        Assert.IsGreaterThan(item.IndexOf("\n    }", hover, StringComparison.Ordinal), active, "The pressed state is kept to the pointers that can hover.");
        Assert.Contains("var(--bit-Nav-pressed-background", item[active..]);
    }

    [TestMethod]
    public void BitNavShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium nav
        // an unset one stands for.
        Assert.Contains("font-size: var(--bit-nav-item-font-size, var(--bit-Nav-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("min-height: var(--bit-nav-item-min-height, var(--bit-Nav-item-min-height, #{spacing(6)}));", stylesheet);
        Assert.Contains("font-size: var(--bit-nav-icon-size, var(--bit-Nav-icon-size, #{$siz-icon-md}));", stylesheet);
        Assert.Contains("font-size: var(--bit-nav-header-font-size, var(--bit-Nav-header-font-size, #{$tg-fs-lg}));", stylesheet);

        // So does an explicit Color for the icon and the indicator, and an explicit Accent for the hover, the press
        // and the selection; an unset Accent is the PrimaryBackground one.
        // (The states set the state variables the rules at rest read first, rather than painting the colors themselves.)
        Assert.Contains("color: var(--bit-nav-sfg, var(--bit-nav-clr, #{$nav-icon-color}));", stylesheet);
        Assert.Contains("border-color: var(--bit-nav-clr, var(--bit-Nav-indicator-color, #{$clr-pri}));", stylesheet);
        Assert.Contains("--bit-nav-sbg: var(--bit-nav-clr-hover, var(--bit-Nav-hover-background, #{$clr-bg-pri-hover}));", stylesheet);
        Assert.Contains("background-color: var(--bit-nav-sbg, var(--bit-nav-clr-active, var(--bit-Nav-selected-background, #{$clr-bg-pri-active})));", stylesheet);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Nav-[a-z-]+, var\(--bit-nav-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitNavShouldLetEveryStateRecolorTheIconAndTheDescriptionOfItsItem()
    {
        var stylesheet = ReadStylesheet();

        // A theme that gives a state a vivid fill moves the text with --bit-Nav-*-color; without a variable of their own
        // the icon and the description stayed in their colors at rest, unreadable on that fill. Each falls back on the
        // one at rest, so a nav that sets none of them looks as it did.
        Assert.Contains("$nav-icon-color: var(--bit-Nav-icon-color, #{$clr-pri});", stylesheet);
        Assert.Contains("$nav-description-color: var(--bit-Nav-description-color, #{$clr-fg-ter});", stylesheet);
        Assert.Contains("$nav-hover-color: var(--bit-Nav-hover-color, #{$clr-fg-pri});", stylesheet);
        Assert.Contains("$nav-hover-icon-color: var(--bit-Nav-hover-icon-color, #{$nav-icon-color});", stylesheet);
        Assert.Contains("$nav-hover-description-color: var(--bit-Nav-hover-description-color, #{$nav-description-color});", stylesheet);

        // Each state reads its own variables, in its own rule: a string searched for in the whole stylesheet would be
        // found in another state's fallbacks too.
        var item = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-ict {");
        var hover = SourceFiles.GetScssBlock(item, "&:hover {");
        Assert.Contains("@include accented-item-content($nav-hover-color, $nav-hover-icon-color, $nav-hover-description-color, $state: true);", hover);

        // On a touch screen no hover ever applies, so the press falls back on the colors at rest, not on a hover's a
        // theme set for a fill the tap never shows; where the pointer can hover, a press is a hover too, and that rule
        // comes later to win there.
        var pressed = SourceFiles.GetScssBlock(item, "\n    &:active {");
        Assert.Contains("var(--bit-Nav-pressed-color, #{$clr-fg-pri}),", pressed);
        Assert.Contains("var(--bit-Nav-pressed-icon-color, #{$nav-icon-color}),", pressed);
        Assert.Contains("var(--bit-Nav-pressed-description-color, #{$nav-description-color}),", pressed);
        Assert.IsFalse(pressed.Contains("hover", StringComparison.Ordinal), "A press on a touch screen falls back on the hovered colors.");

        var hoverablePressAt = item.LastIndexOf("@media (hover: hover) {", StringComparison.Ordinal);
        Assert.IsGreaterThan(item.IndexOf("\n    &:active {", StringComparison.Ordinal), hoverablePressAt, "The press of a pointer that can hover is not declared after the one of every pointer.");
        var hoverablePress = SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(item[hoverablePressAt..], "@media (hover: hover) {"), "&:active {");
        Assert.Contains("var(--bit-Nav-pressed-color, #{$nav-hover-color}),", hoverablePress);
        Assert.Contains("var(--bit-Nav-pressed-icon-color, #{$nav-hover-icon-color}),", hoverablePress);
        Assert.Contains("var(--bit-Nav-pressed-description-color, #{$nav-hover-description-color}),", hoverablePress);

        var selected = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-sel,");
        Assert.Contains("var(--bit-Nav-selected-color, #{$clr-fg-pri}),", selected);
        Assert.Contains("var(--bit-Nav-selected-icon-color, #{$nav-icon-color}),", selected);
        Assert.Contains("var(--bit-Nav-selected-description-color, #{$nav-description-color}));", selected);

        // An explicit Accent and an explicit Color still win over them: the mixin reads its own private variables first.
        var mixin = SourceFiles.GetScssBlock(stylesheet, "@mixin accented-item-content(");
        Assert.Contains("$text: var(--bit-nav-clr-text, #{$color});", mixin);
        Assert.Contains("$icon: var(--bit-nav-clr-icon, var(--bit-nav-clr, #{$icon-color}));", mixin);
        Assert.Contains("$des: var(--bit-nav-clr-des, #{$description-color});", mixin);
    }

    [TestMethod]
    public void BitNavShouldKeepTheSelectionUnderThePointer()
    {
        var stylesheet = ReadStylesheet();

        // Right after a click the pointer is still on the item just picked, so the hover and the press of the selected
        // item (and of the collapsed branch that holds it) give their feedback on the selected fill, in the selected
        // colors, rather than taking the hover's and the press's, which turned a vivid selection into the neutral hover
        // grey - and still give one, since a collapsed branch is a control the pointer toggles.
        var selected = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-sel,");
        var hover = SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(selected, "@media (hover: hover) {"), "&:hover:not(:disabled) {");
        var pressed = SourceFiles.GetScssBlock(selected, "\n    &:active:not(:disabled) {");
        Assert.Contains("@include selected-item-state(--bit-nav-clr-sel-hover, $nav-selected-hover-background, $nav-selected-hover-background-fallback);", hover);
        Assert.Contains("@include selected-item-state(--bit-nav-clr-sel-active, $nav-selected-pressed-background, $nav-selected-pressed-background-fallback);", pressed);

        // The fill reads the Accent's own state fill first, then the state's public variable, then the selected fill with
        // the selected color mixed in - the press over the hover's, so it is felt on a touch screen and over a hover fill
        // a theme set alike; color-mix sits behind @supports, with the selected fill kept where it is not understood.
        Assert.Contains("@return color-mix(in srgb, #{$on} 8%, #{$fill});", stylesheet);
        Assert.Contains("$nav-selected-hover-background: var(--bit-Nav-selected-hover-background, #{nav-selected-state-fill($nav-selected-color, $nav-selected-background)});", stylesheet);
        Assert.Contains("$nav-selected-pressed-background: var(--bit-Nav-selected-pressed-background, #{nav-selected-state-fill($nav-selected-hover-color, $nav-selected-hover-background)});", stylesheet);
        Assert.Contains("$nav-selected-hover-background-fallback: var(--bit-Nav-selected-hover-background, #{$nav-selected-background});", stylesheet);
        Assert.Contains("$nav-selected-pressed-background-fallback: var(--bit-Nav-selected-pressed-background, #{$nav-selected-hover-background-fallback});", stylesheet);

        var state = SourceFiles.GetScssBlock(stylesheet, "@mixin selected-item-state($accent-fill, $fill, $fallback) {");
        Assert.Contains("--bit-nav-sbg: var(#{$accent-fill}, #{$fallback});", state);
        Assert.Contains("--bit-nav-sbg: var(#{$accent-fill}, #{$fill});", SourceFiles.GetScssBlock(state, "@supports (color: color-mix(in srgb, red, blue)) {"));

        // Every part takes the selected state's own colors, which fall back on the selected ones, the Accent and Color
        // still read first; a collapsed group header paints its text from the one the state sets on it.
        Assert.Contains("@include accented-item-content($nav-selected-hover-color, $nav-selected-hover-icon-color, $nav-selected-hover-description-color, $state: true);", state);
        Assert.Contains("--bit-nav-sfg: var(--bit-nav-clr-text, #{$nav-selected-hover-color});", state);
        Assert.Contains("$nav-selected-hover-color: var(--bit-Nav-selected-hover-color, #{$nav-selected-color});", stylesheet);
        Assert.Contains("$nav-selected-hover-icon-color: var(--bit-Nav-selected-hover-icon-color, var(--bit-Nav-selected-icon-color, #{$nav-icon-color}));", stylesheet);
        Assert.Contains("$nav-selected-hover-description-color: var(--bit-Nav-selected-hover-description-color, var(--bit-Nav-selected-description-color, #{$nav-description-color}));", stylesheet);
        Assert.Contains("color: var(--bit-nav-sfg, var(--bit-nav-clr-text, var(--bit-Nav-selected-color, #{$clr-fg-pri})));", SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb.bit-nav-sca {"));
        Assert.Contains("--bit-nav-sfg: initial;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb {"));

        // An explicit Accent publishes those fills off its own selected fill and on-color, ahead of the public variables.
        var accent = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-nav-a#{$role} {");
        Assert.Contains("--bit-nav-clr-sel-hover: #{role($tokens, active)};", accent);
        Assert.Contains("--bit-nav-clr-sel-active: #{role($tokens, active)};", accent);
        var mixed = SourceFiles.GetScssBlock(accent, "@supports (color: color-mix(in srgb, red, blue)) {");
        Assert.Contains("--bit-nav-clr-sel-hover: #{nav-selected-state-fill(role($tokens, on), role($tokens, active))};", mixed);
        Assert.Contains("--bit-nav-clr-sel-active: #{nav-selected-state-fill(role($tokens, on), nav-selected-state-fill(role($tokens, on), role($tokens, active)))};", mixed);

        // The :not(:disabled) keeps a disabled group header out, and gives the selectors the weight of the header's own
        // states, (0,3,0) both, so it is coming later that lets them win there; the states of an item weigh (0,2,0) and
        // lose to them wherever they are declared.
        var selectedAt = stylesheet.IndexOf("\n.bit-nav-sel,", StringComparison.Ordinal);
        Assert.Contains("&:hover:not(:disabled, .bit-nav-gst) {", SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb {"));
        Assert.Contains("&:active:not(:disabled, .bit-nav-gst) {", SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb {"));
        Assert.IsGreaterThan(stylesheet.IndexOf("\n.bit-nav-gcb {", StringComparison.Ordinal), selectedAt, "The selected item's states are declared ahead of the group header's, which weigh as much.");
    }

    [TestMethod]
    public void BitNavShouldLetEveryAccentWinOverTheStateColorsOfTheIconAndTheDescription()
    {
        var accent = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-nav-a#{$role} {");

        // A semantic accent draws them in its on-color. A surface one keeps them in the colors they have at rest on its
        // tint - but publishes them all the same, so a hovered, pressed or selected variable a theme set for its own
        // fill never lands on the accent's.
        Assert.Contains("--bit-nav-clr-icon: #{role($tokens, on)};", accent);
        Assert.Contains("--bit-nav-clr-des: #{role($tokens, on)};", accent);
        Assert.Contains("--bit-nav-clr-icon: var(--bit-nav-clr, #{$nav-icon-color});", accent);
        Assert.Contains("--bit-nav-clr-des: #{$nav-description-color};", accent);
    }

    [TestMethod]
    public void BitNavShouldPublishItsColorAccentAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav {");

        // A nav never inherits another one's Color, Accent or Size from an ancestor carrying those classes: each root
        // starts the values they publish out unset, and the classes - declared further down at the same weight - still
        // win on the root that carries them.
        foreach (var property in new[] { "--bit-nav-item-font-size", "--bit-nav-item-min-height", "--bit-nav-icon-size", "--bit-nav-des-font-size",
                                         "--bit-nav-header-font-size", "--bit-nav-header-min-height", "--bit-nav-chevron-size", "--bit-nav-clr",
                                         "--bit-nav-clr-text", "--bit-nav-clr-hover", "--bit-nav-clr-active", "--bit-nav-clr-sel-hover",
                                         "--bit-nav-clr-sel-active", "--bit-nav-clr-icon", "--bit-nav-clr-des" })
        {
            Assert.Contains($"{property}: initial;", root);
        }

        var rootAt = stylesheet.IndexOf("\n.bit-nav {", StringComparison.Ordinal);
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-nav-md {", StringComparison.Ordinal), rootAt, "The size classes are declared ahead of the root that resets them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n    .bit-nav-a#{$role} {", StringComparison.Ordinal), rootAt, "The accent classes are declared ahead of the root that resets them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n    .bit-nav-#{$role} {", StringComparison.Ordinal), rootAt, "The color classes are declared ahead of the root that resets them.");
    }

    [TestMethod]
    public void BitNavShouldDrawTheBarOfTheSelectedItemAloneInForcedColors()
    {
        const string forcedColors = "@media (forced-colors: active)";
        const string item = ".bit-nav-ict";
        const string selected = ".bit-nav-sel, .bit-nav-sca";
        const string hidden = "@include forced-hidden-border";
        const string floor = "border-inline-start-width: max(var(--bit-Nav-indicator-width, #{$shp-border-width-thick}), #{$shp-border-width-thick})";

        // Every rule of the forced-colors block that touches a border, in the order written, nested ones included. Any
        // border declaration counts - a style, a width, a shorthand of any side - since each can hide the bar or move the
        // content when the selection does.
        var borderRules = SourceFiles.GetScssRules(ReadStylesheet())
            .Where(rule => rule.Ancestors.Contains(forcedColors))
            .Select(rule => (Header: Regex.Replace(rule.Header, @"\s+", " "),
                             Ancestors: string.Join(" > ", rule.Ancestors),
                             Borders: rule.Declarations.Where(d => d.StartsWith("border", StringComparison.Ordinal) || d.StartsWith(hidden, StringComparison.Ordinal))
                                                       .Select(d => Regex.Replace(d, @"\s+", " "))
                                                       .ToArray()))
            .Where(rule => rule.Borders.Length > 0)
            .ToArray();

        // The transparent border every item reserves the bar with is forced to a system color like any other, so the
        // items draw it in Canvas, their own background in this mode, at a width the public variable cannot take below
        // the thick stroke - the bar being the one mark of the selection left - and the selected ones in Highlight. Two
        // rules, both straight in the block, and nothing else there touching a border. The list is in the order the
        // parser read the rules in, so it also pins the selected item's rule after the item's, which weighs the same and
        // so wins by coming later - a copy of the item's rule after it would be a third entry.
        Assert.AreSequenceEqual(new[] { $"{item}: {hidden}; {floor}", $"{selected}: border-color: Highlight" }, borderRules.Select(rule => $"{rule.Header}: {string.Join("; ", rule.Borders)}").ToArray(), "The forced-colors block draws the bar some other way than the two rules that hide it and then mark the selection, in that order.");
        Assert.IsTrue(borderRules.All(rule => rule.Ancestors == forcedColors), "A border rule of the forced-colors block is nested in another rule.");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Navs", "Nav", "BitNav.scss");
}
