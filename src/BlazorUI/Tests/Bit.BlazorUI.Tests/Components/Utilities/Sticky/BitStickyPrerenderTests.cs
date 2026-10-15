using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Sticky;

/// <summary>
/// What a sticky looks like before the page is interactive. The stuck detection is only attached after a render,
/// so a prerender - whose JS runtime throws - renders the plain, unstuck element and reaches for no script.
/// </summary>
[TestClass]
public class BitStickyPrerenderTests
{
    [TestMethod]
    public async Task BitStickyShouldPrerenderWithoutTheScript()
    {
        var html = await Prerenderer.RenderAsync<BitSticky>(new Dictionary<string, object?>
        {
            [nameof(BitSticky.Element)] = "header",
            [nameof(BitSticky.Placement)] = BitPlacement.Bottom,
            [nameof(BitSticky.ZIndex)] = 3,
            [nameof(BitSticky.StuckClass)] = "my-stuck",
            [nameof(BitSticky.ElevateOnStuck)] = true,
            [nameof(BitSticky.ScrollPadding)] = true,
            [nameof(BitSticky.AriaLabel)] = "Filters",
            [nameof(BitSticky.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Pinned")),
        });

        StringAssert.Contains(html, "<header");
        StringAssert.Contains(html, "bit-stk-btm");
        StringAssert.Contains(html, "bit-stk-elv");
        StringAssert.Contains(html, "z-index: 3");
        StringAssert.Contains(html, @"aria-label=""Filters""");
        StringAssert.Contains(html, "Pinned");

        // A header has a role of its own, so the name does not make it a group.
        Assert.DoesNotContain("role=", html);

        // Nothing has scrolled yet, so nothing is stuck.
        Assert.DoesNotContain("bit-stk-stc", html);
        Assert.DoesNotContain("my-stuck", html);
    }

    [TestMethod]
    public async Task BitStickyShouldPrerenderAPureCssStickyAsIs()
    {
        var html = await Prerenderer.RenderAsync<BitSticky>(new Dictionary<string, object?>
        {
            [nameof(BitSticky.Top)] = "10",
        });

        StringAssert.Contains(html, "top: 10px");
        StringAssert.Contains(html, @"class=""bit-stk""");
    }
}
