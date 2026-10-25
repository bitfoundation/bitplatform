using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.ScrollablePane;

/// <summary>
/// What a pane looks like on a statically rendered page, which has no render after the first one and no JavaScript
/// to call: the browser side and the initial position wait for the after-render pass, so a pane that asked for them
/// still renders its box, its styles and its content without throwing.
/// </summary>
[TestClass]
public class BitScrollablePanePrerenderTests
{
    [TestMethod]
    public async Task BitScrollablePaneShouldRenderItsContentAndStylesOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitScrollablePane>(new Dictionary<string, object?>
        {
            [nameof(BitScrollablePane.Height)] = "10rem",
            [nameof(BitScrollablePane.Fade)] = true,
            [nameof(BitScrollablePane.AutoScroll)] = true,
            [nameof(BitScrollablePane.InitialScrollTop)] = 120d,
            [nameof(BitScrollablePane.Focusable)] = true,
            [nameof(BitScrollablePane.AriaLabel)] = "Release notes",
            [nameof(BitScrollablePane.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        StringAssert.Contains(html, "Hello");
        StringAssert.Contains(html, "height:10rem");
        StringAssert.Contains(html, "bit-scp-fad");
        StringAssert.Contains(html, "tabindex=\"0\"");
        StringAssert.Contains(html, "role=\"region\"");
        StringAssert.Contains(html, "aria-label=\"Release notes\"");
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollingApiShouldDoNothingBeforeTheFirstRender()
    {
        // Every call quietly does nothing until there is something to do it to, rather than throwing on an empty
        // element reference.
        var pane = new BitScrollablePane();

        await pane.ScrollToEnd();
        await pane.ScrollToStart();
        await pane.ScrollTo(0, 0);
        await pane.ScrollBy(0, 0);
        await pane.ScrollToElement("row");
        await pane.FocusAsync();
        await pane.FocusAsync(true);
        await pane.Refresh();

        Assert.IsNull(await pane.GetScrollOffset());
    }
}
