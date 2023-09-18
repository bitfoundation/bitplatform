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
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.IsVisible, true);
        });

        var element = com.Find(".bit-ovl");

        element.Click();

        Assert.IsFalse(element.ClassList.Contains("bit-ovl-vis"));
    }

}
