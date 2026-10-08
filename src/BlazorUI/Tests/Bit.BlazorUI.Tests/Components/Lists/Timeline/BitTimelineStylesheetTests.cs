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
        var fill = SourceFiles.GetScssBlock(stylesheet, "@mixin timeline-variant-fill {");
        StringAssert.Contains(fill, "--bit-tln-dot-clr-bg: var(--bit-tln-clr, var(--bit-Timeline-dot-background, #{$clr-pri}));");
        StringAssert.Contains(fill, "--bit-tln-dot-clr-brd: var(--bit-tln-clr, var(--bit-Timeline-dot-border-color, #{$clr-pri}));");
        StringAssert.Contains(fill, "--bit-tln-dot-ico-clr: var(--bit-tln-clr-fg, var(--bit-Timeline-icon-color, #{$clr-pri-text}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "@mixin timeline-variant-outline {"), "--bit-tln-dot-clr-bg: var(--bit-Timeline-dot-background, transparent);");

        // Left in front: the legacy line color behind the public one, and the plate of a Text dot, which no Color paints.
        var publicFirst = Regex.Matches(stylesheet, @"var\(--bit-Timeline-[a-z-]+, var\(--bit-tln-([a-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        CollectionAssert.AreEqual(new[] { "dot-ico-clr-bg", "dvd-clr" }, publicFirst);
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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Lists", "Timeline", "BitTimeline.scss");
}
