using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.SwipeTrap;

/// <summary>
/// What a swipe trap looks like before the page is interactive. The gesture is only wired up after a render, so a
/// prerender - whose JS runtime throws - renders the container with its keyboard affordances and reaches for no script.
/// </summary>
[TestClass]
public class BitSwipeTrapPrerenderTests
{
    [TestMethod]
    public async Task BitSwipeTrapShouldPrerenderWithoutTheScript()
    {
        var html = await Prerenderer.RenderAsync<BitSwipeTrap>(new Dictionary<string, object?>
        {
            [nameof(BitSwipeTrap.OrientationLock)] = BitSwipeOrientation.Horizontal,
            [nameof(BitSwipeTrap.KeyboardTrigger)] = true,
            [nameof(BitSwipeTrap.AriaLabel)] = "Swipe to dismiss",
            [nameof(BitSwipeTrap.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Swipe me")),
        });

        StringAssert.Contains(html, "bit-stp-hrz");
        StringAssert.Contains(html, @"role=""group""");
        StringAssert.Contains(html, @"tabindex=""0""");
        StringAssert.Contains(html, @"aria-keyshortcuts=""ArrowLeft ArrowRight""");
        StringAssert.Contains(html, @"aria-label=""Swipe to dismiss""");
        StringAssert.Contains(html, "Swipe me");
    }

    [TestMethod]
    public async Task BitSwipeTrapShouldPrerenderAPlainTrapAsAPlainContainer()
    {
        var html = await Prerenderer.RenderAsync<BitSwipeTrap>();

        StringAssert.Contains(html, @"class=""bit-stp""");
        Assert.DoesNotContain("tabindex", html);
        Assert.DoesNotContain("role=", html);
    }
}
