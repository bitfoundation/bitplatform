using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

/// <summary>
/// What an overlay looks like before the page is interactive. The focus origin, the scroller and the Escape watch are
/// all settled after a render, so a prerender - whose JS runtime throws - renders the layer in the state its
/// parameters say and reaches for no script.
/// </summary>
[TestClass]
public class BitOverlayPrerenderTests
{
    [TestMethod]
    public async Task BitOverlayShouldPrerenderAnOpenOverlayWithoutTheScript()
    {
        var html = await Prerenderer.RenderAsync<BitOverlay>(new Dictionary<string, object?>
        {
            [nameof(BitOverlay.IsOpen)] = true,
            [nameof(BitOverlay.AutoToggleScroll)] = true,
            [nameof(BitOverlay.ModeFull)] = true,
            [nameof(BitOverlay.Position)] = BitPosition.Center,
            [nameof(BitOverlay.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Please wait")),
        });

        StringAssert.Contains(html, "bit-ovl-opn");
        StringAssert.Contains(html, "bit-ovl-mfl");
        StringAssert.Contains(html, "bit-ovl-ctr");
        StringAssert.Contains(html, @"tabindex=""-1""");
        StringAssert.Contains(html, "bit-ovl-cnt");
        StringAssert.Contains(html, "Please wait");

        // An open layer is live from the first paint, and the scroll offset is nothing a prerender could have measured.
        Assert.DoesNotContain("inert", html);
        Assert.DoesNotContain("top:", html);
    }

    [TestMethod]
    public async Task BitOverlayShouldPrerenderAClosedOverlayInert()
    {
        var html = await Prerenderer.RenderAsync<BitOverlay>(new Dictionary<string, object?>
        {
            [nameof(BitOverlay.DefaultIsOpen)] = false,
        });

        StringAssert.Contains(html, "inert");
        Assert.DoesNotContain("bit-ovl-opn", html);
    }
}
