using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Accordion;

/// <summary>
/// What an accordion sends before the page is interactive: the after-render pass never runs on a prerender, so the
/// header, the panel and its state have to come out of the render alone, and nothing of it may reach for the JS side.
/// </summary>
[TestClass]
public class BitAccordionPrerenderTests
{
    [TestMethod]
    public async Task BitAccordionShouldRenderACollapsedPanelBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccordion>(new Dictionary<string, object?>
        {
            [nameof(BitAccordion.Title)] = "Shipping",
            [nameof(BitAccordion.ChildContent)] = (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"body\">Body</span>")),
        });

        Assert.Contains("bit-acd-hdr", html);
        Assert.Contains("Shipping", html);
        Assert.Contains("aria-expanded=\"false\"", html);
        Assert.Contains("bit-acd-cco", html);
        // The content is in the page from the first render, so a reader without a runtime still has it.
        Assert.Contains("class=\"body\"", html);
    }

    [TestMethod]
    public async Task BitAccordionShouldRenderAnExpandedPanelBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccordion>(new Dictionary<string, object?>
        {
            [nameof(BitAccordion.Title)] = "Shipping",
            [nameof(BitAccordion.DefaultIsExpanded)] = true,
        });

        Assert.Contains("bit-acd-exp", html);
        Assert.Contains("aria-expanded=\"true\"", html);
        Assert.Contains("bit-acd-cex", html);
    }

    [TestMethod]
    public async Task BitAccordionShouldHideACollapsedSearchablePanelUntilFoundBeforeInteractivity()
    {
        // Nothing was played to get to the collapsed state, so the attribute is on from the first render, with no
        // transition to wait for and so no call to the JS side.
        var html = await Prerenderer.RenderAsync<BitAccordion>(new Dictionary<string, object?>
        {
            [nameof(BitAccordion.Title)] = "Shipping",
            [nameof(BitAccordion.HiddenUntilFound)] = true,
        });

        Assert.Contains("hidden=\"until-found\"", html);
        Assert.Contains("bit-acd-huf", html);
    }
}
