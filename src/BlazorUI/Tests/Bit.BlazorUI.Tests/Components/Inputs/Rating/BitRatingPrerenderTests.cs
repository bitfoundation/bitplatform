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

        Assert.AreEqual(5, Regex.Matches(html, "class=\"bit-rtg-btn").Count);
        StringAssert.Contains(html, "role=\"radiogroup\"");
        StringAssert.Contains(html, "Quality");
        // The fourth item is half filled by the 3.5.
        StringAssert.Contains(html, "width:50%");
        StringAssert.Contains(html, "value=\"3.5\"");
        StringAssert.Contains(html, "autofocus");
    }

    [TestMethod]
    public async Task BitRatingShouldPrerenderAReadOnlyRatingAsAnImage()
    {
        var html = await Prerenderer.RenderAsync<BitRating>(new Dictionary<string, object?>
        {
            [nameof(BitRating.ReadOnly)] = true,
            [nameof(BitRating.DefaultValue)] = 4.0,
        });

        StringAssert.Contains(html, "role=\"img\"");
        Assert.IsFalse(html.Contains("role=\"radio\""));
    }
}
