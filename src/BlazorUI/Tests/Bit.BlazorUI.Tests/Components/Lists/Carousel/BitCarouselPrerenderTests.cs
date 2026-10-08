using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Carousel;

/// <summary>
/// What a carousel looks like before the page is interactive - prerendered, or statically rendered for good -
/// where it is never measured. Everything that does not take a measurement (the pages, the dots, which slides
/// are on screen and where they sit) is laid out all the same.
/// </summary>
[TestClass]
public class BitCarouselPrerenderTests
{
    [TestMethod]
    public async Task BitCarouselShouldPrerenderItsDots()
    {
        var html = await RenderAsync(itemsCount: 5, new() { [nameof(BitCarousel.VisibleItemsCount)] = 3 });

        // Five slides three at a time, moved one at a time, are three steps.
        Assert.AreEqual(3, Regex.Matches(html, @"class=""bit-csl-dot").Count);
        StringAssert.Contains(html, "bit-csl-cud");
    }

    [TestMethod]
    public async Task BitCarouselShouldPrerenderEveryVisibleSlide()
    {
        var html = await RenderAsync(itemsCount: 5, new() { [nameof(BitCarousel.VisibleItemsCount)] = 3 });

        var slides = Slides(html);

        Assert.HasCount(5, slides);

        for (int i = 0; i < 5; i++)
        {
            var onScreen = i < 3;

            Assert.AreEqual(onScreen is false, slides[i].Contains(" inert"), $"slide {i}");
            Assert.AreEqual(onScreen is false, slides[i].Contains(@"aria-hidden=""true"""), $"slide {i}");

            // Each slide takes its share of the container and sits at its own place in the run, in percentages,
            // since there is no measurement to take pixels from.
            StringAssert.Contains(slides[i], "width:calc(100% / 3)");
            StringAssert.Contains(slides[i], $"translateX({i * 100}%)");
        }
    }

    [TestMethod]
    public async Task BitCarouselShouldPrerenderItsDefaultPage()
    {
        var html = await RenderAsync(itemsCount: 4, new() { [nameof(BitCarousel.DefaultPage)] = 3 });

        var slides = Slides(html);

        Assert.IsTrue(slides[0].Contains(" inert"));
        Assert.IsFalse(slides[2].Contains(" inert"));
        StringAssert.Contains(slides[2], "translateX(0%)");
        StringAssert.Contains(slides[0], "translateX(-200%)");

        Assert.IsTrue(Regex.IsMatch(html, @"aria-label=""Slide 3""[^>]*aria-current=""true"""));
    }

    [TestMethod]
    public async Task BitCarouselShouldNotPrerenderABackButtonOnItsFirstPage()
    {
        var html = await RenderAsync(itemsCount: 3, []);

        // The button that goes back is the one on the left of a left-to-right carousel.
        Assert.IsTrue(Regex.IsMatch(html, @"class=""bit-csl-rbt[^""]*""\s+style=""display:none"));
        Assert.IsFalse(Regex.IsMatch(html, @"class=""bit-csl-lbt[^""]*""\s+style=""display:none"));
    }



    private static Task<string> RenderAsync(int itemsCount, Dictionary<string, object?> parameters)
    {
        parameters[nameof(BitCarousel.ChildContent)] = (RenderFragment)(builder =>
        {
            for (int i = 0; i < itemsCount; i++)
            {
                var index = i;

                builder.OpenComponent<BitCarouselItem>(0);
                builder.SetKey(index);
                builder.AddComponentParameter(1, nameof(BitCarouselItem.ChildContent), (RenderFragment)(b => b.AddContent(0, $"slide-{index}")));
                builder.CloseComponent();
            }
        });

        return Prerenderer.RenderAsync<BitCarousel>(parameters);
    }

    // The opening tag of every slide, in order.
    private static List<string> Slides(string html)
    {
        return Regex.Matches(html, @"<div role=""group""\s+aria-roledescription=""slide""[^>]*>")
                    .Select(m => m.Value)
                    .ToList();
    }
}
