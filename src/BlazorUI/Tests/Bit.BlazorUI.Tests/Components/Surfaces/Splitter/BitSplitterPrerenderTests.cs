using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Splitter;

/// <summary>
/// What a splitter looks like on a statically rendered page, which has no render after the first one and no
/// JavaScript to call: the drag engine waits for the after-render pass, so a prerender shows the two panels laid out
/// at the sizes the parameters declare, with the separator and its accessibility, and never throws.
/// </summary>
[TestClass]
public class BitSplitterPrerenderTests
{
    [TestMethod]
    public async Task BitSplitterShouldRenderItsPanelsAndSeparatorOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitSplitter>(new Dictionary<string, object?>
        {
            [nameof(BitSplitter.Id)] = "the-splitter",
            [nameof(BitSplitter.FirstPanelSize)] = (int?)150,
            [nameof(BitSplitter.Collapsible)] = true,
            [nameof(BitSplitter.ShowCollapseButton)] = true,
            [nameof(BitSplitter.AriaLabel)] = "Resize the panels",
            [nameof(BitSplitter.FirstPanel)] = (RenderFragment)(b => b.AddContent(0, "First")),
            [nameof(BitSplitter.SecondPanel)] = (RenderFragment)(b => b.AddContent(0, "Second")),
        });

        StringAssert.Contains(html, "First");
        StringAssert.Contains(html, "Second");
        StringAssert.Contains(html, "--bit-spl-fpn-size:150px");
        StringAssert.Contains(html, "role=\"separator\"");
        StringAssert.Contains(html, "aria-label=\"Resize the panels\"");
        StringAssert.Contains(html, "aria-controls=\"the-splitter-fpn\"");
        StringAssert.Contains(html, "bit-spl-cbt");
    }

    [TestMethod]
    public async Task BitSplitterApiShouldDoNothingBeforeTheJavaScriptSideIsUp()
    {
        // A splitter that has not been rendered yet has nothing to measure and nothing to put back in step.
        var splitter = new BitSplitter();

        Assert.IsNull(await splitter.GetPercent());
    }
}
