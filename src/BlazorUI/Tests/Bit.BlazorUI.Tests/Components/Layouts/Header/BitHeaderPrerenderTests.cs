using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Header;

/// <summary>
/// What a header looks like before the page is interactive - prerendered, or statically rendered for good - where
/// the scroll script that drives Reveal and ElevateOnScroll is never set up: it starts revealed and flat, its skip
/// link is already the first thing in it, and only an explicit Hidden takes it out of reach, so the markup a visitor
/// sees first is the one the script starts from.
/// </summary>
[TestClass]
public class BitHeaderPrerenderTests
{
    [TestMethod]
    public async Task BitHeaderShouldPrerenderRevealedAndFlat()
    {
        var html = await Prerenderer.RenderAsync<BitHeader>(new Dictionary<string, object?>
        {
            [nameof(BitHeader.Reveal)] = true,
            [nameof(BitHeader.ElevateOnScroll)] = true,
            [nameof(BitHeader.Fixed)] = true,
            [nameof(BitHeader.SkipLinkHref)] = "#main",
            [nameof(BitHeader.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Prerendered")),
        });

        StringAssert.Contains(html, "<header");
        StringAssert.Contains(html, "bit-hdr-fix");
        StringAssert.Contains(html, "bit-hdr-anm");
        StringAssert.Contains(html, "bit-hdr-esc");
        StringAssert.Contains(html, "Prerendered");

        // The skip link works without a script at all, so it is there on the first paint.
        StringAssert.Contains(html, "bit-hdr-skp");
        StringAssert.Contains(html, @"href=""https://localhost/#main""");

        // Nothing has scrolled yet, so there is nothing to slide away from or to lift above.
        Assert.IsFalse(html.Contains("bit-hdr-hdn"));
        Assert.IsFalse(html.Contains("bit-hdr-elv"));
        Assert.IsFalse(html.Contains(" inert"));
    }

    [TestMethod]
    public async Task BitHeaderShouldPrerenderAHiddenHeaderOutOfReach()
    {
        var html = await Prerenderer.RenderAsync<BitHeader>(new Dictionary<string, object?>
        {
            [nameof(BitHeader.Hidden)] = true,
            [nameof(BitHeader.Fixed)] = true,
        });

        // The explicit Hidden needs no script, so the first paint is already slid away and inert.
        StringAssert.Contains(html, "bit-hdr-hdn");
        StringAssert.Contains(html, " inert");
    }
}
