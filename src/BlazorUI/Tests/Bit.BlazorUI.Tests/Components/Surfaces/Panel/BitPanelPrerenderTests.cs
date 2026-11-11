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

        Assert.Contains("bit-pnl-opn", html);
        Assert.Contains("bit-pnl-ovl-opn", html);
        Assert.Contains("Settings", html);
        Assert.Contains("Hello", html);
        Assert.Contains("bit-pnl-cls", html);
        Assert.Contains("role=\"dialog\"", html);
        Assert.Contains("aria-modal=\"true\"", html);
        Assert.DoesNotContain("inert", html, "An open panel is marked inert on the static page.");
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
        Assert.Contains("inert", html);
        Assert.Contains("bit-pnl-ovl", html);
        Assert.DoesNotContain("Hello", html);
        Assert.DoesNotContain("bit-pnl-opn", html);
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

        Assert.Contains("bit-pnl-top", html);
        Assert.Contains("aria-label=\"Dismiss\"", html);
    }
}
