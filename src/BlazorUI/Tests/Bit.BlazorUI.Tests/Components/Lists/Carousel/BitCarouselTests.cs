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
            var dots = component.FindAll("button.bit-csl-dot");
            Assert.AreEqual(3, dots.Count);
            Assert.AreEqual("Slide 1", dots[0].GetAttribute("aria-label"));
            Assert.AreEqual("true", dots[0].GetAttribute("aria-disabled"));
            Assert.IsNull(dots[1].GetAttribute("aria-disabled"));
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
    public async Task BitCarouselShouldRespectDefaultPage()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.DefaultPage, 2);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.Carousel.CurrentPage));

        await Task.CompletedTask;
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
}
