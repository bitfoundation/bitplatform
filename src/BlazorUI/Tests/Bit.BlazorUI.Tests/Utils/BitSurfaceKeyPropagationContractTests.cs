using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins that the surfaces which close on Escape stop no key from bubbling on to the Blazor handlers of the page around
/// them, so an app's keyboard shortcuts (Ctrl+S on a layout) keep working while one is open. Telling a nested surface's
/// Escape from an outer one's own is done in the browser instead (Utils._claimedEscapes, read back by each surface's
/// OnEscapeVerdict), which none of these renders runs; the component tests pin how each surface reads the verdict.
/// </summary>
[TestClass]
public sealed class BitSurfaceKeyPropagationContractTests : BunitTestContext
{
    [TestMethod]
    public void AnOpenDialogShouldLetTheKeysThroughToThePage()
    {
        var keys = PressInside(builder =>
        {
            builder.OpenComponent<BitDialog>(0);
            builder.AddAttribute(1, nameof(BitDialog.IsOpen), true);
            builder.AddAttribute(2, nameof(BitDialog.Title), "Title");
            builder.CloseComponent();
        }, ".bit-dlg");

        Assert.AreEqual(2, keys);
    }

    [TestMethod]
    public void AnOpenModalShouldLetTheKeysThroughToThePage()
    {
        var keys = PressInside(builder =>
        {
            builder.OpenComponent<BitModal>(0);
            builder.AddAttribute(1, nameof(BitModal.IsOpen), true);
            builder.CloseComponent();
        }, ".bit-mdl");

        Assert.AreEqual(2, keys);
    }

    [TestMethod]
    public void AnOpenPanelShouldLetTheKeysThroughToThePage()
    {
        var keys = PressInside(builder =>
        {
            builder.OpenComponent<BitPanel>(0);
            builder.AddAttribute(1, nameof(BitPanel.IsOpen), true);
            builder.CloseComponent();
        }, ".bit-pnl-cnt");

        Assert.AreEqual(2, keys);
    }

    [TestMethod]
    public void AnOpenCalloutShouldLetTheKeysOfItsAnchorThroughToThePage()
    {
        var keys = PressInside(builder =>
        {
            builder.OpenComponent<BitCallout>(0);
            builder.AddAttribute(1, nameof(BitCallout.Anchor), (RenderFragment)(b => b.AddMarkupContent(0, "<button>Anchor</button>")));
            builder.CloseComponent();
        }, ".bit-clo", open: rendered =>
        {
            rendered.Find(".bit-clo-acn").Click();

            Assert.IsTrue(rendered.FindComponent<BitCallout>().Instance.IsOpen);
        });

        Assert.AreEqual(2, keys);
    }

    // Renders the surface inside an element of the page listening for keys, opens it where that takes more than its
    // IsOpen, and presses an app's shortcut and Escape inside it: how many of the two the page heard.
    private int PressInside(RenderFragment surface, string selector, Action<IRenderedComponent<IComponent>>? open = null)
    {
        var keys = 0;

        var rendered = Context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => keys++));
            builder.AddContent(2, surface);
            builder.CloseElement();
        });

        open?.Invoke(rendered);

        rendered.Find(selector).KeyDown(new KeyboardEventArgs { Key = "s", CtrlKey = true });
        rendered.Find(selector).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        return keys;
    }
}
