using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Virtualize;

/// <summary>
/// Covers what a BitVirtualize sends before the page is interactive (a prerender, or a static SSR page): no
/// OnAfterRender runs and the browser side is never set up, so the list renders without a measured viewport.
/// </summary>
[TestClass]
public class BitVirtualizePrerenderTests : BunitTestContext
{
    private static readonly RenderFragment<int> itemTemplate = item => builder => builder.AddContent(0, $"Item {item};");

    private static readonly RenderFragment<BitVirtualizePlaceholderContext> placeholderTemplate =
        ctx => builder => builder.AddContent(0, $"Placeholder {ctx.Index} ({ctx.Size});");

    private static int[] IndicesIn(string html) =>
        Regex.Matches(html, "data-bit-vir-index=\"(\\d+)\"").Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();

    [TestMethod]
    public async Task BitVirtualizeShouldPrerenderTheFirstWindowOfItems()
    {
        var html = await Prerenderer.RenderAsync<BitVirtualize<int>>(new Dictionary<string, object?>
        {
            [nameof(BitVirtualize<int>.Items)] = Enumerable.Range(0, 1_000).ToArray(),
            [nameof(BitVirtualize<int>.ItemSize)] = 50f,
            [nameof(BitVirtualize<int>.ItemTemplate)] = itemTemplate,
        });

        // No viewport is measured yet, so a 600px screen of items is assumed, plus the overscan after it.
        StringAssert.Contains(html, "Item 0;");
        StringAssert.Contains(html, "Item 18;");
        Assert.IsFalse(html.Contains("Item 19;"));
        // The spacer already holds the whole list, so the page has its final height.
        StringAssert.Contains(html, "height:50000px");
    }

    [TestMethod]
    public async Task BitVirtualizeShouldPrerenderTheStickyHeaderOfTheFirstGroup()
    {
        var html = await Prerenderer.RenderAsync<BitVirtualize<int>>(new Dictionary<string, object?>
        {
            [nameof(BitVirtualize<int>.Items)] = Enumerable.Range(0, 100).ToArray(),
            [nameof(BitVirtualize<int>.ItemSize)] = 50f,
            [nameof(BitVirtualize<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitVirtualize<int>.IsStickyItem)] = (Func<int, bool>)(i => i % 10 == 0),
        });

        // The measured list pins the header of the group at its top, so the first window does as well.
        StringAssert.Contains(html, "bit-vir-stk");
        // The next header is where the pinned one is pushed out from.
        StringAssert.Contains(html, "data-bit-vir-sticky-next=\"500\"");
    }

