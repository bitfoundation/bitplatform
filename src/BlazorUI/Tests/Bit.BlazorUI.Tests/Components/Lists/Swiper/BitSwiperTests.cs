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

    [TestMethod,
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

    [TestMethod]
    public void BitSwiperShouldRenderDefaultIconsWithRotationOnPrev()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var component = RenderComponent<BitSwiper>();

        var nextIcon = component.Find(".bit-swp-rbt i");
        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bit-icon--ChevronRight"));
        Assert.IsTrue(prevIcon.ClassList.Contains("bit-icon--ChevronRight"));
        Assert.IsTrue(prevIcon.ClassList.Contains("bit-ico-r180"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectNextIconNameWhenProvided()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.NextIconName, "ChevronRightSmall");
        });

        var nextIcon = component.Find(".bit-swp-rbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bit-icon--ChevronRightSmall"));
        Assert.IsTrue(nextIcon.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectPrevIconNameWhenProvided()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.PrevIconName, "ChevronLeft");
        });

        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(prevIcon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(prevIcon.ClassList.Contains("bit-icon--ChevronLeft"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectNextIconWhenProvided()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var nextIconInfo = BitIconInfo.Fa("solid chevron-right");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.NextIcon, nextIconInfo);
        });

        var nextIcon = component.Find(".bit-swp-rbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(nextIcon.ClassList.Contains("fa-chevron-right"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectPrevIconWhenProvided()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var prevIconInfo = BitIconInfo.Fa("solid chevron-left");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.PrevIcon, prevIconInfo);
        });

        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(prevIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(prevIcon.ClassList.Contains("fa-chevron-left"));
    }

    [TestMethod]
    public void BitSwiperNextIconShouldTakePrecedenceOverNextIconName()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var nextIconInfo = BitIconInfo.Bi("arrow-right-circle");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.NextIcon, nextIconInfo);
            parameters.Add(p => p.NextIconName, "ChevronRight");
        });

        var nextIcon = component.Find(".bit-swp-rbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bi-arrow-right-circle"));
        Assert.IsFalse(nextIcon.ClassList.Contains("bit-icon--ChevronRight"));
    }

    [TestMethod]
    public void BitSwiperPrevIconShouldTakePrecedenceOverPrevIconName()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Observers.registerResize");
        Context.JSInterop.SetupVoid("BitBlazorUI.Swiper.setup");

        var prevIconInfo = BitIconInfo.Bi("arrow-left-circle");

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.PrevIcon, prevIconInfo);
            parameters.Add(p => p.PrevIconName, "ChevronLeft");
        });

        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(prevIcon.ClassList.Contains("bi-arrow-left-circle"));
        Assert.IsFalse(prevIcon.ClassList.Contains("bit-icon--ChevronLeft"));
    }
}
