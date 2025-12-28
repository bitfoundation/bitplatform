using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.ScrollablePane;

[TestClass]
public class BitScrollablePaneTests : BunitTestContext
{
    [TestMethod]
    public void BitScrollablePaneShouldRenderChildContent()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.AddChildContent("<p class=\"pane-content\">ScrollablePane Content</p>");
        });

        component.MarkupMatches(@"
<div class=""bit-scp"" id:ignore>
    <p class=""pane-content"">
        ScrollablePane Content
    </p>
</div>");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitScrollablePaneShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-scp");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitScrollablePaneShouldInvokeOnScroll()
    {
        var scrolled = false;

        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OnScroll, () => scrolled = true);
        });

        var root = component.Find(".bit-scp");

        root.TriggerEvent("onscroll", EventArgs.Empty);

        Assert.IsTrue(scrolled);
    }

    [TestMethod]
    public void BitScrollablePaneShouldAutoScrollAfterRender()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.ScrollablePane.scrollToEnd");

        RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.AutoScroll, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.scrollToEnd");
    }

    [TestMethod]
    public async Task BitScrollablePaneScrollToEndShouldCallJs()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.ScrollablePane.scrollToEnd");

        var component = RenderComponent<BitScrollablePane>();

        await component.Instance.ScrollToEnd();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.ScrollablePane.scrollToEnd");
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Overflow, BitOverflow.Hidden);
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("overflow:hidden"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowXStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverflowX, BitOverflow.Scroll);
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("overflow-x:scroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowYStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverflowY, BitOverflow.Visible);
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("overflow-y:visible"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectOverflowXYStyles()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.OverflowX, BitOverflow.Scroll);
            parameters.Add(p => p.OverflowY, BitOverflow.Visible);
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("overflow:scroll visible"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldRespectDimensions()
    {
        var component = RenderComponent<BitScrollablePane>(parameters =>
        {
            parameters.Add(p => p.Width, "200px");
            parameters.Add(p => p.Height, "120px");
        });

        var style = component.Find(".bit-scp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("width:200px"));
        Assert.IsTrue(style.Contains("height:120px"));
    }
}
