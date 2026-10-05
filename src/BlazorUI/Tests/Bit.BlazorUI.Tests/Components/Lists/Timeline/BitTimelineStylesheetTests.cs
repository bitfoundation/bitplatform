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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Lists", "Timeline", "BitTimeline.scss");
}
