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

        StringAssert.Contains(html, "bit-acd-hdr");
        StringAssert.Contains(html, "Shipping");
        StringAssert.Contains(html, "aria-expanded=\"false\"");
        StringAssert.Contains(html, "bit-acd-cco");
        // The content is in the page from the first render, so a reader without a runtime still has it.
        StringAssert.Contains(html, "class=\"body\"");
    }

    [TestMethod]
    public async Task BitAccordionShouldRenderAnExpandedPanelBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccordion>(new Dictionary<string, object?>
        {
            [nameof(BitAccordion.Title)] = "Shipping",
            [nameof(BitAccordion.DefaultIsExpanded)] = true,
        });

        StringAssert.Contains(html, "bit-acd-exp");
        StringAssert.Contains(html, "aria-expanded=\"true\"");
        StringAssert.Contains(html, "bit-acd-cex");
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

        StringAssert.Contains(html, "hidden=\"until-found\"");
        StringAssert.Contains(html, "bit-acd-huf");
    }
}
