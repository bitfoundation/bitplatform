using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// What a Modal looks like on a statically rendered page, which has no render after the first one and no JavaScript
/// to call: the focus handling, the hold on the page and the Escape watch all wait for the after-render pass, so a
/// prerender shows the Modal in the state it was given, with its chrome and its dialog semantics, and never throws.
/// </summary>
[TestClass]
public class BitModalPrerenderTests
{
    [TestMethod]
    public async Task BitModalShouldRenderAnOpenModalWithItsChromeOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitModal>(new Dictionary<string, object?>
        {
            [nameof(BitModal.IsOpen)] = true,
            [nameof(BitModal.HeaderText)] = "Settings",
            [nameof(BitModal.ShowCloseButton)] = true,
            [nameof(BitModal.FooterText)] = "Footer",
            [nameof(BitModal.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        StringAssert.Contains(html, "bit-mdl");
        StringAssert.Contains(html, "bit-mdl-ovl");
        StringAssert.Contains(html, "Settings");
        StringAssert.Contains(html, "Hello");
        StringAssert.Contains(html, "Footer");
        StringAssert.Contains(html, "bit-mdl-cls");
        StringAssert.Contains(html, "role=\"dialog\"");
        StringAssert.Contains(html, "aria-modal=\"true\"");
        Assert.IsFalse(html.Contains("inert"), "An open Modal is marked inert on the static page.");
    }

    [TestMethod]
    public async Task BitModalShouldRenderNothingOfAClosedModalOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitModal>(new Dictionary<string, object?>
        {
            [nameof(BitModal.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        // A closed Modal that was never opened is not in the page at all, kept-mounted or not.
        Assert.IsFalse(html.Contains("bit-mdl"));
        Assert.IsFalse(html.Contains("Hello"));
    }

    [TestMethod]
    public async Task BitModalShouldRenderTheCascadedDefaultsOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitParams>(new Dictionary<string, object?>
        {
            [nameof(BitParams.Parameters)] = new List<IBitComponentParams>
            {
                new BitModalParams { Position = BitPosition.TopCenter, ShowCloseButton = true, CloseButtonTitle = "Dismiss" },
            },
            [nameof(BitParams.ChildContent)] = (RenderFragment)(b =>
            {
                b.OpenComponent<BitModal>(0);
                b.AddComponentParameter(1, nameof(BitModal.IsOpen), true);
                b.CloseComponent();
            }),
        });

        StringAssert.Contains(html, "bit-mdl-tcr");
        StringAssert.Contains(html, "aria-label=\"Dismiss\"");
    }
}
