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
        var forced = stylesheet[stylesheet.IndexOf("@media (forced-colors: active)", System.StringComparison.Ordinal)..];

        // The root paints its selected and disabled states from .bit-tag.bit-tag-sel / .bit-tag.bit-dis, so the system
        // colors only win with the same two classes and a later place in the file.
        StringAssert.Contains(forced, "\n    .bit-tag.bit-tag-sel {");
        StringAssert.Contains(forced, "\n    .bit-tag.bit-dis {");
    }

    [TestMethod]
    public void BitTagDisabledRootShouldOnlyKeepAnsweringThePointerForItsTitle()
    {
        var stylesheet = ReadStylesheet();
        var start = stylesheet.IndexOf("&.bit-dis {", System.StringComparison.Ordinal);
        var disabled = stylesheet[start..stylesheet.IndexOf("\n    }", start, System.StringComparison.Ordinal)];

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
            var disabled = SourceFiles.GetScssBlock(rule, "&[aria-disabled=\"true\"] {");

            StringAssert.Contains(disabled, "background-color: transparent;", control);
            Assert.IsTrue(rule.IndexOf("&:active", System.StringComparison.Ordinal) < rule.IndexOf("&[aria-disabled", System.StringComparison.Ordinal), control);
        }
    }

    [TestMethod]
    public void BitTagForcedColorsShouldKeepTheHighlightRingOnADisabledSelectedTag()
    {
        var stylesheet = ReadStylesheet();
        var forced = stylesheet[stylesheet.IndexOf("@media (forced-colors: active)", System.StringComparison.Ordinal)..];

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
        StringAssert.Contains(content, "min-height: var(--bit-Tag-min-height, var(--bit-tag-sz-min-height));");
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"^\s*min-height:", RegexOptions.Multiline).Count);
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"calc\([^)]*border-width|brd-w"), "the rule width must not enter a calc()");
    }

    // The header documents the variables in comments; only the rules are what the browser reads.
    private static string ReadStylesheet() => SourceFiles.StripScssComments(SourceFiles.Read("Bit.BlazorUI", "Components", "Notifications", "Tag", "BitTag.scss"));
}
