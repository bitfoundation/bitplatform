using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

/// <summary>
/// What a breadcrumb sends before the page is interactive: the after-render pass, where it registers the keys of its
/// menu, its resize observer and its scrolling with the browser, never runs on a prerender (or on a static SSR page at
/// all), so the trail, its split into the displayed and the collapsed steps and the structured data have to come out
/// of the render itself, with a JS runtime that throws on every call.
/// </summary>
[TestClass]
public class BitBreadcrumbPrerenderTests
{
    [TestMethod]
    public async Task BitBreadcrumbShouldRenderTheTrailAndItsCurrentPageBeforeInteractivity()
    {
        var document = await Prerender(new());

        var trail = document.QuerySelector("nav.bit-brc");

        Assert.IsNotNull(trail);
        Assert.AreEqual("Breadcrumb", trail.GetAttribute("aria-label"));

        var links = document.QuerySelectorAll(".bit-brc-icn a.bit-brc-itm");

        Assert.AreEqual(4, links.Length);
        Assert.AreEqual("page", links[3].GetAttribute("aria-current"));
        Assert.AreEqual(0, document.QuerySelectorAll(".bit-brc-obt").Length);
    }

    [TestMethod]
    public async Task BitBreadcrumbShouldCollapseTheStepsItIsToldToBeforeInteractivity()
    {
        var document = await Prerender(new()
        {
            [nameof(BitBreadcrumb<BitBreadcrumbItem>.MaxDisplayedItems)] = (uint)2,
            [nameof(BitBreadcrumb<BitBreadcrumbItem>.OverflowIndex)] = (uint)1,
        });

        // A fixed cap needs no measurement: the root and the current page stay, the middle goes into the menu.
        var button = document.QuerySelector(".bit-brc-obt");

        Assert.IsNotNull(button);
        Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
        Assert.AreEqual(2, document.QuerySelectorAll(".bit-brc-icn a.bit-brc-itm").Length);
        Assert.AreEqual(2, document.QuerySelectorAll(".bit-brc-cal .bit-brc-ofi").Length);
    }

    [TestMethod]
    public async Task BitBreadcrumbShouldWriteItsStructuredDataBeforeInteractivity()
    {
        var document = await Prerender(new() { [nameof(BitBreadcrumb<BitBreadcrumbItem>.StructuredData)] = true });

        var script = document.QuerySelector("script[type='application/ld+json']");

        Assert.IsNotNull(script);
        StringAssert.Contains(script.TextContent, "\"BreadcrumbList\"");
        StringAssert.Contains(script.TextContent, "\"Folder 4\"");
    }

    private static async Task<IDocument> Prerender(Dictionary<string, object?> parameters)
    {
        parameters[nameof(BitBreadcrumb<BitBreadcrumbItem>.Items)] = new List<BitBreadcrumbItem>
        {
            new() { Text = "Folder 1", Href = "/folder-1" },
            new() { Text = "Folder 2", Href = "/folder-2" },
            new() { Text = "Folder 3", Href = "/folder-3" },
            new() { Text = "Folder 4", Href = "/folder-4", IsSelected = true },
        };

        var html = await Prerenderer.RenderAsync<BitBreadcrumb<BitBreadcrumbItem>>(parameters);

        return new HtmlParser().ParseDocument(html);
    }
}
