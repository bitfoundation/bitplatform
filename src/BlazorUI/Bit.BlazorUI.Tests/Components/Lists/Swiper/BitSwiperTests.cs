using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Swiper;

[TestClass]
public class BitSwiperTests : BunitTestContext
{
    [TestMethod]
    public void BitSwiperShouldHideNavigationWhenRequested()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.HideNextPrev, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-swp-lbt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-swp-rbt").Count);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitSwiperShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-swp");

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
    public void BitSwiperShouldRespectRtlDirection()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-swp");
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));

        var containerStyle = component.Find(".bit-swp-cnt").GetAttribute("style") ?? string.Empty;
        Assert.IsTrue(containerStyle.Contains("direction:rtl"));
    }

    [TestMethod]
    public void BitSwiperShouldRegisterJsInteropOnFirstRender()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        RenderComponent<BitSwiper>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.setup");
    }

    [TestMethod]
    public async Task BitSwiperShouldDisposeJsInteropOnDispose()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.dispose");
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.unregisterResize");

        var component = RenderComponent<BitSwiper>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.dispose");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Observers.unregisterResize");
    }

    [TestMethod]
    public void BitSwiperShouldRenderItems()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var component = RenderComponent<BitSwiperTest>();

        var items = component.FindAll(".bit-swpi");

        Assert.AreEqual(3, items.Count);

        Assert.AreEqual(1, component.FindAll(".bit-swp-lbt").Count);
        Assert.AreEqual(1, component.FindAll(".bit-swp-rbt").Count);
    }
}
