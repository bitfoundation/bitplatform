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
        "--bit-Tag-fill-color",
        "--bit-Tag-fill-background",
        "--bit-Tag-fill-border-color",
        "--bit-Tag-fill-hover-background",
        "--bit-Tag-fill-active-background",
        "--bit-Tag-fill-selected-color",
        "--bit-Tag-fill-selected-background",
        "--bit-Tag-fill-selected-border-color",
        "--bit-Tag-fill-disabled-background",
        "--bit-Tag-fill-disabled-border-color",
        "--bit-Tag-outline-color",
        "--bit-Tag-outline-background",
        "--bit-Tag-outline-border-color",
        "--bit-Tag-outline-hover-background",
        "--bit-Tag-outline-active-background",
        "--bit-Tag-outline-selected-color",
        "--bit-Tag-outline-selected-background",
        "--bit-Tag-outline-selected-border-color",
        "--bit-Tag-outline-disabled-background",
        "--bit-Tag-outline-disabled-border-color",
        "--bit-Tag-text-color",
        "--bit-Tag-text-background",
        "--bit-Tag-text-border-color",
        "--bit-Tag-text-hover-background",
        "--bit-Tag-text-active-background",
        "--bit-Tag-text-selected-color",
        "--bit-Tag-text-selected-background",
        "--bit-Tag-text-selected-border-color",
        "--bit-Tag-text-disabled-background",
        "--bit-Tag-text-disabled-border-color",
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

        // nor the text the press hands the content and the dismiss button, which the disabled root takes back
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "&.bit-dis {"), "--bit-tag-wash-fg: initial;");
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
        StringAssert.Contains(fill, "--bit-tag-v-fg: var(--bit-tag-clr-txt, var(--bit-Tag-fill-color, var(--bit-Tag-color, var(--bit-tag-dft-clr-txt, #{$clr-pri-text}))));");
        StringAssert.Contains(fill, "--bit-tag-v-bg: var(--bit-tag-clr, var(--bit-Tag-fill-background, var(--bit-Tag-background, var(--bit-tag-dft-clr, #{$clr-pri}))));");
        StringAssert.Contains(fill, "--bit-tag-v-sel-bg: var(--bit-tag-clr-active, var(--bit-Tag-fill-selected-background, var(--bit-Tag-selected-background, var(--bit-tag-dft-clr-active, #{$clr-pri-active}))));");
        StringAssert.Contains(fill, "--bit-tag-v-dis-bg: var(--bit-tag-clr-dis, var(--bit-Tag-fill-disabled-background, var(--bit-Tag-disabled-background, var(--bit-tag-dft-clr-dis, #{$clr-pri-dis}))));");
        var outline = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag-otl {");
        StringAssert.Contains(outline, "--bit-tag-v-hover-bg: var(--bit-tag-clr-tint, var(--bit-Tag-outline-hover-background, var(--bit-Tag-hover-background, var(--bit-tag-dft-clr-tint, #{$clr-pri-tint}))));");
        StringAssert.Contains(stylesheet, "--bit-tag-fg: var(--bit-tag-clr-dis-text, var(--bit-Tag-disabled-color, var(--bit-tag-dft-clr-dis-text, #{$clr-pri-dis-text})));");

        // What the role leaves alone stays the variables': the transparent background of Outline and Text.
        StringAssert.Contains(outline, "--bit-tag-v-bg: var(--bit-Tag-outline-background, var(--bit-Tag-background, transparent));");

        // The default a host component gives its tags (--bit-tag-dft-*) is the one private value read after them.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Tag-[a-z-]+, var\(--bit-tag-(?!dft-)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitTagShouldReadTheDefaultOfItsHostAsTheLastFallbackOfEveryRoleColor()
    {
        var stylesheet = ReadStylesheet();

        // A component of the library that renders a tag gives it a default role other than the primary one through a
        // class (bit-tag-dft-ter), never through Color: so every color the primary role stands in for reads that default
        // right ahead of it - behind the role of an explicit Color and the public variables, which keep the last word.
        // Which default is told by the color being declared, never by the one it happens to read: the text of an
        // unfilled tag stands the primary main in for the text color the role is read in on the page (role-text, which
        // keeps the primary main), a selected or a hovered one the primary foreground for the one it reads over the tint -
        // leaned deeper where the pointer's wash meets the selection's - and every other color the slot the primary token
        // names - the pressed wash of an unfilled tag being the main mixed in.
        var fallbacks = 0;
        foreach (Match declaration in Regex.Matches(stylesheet, @"(--bit-tag-[a-z-]+):([^;]+);"))
        {
            var property = declaration.Groups[1].Value;
            foreach (Match fallback in Regex.Matches(declaration.Groups[2].Value, @"var\((--bit-tag-[a-z-]+), #\{(?:\$clr-pri(?<slot>-[a-z-]+)?|(?<wash>translucent)\(\$clr-pri, \d+%\)|(?<deep>deep-wash-text)\(\$clr-pri-fg\))\}\)"))
            {
                fallbacks++;
                var slot = fallback.Groups["wash"].Success ? "-tint-active" : fallback.Groups["deep"].Success ? "-sel-wash-fg" : fallback.Groups["slot"].Value switch { "-text" => "-txt", var other => other };
                var expected = (property, slot) switch
                {
                    ("--bit-tag-v-fg", "") => "--bit-tag-dft-clr-fg",
                    ("--bit-tag-v-sel-fg" or "--bit-tag-v-wash-fg", "-fg") => "--bit-tag-dft-clr-sel-fg",
                    _ => $"--bit-tag-dft-clr{slot}",
                };

                Assert.AreEqual(expected, fallback.Groups[1].Value, $"{property} reads {fallback.Value} behind the wrong host default.");
            }
        }

        Assert.IsTrue(fallbacks > 0, "No color falls back to the primary role.");
        Assert.AreEqual(Regex.Matches(stylesheet, @"\$clr-pri\b").Count, fallbacks, "A primary role token is read other than as the last fallback of a color.");

        // The classes mirror the role classes - the same roles, the same values - and are never reset by the role classes.
        var roles = SourceFiles.GetScssDeclarations(stylesheet, ".bit-tag-#{$role} {");
        var defaults = SourceFiles.GetScssDeclarations(stylesheet, ".bit-tag-dft-#{$role} {");
        StringAssert.Contains(stylesheet, "@each $role, $tokens in $bit-color-roles {\n    .bit-tag-dft-#{$role} {");
        Assert.AreEqual(roles.Replace(".bit-tag-#{$role}", ".bit-tag-dft-#{$role}").Replace("--bit-tag-clr", "--bit-tag-dft-clr"), defaults);

        // Declared after the root rule at the same weight, so the class wins over the reset on the root that carries it.
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-tag {", System.StringComparison.Ordinal) < stylesheet.IndexOf(".bit-tag-dft-#{$role} {", System.StringComparison.Ordinal));
    }

    [TestMethod,
        DataRow(".bit-tag-otl", "outline"),
        DataRow(".bit-tag-txt", "text")]
    public void BitTagShouldDrawTheTextOfAnUnfilledVariantInTheRoleTextColor(string selector, string variant)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // The text of an Outline or Text tag sits on the page rather than on its fill, so it is drawn in the role's text
        // color: a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white.
        StringAssert.Contains(block, $"--bit-tag-v-fg: var(--bit-tag-clr-fg, var(--bit-Tag-{variant}-color, var(--bit-Tag-color, var(--bit-tag-dft-clr-fg, #{{$clr-pri}}))));");

        // A selected one sits on a wash of the role instead - its tint, the surface the role's fg is solved to be read on -
        // and so reads that fg, the primary role's too, whose main drops under 4.5:1 there. Never on the light tier: that one
        // is a pastel which in a dark palette is as light as the role's text. The pointer lays a wash over the content, which
        // deepens a selected tag's rather than replacing it, so no selected step of its own is needed.
        StringAssert.Contains(block, $"--bit-tag-v-sel-fg: var(--bit-tag-clr-sel-fg, var(--bit-Tag-{variant}-selected-color, var(--bit-Tag-selected-color, var(--bit-Tag-{variant}-color, var(--bit-Tag-color, var(--bit-tag-dft-clr-sel-fg, #{{$clr-pri-fg}}))))));");
        StringAssert.Contains(block, $"--bit-tag-v-sel-bg: var(--bit-tag-clr-tint, var(--bit-Tag-{variant}-selected-background, var(--bit-Tag-selected-background, var(--bit-tag-dft-clr-tint, #{{$clr-pri-tint}}))));");
        StringAssert.Contains(block, $"--bit-tag-v-hover-bg: var(--bit-tag-clr-tint, var(--bit-Tag-{variant}-hover-background, var(--bit-Tag-hover-background, var(--bit-tag-dft-clr-tint, #{{$clr-pri-tint}}))));");
        StringAssert.Contains(block, $"--bit-tag-v-active-bg: var(--bit-tag-clr-tint-active, var(--bit-Tag-{variant}-active-background, var(--bit-Tag-active-background, var(--bit-tag-dft-clr-tint-active, #{{translucent($clr-pri, 16%)}}))));");
        StringAssert.DoesNotMatch(stylesheet, new Regex(@"clr-[a-z-]*light|\(\$tokens, light"));
        StringAssert.DoesNotMatch(stylesheet, new Regex("v-sel-hover-bg|v-sel-active-bg"));

        // Under the pointer the text sits on a wash too, so it reads that fg as well, the primary role's included: its main,
        // which the text keeps at rest, drops under 4.5:1 there. On a selected chip the pointer's wash is laid over the
        // selection's, past the tint the fg is solved for, so the fg is leaned deeper there (deep-wash-text). A public
        // variable keeps the last word on both, as it does at rest.
        StringAssert.Contains(block, $"--bit-tag-v-wash-fg: var(--bit-tag-clr-sel-fg, var(--bit-Tag-{variant}-color, var(--bit-Tag-color, var(--bit-tag-dft-clr-sel-fg, #{{$clr-pri-fg}}))));");
        StringAssert.Contains(block, $"--bit-tag-v-sel-wash-fg: var(--bit-tag-clr-sel-wash-fg, var(--bit-Tag-{variant}-selected-color, var(--bit-Tag-selected-color, var(--bit-Tag-{variant}-color, var(--bit-Tag-color, var(--bit-tag-dft-clr-sel-wash-fg, #{{deep-wash-text($clr-pri-fg)}}))))));");
        Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag-fil {").Contains("wash-fg"), "A Fill tag keeps the text it has at rest.");

        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tag {");
        StringAssert.Contains(root, "--bit-tag-wash-fg: var(--bit-tag-v-wash-fg);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "&.bit-tag-sel {"), "--bit-tag-wash-fg: var(--bit-tag-v-sel-wash-fg);");

        // Both parts the pointer lays a wash on take the text through a variable, and inherit the tag's otherwise.
        foreach (var (part, slot) in new[] { ("\n.bit-tag-int {", "cnt"), ("\n.bit-tag-cls {", "cls") })
        {
            var rule = SourceFiles.GetScssBlock(stylesheet, part);
            StringAssert.Contains(SourceFiles.GetScssDeclarations(rule, "&:hover {"), $"--bit-tag-{slot}-sfg: var(--bit-tag-wash-fg);", part);
            StringAssert.Contains(SourceFiles.GetScssDeclarations(rule, "&:active {"), $"--bit-tag-{slot}-sfg: var(--bit-tag-wash-fg);", part);
        }

        foreach (var part in new[] { "cnt", "cls" })
        {
            var rule = SourceFiles.GetScssDeclarations(stylesheet, $"\n.bit-tag-{part} {{");
            StringAssert.Contains(rule, $"--bit-tag-{part}-sfg: initial;", part);
            StringAssert.Contains(rule, $"color: var(--bit-tag-{part}-sfg, currentcolor);", part);
        }

        var roles = SourceFiles.GetScssDeclarations(stylesheet, ".bit-tag-#{$role} {");
        StringAssert.Contains(roles, "--bit-tag-clr-fg: #{role-text($role, $tokens)};");
        StringAssert.Contains(roles, "--bit-tag-clr-sel-fg: #{role($tokens, fg)};");
        StringAssert.Contains(roles, "--bit-tag-clr-sel-wash-fg: #{deep-wash-text(role($tokens, fg))};");
        StringAssert.Contains(roles, "--bit-tag-clr-tint: #{role-wash($tokens)};");
        StringAssert.Contains(roles, "--bit-tag-clr-tint-active: #{role-wash($tokens, 16%)};");
    }

    [TestMethod,
        DataRow("fill", ".bit-tag-fil"),
        DataRow("outline", ".bit-tag-otl"),
        DataRow("text", ".bit-tag-txt")]
    public void BitTagShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of every shared color variable, so a theme can set one look apart
        // from the other two on :root - an outline tag's neutral rule without a ring around every filled one.
        foreach (var name in new[] { "color", "background", "border-color", "hover-background", "active-background",
                                     "selected-color", "selected-background", "selected-border-color",
                                     "disabled-background", "disabled-border-color" })
        {
            StringAssert.Contains(block, $"var(--bit-Tag-{variant}-{name}, var(--bit-Tag-{name}, ",
                                  $"{selector} does not read --bit-Tag-{variant}-{name} ahead of --bit-Tag-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-Tag-{variant}-[a-z-]+, )var\(--bit-Tag-(?:hover-|active-|selected-|disabled-)?(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Tag-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod]
    public void BitTagShouldPublishItsColorSizeAndShapeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tag {");

        // A tag can sit in the template of another one, which must not inherit the outer tag's Color, Size or Shape:
        // each root starts the values those classes publish out unset, and the classes - declared further down at the
        // same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tag-clr", "--bit-tag-clr-fg", "--bit-tag-clr-sel-fg", "--bit-tag-clr-sel-wash-fg", "--bit-tag-clr-txt", "--bit-tag-clr-dis",
                                         "--bit-tag-clr-dis-text", "--bit-tag-clr-active", "--bit-tag-clr-tint", "--bit-tag-clr-tint-active",
                                         // The default a host gave a tag, which a tag in its template must not inherit either.
                                         "--bit-tag-dft-clr", "--bit-tag-dft-clr-fg", "--bit-tag-dft-clr-sel-fg", "--bit-tag-dft-clr-sel-wash-fg", "--bit-tag-dft-clr-txt",
                                         "--bit-tag-dft-clr-dis", "--bit-tag-dft-clr-dis-text", "--bit-tag-dft-clr-active", "--bit-tag-dft-clr-tint", "--bit-tag-dft-clr-tint-active",
                                         // Only Outline and Text declare these, so a Fill tag in another one's template does not inherit them.
                                         "--bit-tag-v-wash-fg", "--bit-tag-v-sel-wash-fg",
                                         "--bit-tag-sz-gap", "--bit-tag-sz-lbl-gap", "--bit-tag-sz-fs", "--bit-tag-sz-stx-fs", "--bit-tag-sz-img",
                                         "--bit-tag-sz-pad-y", "--bit-tag-sz-pad-x", "--bit-tag-sz-min-height", "--bit-tag-radius" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    // The header documents the variables in comments; only the rules are what the browser reads.
    private static string ReadStylesheet() => SourceFiles.StripScssComments(SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Notifications", "Tag", "BitTag.scss"));
}
