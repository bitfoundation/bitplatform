using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

/// <summary>
/// What a panel looks like on a statically rendered page, which has no render after the first one and no JavaScript
/// to call: everything the panel registers on the page waits for the after-render pass, so a prerender shows the
/// panel in the state it was given without throwing.
/// </summary>
[TestClass]
public class BitPanelPrerenderTests
{
    [TestMethod]
    public async Task BitPanelShouldRenderAnOpenPanelWithItsContentOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitPanel>(new Dictionary<string, object?>
        {
            [nameof(BitPanel.IsOpen)] = true,
            [nameof(BitPanel.HeaderText)] = "Settings",
            [nameof(BitPanel.ShowCloseButton)] = true,
            [nameof(BitPanel.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        StringAssert.Contains(html, "bit-pnl-opn");
        StringAssert.Contains(html, "bit-pnl-ovl-opn");
        StringAssert.Contains(html, "Settings");
        StringAssert.Contains(html, "Hello");
        StringAssert.Contains(html, "bit-pnl-cls");
        StringAssert.Contains(html, "role=\"dialog\"");
        StringAssert.Contains(html, "aria-modal=\"true\"");
        Assert.IsFalse(html.Contains("inert"), "An open panel is marked inert on the static page.");
    }

    [TestMethod]
    public async Task BitPanelShouldRenderAClosedPanelInertAndEmptyOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitPanel>(new Dictionary<string, object?>
        {
            [nameof(BitPanel.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        // The content goes into the page on the first opening, and a closed panel is kept out of the tab order and
        // the reading order of the static page the same way it is on the interactive one.
        StringAssert.Contains(html, "inert");
        StringAssert.Contains(html, "bit-pnl-ovl");
        Assert.IsFalse(html.Contains("Hello"));
        Assert.IsFalse(html.Contains("bit-pnl-opn"));
    }

    [TestMethod]
    public async Task BitPanelShouldRenderTheCascadedParametersOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitParams>(new Dictionary<string, object?>
        {
            [nameof(BitParams.Parameters)] = new List<IBitComponentParams>
            {
                new BitPanelParams { Placement = BitPlacement.Top, ShowCloseButton = true, CloseButtonTitle = "Dismiss" },
            },
            [nameof(BitParams.ChildContent)] = (RenderFragment)(b =>
            {
                b.OpenComponent<BitPanel>(0);
                b.AddAttribute(1, nameof(BitPanel.IsOpen), true);
                b.CloseComponent();
            }),
        });

        StringAssert.Contains(html, "bit-pnl-top");
        StringAssert.Contains(html, "aria-label=\"Dismiss\"");
    }
}
