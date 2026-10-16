using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.PullToRefresh;

/// <summary>
/// What a pull to refresh looks like before the page is interactive. The gesture is set up on the first after-render
/// and updated only once rendered, so a prerender - whose JS runtime throws - renders the anchor with an idle, silent
/// strip and reaches for no script.
/// </summary>
[TestClass]
public class BitPullToRefreshPrerenderTests
{
    [TestMethod]
    public async Task BitPullToRefreshShouldPrerenderTheAnchorAndAnIdleStrip()
    {
        var html = await Prerenderer.RenderAsync<BitPullToRefresh>(new Dictionary<string, object?>
        {
            [nameof(BitPullToRefresh.Trigger)] = 120,
            [nameof(BitPullToRefresh.FullWidth)] = true,
            [nameof(BitPullToRefresh.Color)] = BitColor.Success,
            [nameof(BitPullToRefresh.AriaLabel)] = "Pull to refresh the feed",
            [nameof(BitPullToRefresh.ChildContent)] = (RenderFragment)(b => b.AddMarkupContent(0, "<ul class=\"feed\"><li>one</li></ul>")),
        });

        StringAssert.Contains(html, "bit-ptr-flw");
        StringAssert.Contains(html, "--bit-ptr-color:var(--bit-clr-suc)");
        StringAssert.Contains(html, @"role=""group""");
        StringAssert.Contains(html, @"aria-label=""Pull to refresh the feed""");
        StringAssert.Contains(html, @"class=""feed""");
        StringAssert.Contains(html, "bit-ptr-lod");
        StringAssert.Contains(html, "bit-ptr-spw");

        // The live region is there from the start, with nothing to say yet, and the indicator is not refreshing.
        StringAssert.Contains(html, @"<span role=""status"" class=""bit-ptr-vhd""></span>");
        Assert.DoesNotContain("bit-ptr-spin", html);
        Assert.DoesNotContain("bit-ptr-crl", html);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldPrerenderADisabledOneAsDisabled()
    {
        var html = await Prerenderer.RenderAsync<BitPullToRefresh>(new Dictionary<string, object?>
        {
            [nameof(BitPullToRefresh.Disabled)] = true,
        });

        StringAssert.Contains(html, "bit-dis");
    }
}
