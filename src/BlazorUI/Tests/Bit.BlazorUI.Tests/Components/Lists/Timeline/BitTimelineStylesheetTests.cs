using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Timeline;

/// <summary>
/// Pins what keeps a timeline nested in the template of an item from taking the look of the one around it, which a
/// bUnit render cannot see: every rule a timeline-level class drives selects the items of that timeline alone, with a
/// child combinator, which holds because each item is a direct child of the root in all three APIs.
/// </summary>
[TestClass]
public class BitTimelineStylesheetTests : BunitTestContext
{
    [TestMethod]
    public void BitTimelineShouldRenderEveryItemAsADirectChildOfTheRoot()
    {
        var items = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.Add(p => p.Items, [new() { PrimaryText = "1" }, new() { PrimaryText = "2", OnClick = _ => { } }]);
        });

        var options = RenderComponent<BitTimeline<BitTimelineOption>>(parameters =>
        {
            parameters.Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<BitTimelineOption>(0);
                builder.AddAttribute(1, nameof(BitTimelineOption.PrimaryText), "1");
                builder.CloseComponent();
                builder.OpenComponent<BitTimelineOption>(2);
                builder.AddAttribute(3, nameof(BitTimelineOption.PrimaryText), "2");
                builder.CloseComponent();
            });
        });

        foreach (var root in new[] { items.Find(".bit-tln"), options.Find(".bit-tln") })
        {
            Assert.AreEqual(2, root.Children.Length);
            Assert.IsTrue(root.Children.All(c => c.ClassList.Contains("bit-tln-itm")));
        }
    }

    [TestMethod]
    public void BitTimelineShouldSelectOnlyItsOwnItemsForTheTimelineLevelVariants()
    {
        var stylesheet = ReadStylesheet();

        // A descendant combinator would also reach the items of a nested timeline, where the rule of the outer one
        // could win on source order over the rule of the nested one: a Fill timeline inside an Outline one would then
        // paint outlined dots.
        var descendant = new Regex(@"^\.bit-(tln-(fil|otl|txt|ldd|ldt)(\.bit-dis)?|dis) \.bit-tln-itm", RegexOptions.Multiline);

        Assert.IsFalse(descendant.IsMatch(stylesheet), descendant.Match(stylesheet).Value);

        StringAssert.Contains(stylesheet, "\n.bit-tln-fil > .bit-tln-itm {");
        StringAssert.Contains(stylesheet, "\n.bit-tln-otl > .bit-tln-itm {");
        StringAssert.Contains(stylesheet, "\n.bit-tln-txt > .bit-tln-itm {");
        StringAssert.Contains(stylesheet, "\n.bit-tln-ldd > .bit-tln-itm {");
        StringAssert.Contains(stylesheet, "\n.bit-tln-ldt > .bit-tln-itm {");
    }

    [TestMethod]
    public void BitTimelineShouldLayOutOnlyItsOwnItemsHorizontally()
    {
        var horizontal = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tln-hrz {");

        // The parts of an item are only reached through the item, or the button of a clickable one, that holds them.
        Assert.IsFalse(Regex.IsMatch(horizontal, @"\n    \.bit-tln-(pcn|scn|dvd|itm)"), "A horizontal rule selects the parts of any descendant timeline.");

        StringAssert.Contains(horizontal, "\n    > .bit-tln-prt,\n    > .bit-tln-itm > .bit-tln-prt {");
        StringAssert.Contains(horizontal, "\n    > .bit-tln-itm.bit-tln-irv {");
    }

    [TestMethod]
    public void BitTimelineShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size - of the timeline or of an item - publishes these, so they are read before the variable, which
        // only restyles the medium timeline an unset one stands for.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-tln-fs, var(--bit-Timeline-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-tln-dot-size: var(--bit-tln-dot-sz, var(--bit-Timeline-dot-size, #{spacing(3.75)}));");

        // So does an explicit Color, for every dot color its role paints; the transparent dot of Outline and Text is the
        // variable's alone.
        var fill = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tln-fil > .bit-tln-itm {");
        StringAssert.Contains(fill, "var(--bit-tln-clr, var(--bit-Timeline-fill-dot-background, var(--bit-Timeline-dot-background, #{$clr-pri}))),");
        StringAssert.Contains(fill, "var(--bit-tln-clr, var(--bit-Timeline-fill-dot-border-color, var(--bit-Timeline-dot-border-color, #{$clr-pri}))),");
        StringAssert.Contains(fill, "var(--bit-tln-clr-fg, var(--bit-Timeline-fill-icon-color, var(--bit-Timeline-icon-color, #{$clr-pri-text})))");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tln-otl > .bit-tln-itm {"), "var(--bit-Timeline-outline-dot-background, var(--bit-Timeline-dot-background, transparent)),");

        // Left in front: the legacy line color behind the public one.
        var publicFirst = Regex.Matches(stylesheet, @"var\(--bit-Timeline-[a-z-]+, var\(--bit-tln-([a-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        CollectionAssert.AreEqual(new[] { "dvd-clr" }, publicFirst);
    }

    [TestMethod,
        DataRow("fill", ".bit-tln-fil > .bit-tln-itm", "timeline-variant-fill"),
        DataRow("fill", ".bit-tln-itm.bit-tln-ivf", "timeline-variant-fill"),
        DataRow("outline", ".bit-tln-otl > .bit-tln-itm", "timeline-variant-outline"),
        DataRow("outline", ".bit-tln-itm.bit-tln-ivo", "timeline-variant-outline"),
        DataRow("text", ".bit-tln-txt > .bit-tln-itm", "timeline-variant-text"),
        DataRow("text", ".bit-tln-itm.bit-tln-ivt", "timeline-variant-text")]
    public void BitTimelineShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string mixin)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant - of the timeline or of an item - reads a tier of its own ahead of every shared dot color, so a
        // theme can set one look apart from the other two on :root, and hands the chains to the mixin of its variant.
        StringAssert.Contains(block, $"@include {mixin}(");
        foreach (var name in new[] { "dot-background", "dot-border-color", "icon-color" })
        {
            StringAssert.Contains(block, $"var(--bit-Timeline-{variant}-{name}, var(--bit-Timeline-{name}, ",
                                  $"{selector} does not read --bit-Timeline-{variant}-{name} ahead of --bit-Timeline-{name}.");
        }

        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Timeline-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // The mixins, which no variant qualifies, read none of the dot colors themselves.
        Assert.IsFalse(Regex.IsMatch(SourceFiles.GetScssBlock(stylesheet, $"@mixin {mixin}("), @"var\(--bit-Timeline-"),
                       $"{mixin} reads a public variable itself.");
    }

    [TestMethod]
    public void BitTimelineTextPlateShouldTakeTheShapeOfItsDot()
    {
        var plate = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tln-txt .bit-tln-ico,");

        // The plate copies the corners of the dot it sits in, so the radius stays one variable for every variant, and is
        // filled with what the item's own variant resolved: the rule also matches the icons of the items of a Text
        // timeline that override their variant, which must not be handed the Text tier. Only the Text variant paints it.
        StringAssert.Contains(plate, "border-radius: inherit;");
        StringAssert.Contains(plate, "background-color: var(--bit-tln-dot-ico-clr-bg);");
        Assert.IsFalse(Regex.IsMatch(plate, @"var\(--bit-Timeline-"), "The plate reads a public variable past the variant of its item.");

        var stylesheet = ReadStylesheet();
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "@mixin timeline-variant-text("), "--bit-tln-dot-ico-clr-bg: #{$dot-background};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "@mixin timeline-variant-fill("), "--bit-tln-dot-ico-clr-bg: transparent;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "@mixin timeline-variant-outline("), "--bit-tln-dot-ico-clr-bg: transparent;");
    }

    [TestMethod]
    public void BitTimelineShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tln {");

        // A timeline can sit in the template of an item of another one, which must not inherit the outer timeline's
        // Color or Size: each root starts the values those classes publish out unset, and the classes - declared further
        // down at the same weight - still win on the root that carries them. The items are left to inherit their root's.
        foreach (var property in new[] { "--bit-tln-clr", "--bit-tln-clr-fg", "--bit-tln-clr-focus", "--bit-tln-clr-dis-bg", "--bit-tln-clr-dis-text",
                                         "--bit-tln-dot-sz", "--bit-tln-fs" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Lists", "Timeline", "BitTimeline.scss");
}
