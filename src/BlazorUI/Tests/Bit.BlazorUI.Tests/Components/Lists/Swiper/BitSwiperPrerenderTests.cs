using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Swiper;

/// <summary>
/// What a swiper looks like before the page is interactive - prerendered, or statically rendered for good - where
/// the script that measures it never runs: the region, its slides, its buttons and the sizing a VisibleItemsCount
/// writes are laid out all the same, while the dots, which count the pages the measurement reports, wait for it.
/// </summary>
[TestClass]
public class BitSwiperPrerenderTests
{
    [TestMethod]
    public async Task BitSwiperShouldPrerenderItsRegionAndEverySlide()
    {
        var html = await RenderAsync(itemsCount: 3, []);

        Assert.IsTrue(Regex.IsMatch(html, @"role=""region""[^>]*aria-roledescription=""carousel"""));
        Assert.HasCount(3, Regex.Matches(html, @"aria-roledescription=""slide"""));
        Assert.Contains("slide-0", html);
        Assert.Contains("slide-2", html);

        // The two buttons need no measurement to be there, and the keyboard reaches the root from the first paint.
        Assert.Contains("bit-swp-lbt", html);
        Assert.Contains("bit-swp-rbt", html);
        Assert.Contains(@"tabindex=""0""", html);
    }

    [TestMethod]
    public async Task BitSwiperShouldPrerenderTheSizingOfItsItems()
    {
        var html = await RenderAsync(itemsCount: 3, new() { [nameof(BitSwiper.VisibleItemsCount)] = 2, [nameof(BitSwiper.Gap)] = "1rem" });

        // The share of the width each item takes is a formula of the parameters, so it needs no measurement.
        Assert.Contains("--bit-swp-isz:calc((100% - 1 * var(--bit-swp-gap, 0px)) / 2)", html);
        Assert.Contains("--bit-swp-gap:1rem", html);
    }

    [TestMethod]
    public async Task BitSwiperShouldNotPrerenderDotsBeforeThePagesAreKnown()
    {
        var html = await RenderAsync(itemsCount: 3, new() { [nameof(BitSwiper.ShowDots)] = true });

        // How many pages there are is what the measurement reports, so there is nothing to draw a dot for yet.
        Assert.DoesNotContain("bit-swp-dot", html);
    }

    [TestMethod]
    public async Task BitSwiperShouldPrerenderThePlayPauseButtonAheadOfTheSlides()
    {
        var html = await RenderAsync(itemsCount: 3, new() { [nameof(BitSwiper.AutoPlay)] = true, [nameof(BitSwiper.ShowPlayPause)] = true });

        var button = html.IndexOf("bit-swp-ppb", System.StringComparison.Ordinal);
        var slide = html.IndexOf("slide-0", System.StringComparison.Ordinal);

        // The motion can be stopped before anything else is dealt with, as the carousel pattern asks.
        Assert.IsGreaterThanOrEqualTo(0, button, "No play/pause button was prerendered.");
        Assert.IsLessThan(slide, button, "The play/pause button is not ahead of the slides.");
    }

    private static Task<string> RenderAsync(int itemsCount, Dictionary<string, object?> parameters)
    {
        parameters[nameof(BitSwiper.ChildContent)] = (RenderFragment)(builder =>
        {
            for (int i = 0; i < itemsCount; i++)
            {
                var index = i;

                builder.OpenComponent<BitSwiperItem>(0);
                builder.SetKey(index);
                builder.AddComponentParameter(1, nameof(BitSwiperItem.ChildContent), (RenderFragment)(b => b.AddContent(0, $"slide-{index}")));
                builder.CloseComponent();
            }
        });

        return Prerenderer.RenderAsync<BitSwiper>(parameters);
    }
}
