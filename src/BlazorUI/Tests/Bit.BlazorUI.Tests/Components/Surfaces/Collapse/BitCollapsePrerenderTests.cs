using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Collapse;

/// <summary>
/// What a collapse sends before the page is interactive: the after-render pass never runs on a prerender, so the
/// state it starts in has to come out of the render alone, with no transition to end and no JS side to ask.
/// </summary>
[TestClass]
public class BitCollapsePrerenderTests
{
    private static readonly RenderFragment _content = builder => builder.AddMarkupContent(0, "<span class=\"body\">Body</span>");

    [TestMethod]
    public async Task BitCollapseShouldRenderClosedBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitCollapse>(new Dictionary<string, object?>
        {
            [nameof(BitCollapse.ChildContent)] = _content,
        });

        StringAssert.Contains(html, "bit-col-col");
        StringAssert.Contains(html, "bit-col-cco");
        // Closed content is out of reach from the first frame, not only once the runtime arrives.
        StringAssert.Contains(html, "inert");
        StringAssert.Contains(html, "tabindex=\"-1\"");
        StringAssert.Contains(html, "class=\"body\"");
    }

    [TestMethod]
    public async Task BitCollapseShouldRenderOpenBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitCollapse>(new Dictionary<string, object?>
        {
            [nameof(BitCollapse.ChildContent)] = _content,
            [nameof(BitCollapse.DefaultExpanded)] = true,
        });

        StringAssert.Contains(html, "bit-col-exp");
        StringAssert.Contains(html, "bit-col-cex");
        Assert.IsFalse(html.Contains("inert"));
        StringAssert.Contains(html, "tabindex=\"0\"");
    }

    [TestMethod]
    public async Task BitCollapseShouldHideAClosedSearchableSectionUntilFoundBeforeInteractivity()
    {
        // The state it starts in is where it begins rather than something it transitioned into, so the attribute
        // is on from the first render and nothing waits for a close that never played.
        var html = await Prerenderer.RenderAsync<BitCollapse>(new Dictionary<string, object?>
        {
            [nameof(BitCollapse.ChildContent)] = _content,
            [nameof(BitCollapse.HiddenUntilFound)] = true,
        });

        StringAssert.Contains(html, "hidden=\"until-found\"");
        StringAssert.Contains(html, "bit-col-huf");
        StringAssert.Contains(html, "class=\"body\"");
    }
}
