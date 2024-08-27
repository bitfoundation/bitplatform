using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

[TestClass]
public class BitOverlayTests : BunitTestContext
{

    [TestMethod]
    public void BitOverlayInitialTest()
    {
        var com = RenderComponent<BitOverlay>();
        var element = com.Find(".bit-ovl");

        Assert.IsFalse(element.ClassList.Contains("bit-ovl-vis")); //shouldn't be visible when rendered

    }

    [TestMethod]
    public void BitOverlayIsVisibleTest()
    {
        var isVisible = true;
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsVisible, isVisible, value => isVisible = value);
        });

        var element = com.Find(".bit-ovl");

        Assert.IsTrue(element.ClassList.Contains("bit-ovl-vis"));
    }

    [TestMethod]
    public void BitOverlayAutoCloseTest()
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
    public void BitOverlayDisabledAutoCloseTest()
    {
        var isVisible = true;
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsVisible, isVisible, value => isVisible = value);
            parameters.Add(p => p.NoAutoClose, true);
        });

        var element = com.Find(".bit-ovl");

        element.Click();

        Assert.IsTrue(element.ClassList.Contains("bit-ovl-vis"));
    }

    [TestMethod]
    public void BitOverlayAbsolutePositionTest()
    {
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Add(p => p.AbsolutePosition, true);
        });

        var element = com.Find(".bit-ovl");

        Assert.IsTrue(element.ClassList.Contains("bit-ovl-abs"));
    }

    [TestMethod]
    public void BitOverlayAutoToggleScrollTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var isVisible = true;
        var com = RenderComponent<BitOverlay>(parameters =>
        {
            parameters.Bind(p => p.IsVisible, isVisible, value => isVisible = value);
            parameters.Add(p => p.AutoToggleScroll, true);
        });

        var element = com.Find(".bit-ovl");
        element.Click();

        //AutoToggleScroll is false by default so it should invoke "BitBlazorUI.Overlay.toggleScroll" once and then once again on closing component
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Overlay.toggleScroll", 2);
    }

}
