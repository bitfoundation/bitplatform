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

        Assert.Contains("bit-col-col", html);
        Assert.Contains("bit-col-cco", html);
        // Closed content is out of reach from the first frame, not only once the runtime arrives.
        Assert.Contains("inert", html);
        Assert.Contains("tabindex=\"-1\"", html);
        Assert.Contains("class=\"body\"", html);
    }

    [TestMethod]
    public async Task BitCollapseShouldRenderOpenBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitCollapse>(new Dictionary<string, object?>
        {
            [nameof(BitCollapse.ChildContent)] = _content,
            [nameof(BitCollapse.DefaultExpanded)] = true,
        });

        Assert.Contains("bit-col-exp", html);
        Assert.Contains("bit-col-cex", html);
        Assert.DoesNotContain("inert", html);
        Assert.Contains("tabindex=\"0\"", html);
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

        Assert.Contains("hidden=\"until-found\"", html);
        Assert.Contains("bit-col-huf", html);
        Assert.Contains("class=\"body\"", html);
    }
}
