using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Tag;

/// <summary>
/// Pins the public CSS variables of the tag, which a bUnit render cannot see: they are read off the root with a
/// fallback and never declared, so a value set on an ancestor or on the Style of one tag is the one that applies.
/// </summary>
[TestClass]
public class BitTagStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Tag-color",
        "--bit-Tag-background",
        "--bit-Tag-border-color",
        "--bit-Tag-icon-color",
        "--bit-Tag-hover-background",
        "--bit-Tag-active-background",
        "--bit-Tag-selected-color",
        "--bit-Tag-selected-background",
        "--bit-Tag-selected-border-color",
        "--bit-Tag-disabled-color",
        "--bit-Tag-disabled-background",
        "--bit-Tag-disabled-border-color",
        "--bit-Tag-focus-color",
        "--bit-Tag-radius",
        "--bit-Tag-border-width",
        "--bit-Tag-shadow",
        "--bit-Tag-min-height",
        "--bit-Tag-max-width",
        "--bit-Tag-padding-x",
        "--bit-Tag-padding-y",
        "--bit-Tag-gap",
        "--bit-Tag-font-size",
        "--bit-Tag-font-weight",
        "--bit-Tag-secondary-font-size",
        "--bit-Tag-icon-size",
        "--bit-Tag-image-size",
    ];

    [TestMethod]
    public void BitTagShouldReadEveryPublicVariableWithAFallback()
    {
        var stylesheet = ReadStylesheet();

        foreach (var variable in PublicVariables)
        {
            StringAssert.Contains(stylesheet, $"var({variable}, ", $"{variable} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitTagShouldReadNoPublicVariableItDoesNotDocument()
    {
        var read = Regex.Matches(ReadStylesheet(), @"var\((--bit-Tag-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.AreEquivalent(PublicVariables, read.ToArray());
    }

    [TestMethod]
    public void BitTagShouldNeverDeclareAPublicVariable()
    {
        // A declaration would stop the value an ancestor set from inheriting down to the tag.
        var declaration = new Regex(@"^\s*--bit-Tag-[a-z-]+\s*:", RegexOptions.Multiline);

        Assert.IsFalse(declaration.IsMatch(ReadStylesheet()), declaration.Match(ReadStylesheet()).Value);
    }

    [TestMethod]
    public void BitTagForcedColorsStatesShouldOutrankThePaintOfTheRoot()
    {
        var stylesheet = ReadStylesheet();
        var forced = SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active) {");

        // The root paints its selected and disabled states from .bit-tag.bit-tag-sel / .bit-tag.bit-dis, so the system
        // colors only win with the same two classes and a later place in the file.
        StringAssert.Contains(forced, "\n    .bit-tag.bit-tag-sel {");
        StringAssert.Contains(forced, "\n    .bit-tag.bit-dis {");
    }

    [TestMethod]
    public void BitTagDisabledRootShouldOnlyKeepAnsweringThePointerForItsTitle()
    {
        var stylesheet = ReadStylesheet();
        var disabled = SourceFiles.GetScssBlock(stylesheet, "&.bit-dis {");

        // the whole tag is inert - a handler on the root and a link in a template included - except for a root
        // that carries a title, which keeps the hover so the Title still shows; its content stays inert even then
        StringAssert.Contains(disabled, "&:not([title]),\n        &[title=\"\"] {\n            pointer-events: none;", disabled);
        StringAssert.Contains(disabled, ".bit-tag-cnt {\n            pointer-events: none;", disabled);
    }

    [TestMethod]
    public void BitTagDisabledControlsShouldNotShowThePressPaint()
    {
        var stylesheet = ReadStylesheet();

        // a control kept focusable by AllowDisabledFocus is still made :active by a held Space
        foreach (var control in new[] { "\n.bit-tag-int {", "\n.bit-tag-cls {" })
        {
            var rule = SourceFiles.GetScssBlock(stylesheet, control);
            var disabled = SourceFiles.GetScssDeclarations(rule, "&[aria-disabled=\"true\"] {");

            StringAssert.Contains(disabled, "background-color: transparent;", control);
            Assert.IsTrue(rule.IndexOf("&:active", System.StringComparison.Ordinal) < rule.IndexOf("&[aria-disabled", System.StringComparison.Ordinal), control);
        }
    }

    [TestMethod]
    public void BitTagForcedColorsShouldKeepTheHighlightRingOnADisabledSelectedTag()
    {
        var stylesheet = ReadStylesheet();
        var forced = SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active) {");

        // a disabled tag is painted Canvas again, where a HighlightText ring would vanish
        foreach (Match match in Regex.Matches(forced, @"\.bit-tag\.bit-tag-sel[^{,]*(?:,|\{)"))
        {
            if (match.Value.StartsWith(".bit-tag.bit-tag-sel {", System.StringComparison.Ordinal)) continue;

            StringAssert.Contains(match.Value, ":not(.bit-dis)", match.Value);
        }

        StringAssert.Contains(forced, "&:not(.bit-dis) .bit-tag-int,");
    }

    [TestMethod]
    public void BitTagControlsKeptFocusableWhileDisabledShouldStillIgnoreThePointer()
    {
        var stylesheet = ReadStylesheet();

        // anchored at the start of a line, since the reversed tag nests a rule of the dismiss button of its own
        foreach (var control in new[] { "\n.bit-tag-int {", "\n.bit-tag-cls {" })
        {
            var rule = SourceFiles.GetScssBlock(stylesheet, control);

            StringAssert.Contains(rule, "&[aria-disabled=\"true\"]", control);
        }
    }

    [TestMethod]
    public void BitTagHeightsShouldComeFromTheChipTokensOfTheTheme()
    {
        var stylesheet = ReadStylesheet();

        // a preset re-sizes every tag through --bit-siz-chip-*, so no size class may pin a height of its own
        foreach (var size in new[] { "sm", "md", "lg" })
        {
            StringAssert.Contains(stylesheet, $"--bit-tag-sz-min-height: #{{$siz-chip-{size}}};", size);
        }
    }

    [TestMethod]
    public void BitTagHeightShouldBeSetInsideTheRule()
    {
        var stylesheet = ReadStylesheet();
        var content = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag-cnt {");

        // calc() cannot add a unitless 0 or a keyword border width to a length, so the height never adds the
        // rule back in: it is the min-height of the content, which sits inside the rule
        StringAssert.Contains(content, "min-height: var(--bit-tag-sz-min-height, var(--bit-Tag-min-height, #{$siz-chip-md}));");
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"^\s*min-height:", RegexOptions.Multiline).Count);
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"calc\([^)]*border-width|brd-w"), "the rule width must not enter a calc()");
    }

    [TestMethod]
    public void BitTagShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size or Shape publishes these, so they are read before the variable, which only restyles the
        // default an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-tag-pad-x: var(--bit-tag-sz-pad-x, var(--bit-Tag-padding-x, calc(#{$siz-ctrl-pad-x-md} / 2)));");
        StringAssert.Contains(stylesheet, "--bit-tag-pad-y: var(--bit-tag-sz-pad-y, var(--bit-Tag-padding-y, ");
        StringAssert.Contains(stylesheet, "--bit-tag-gap: var(--bit-tag-sz-gap, var(--bit-Tag-gap, ");
        StringAssert.Contains(stylesheet, "--bit-tag-fs: var(--bit-tag-sz-fs, var(--bit-Tag-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "var(--bit-tag-sz-stx-fs, var(--bit-Tag-secondary-font-size, #{$tg-fs-xs}))");
        StringAssert.Contains(stylesheet, "var(--bit-tag-sz-img, var(--bit-Tag-image-size, ");
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-tag-radius, var(--bit-Tag-radius, #{$shp-radius-chip}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag-rnd {"), "--bit-tag-radius: #{$shp-radius-chip};");

        // So does an explicit Color, for every color its role paints - at rest, selected, hovered and disabled.
        var fill = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag-fil {");
        StringAssert.Contains(fill, "--bit-tag-v-fg: var(--bit-tag-clr-txt, var(--bit-Tag-color, #{$clr-pri-text}));");
        StringAssert.Contains(fill, "--bit-tag-v-bg: var(--bit-tag-clr, var(--bit-Tag-background, #{$clr-pri}));");
        StringAssert.Contains(fill, "--bit-tag-v-sel-bg: var(--bit-tag-clr-active, var(--bit-Tag-selected-background, #{$clr-pri-active}));");
        StringAssert.Contains(fill, "--bit-tag-v-dis-bg: var(--bit-tag-clr-dis, var(--bit-Tag-disabled-background, #{$clr-pri-dis}));");
        var outline = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag-otl {");
        StringAssert.Contains(outline, "--bit-tag-v-hover-bg: var(--bit-tag-clr-light, var(--bit-Tag-hover-background, #{$clr-pri-light}));");
        StringAssert.Contains(stylesheet, "--bit-tag-fg: var(--bit-tag-clr-dis-text, var(--bit-Tag-disabled-color, #{$clr-pri-dis-text}));");

        // What the role leaves alone stays the variable's: the transparent background of Outline and Text.
        StringAssert.Contains(outline, "--bit-tag-v-bg: var(--bit-Tag-background, transparent);");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Tag-[a-z-]+, var\(--bit-tag-(clr|sz|radius|v-)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitTagShouldPublishItsColorSizeAndShapeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag {");

        // A tag can sit in the template of another one, which must not inherit the outer tag's Color, Size or Shape:
        // each root starts the values those classes publish out unset, and the classes - declared further down at the
        // same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tag-clr", "--bit-tag-clr-txt", "--bit-tag-clr-dis", "--bit-tag-clr-dis-text", "--bit-tag-clr-active",
                                         "--bit-tag-clr-light", "--bit-tag-clr-light-hover", "--bit-tag-clr-light-active",
                                         "--bit-tag-sz-gap", "--bit-tag-sz-lbl-gap", "--bit-tag-sz-fs", "--bit-tag-sz-stx-fs", "--bit-tag-sz-img",
                                         "--bit-tag-sz-pad-y", "--bit-tag-sz-pad-x", "--bit-tag-sz-min-height", "--bit-tag-radius" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    // The header documents the variables in comments; only the rules are what the browser reads.
    private static string ReadStylesheet() => SourceFiles.StripScssComments(SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Notifications", "Tag", "BitTag.scss"));
}
