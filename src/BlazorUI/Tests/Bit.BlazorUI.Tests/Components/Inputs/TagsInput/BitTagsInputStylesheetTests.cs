using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

/// <summary>
/// Pins the order the tags input's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color
/// or Size outranks the public --bit-TagsInput-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitTagsInputStylesheetTests
{
    [TestMethod]
    public void BitTagsInputShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for.
        Assert.Contains("font-size: var(--bit-tgi-fontsize, var(--bit-TagsInput-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("gap: var(--bit-tgi-gap, var(--bit-TagsInput-gap, #{spacing(0.375)}));", stylesheet);
        Assert.Contains("min-height: var(--bit-tgi-minheight, var(--bit-TagsInput-min-height, #{$siz-ctrl-md}));", stylesheet);
        Assert.Contains("var(--bit-tgi-tagminheight, var(--bit-TagsInput-tag-min-height, #{$siz-chip-sm}))", stylesheet);
        Assert.Contains("var(--bit-tgi-tagfontsize, var(--bit-TagsInput-tag-font-size, #{$tg-fs-xs}))", stylesheet);
        Assert.Contains("var(--bit-tgi-iconsize, var(--bit-TagsInput-icon-size, #{$siz-icon-md}))", stylesheet);
        Assert.Contains("var(--bit-tgi-helperfontsize, var(--bit-TagsInput-description-font-size, #{$tg-fs-xs}))", stylesheet);

        // So does an explicit Color: the chip colors of the tag variant are the Color's (unset while it is), then the
        // variable's, then the primary chip's; the field's focus ring and rule and the spinner rank the same way. The
        // chip reads them behind the value its own hover moves, which is unset while the chip is not hovered. The
        // variables and the primary chip are resolved on the chip by its tag variant (the -pub values), each variable
        // behind the tag variant's own twin of it.
        Assert.Contains("background-color: var(--bit-tgi-tag-sbg, var(--bit-tgi-tag-bg, var(--bit-tgi-tag-bg-pub)));", stylesheet);
        Assert.Contains("color: var(--bit-tgi-tag-sfg, var(--bit-tgi-tag-clr, var(--bit-tgi-tag-clr-pub)));", stylesheet);
        Assert.Contains("var(--bit-tgi-tag-ring, var(--bit-tgi-tag-ring-pub))", stylesheet);
        Assert.Contains("--bit-tgi-tag-bg-pub: var(--bit-TagsInput-fill-tag-background, var(--bit-TagsInput-tag-background, var(--bit-tgi-tag-bg-dft)));", stylesheet);
        Assert.Contains("--bit-tgi-tag-clr-pub: var(--bit-TagsInput-fill-tag-color, var(--bit-TagsInput-tag-color, var(--bit-tgi-tag-clr-dft)));", stylesheet);
        Assert.Contains("--bit-tgi-tag-ring-pub: var(--bit-TagsInput-fill-tag-focus-color, var(--bit-TagsInput-tag-focus-color, var(--bit-tgi-tag-ring-dft)));", stylesheet);
        Assert.Contains("focus-ring-own(var(--bit-tgi-clr-focus, var(--bit-TagsInput-focus-color)))", stylesheet);
        Assert.Contains("var(--bit-tgi-clr, var(--bit-TagsInput-focus-border-color, #{$clr-pri}))", stylesheet);
        Assert.Contains("var(--bit-tgi-clr, var(--bit-TagsInput-spinner-color, #{$clr-pri}))", stylesheet);
        Assert.Contains("--bit-tgi-tag-bg: var(--bit-tgi-clr);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgi-tgf {"));
        Assert.Contains("--bit-tgi-tag-bg-dft: #{$clr-pri};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgi-tgf {"));

        // The values the parameters publish are never read behind a public variable. The -dft values are the
        // Variant's recipe for the primary chip an unset Color stands for, so the variables stay ahead of them.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-TagsInput-[a-z-]+, var\(--bit-tgi-(clr|clr-txt|clr-hover|clr-focus|gap|fontsize|iconsize|helperfontsize|tagfontsize|minheight|padding|tagminheight|tagpadding|tag-clr|tag-brd|tag-bg|tag-ring|tag-clr-hover|tag-brd-hover|tag-bg-hover)\)"),
                       "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-tgi-fil"),
        DataRow("outline", ".bit-tgi-otl"),
        DataRow("text", ".bit-tgi-txt")]
    public void BitTagsInputShouldReadTheFieldVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n{selector} {{");

        // Each Variant reads a tier of its own ahead of the shared field colors, so a theme can set one look of the
        // field apart from the other two on :root.
        foreach (var name in new[] { "background", "border-color", "hover-border-color" })
        {
            Assert.Contains($"var(--bit-TagsInput-{variant}-{name}, var(--bit-TagsInput-{name}, ", block, $"{selector} does not read --bit-TagsInput-{variant}-{name} ahead of --bit-TagsInput-{name}.");
        }

        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-TagsInput-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod,
        DataRow("fill", ".bit-tgi-tgf"),
        DataRow("outline", ".bit-tgi-tgo"),
        DataRow("text", ".bit-tgi-tgt")]
    public void BitTagsInputShouldReadTheChipVariablesOfItsTagVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        // The tag variant resolves the public chip colors on the chip itself - so a chip handed one through GetTagStyle
        // still takes it - each behind the tag variant's own twin of it, named the way the field's are.
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n{selector} .bit-tgi-tag,\n{selector} .bit-tgi-tgl {{");

        foreach (var name in new[] { "tag-color", "tag-background", "tag-border-color", "tag-focus-color", "tag-hover-color",
                                     "tag-hover-background", "toggle-hover-color", "toggle-hover-background" })
        {
            Assert.Contains($"var(--bit-TagsInput-{variant}-{name}, var(--bit-TagsInput-{name}, ", block, $"{selector} does not read --bit-TagsInput-{variant}-{name} ahead of --bit-TagsInput-{name}.");
        }

        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-TagsInput-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another tag variant.");
    }

    [TestMethod]
    [DataRow("\n.bit-tgi-dbt {")]
    [DataRow("\n.bit-tgi-rbt {")]
    public void BitTagsInputChipButtonsShouldDrawTheirRingInsideTheChip(string rule)
    {
        // The dismiss and reorder buttons stretch to the height of a chip that clips its overflow, so a ring drawn on
        // their edge was cut off along the chip's: it is drawn inside them, like the chip's own.
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), rule);

        Assert.Contains("@include focus-ring-inset(var(--bit-tgi-tag-ring, var(--bit-tgi-tag-ring-pub)));", block);
        Assert.DoesNotContain("outline-offset: 0", block, "The ring is drawn on the edge the chip clips.");
    }

    [TestMethod]
    public void BitTagsInputShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tgi {");

        // A tags input can sit in a template of another one, which must not inherit the outer field's Color or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at
        // the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tgi-clr", "--bit-tgi-clr-fg", "--bit-tgi-clr-txt", "--bit-tgi-clr-hover", "--bit-tgi-clr-focus", "--bit-tgi-gap",
                                         "--bit-tgi-fontsize", "--bit-tgi-iconsize", "--bit-tgi-helperfontsize", "--bit-tgi-tagfontsize",
                                         "--bit-tgi-minheight", "--bit-tgi-padding", "--bit-tgi-tagminheight", "--bit-tgi-tagpadding" })
        {
            Assert.Contains($"{property}: initial;", root);
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "TagsInput", "BitTagsInput.scss");

    [TestMethod,
        DataRow("\n.bit-tgi-tgo {"),
        DataRow("\n.bit-tgi-tgt {")]
    public void BitTagsInputShouldDrawTheTextOfAnUnfilledChipInTheRoleTextColor(string selector)
    {
        var stylesheet = ReadStylesheet();
        var variant = SourceFiles.GetScssBlock(stylesheet, selector);

        // The text of an Outline or Text chip sits on the field rather than on the role's fill, so it is drawn in the role's
        // text color - a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white - and
        // kept under the pointer, as a BitTag keeps it, where the hover color of such a role is no more readable.
        Assert.Contains("--bit-tgi-tag-clr: var(--bit-tgi-clr-fg);", variant);
        Assert.Contains("--bit-tgi-tag-clr-hover: var(--bit-tgi-clr-fg);", variant);
        Assert.Contains("--bit-tgi-tag-clr-dft: #{$clr-pri};", variant);
        Assert.Contains("--bit-tgi-tag-clr-hover-dft: #{$clr-pri};", variant);

        // Only the text: the focus ring keeps the role's main.
        Assert.Contains("--bit-tgi-tag-ring: var(--bit-tgi-clr);", variant);

        Assert.Contains("--bit-tgi-clr-fg: #{role-text($role, $tokens)};", SourceFiles.GetScssBlock(stylesheet, "\n    .bit-tgi-#{$role} {"));
    }
}
