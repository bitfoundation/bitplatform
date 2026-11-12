using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Footer;

/// <summary>
/// What a footer looks like before the page is interactive - prerendered, or statically rendered for good - where
/// the scroll script that drives Reveal and ElevateOnScroll is never set up: it starts revealed and flat, and only an
/// explicit Hidden takes it out of reach, so the markup a visitor sees first is the one the script starts from.
/// </summary>
[TestClass]
public class BitFooterPrerenderTests
{
    [TestMethod]
    public async Task BitFooterShouldPrerenderRevealedAndFlat()
    {
        var html = await Prerenderer.RenderAsync<BitFooter>(new Dictionary<string, object?>
        {
            [nameof(BitFooter.Reveal)] = true,
            [nameof(BitFooter.ElevateOnScroll)] = true,
            [nameof(BitFooter.Fixed)] = true,
            [nameof(BitFooter.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Prerendered")),
        });

        Assert.Contains("<footer", html);
        Assert.Contains("bit-ftr-fix", html);
        Assert.Contains("bit-ftr-anm", html);
        Assert.Contains("bit-ftr-esc", html);
        Assert.Contains("Prerendered", html);

        // Nothing has scrolled yet, so there is nothing to slide away from or to shadow.
        Assert.DoesNotContain("bit-ftr-hdn", html);
        Assert.DoesNotContain("bit-ftr-elv", html);
        Assert.DoesNotContain(" inert", html);
    }

    [TestMethod]
    public async Task BitFooterShouldPrerenderAHiddenFooterOutOfReach()
    {
        var html = await Prerenderer.RenderAsync<BitFooter>(new Dictionary<string, object?>
        {
            [nameof(BitFooter.Hidden)] = true,
            [nameof(BitFooter.Fixed)] = true,
        });

        // The explicit Hidden needs no script, so the first paint is already slid away and inert.
        Assert.Contains("bit-ftr-hdn", html);
        Assert.Contains(" inert", html);
    }
}
