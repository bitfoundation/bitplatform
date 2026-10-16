using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Tooltip;

/// <summary>
/// What a tooltip looks like on a statically rendered page, which has no render after the first one and no
/// JavaScript to call: the Escape listeners and the mirrored relationship wait for the after-render pass, so a
/// prerender shows the anchor with the text that describes it, and never throws.
/// </summary>
[TestClass]
public class BitTooltipPrerenderTests
{
    [TestMethod]
    public async Task BitTooltipShouldRenderItsTextAndRelationshipOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitTooltip>(new Dictionary<string, object?>
        {
            [nameof(BitTooltip.Id)] = "tip",
            [nameof(BitTooltip.Text)] = "Describes the anchor",
            [nameof(BitTooltip.ChildContent)] = (RenderFragment)(b => b.AddMarkupContent(0, "<button>Anchor</button>")),
        });

        StringAssert.Contains(html, "Anchor");
        StringAssert.Contains(html, "Describes the anchor");
        StringAssert.Contains(html, "role=\"tooltip\"");
        // The description is there before any script runs, so a screen reader has the text the moment the anchor is reached.
        StringAssert.Contains(html, "aria-describedby=\"tip-ttp\"");
        Assert.IsFalse(html.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public async Task BitTooltipShouldRenderADefaultShownTooltipShownOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitTooltip>(new Dictionary<string, object?>
        {
            [nameof(BitTooltip.Text)] = "Tip",
            [nameof(BitTooltip.DefaultIsShown)] = true,
            [nameof(BitTooltip.LazyRender)] = true,
        });

        StringAssert.Contains(html, "bit-ttp-vis");
        // A lazily rendered content that starts shown is rendered on the static page too.
        StringAssert.Contains(html, "Tip");
    }
}
