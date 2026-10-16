using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.Button;

/// <summary>
/// What a button sends before the page is interactive: the drag script of a draggable floating button is only enabled
/// in the after-render pass, which never runs on a prerender, so the markup has to come out whole without it.
/// </summary>
[TestClass]
public class BitButtonPrerenderTests
{
    [TestMethod]
    public async Task BitButtonShouldPrerenderADraggableFloatingButtonWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitButton>(new Dictionary<string, object?>
        {
            [nameof(BitButton.Float)] = true,
            [nameof(BitButton.Draggable)] = true,
            [nameof(BitButton.FloatPosition)] = BitPosition.TopLeft,
            [nameof(BitButton.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Move me")),
        });

        StringAssert.Contains(html, "bit-btn-ffx");
        StringAssert.Contains(html, "bit-btn-drg");
        StringAssert.Contains(html, "bit-btn-tlf");
        StringAssert.Contains(html, "aria-keyshortcuts=\"ArrowUp ArrowDown ArrowLeft ArrowRight\"");
        StringAssert.Contains(html, "Move me");
    }

    [TestMethod]
    public async Task BitButtonShouldPrerenderTheLoadingStateWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitButton>(new Dictionary<string, object?>
        {
            [nameof(BitButton.IsLoading)] = true,
            [nameof(BitButton.LoadingLabel)] = "Saving...",
            [nameof(BitButton.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Save")),
        });

        StringAssert.Contains(html, "aria-busy=\"true\"");
        StringAssert.Contains(html, "bit-btn-spn");
        StringAssert.Contains(html, "role=\"status\"");
    }
}
