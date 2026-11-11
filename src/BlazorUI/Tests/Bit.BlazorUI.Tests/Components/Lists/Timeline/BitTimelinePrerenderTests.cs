using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Timeline;

/// <summary>
/// What a timeline looks like before the page is interactive - prerendered, or statically rendered for good - where
/// the script that wires the keyboard activation of a clickable item never runs: the list, its items, and the button
/// of a clickable one are laid out all the same, so the first paint is the one the script attaches to.
/// </summary>
[TestClass]
public class BitTimelinePrerenderTests
{
    [TestMethod]
    public async Task BitTimelineShouldPrerenderItsItemsAsAList()
    {
        var html = await Prerenderer.RenderAsync<BitTimeline<BitTimelineItem>>(new Dictionary<string, object?>
        {
            [nameof(BitTimeline<BitTimelineItem>.Items)] = new[]
            {
                new BitTimelineItem { PrimaryText = "First", SecondaryText = "One" },
                new BitTimelineItem { PrimaryText = "Second" },
            },
        });

        Assert.IsTrue(Regex.IsMatch(html, @"<ol[^>]*role=""list"""));
        Assert.HasCount(2, Regex.Matches(html, @"class=""bit-tln-itm"));
        Assert.Contains("First", html);
        Assert.Contains("Second", html);

        // A presentational timeline exposes no controls, so nothing is prerendered for the keyboard to reach.
        Assert.DoesNotContain(@"role=""button""", html);
        Assert.DoesNotContain("tabindex", html);
    }

    [TestMethod]
    public async Task BitTimelineShouldPrerenderTheButtonOfAClickableItem()
    {
        var html = await Prerenderer.RenderAsync<BitTimeline<BitTimelineItem>>(new Dictionary<string, object?>
        {
            [nameof(BitTimeline<BitTimelineItem>.Items)] = new[]
            {
                new BitTimelineItem { PrimaryText = "Clickable", OnClick = _ => { } },
                new BitTimelineItem { PrimaryText = "Plain" },
            },
        });

        // The button is a plain element with the role, since its keys are wired in the browser once it is interactive;
        // it is there from the first paint, and only the clickable item carries one.
        Assert.ContainsSingle(Regex.Matches(html, @"class=""bit-tln-btn"));
        Assert.IsTrue(Regex.IsMatch(html, @"role=""button""[^>]*tabindex=""0"""));
    }
}
