using Bit.BlazorUI.Tests.Components.Lists.BasicList;
using Bunit;
using Microsoft.AspNetCore.Components.Web.Virtualization;
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

        // next/prev buttons should exist by default
        var leftBtn = component.Find(".bit-csl-lbt");
        var rightBtn = component.Find(".bit-csl-rbt");
        Assert.IsNotNull(leftBtn);
        Assert.IsNotNull(rightBtn);

        // dots container exists (may be empty before dimensions are computed)
        var dotsContainer = component.FindAll(".bit-csl-dcn");
        Assert.AreEqual(1, dotsContainer.Count);
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
}
