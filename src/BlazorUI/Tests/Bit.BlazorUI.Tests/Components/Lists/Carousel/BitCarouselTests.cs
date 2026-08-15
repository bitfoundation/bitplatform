using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Carousel;

[TestClass]
public partial class BitCarouselTests : BunitTestContext
{
    [TestInitialize]
    public void Init()
    {
        Services.AddScoped(_ => new BitPageVisibility(new TestJsRuntime()));
    }

    [TestMethod]
    public void BitCarouselShouldRenderItemsAndControls()
    {
        var component = RenderComponent<BitCarouselTest>();

        var items = component.FindAll(".bit-crsi");
        Assert.AreEqual(3, items.Count);

        // container
        var container = component.Find(".bit-csl-cnt");
        Assert.IsNotNull(container);

        // next/prev buttons should exist by default and be real buttons
        var leftBtn = component.Find("button.bit-csl-lbt");
        var rightBtn = component.Find("button.bit-csl-rbt");
        Assert.IsNotNull(leftBtn);
        Assert.IsNotNull(rightBtn);

        // the dots appear once the carousel has been laid out and knows its pages count
        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));
    }

    [TestMethod]
    public void BitCarouselShouldRenderAccessibilityAttributes()
    {
        var component = RenderComponent<BitCarouselTest>();

        var root = component.Find(".bit-csl");
        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("carousel", root.GetAttribute("aria-roledescription"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));

        var items = component.FindAll(".bit-crsi");
        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            Assert.AreEqual("group", item.GetAttribute("role"));
            Assert.AreEqual("slide", item.GetAttribute("aria-roledescription"));
            Assert.AreEqual($"{index + 1} of 3", item.GetAttribute("aria-label"));
        }

        component.WaitForAssertion(() =>
        {
            var dotsGroup = component.Find(".bit-csl-dts");
            Assert.AreEqual("group", dotsGroup.GetAttribute("role"));
            Assert.AreEqual("Choose slide to display", dotsGroup.GetAttribute("aria-label"));

            var dots = component.FindAll("button.bit-csl-dot");
            Assert.AreEqual(3, dots.Count);
            Assert.AreEqual("Slide 1", dots[0].GetAttribute("aria-label"));
            Assert.AreEqual("true", dots[0].GetAttribute("aria-current"));
            Assert.IsNull(dots[1].GetAttribute("aria-current"));
        });
    }

    [TestMethod]
    public void BitCarouselShouldRespectCustomAriaLabels()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.DotAriaLabel, "Page");
            parameters.Add(p => p.GoLeftAriaLabel, "Go left");
            parameters.Add(p => p.GoRightAriaLabel, "Go right");
        });

        Assert.AreEqual("Go left", component.Find(".bit-csl-lbt").GetAttribute("aria-label"));
        Assert.AreEqual("Go right", component.Find(".bit-csl-rbt").GetAttribute("aria-label"));

        component.WaitForAssertion(() =>
        {
            var dots = component.FindAll(".bit-csl-dot");
            Assert.AreEqual("Page 2", dots[1].GetAttribute("aria-label"));
        });
    }

    [TestMethod]
    public void BitCarouselShouldRenderDefaultNavigationAriaLabels()
    {
        var component = RenderComponent<BitCarouselTest>();

        Assert.AreEqual("Next slide", component.Find(".bit-csl-lbt").GetAttribute("aria-label"));
        Assert.AreEqual("Previous slide", component.Find(".bit-csl-rbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCarouselShouldFlipNavigationAriaLabelsInRtl()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("Previous slide", component.Find(".bit-csl-lbt").GetAttribute("aria-label"));
        Assert.AreEqual("Next slide", component.Find(".bit-csl-rbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCarouselShouldHideDotsWhenHideDotsTrue()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.HideDots, true);
        });

        var dots = component.FindAll(".bit-csl-dcn");
        Assert.AreEqual(0, dots.Count);
    }

    [TestMethod]
    public void BitCarouselShouldHideDotsWhenSinglePage()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 3);
            parameters.Add(p => p.VisibleItemsCount, 3);
        });

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll(".bit-csl-dcn").Count));
    }

    [TestMethod]
    public void BitCarouselShouldHideNextPrevWhenHideNextPrevTrue()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.HideNextPrev, true);
        });

        var left = component.FindAll(".bit-csl-lbt");
        var right = component.FindAll(".bit-csl-rbt");

        Assert.AreEqual(0, left.Count);
        Assert.AreEqual(0, right.Count);
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-csl-apri")]
    [DataRow(BitColorKind.Secondary, "bit-csl-asec")]
    [DataRow(BitColorKind.Tertiary, "bit-csl-ater")]
    [DataRow(BitColorKind.Transparent, "bit-csl-atra")]
    public void BitCarouselShouldRespectAccent(BitColorKind accent, string expectedClass)
    {
        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.Add(p => p.Accent, accent);
        });

        var root = component.Find(".bit-csl");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-csl-pri")]
    [DataRow(BitColor.Secondary, "bit-csl-sec")]
    [DataRow(BitColor.Tertiary, "bit-csl-ter")]
    [DataRow(BitColor.Info, "bit-csl-inf")]
    [DataRow(BitColor.Success, "bit-csl-suc")]
    [DataRow(BitColor.Warning, "bit-csl-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-csl-swr")]
    [DataRow(BitColor.Error, "bit-csl-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-csl-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-csl-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-csl-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-csl-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-csl-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-csl-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-csl-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-csl-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-csl-tbr")]
    public void BitCarouselShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var root = component.Find(".bit-csl");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-csl-sm")]
    [DataRow(BitSize.Medium, "bit-csl-md")]
    [DataRow(BitSize.Large, "bit-csl-lg")]
    public void BitCarouselShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var root = component.Find(".bit-csl");
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitCarouselShouldRespectVertical()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        var root = component.Find(".bit-csl");
        Assert.IsTrue(root.ClassList.Contains("bit-csl-vrt"));
    }

    [TestMethod]
    public void BitCarouselShouldRespectGap()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Gap, "1rem");
        });

        var root = component.Find(".bit-csl");
        StringAssert.Contains(root.GetAttribute("style"), "--bit-csl-gap:1rem");
    }

    [TestMethod]
    public void BitCarouselShouldRespectNoKeyboard()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.NoKeyboard, true);
        });

        var root = component.Find(".bit-csl");
        Assert.IsFalse(root.HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCarouselShouldRespectNoDrag()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.NoDrag, true);
        });

        var container = component.Find(".bit-csl-cnt");
        Assert.IsTrue(container.ClassList.Contains("bit-csl-ndr"));
    }

    [TestMethod]
    public void BitCarouselShouldRespectIsEnabledFalse()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        var root = component.Find(".bit-csl");
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsFalse(root.HasAttribute("tabindex"));

        Assert.IsTrue(component.Find(".bit-csl-lbt").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-csl-rbt").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitCarouselShouldRenderPlayPauseButtonAndToggle()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.ShowPlayPause, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-csl-ppb").Count));

        var button = component.Find(".bit-csl-ppb");
        Assert.AreEqual("Stop automatic slide show", button.GetAttribute("aria-label"));

        button.Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("Start automatic slide show", component.Find(".bit-csl-ppb").GetAttribute("aria-label"));
        });

        Assert.IsTrue(component.Instance.Carousel.IsPaused);

        component.Find(".bit-csl-ppb").Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("Stop automatic slide show", component.Find(".bit-csl-ppb").GetAttribute("aria-label"));
        });

        Assert.IsFalse(component.Instance.Carousel.IsPaused);
    }

    [TestMethod]
    public void BitCarouselShouldNotRenderPlayPauseButtonWithoutAutoPlay()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ShowPlayPause, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        Assert.AreEqual(0, component.FindAll(".bit-csl-ppb").Count);
    }

    [TestMethod]
    public async Task BitCarouselShouldNavigateWithPublicApi()
    {
        var changedPage = -1;

        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.OnChange, (int page) => changedPage = page);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var carousel = component.Instance.Carousel;

        Assert.AreEqual(0, carousel.CurrentPage);
        Assert.AreEqual(3, carousel.ItemsCount);
        Assert.AreEqual(3, carousel.PagesCount);

        await component.InvokeAsync(carousel.GoNext);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, carousel.CurrentPage);
            Assert.AreEqual(1, changedPage);
        });

        await component.InvokeAsync(carousel.GoPrev);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, carousel.CurrentPage);
            Assert.AreEqual(0, changedPage);
        });

        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, carousel.CurrentPage);
            Assert.AreEqual(2, changedPage);
        });
    }

    [TestMethod]
    public async Task BitCarouselShouldNotNavigatePastEndsWithoutInfiniteScrolling()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var carousel = component.Instance.Carousel;

        await component.InvokeAsync(carousel.GoPrev);

        Assert.AreEqual(0, carousel.CurrentPage);

        await component.InvokeAsync(carousel.GoNext);
        await component.InvokeAsync(carousel.GoNext);
        await component.InvokeAsync(carousel.GoNext);
        await component.InvokeAsync(carousel.GoNext);

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));
    }

    [TestMethod]
    public async Task BitCarouselShouldWrapAroundWithInfiniteScrolling()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var carousel = component.Instance.Carousel;

        await component.InvokeAsync(carousel.GoPrev);

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));

        await component.InvokeAsync(carousel.GoNext);

        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldNavigateWithDots()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        component.FindAll(".bit-csl-dot")[2].Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, component.Instance.Carousel.CurrentPage);

            var dots = component.FindAll(".bit-csl-dot");
            Assert.IsFalse(dots[0].ClassList.Contains("bit-csl-cud"));
            Assert.IsTrue(dots[2].ClassList.Contains("bit-csl-cud"));
        });
    }

    [TestMethod]
    public void BitCarouselShouldMarkOffscreenItemsHidden()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-crsi");

            Assert.IsNull(items[0].GetAttribute("aria-hidden"));
            Assert.AreEqual("true", items[1].GetAttribute("aria-hidden"));
            Assert.AreEqual("true", items[2].GetAttribute("aria-hidden"));
        });
    }

    [TestMethod]
    public void BitCarouselShouldRespectDefaultPage()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.DefaultPage, 2);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.Carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitCarouselClassStyles
            {
                Root = "custom-root",
                Container = "custom-container",
                Item = "custom-item",
                Dots = "custom-dot",
                CurrentDot = "custom-current-dot"
            });
            parameters.Add(p => p.Styles, new BitCarouselClassStyles
            {
                Root = "margin: 1px;",
                Container = "padding: 1px;"
            });
        });

        var root = component.Find(".bit-csl");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");

        var container = component.Find(".bit-csl-cnt");
        Assert.IsTrue(container.ClassList.Contains("custom-container"));
        StringAssert.Contains(container.GetAttribute("style"), "padding: 1px;");

        var item = component.Find(".bit-crsi");
        Assert.IsTrue(item.ClassList.Contains("custom-item"));

        component.WaitForAssertion(() =>
        {
            var dots = component.FindAll(".bit-csl-dot");
            Assert.IsTrue(dots[0].ClassList.Contains("custom-dot"));
            Assert.IsTrue(dots[0].ClassList.Contains("custom-current-dot"));
            Assert.IsFalse(dots[1].ClassList.Contains("custom-current-dot"));
        });
    }

    [TestMethod]
    public void BitCarouselShouldRespectRtl()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-csl");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));

        var container = component.Find(".bit-csl-cnt");
        StringAssert.Contains(container.GetAttribute("style"), "direction:rtl");
    }

    [TestMethod]
    public async Task BitCarouselShouldStopAutoPlayOnInteraction()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.StopOnInteraction, true);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var leftButton = component.Find(".bit-csl-lbt");
        await leftButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.Carousel.IsPaused));
    }

    [TestMethod]
    public void BitCarouselShouldClampVisibleAndScrollCounts()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 2);
            parameters.Add(p => p.VisibleItemsCount, 5);
            parameters.Add(p => p.ScrollItemsCount, 9);
        });

        // 2 items shown at once (clamped from 5) leaves a single page.
        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.Carousel.PagesCount));
    }

    [TestMethod]
    public void BitCarouselShouldNavigateWithKeyboard()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });
        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });
        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "End" });
        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Home" });
        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldFlipKeyboardNavigationInRtl()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        // In a right-to-left carousel the next slide sits on the left, so ArrowLeft moves forwards.
        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });
        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });
        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldNavigateWithKeyboardVertically()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown" });
        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowUp" });
        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldIgnoreModifiedNavigationKeys()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        root.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

        Assert.AreEqual(0, carousel.CurrentPage);
    }

    [TestMethod]
    public void BitCarouselShouldNavigateWithWheel()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Wheel, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var container = component.Find(".bit-csl-cnt");
        var carousel = component.Instance.Carousel;

        container.Wheel(new Microsoft.AspNetCore.Components.Web.WheelEventArgs { DeltaY = 100 });

        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldIgnoreWheelWhenWheelNotEnabled()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var container = component.Find(".bit-csl-cnt");

        container.Wheel(new Microsoft.AspNetCore.Components.Web.WheelEventArgs { DeltaY = 100 });

        Assert.AreEqual(0, component.Instance.Carousel.CurrentPage);
    }

    [TestMethod]
    public void BitCarouselShouldClampDefaultPageToLastPage()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.DefaultPage, 99);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.Carousel.CurrentPage));
    }

    [TestMethod]
    public async Task BitCarouselShouldKeepLastPageFullWithUnevenItems()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 5);
            parameters.Add(p => p.VisibleItemsCount, 2);
            parameters.Add(p => p.ScrollItemsCount, 2);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.Carousel.PagesCount));

        var carousel = component.Instance.Carousel;

        await component.InvokeAsync(carousel.GoNext);
        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));

        // The last move only advances by one item (instead of two), so the last page holds the last
        // two items instead of the last item and a blank space.
        await component.InvokeAsync(carousel.GoNext);
        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));

        await component.InvokeAsync(carousel.GoNext);
        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));
    }

    [TestMethod]
    public async Task BitCarouselShouldWrapPartialLastPageWithInfiniteScrolling()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 5);
            parameters.Add(p => p.VisibleItemsCount, 2);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.Carousel.PagesCount));

        var carousel = component.Instance.Carousel;

        // The last page holds a single item, so its second slot borrows the first item of the carousel.
        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));

        await component.InvokeAsync(() => carousel.GoTo(1));

        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldAutoPlayAdvancePages()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
        });

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.IsTrue(carousel.CurrentPage > 0), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitCarouselShouldAutoPlayBackwardsWithAutoPlayReverse()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayReverse, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        var carousel = component.Instance.Carousel;

        // Playing backwards from the first page of an infinite carousel wraps around to the last one.
        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitCarouselShouldStopAutoPlayOnLastSlide()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.StopOnLastSlide, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
        });

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage), TimeSpan.FromSeconds(5));

        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPaused), TimeSpan.FromSeconds(5));

        Assert.IsFalse(carousel.IsPlaying);
        Assert.AreEqual(2, carousel.CurrentPage);
    }

    [TestMethod]
    public async Task BitCarouselShouldNotAutoPlayWhilePaused()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
        });

        var carousel = component.Instance.Carousel;

        await component.InvokeAsync(carousel.Pause);

        Assert.IsFalse(carousel.IsPlaying);

        // A few intervals worth of quiet proves the timer is not ticking behind the paused state.
        await Task.Delay(250);

        Assert.AreEqual(0, carousel.CurrentPage);
    }

    [TestMethod]
    public async Task BitCarouselShouldSurviveOnChangeThrowingDuringAutoPlay()
    {
        // The timer elapses on a thread pool thread, so an exception the navigation lets through used
        // to escape the async void handler and take the whole process (this test host included) down.
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
            parameters.Add(p => p.InfiniteScrolling, true);
            parameters.Add(p => p.OnChange, (int _) => throw new InvalidOperationException("boom"));
        });

        var carousel = component.Instance.Carousel;

        // The instance is polled directly (instead of through WaitForAssertion) because the thrown
        // exception ends up at the renderer, which rethrows it from the render-tree helpers.
        for (var i = 0; i < 100 && carousel.CurrentPage == 0; i++)
        {
            await Task.Delay(50);
        }

        Assert.IsTrue(carousel.CurrentPage > 0);

        // The test (and the test host) surviving a few more failing ticks is the actual assertion here.
        await Task.Delay(250);
    }

    [TestMethod]
    public async Task BitCarouselShouldHandleEmptyCarouselSafely()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 0);
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
        });

        var carousel = component.Instance.Carousel;

        Assert.AreEqual(0, carousel.ItemsCount);
        Assert.AreEqual(0, carousel.PagesCount);
        Assert.IsFalse(carousel.IsPlaying);

        await component.InvokeAsync(carousel.GoNext);
        await component.InvokeAsync(carousel.GoPrev);
        await component.InvokeAsync(() => carousel.GoTo(2));

        // A few intervals worth of quiet proves the timer never starts against an empty carousel.
        await Task.Delay(250);

        Assert.AreEqual(0, carousel.CurrentPage);
    }

    [TestMethod]
    public async Task BitCarouselShouldDropNavigationStartedMidMove()
    {
        var component = RenderComponent<BitCarouselTest>();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var carousel = component.Instance.Carousel;

        // The second move starts while the first one is still animating, so it is dropped rather than
        // applied on top of the half-applied state of the first one.
        await component.InvokeAsync(() => Task.WhenAll(carousel.GoNext(), carousel.GoNext()));

        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));

        Assert.AreEqual(1, carousel.CurrentPage);
    }

    [TestMethod]
    public async Task BitCarouselShouldClampStateWhenItemsRemoved()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 50d);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var carousel = component.Instance.Carousel;

        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));

        component.Render(parameters => parameters.Add(p => p.ItemsCount, 1));

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, carousel.ItemsCount);
            Assert.AreEqual(1, carousel.PagesCount);
            Assert.AreEqual(0, carousel.CurrentPage);
        });

        // A single page has nowhere to rotate to, so losing the items also stops the auto play.
        Assert.IsFalse(carousel.IsPlaying);
    }

    [TestMethod]
    public async Task BitCarouselShouldKeepWrappedPageAcrossRefreshWithInfiniteScrolling()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 5);
            parameters.Add(p => p.VisibleItemsCount, 2);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.Carousel.PagesCount));

        var carousel = component.Instance.Carousel;

        // The last page holds the last item and the first one it borrowed from the other end.
        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-crsi");
            Assert.IsNull(items[4].GetAttribute("aria-hidden"));
            Assert.IsNull(items[0].GetAttribute("aria-hidden"));
        });

        await component.InvokeAsync(carousel.Refresh);

        // A re-layout keeps the borrowed slide on the page instead of cutting it off.
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, carousel.CurrentPage);

            var items = component.FindAll(".bit-crsi");
            Assert.IsNull(items[4].GetAttribute("aria-hidden"));
            Assert.IsNull(items[0].GetAttribute("aria-hidden"));
            Assert.AreEqual("true", items[2].GetAttribute("aria-hidden"));
        });
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(-100d)]
    [DataRow(double.MaxValue)]
    public void BitCarouselShouldSurviveUnusableAutoPlayInterval(double interval)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, interval);
        });

        var carousel = component.Instance.Carousel;

        // The unusable interval is replaced instead of being handed to the timer (which throws on it),
        // so the auto play still comes up.
        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPlaying), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitCarouselShouldRenderControlsBeforeSlides()
    {
        var component = RenderComponent<BitCarouselTest>();

        // The carousel pattern of the ARIA authoring practices puts the controls before the content
        // they control, so the next/prev buttons have to come before the slides in the DOM.
        var container = component.Find(".bit-csl-cnt");

        Assert.IsTrue(container.Children[0].ClassList.Contains("bit-csl-lbt"));
        Assert.IsTrue(container.Children[1].ClassList.Contains("bit-csl-rbt"));
        Assert.IsTrue(container.Children[2].ClassList.Contains("bit-crsi"));

        Assert.AreEqual("false", container.GetAttribute("aria-atomic"));
    }

    [TestMethod]
    public void BitCarouselShouldNavigateForwardOnWheelDownInRtl()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Wheel, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var container = component.Find(".bit-csl-cnt");
        var carousel = component.Instance.Carousel;

        // Rolling the wheel forward moves forward through the content no matter which way the
        // carousel is laid out, so it goes to the next page in right-to-left too.
        container.Wheel(new Microsoft.AspNetCore.Components.Web.WheelEventArgs { DeltaY = 100 });

        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldNavigateVisuallyOnHorizontalWheelInRtl()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Wheel, true);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var container = component.Find(".bit-csl-cnt");
        var carousel = component.Instance.Carousel;

        // A horizontal scroll is a physical motion along the slides, so in right-to-left scrolling
        // to the right reveals the slides sitting on the right, which are the previous ones.
        container.Wheel(new Microsoft.AspNetCore.Components.Web.WheelEventArgs { DeltaX = 100 });

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));
    }

    [TestMethod]
    public void BitCarouselShouldIgnoreWheelWhenDisabled()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Wheel, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        var container = component.Find(".bit-csl-cnt");

        container.Wheel(new Microsoft.AspNetCore.Components.Web.WheelEventArgs { DeltaY = 100 });

        Assert.AreEqual(0, component.Instance.Carousel.CurrentPage);
    }

    [TestMethod]
    public void BitCarouselShouldPauseAutoPlayOnHoverAndResumeOnLeave()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPlaying));

        root.MouseEnter(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        component.WaitForAssertion(() => Assert.IsFalse(carousel.IsPlaying));

        root.MouseLeave(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPlaying));
    }

    [TestMethod]
    public void BitCarouselShouldNotPauseAutoPlayOnHoverWhenPauseOnHoverDisabled()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.PauseOnHover, false);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPlaying));

        root.MouseEnter(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.IsTrue(carousel.IsPlaying);
    }

    [TestMethod]
    public void BitCarouselShouldPauseAutoPlayOnFocusAndResumeOnFocusOut()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        var root = component.Find(".bit-csl");
        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPlaying));

        root.FocusIn(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());

        component.WaitForAssertion(() => Assert.IsFalse(carousel.IsPlaying));

        root.FocusOut(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());

        component.WaitForAssertion(() => Assert.IsTrue(carousel.IsPlaying));
    }

    [TestMethod]
    public void BitCarouselShouldForceSinglePageItemsWithFade()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
            parameters.Add(p => p.ItemsCount, 4);
            parameters.Add(p => p.VisibleItemsCount, 3);
        });

        // A fading carousel shows exactly one slide at a time, so the visible count collapses to 1
        // and every item becomes a page of its own.
        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.Carousel.PagesCount));
    }

    [TestMethod]
    public async Task BitCarouselShouldCrossFadeSlidesWithFade()
    {
        Context.JSInterop.Setup<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", _ => true)
            .SetResult(new BoundingClientRect { Width = 900, Height = 300 });

        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Fade, true);
        });

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-crsi");
            StringAssert.Contains(items[0].GetAttribute("style"), "opacity:1");
            StringAssert.Contains(items[1].GetAttribute("style"), "opacity:0");
        });

        await component.InvokeAsync(component.Instance.Carousel.GoNext);

        // The slides cross-fade in place: the incoming one is stacked on top and faded in while the
        // outgoing one is faded out, with no transform involved.
        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-crsi");
            StringAssert.Contains(items[1].GetAttribute("style"), "opacity:1");
            StringAssert.Contains(items[1].GetAttribute("style"), "transition:opacity");
            StringAssert.Contains(items[0].GetAttribute("style"), "opacity:0");
            Assert.IsFalse(items[1].GetAttribute("style")!.Contains("translate"));
        });
    }

    [TestMethod]
    [DataRow(700d, 2)]
    [DataRow(1000d, 3)]
    public void BitCarouselShouldResolveResponsiveVisibleItemsCount(double width, int expectedVisible)
    {
        Context.JSInterop.Setup<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", _ => true)
            .SetResult(new BoundingClientRect { Width = width, Height = 300 });

        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 6);
            parameters.Add(p => p.VisibleItemsCountSm, 2);
            parameters.Add(p => p.VisibleItemsCountMd, 3);
        });

        // The breakpoints go by the width of the carousel itself: from 600px up the Sm value
        // applies, and from 960px up the Md value overrides it.
        component.WaitForAssertion(() => Assert.AreEqual(6 / expectedVisible, component.Instance.Carousel.PagesCount));
    }

    [TestMethod]
    public void BitCarouselShouldFallBackToBaseVisibleItemsCountBelowBreakpoints()
    {
        Context.JSInterop.Setup<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", _ => true)
            .SetResult(new BoundingClientRect { Width = 500, Height = 300 });

        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 6);
            parameters.Add(p => p.VisibleItemsCountSm, 2);
            parameters.Add(p => p.VisibleItemsCountMd, 3);
        });

        // Below the smallest breakpoint that was set, the base VisibleItemsCount applies.
        component.WaitForAssertion(() => Assert.AreEqual(6, component.Instance.Carousel.PagesCount));
    }

    [TestMethod]
    public void BitCarouselShouldRespectAnimationDuration()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AnimationDuration, 1.5);
        });

        var root = component.Find(".bit-csl");

        // The duration is handed to the stylesheet as the -full token, so the reduced-motion
        // media query can still collapse it.
        StringAssert.Contains(root.GetAttribute("style"), "--bit-csl-dur-full:1.5s");
    }

    [TestMethod]
    public void BitCarouselShouldClampNegativeAnimationDurationToZero()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.AnimationDuration, -3d);
        });

        var root = component.Find(".bit-csl");

        StringAssert.Contains(root.GetAttribute("style"), "--bit-csl-dur-full:0s");
    }

    [TestMethod]
    public void BitCarouselShouldRespectCustomIconNames()
    {
        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.Add(p => p.GoLeftIconName, "Add");
            parameters.Add(p => p.GoRightIconName, "Remove");
        });

        var leftIcon = component.Find(".bit-csl-lbt i");
        var rightIcon = component.Find(".bit-csl-rbt i");

        Assert.IsTrue(leftIcon.ClassList.Contains("bit-icon--Add"));
        Assert.IsTrue(rightIcon.ClassList.Contains("bit-icon--Remove"));
    }

    [TestMethod]
    public void BitCarouselShouldRenderVerticalDefaultIcons()
    {
        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        // The "left" (next) button moves to the bottom of a vertical carousel and the "right"
        // (prev) one to the top, so their default chevrons point down and up.
        var leftIcon = component.Find(".bit-csl-lbt i");
        var rightIcon = component.Find(".bit-csl-rbt i");

        Assert.IsTrue(leftIcon.ClassList.Contains("bit-icon--ChevronDown"));
        Assert.IsTrue(rightIcon.ClassList.Contains("bit-icon--ChevronUp"));
    }

    [TestMethod]
    public async Task BitCarouselShouldAlignLastPageToLastItemOnGoTo()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 5);
            parameters.Add(p => p.VisibleItemsCount, 2);
            parameters.Add(p => p.ScrollItemsCount, 2);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.Carousel.PagesCount));

        var carousel = component.Instance.Carousel;

        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));

        await component.InvokeAsync(() => carousel.GoTo(1));

        component.WaitForAssertion(() => Assert.AreEqual(0, carousel.CurrentPage));
    }
}