    [TestMethod]
    public async Task BitVirtualizeShouldPrerenderNoStickyHeaderWhenTheFirstItemStartsNoGroup()
    {
        var html = await Prerenderer.RenderAsync<BitVirtualize<int>>(new Dictionary<string, object?>
        {
            [nameof(BitVirtualize<int>.Items)] = Enumerable.Range(1, 100).ToArray(),
            [nameof(BitVirtualize<int>.ItemSize)] = 50f,
            [nameof(BitVirtualize<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitVirtualize<int>.IsStickyItem)] = (Func<int, bool>)(i => i % 10 == 0),
        });

        Assert.IsFalse(html.Contains("bit-vir-stk"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldPrerenderPlaceholdersForTheFirstWindowOfAProvider()
    {
        var requests = 0;
        BitVirtualizeItemsProvider<int> provider = request =>
        {
            requests++;
            return ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>(Enumerable.Range(request.StartIndex, request.Count).ToList(), 1_000));
        };

        var html = await Prerenderer.RenderAsync<BitVirtualize<int>>(new Dictionary<string, object?>
        {
            [nameof(BitVirtualize<int>.ItemsProvider)] = provider,
            [nameof(BitVirtualize<int>.ItemSize)] = 50f,
            [nameof(BitVirtualize<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitVirtualize<int>.PlaceholderTemplate)] = placeholderTemplate,
        });

        // The provider is the app's data source: it is never called on the server for a page that is not interactive yet.
        Assert.AreEqual(0, requests);

        // The window the provider is first asked for is laid out as placeholders, keeping its room on the page.
        CollectionAssert.AreEqual(Enumerable.Range(0, 19).ToArray(), IndicesIn(html));
        StringAssert.Contains(html, "Placeholder 0 (50);");
        StringAssert.Contains(html, "Placeholder 18 (50);");
        StringAssert.Contains(html, "height:950px");
        StringAssert.Contains(html, "transform:translateY(900px)");
        // The size of the list is not known yet.
        StringAssert.Contains(html, "aria-setsize=\"-1\"");
        Assert.IsFalse(html.Contains("Item 0;"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldPrerenderEstimatedPlaceholdersForADynamicProvider()
    {
        BitVirtualizeItemsProvider<int> provider = _ => throw new AssertFailedException("the provider must not be called");

        var html = await Prerenderer.RenderAsync<BitVirtualize<int>>(new Dictionary<string, object?>
        {
            [nameof(BitVirtualize<int>.ItemsProvider)] = provider,
            [nameof(BitVirtualize<int>.Dynamic)] = true,
            [nameof(BitVirtualize<int>.EstimatedItemSize)] = 100f,
            [nameof(BitVirtualize<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitVirtualize<int>.PlaceholderTemplate)] = placeholderTemplate,
        });

        // 600px of 100px items, plus the overscan.
        CollectionAssert.AreEqual(Enumerable.Range(0, 13).ToArray(), IndicesIn(html));
        StringAssert.Contains(html, "Placeholder 12 (100);");
        StringAssert.Contains(html, "transform:translateY(1200px)");
        StringAssert.Contains(html, "height:1300px");
    }

    [TestMethod]
    public async Task BitVirtualizeShouldPrerenderTheLoadingTemplateOfAProvider()
    {
        BitVirtualizeItemsProvider<int> provider = _ => throw new AssertFailedException("the provider must not be called");

        var html = await Prerenderer.RenderAsync<BitVirtualize<int>>(new Dictionary<string, object?>
        {
            [nameof(BitVirtualize<int>.ItemsProvider)] = provider,
            [nameof(BitVirtualize<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitVirtualize<int>.PlaceholderTemplate)] = placeholderTemplate,
            [nameof(BitVirtualize<int>.LoadingTemplate)] = (RenderFragment)(b => b.AddContent(0, "Loading the list;")),
        });

        // The LoadingTemplate is what the app chose to show until the first load, so it still takes precedence.
        StringAssert.Contains(html, "Loading the list;");
        Assert.IsFalse(html.Contains("Placeholder"));
    }

    [TestMethod]
    public void BitVirtualizeShouldReplaceThePrerenderedPlaceholdersInPlace()
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = 300 });

        var pending = new TaskCompletionSource<BitVirtualizeItemsProviderResult<int>>();
        var requests = new List<BitVirtualizeItemsProviderRequest>();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, request =>
            {
                requests.Add(request);
                return new ValueTask<BitVirtualizeItemsProviderResult<int>>(pending.Task);
            });
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.PlaceholderTemplate, placeholderTemplate);
        });

        // While the first window is on its way, the placeholders stand where its items are going to land.
        Assert.AreEqual(1, requests.Count);
        var placeholders = component.FindAll(".bit-vir-itm");
        Assert.AreEqual(requests[0].Count, placeholders.Count);
        Assert.AreEqual("Placeholder 0 (50);", placeholders[0].TextContent);
        Assert.AreEqual("true", placeholders[0].GetAttribute("aria-busy"));

        var first = component.Find("[data-bit-vir-index='1']");

        component.InvokeAsync(() => pending.SetResult(new BitVirtualizeItemsProviderResult<int>(Enumerable.Range(0, requests[0].Count).ToList(), 1_000)));

        // The same elements now hold the items: nothing is removed and inserted again.
        var item = component.Find("[data-bit-vir-index='1']");
        Assert.AreEqual("Item 1;", item.TextContent);
        Assert.IsFalse(item.HasAttribute("aria-busy"));
        Assert.AreEqual("1000", item.GetAttribute("aria-setsize"));
        Assert.AreEqual(first.GetAttribute("style"), item.GetAttribute("style"));
        Assert.AreEqual("height:50000px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldNotRenderPlaceholdersForAnEmptyProvider()
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = 300 });

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>([], 0)));
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.PlaceholderTemplate, placeholderTemplate);
            parameters.Add(p => p.EmptyTemplate, b => b.AddContent(0, "Empty"));
        });

        Assert.AreEqual(0, component.FindAll(".bit-vir-itm").Count);
        Assert.AreEqual("Empty", component.Find(".bit-vir-emp").TextContent);
    }
}
