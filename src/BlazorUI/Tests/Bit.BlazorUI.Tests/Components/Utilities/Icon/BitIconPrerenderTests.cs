using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

/// <summary>
/// What an icon looks like before the page is interactive - prerendered, or statically rendered for good. The one
/// interop call the icon makes (the Space key it stops from scrolling the page under an interactive icon) is made
/// after the render, which a prerender never runs, so an icon with a handler prerenders like any other rather than
/// failing on a JS runtime it cannot reach.
/// </summary>
[TestClass]
public class BitIconPrerenderTests
{
    [TestMethod]
    public async Task BitIconShouldPrerenderADecorativeIconHidden()
    {
        var html = await Prerenderer.RenderAsync<BitIcon>(new Dictionary<string, object?>
        {
            [nameof(BitIcon.IconName)] = "Add",
        });

        Assert.Contains("bit-icon--Add", html);
        Assert.Contains(@"aria-hidden=""true""", html);
        Assert.DoesNotContain("role=", html);
    }

    [TestMethod]
    public async Task BitIconShouldPrerenderANamedIconAsAPicture()
    {
        var html = await Prerenderer.RenderAsync<BitIcon>(new Dictionary<string, object?>
        {
            [nameof(BitIcon.IconName)] = "Add",
            [nameof(BitIcon.AriaLabel)] = "Add a row",
        });

        Assert.Contains(@"role=""img""", html);
        Assert.Contains(@"aria-label=""Add a row""", html);
        Assert.DoesNotContain("aria-hidden", html);
    }

    [TestMethod]
    public async Task BitIconShouldPrerenderAnInteractiveIconAsAButtonWithoutReachingForTheBrowser()
    {
        var html = await Prerenderer.RenderAsync<BitIcon>(new Dictionary<string, object?>
        {
            [nameof(BitIcon.IconName)] = "Delete",
            [nameof(BitIcon.OnClick)] = EventCallback.Factory.Create<MouseEventArgs>(this, () => { }),
        });

        // The tab stop, the role and the fallback name are all in the markup; the key suppression waits for the browser.
        Assert.Contains(@"role=""button""", html);
        Assert.Contains(@"tabindex=""0""", html);
        Assert.Contains(@"aria-label=""Delete""", html);
        Assert.Contains("bit-ico-int", html);
    }

    [TestMethod]
    public async Task BitIconShouldPrerenderALigatureMarkedAsNotToTranslate()
    {
        var html = await Prerenderer.RenderAsync<BitIcon>(new Dictionary<string, object?>
        {
            [nameof(BitIcon.Icon)] = BitIconInfo.Ms("home"),
        });

        Assert.Contains(@"translate=""no""", html);
        Assert.Contains(">home</i>", html);
    }
}
