using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

/// <summary>
/// What a rating sends before the page is interactive (a prerender, or a static SSR page): no OnAfterRender runs, so
/// the javascript side is never set up, and the items still have to be there with the value they were given.
/// </summary>
[TestClass]
public class BitRatingPrerenderTests
{
    [TestMethod]
    public async Task BitRatingShouldPrerenderItsItemsWithoutTheBrowser()
    {
        var html = await Prerenderer.RenderAsync<BitRating>(new Dictionary<string, object?>
        {
            [nameof(BitRating.Label)] = "Quality",
            [nameof(BitRating.Precision)] = 0.5,
            [nameof(BitRating.DefaultValue)] = 3.5,
            [nameof(BitRating.AutoFocus)] = true,
        });

        Assert.HasCount(5, Regex.Matches(html, "class=\"bit-rtg-btn"));
        Assert.Contains("role=\"radiogroup\"", html);
        Assert.Contains("Quality", html);
        // The fourth item is half filled by the 3.5.
        Assert.Contains("width:50%", html);
        Assert.Contains("value=\"3.5\"", html);
        Assert.Contains("autofocus", html);
    }

    [TestMethod]
    public async Task BitRatingShouldPrerenderAReadOnlyRatingAsAnImage()
    {
        var html = await Prerenderer.RenderAsync<BitRating>(new Dictionary<string, object?>
        {
            [nameof(BitRating.ReadOnly)] = true,
            [nameof(BitRating.DefaultValue)] = 4.0,
        });

        Assert.Contains("role=\"img\"", html);
        Assert.DoesNotContain("role=\"radio\"", html);
    }
}
