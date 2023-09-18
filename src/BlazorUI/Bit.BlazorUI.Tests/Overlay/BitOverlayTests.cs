using System;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Bit.BlazorUI.Tests.Overlay;

[TestClass]
public class BitOverlayTests : BunitTestContext
{
    [TestMethod]
    public void GivenEmptyComponent_ShouldRenderMarkupCorrectly()
    {
        var com = RenderComponent<BitOverlay>();
        var element = com.Find(".bit-ovl");
        Assert.IsTrue(element.HasAttribute("id"));
        //how to check if body(as default scroll selector) has overflow:hidden?
    }

    [TestMethod]
    public void GivenIsVisible_ShouldHasProperClass()
    {
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsVisible, true);
        });

        var element = com.Find(".bit-ovl");

        Assert.IsTrue(element.ClassList.Contains("bit-ovl-vis"));
    }

    [TestMethod]
    public void GivenClickOnVisibleComponent_ShouldInVisibleComponent()
    {
        var isVisible = true;
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsVisible, isVisible, value => isVisible = value);
        });

        var element = com.Find(".bit-ovl");

        element.Click();

        Assert.IsFalse(element.ClassList.Contains("bit-ovl-vis"));
    }

    [TestMethod]
    public void GivenClickOnVisibleDisabledAutoCloseComponent_ShouldntInVisibleComponent()
    {
        var isVisible = true;
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsVisible, isVisible, value => isVisible = value);
            parameters.Add(p => p.AutoClose, false);
        });

        var element = com.Find(".bit-ovl");

        element.Click();

        Assert.IsTrue(element.ClassList.Contains("bit-ovl-vis"));
    }


    [TestMethod]
    public void GivenAbsoluteTrue_ShouldHasProperClass()
    {
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
        });

        var element = com.Find(".bit-ovl");

        Assert.IsTrue(element.ClassList.Contains("bit-ovl-abs"));
    }

    //AutoToggleScroll must test


}
