using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

/// <summary>
/// What an accordion list sends before the page is interactive: the after-render pass never runs on a prerender
/// (or on a static SSR page at all), so whether a list of options is empty has to be learned within the render.
/// </summary>
[TestClass]
public class BitAccordionListPrerenderTests
{
    private static readonly RenderFragment EmptyContent = builder => builder.AddMarkupContent(0, "<span class=\"no-items\">Nothing here</span>");

    [TestMethod]
    public async Task BitAccordionListShouldRenderTheEmptyContentOfAListOfOptionsBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccordionList<BitAccordionListOption>>(new Dictionary<string, object?>
        {
            [nameof(BitAccordionList<BitAccordionListOption>.ChildContent)] = (RenderFragment)(builder => { }),
            [nameof(BitAccordionList<BitAccordionListOption>.EmptyContent)] = EmptyContent,
        });

        StringAssert.Contains(html, "no-items");
    }

    [TestMethod]
    public async Task BitAccordionListShouldNotRenderTheEmptyContentOfAListWithOptionsBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccordionList<BitAccordionListOption>>(new Dictionary<string, object?>
        {
            [nameof(BitAccordionList<BitAccordionListOption>.ChildContent)] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitAccordionListOption>(0);
                builder.AddComponentParameter(1, nameof(BitAccordionListOption.Title), "First");
                builder.CloseComponent();
            }),
            [nameof(BitAccordionList<BitAccordionListOption>.EmptyContent)] = EmptyContent,
        });

        StringAssert.Contains(html, "First");
        Assert.IsFalse(html.Contains("no-items"));
    }

    [TestMethod]
    public async Task BitAccordionListShouldRenderTheEmptyContentOfAnEmptyListOfItemsBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccordionList<BitAccordionListItem>>(new Dictionary<string, object?>
        {
            [nameof(BitAccordionList<BitAccordionListItem>.Items)] = new List<BitAccordionListItem>(),
            [nameof(BitAccordionList<BitAccordionListItem>.EmptyContent)] = EmptyContent,
        });

        StringAssert.Contains(html, "no-items");
    }
}
