using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Callout;

/// <summary>
/// What a callout sends before the page is interactive: the after-render pass never runs on a prerender, so the
/// open state has to come out of the render alone - and nothing of the render may reach for the JS side, which a
/// prerender does not have.
/// </summary>
[TestClass]
public class BitCalloutPrerenderTests
{
    private static readonly RenderFragment _anchor = builder => builder.AddMarkupContent(0, "<button class=\"trigger\">Anchor</button>");
    private static readonly RenderFragment _content = builder => builder.AddMarkupContent(0, "<span class=\"body\">Body</span>");

    [TestMethod]
    public async Task BitCalloutShouldRenderClosedBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitCallout>(new Dictionary<string, object?>
        {
            [nameof(BitCallout.Anchor)] = _anchor,
            [nameof(BitCallout.ChildContent)] = _content,
        });

        StringAssert.Contains(html, "class=\"trigger\"");
        StringAssert.Contains(html, "aria-expanded=\"false\"");
        StringAssert.Contains(html, "display:none");
        Assert.IsFalse(html.Contains("bit-clo-ocl"));
        // The content is in the page from the first render, so a reader without a runtime still has it.
        StringAssert.Contains(html, "class=\"body\"");
    }

    [TestMethod]
    public async Task BitCalloutShouldRenderOpenBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitCallout>(new Dictionary<string, object?>
        {
            [nameof(BitCallout.Anchor)] = _anchor,
            [nameof(BitCallout.ChildContent)] = _content,
            [nameof(BitCallout.DefaultIsOpen)] = true,
        });

        StringAssert.Contains(html, "bit-clo-opn");
        StringAssert.Contains(html, "bit-clo-ocl");
        StringAssert.Contains(html, "aria-expanded=\"true\"");
        StringAssert.Contains(html, "display:block");
        // The Escape is claimed from the render that opens the callout, ahead of any round trip.
        StringAssert.Contains(html, "data-bit-esc=\"claim\"");
    }

    [TestMethod]
    public async Task BitCalloutShouldKeepALazyContentOutOfAClosedCalloutBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitCallout>(new Dictionary<string, object?>
        {
            [nameof(BitCallout.Anchor)] = _anchor,
            [nameof(BitCallout.ChildContent)] = _content,
            [nameof(BitCallout.LazyRender)] = true,
        });

        StringAssert.Contains(html, "bit-clo-cal");
        Assert.IsFalse(html.Contains("class=\"body\""));
    }

    [TestMethod]
    public async Task BitCalloutShouldCorrectADisabledDefaultIsOpenBeforeInteractivity()
    {
        // A disabled callout cannot be left hanging over the page; before the first render there is no callout to
        // hide, only the state to correct, and no JS side to ask.
        var html = await Prerenderer.RenderAsync<BitCallout>(new Dictionary<string, object?>
        {
            [nameof(BitCallout.Anchor)] = _anchor,
            [nameof(BitCallout.ChildContent)] = _content,
            [nameof(BitCallout.DefaultIsOpen)] = true,
            [nameof(BitCallout.Disabled)] = true,
        });

        StringAssert.Contains(html, "bit-dis");
        Assert.IsFalse(html.Contains("bit-clo-ocl"));
        StringAssert.Contains(html, "aria-expanded=\"false\"");
    }
}
