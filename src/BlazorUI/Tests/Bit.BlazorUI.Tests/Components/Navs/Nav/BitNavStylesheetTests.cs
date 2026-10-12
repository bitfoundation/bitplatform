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

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Nav-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitNavShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every nav below
        // it: a declaration on the nav would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitNavShouldLetAGroupHeaderGrowWithItsDescription()
    {
        var header = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-gcb {");

        // A header with a description stacks two lines, which a fixed height would let spill over the rule under it.
        StringAssert.Contains(header, "min-height: var(--bit-nav-header-min-height, var(--bit-Nav-header-min-height");
        Assert.IsFalse(Regex.IsMatch(header, @"(?<![-\w])height\s*:"), "The group header has a fixed height.");
    }

    [TestMethod]
    public void BitNavShouldPutNoRoomBetweenTheChevronAndTheContentOfAnItem()
    {
        // A childless sibling makes up for the chevron with IndentPadding alone, so a gap here would push the text of
        // a parent out of line with it.
        var row = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-mct {");

        Assert.IsFalse(row.Contains("gap"), "The row of an item puts a gap between its chevron and its content.");
    }

    [TestMethod]
    public void BitNavShouldGiveAPressedItemFeedbackOnEveryPointer()
    {
        var item = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-ict {");

        var hover = item.IndexOf("@media (hover: hover)", StringComparison.Ordinal);
        var active = item.IndexOf("&:active {", StringComparison.Ordinal);

        // The hover is kept to the pointers that can hover, the pressed state is not: it is outside that media query.
        Assert.IsTrue(active > 0, "The item has no pressed state.");
        Assert.IsTrue(active > item.IndexOf("\n    }", hover, StringComparison.Ordinal), "The pressed state is kept to the pointers that can hover.");
        StringAssert.Contains(item[active..], "var(--bit-Nav-pressed-background");
    }

    [TestMethod]
    public void BitNavShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium nav
        // an unset one stands for.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-nav-item-font-size, var(--bit-Nav-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "min-height: var(--bit-nav-item-min-height, var(--bit-Nav-item-min-height, #{spacing(6)}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-nav-icon-size, var(--bit-Nav-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-nav-header-font-size, var(--bit-Nav-header-font-size, #{$tg-fs-lg}));");

        // So does an explicit Color for the icon and the indicator, and an explicit Accent for the hover, the press
        // and the selection; an unset Accent is the PrimaryBackground one.
        // (The states set the state variables the rules at rest read first, rather than painting the colors themselves.)
        StringAssert.Contains(stylesheet, "color: var(--bit-nav-sfg, var(--bit-nav-clr, #{$nav-icon-color}));");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-nav-clr, var(--bit-Nav-indicator-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-nav-sbg: var(--bit-nav-clr-hover, var(--bit-Nav-hover-background, #{$clr-bg-pri-hover}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-nav-sbg, var(--bit-nav-clr-active, var(--bit-Nav-selected-background, #{$clr-bg-pri-active})));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Nav-[a-z-]+, var\(--bit-nav-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitNavShouldLetEveryStateRecolorTheIconAndTheDescriptionOfItsItem()
    {
        var stylesheet = ReadStylesheet();

        // A theme that gives a state a vivid fill moves the text with --bit-Nav-*-color; without a variable of their own
        // the icon and the description stayed in their colors at rest, unreadable on that fill. Each falls back on the
        // one at rest, so a nav that sets none of them looks as it did.
        StringAssert.Contains(stylesheet, "$nav-icon-color: var(--bit-Nav-icon-color, #{$clr-pri});");
        StringAssert.Contains(stylesheet, "$nav-description-color: var(--bit-Nav-description-color, #{$clr-fg-ter});");
        StringAssert.Contains(stylesheet, "$nav-hover-color: var(--bit-Nav-hover-color, #{$clr-fg-pri});");
        StringAssert.Contains(stylesheet, "$nav-hover-icon-color: var(--bit-Nav-hover-icon-color, #{$nav-icon-color});");
        StringAssert.Contains(stylesheet, "$nav-hover-description-color: var(--bit-Nav-hover-description-color, #{$nav-description-color});");

        // Each state reads its own variables, in its own rule: a string searched for in the whole stylesheet would be
        // found in another state's fallbacks too.
        var item = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-ict {");
        var hover = SourceFiles.GetScssBlock(item, "&:hover {");
        StringAssert.Contains(hover, "@include accented-item-content($nav-hover-color, $nav-hover-icon-color, $nav-hover-description-color, $state: true);");

        // On a touch screen no hover ever applies, so the press falls back on the colors at rest, not on a hover's a
        // theme set for a fill the tap never shows; where the pointer can hover, a press is a hover too, and that rule
        // comes later to win there.
        var pressed = SourceFiles.GetScssBlock(item, "\n    &:active {");
        StringAssert.Contains(pressed, "var(--bit-Nav-pressed-color, #{$clr-fg-pri}),");
        StringAssert.Contains(pressed, "var(--bit-Nav-pressed-icon-color, #{$nav-icon-color}),");
        StringAssert.Contains(pressed, "var(--bit-Nav-pressed-description-color, #{$nav-description-color}),");
        Assert.IsFalse(pressed.Contains("hover", StringComparison.Ordinal), "A press on a touch screen falls back on the hovered colors.");

        var hoverablePressAt = item.LastIndexOf("@media (hover: hover) {", StringComparison.Ordinal);
        Assert.IsTrue(hoverablePressAt > item.IndexOf("\n    &:active {", StringComparison.Ordinal), "The press of a pointer that can hover is not declared after the one of every pointer.");
        var hoverablePress = SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(item[hoverablePressAt..], "@media (hover: hover) {"), "&:active {");
        StringAssert.Contains(hoverablePress, "var(--bit-Nav-pressed-color, #{$nav-hover-color}),");
        StringAssert.Contains(hoverablePress, "var(--bit-Nav-pressed-icon-color, #{$nav-hover-icon-color}),");
        StringAssert.Contains(hoverablePress, "var(--bit-Nav-pressed-description-color, #{$nav-hover-description-color}),");

        var selected = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-sel,");
        StringAssert.Contains(selected, "var(--bit-Nav-selected-color, #{$clr-fg-pri}),");
        StringAssert.Contains(selected, "var(--bit-Nav-selected-icon-color, #{$nav-icon-color}),");
        StringAssert.Contains(selected, "var(--bit-Nav-selected-description-color, #{$nav-description-color}));");

        // An explicit Accent and an explicit Color still win over them: the mixin reads its own private variables first.
        var mixin = SourceFiles.GetScssBlock(stylesheet, "@mixin accented-item-content(");
        StringAssert.Contains(mixin, "$text: var(--bit-nav-clr-text, #{$color});");
        StringAssert.Contains(mixin, "$icon: var(--bit-nav-clr-icon, var(--bit-nav-clr, #{$icon-color}));");
        StringAssert.Contains(mixin, "$des: var(--bit-nav-clr-des, #{$description-color});");
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
        StringAssert.Contains(hover, "@include selected-item-state(--bit-nav-clr-sel-hover, $nav-selected-hover-background, $nav-selected-hover-background-fallback);");
        StringAssert.Contains(pressed, "@include selected-item-state(--bit-nav-clr-sel-active, $nav-selected-pressed-background, $nav-selected-pressed-background-fallback);");

        // The fill reads the Accent's own state fill first, then the state's public variable, then the selected fill with
        // the selected color mixed in - the press over the hover's, so it is felt on a touch screen and over a hover fill
        // a theme set alike; color-mix sits behind @supports, with the selected fill kept where it is not understood.
        StringAssert.Contains(stylesheet, "@return color-mix(in srgb, #{$on} 8%, #{$fill});");
        StringAssert.Contains(stylesheet, "$nav-selected-hover-background: var(--bit-Nav-selected-hover-background, #{nav-selected-state-fill($nav-selected-color, $nav-selected-background)});");
        StringAssert.Contains(stylesheet, "$nav-selected-pressed-background: var(--bit-Nav-selected-pressed-background, #{nav-selected-state-fill($nav-selected-hover-color, $nav-selected-hover-background)});");
        StringAssert.Contains(stylesheet, "$nav-selected-hover-background-fallback: var(--bit-Nav-selected-hover-background, #{$nav-selected-background});");
        StringAssert.Contains(stylesheet, "$nav-selected-pressed-background-fallback: var(--bit-Nav-selected-pressed-background, #{$nav-selected-hover-background-fallback});");

        var state = SourceFiles.GetScssBlock(stylesheet, "@mixin selected-item-state($accent-fill, $fill, $fallback) {");
        StringAssert.Contains(state, "--bit-nav-sbg: var(#{$accent-fill}, #{$fallback});");
        StringAssert.Contains(SourceFiles.GetScssBlock(state, "@supports (color: color-mix(in srgb, red, blue)) {"), "--bit-nav-sbg: var(#{$accent-fill}, #{$fill});");

        // Every part takes the selected state's own colors, which fall back on the selected ones, the Accent and Color
        // still read first; a collapsed group header paints its text from the one the state sets on it.
        StringAssert.Contains(state, "@include accented-item-content($nav-selected-hover-color, $nav-selected-hover-icon-color, $nav-selected-hover-description-color, $state: true);");
        StringAssert.Contains(state, "--bit-nav-sfg: var(--bit-nav-clr-text, #{$nav-selected-hover-color});");
        StringAssert.Contains(stylesheet, "$nav-selected-hover-color: var(--bit-Nav-selected-hover-color, #{$nav-selected-color});");
        StringAssert.Contains(stylesheet, "$nav-selected-hover-icon-color: var(--bit-Nav-selected-hover-icon-color, var(--bit-Nav-selected-icon-color, #{$nav-icon-color}));");
        StringAssert.Contains(stylesheet, "$nav-selected-hover-description-color: var(--bit-Nav-selected-hover-description-color, var(--bit-Nav-selected-description-color, #{$nav-description-color}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb.bit-nav-sca {"), "color: var(--bit-nav-sfg, var(--bit-nav-clr-text, var(--bit-Nav-selected-color, #{$clr-fg-pri})));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb {"), "--bit-nav-sfg: initial;");

        // An explicit Accent publishes those fills off its own selected fill and on-color, ahead of the public variables.
        var accent = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-nav-a#{$role} {");
        StringAssert.Contains(accent, "--bit-nav-clr-sel-hover: #{role($tokens, active)};");
        StringAssert.Contains(accent, "--bit-nav-clr-sel-active: #{role($tokens, active)};");
        var mixed = SourceFiles.GetScssBlock(accent, "@supports (color: color-mix(in srgb, red, blue)) {");
        StringAssert.Contains(mixed, "--bit-nav-clr-sel-hover: #{nav-selected-state-fill(role($tokens, on), role($tokens, active))};");
        StringAssert.Contains(mixed, "--bit-nav-clr-sel-active: #{nav-selected-state-fill(role($tokens, on), nav-selected-state-fill(role($tokens, on), role($tokens, active)))};");

        // The :not(:disabled) keeps a disabled group header out, and gives the selectors the weight of the header's own
        // states, (0,3,0) both, so it is coming later that lets them win there; the states of an item weigh (0,2,0) and
        // lose to them wherever they are declared.
        var selectedAt = stylesheet.IndexOf("\n.bit-nav-sel,", StringComparison.Ordinal);
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb {"), "&:hover:not(:disabled, .bit-nav-gst) {");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-nav-gcb {"), "&:active:not(:disabled, .bit-nav-gst) {");
        Assert.IsTrue(selectedAt > stylesheet.IndexOf("\n.bit-nav-gcb {", StringComparison.Ordinal), "The selected item's states are declared ahead of the group header's, which weigh as much.");
    }

    [TestMethod]
    public void BitNavShouldLetEveryAccentWinOverTheStateColorsOfTheIconAndTheDescription()
    {
        var accent = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-nav-a#{$role} {");

        // A semantic accent draws them in its on-color. A surface one keeps them in the colors they have at rest on its
        // tint - but publishes them all the same, so a hovered, pressed or selected variable a theme set for its own
        // fill never lands on the accent's.
        StringAssert.Contains(accent, "--bit-nav-clr-icon: #{role($tokens, on)};");
        StringAssert.Contains(accent, "--bit-nav-clr-des: #{role($tokens, on)};");
        StringAssert.Contains(accent, "--bit-nav-clr-icon: var(--bit-nav-clr, #{$nav-icon-color});");
        StringAssert.Contains(accent, "--bit-nav-clr-des: #{$nav-description-color};");
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
            StringAssert.Contains(root, $"{property}: initial;");
        }

        var rootAt = stylesheet.IndexOf("\n.bit-nav {", StringComparison.Ordinal);
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n.bit-nav-md {", StringComparison.Ordinal), "The size classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n    .bit-nav-a#{$role} {", StringComparison.Ordinal), "The accent classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n    .bit-nav-#{$role} {", StringComparison.Ordinal), "The color classes are declared ahead of the root that resets them.");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Navs", "Nav", "BitNav.scss");
}
