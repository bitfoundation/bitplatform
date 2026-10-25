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

        StringAssert.Contains(html, "<footer");
        StringAssert.Contains(html, "bit-ftr-fix");
        StringAssert.Contains(html, "bit-ftr-anm");
        StringAssert.Contains(html, "bit-ftr-esc");
        StringAssert.Contains(html, "Prerendered");

        // Nothing has scrolled yet, so there is nothing to slide away from or to shadow.
        Assert.IsFalse(html.Contains("bit-ftr-hdn"));
        Assert.IsFalse(html.Contains("bit-ftr-elv"));
        Assert.IsFalse(html.Contains(" inert"));
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
        StringAssert.Contains(html, "bit-ftr-hdn");
        StringAssert.Contains(html, " inert");
    }
}
