using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Map;

/// <summary>
/// The map is created by the provider's script once the page is interactive, so what a prerender (or a static SSR
/// page) can show is the loading state the map is about to go through, rather than an empty box.
/// </summary>
[TestClass]
public class BitMapPrerenderTests
{
    [TestMethod]
    public async Task BitMapShouldPrerenderItsLoadingState()
    {
        var html = await Prerenderer.RenderAsync<BitMap<BitLeafletMapProvider>>();

        var document = new HtmlParser().ParseDocument(html);

        var status = document.QuerySelector(".bit-map-status");
        Assert.IsNotNull(status);
        Assert.AreEqual("status", status.GetAttribute("role"));
        StringAssert.Contains(status.TextContent, "Loading map");
        Assert.AreEqual("true", document.QuerySelector(".bit-map-canvas")!.GetAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task BitMapShouldPrerenderItsLoadingTemplate()
    {
        var html = await Prerenderer.RenderAsync<BitMap<BitLeafletMapProvider>>(new Dictionary<string, object?>
        {
            [nameof(BitMap<BitLeafletMapProvider>.LoadingTemplate)] = (RenderFragment)(b => b.AddMarkupContent(0, "<b class=\"own-loader\">Wait</b>")),
        });

        var document = new HtmlParser().ParseDocument(html);

        Assert.IsNotNull(document.QuerySelector(".bit-map-status .own-loader"));
    }

    [TestMethod]
    public async Task BitMapShouldNotPrerenderALoadingStateItWasToldNotToShow()
    {
        var html = await Prerenderer.RenderAsync<BitMap<BitLeafletMapProvider>>(new Dictionary<string, object?>
        {
            [nameof(BitMap<BitLeafletMapProvider>.ShowLoading)] = false,
        });

        var document = new HtmlParser().ParseDocument(html);

        Assert.IsNull(document.QuerySelector(".bit-map-status"));
    }

    [TestMethod]
    public async Task BitMapShouldNotPrerenderALoadingStateForALazyMap()
    {
        // A lazy map stays idle until it scrolls into view, possibly for good: announcing a load all that time would
        // tell assistive technology about one that is not happening.
        var html = await Prerenderer.RenderAsync<BitMap<BitLeafletMapProvider>>(new Dictionary<string, object?>
        {
            [nameof(BitMap<BitLeafletMapProvider>.LazyLoad)] = true,
        });

        var document = new HtmlParser().ParseDocument(html);

        Assert.IsNull(document.QuerySelector(".bit-map-status"));
        Assert.IsNull(document.QuerySelector(".bit-map-canvas")!.GetAttribute("aria-busy"));
    }
}
