using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.InfiniteScrolling;

/// <summary>
/// Covers what a BitInfiniteScrolling sends before the page is interactive (a prerender, or a static SSR page): no
/// OnAfterRender runs, so neither the observer nor the first page of the manual mode can ask for anything.
/// </summary>
[TestClass]
public class BitInfiniteScrollingPrerenderTests : BunitTestContext
{
    private static readonly RenderFragment<int> itemTemplate = item => builder => builder.AddContent(0, $"Item {item};");

    private static BitInfiniteScrollingItemsProvider<int> CountingProvider(List<BitInfiniteScrollingItemsProviderRequest> requests, int total = 100)
    {
        return async request =>
        {
            requests.Add(request);
            await Task.Yield();
            return Enumerable.Range(request.Skip, Math.Clamp(total - request.Skip, 0, request.Count)).ToList();
        };
    }

    private static int Count(string html, string text) => Regex.Matches(html, Regex.Escape(text)).Count;

    [TestMethod]
    public async Task BitInfiniteScrollingShouldPrerenderTheLoadingStateWithoutPreload()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var html = await Prerenderer.RenderAsync<BitInfiniteScrolling<int>>(new Dictionary<string, object?>
        {
            [nameof(BitInfiniteScrolling<int>.ItemsProvider)] = CountingProvider(requests),
            [nameof(BitInfiniteScrolling<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitInfiniteScrolling<int>.PageSize)] = 10,
        });

        // The provider is the app's data source: without Preload it is not called before the page is interactive.
        Assert.AreEqual(0, requests.Count);

        // The first page is what the list does first once it is interactive, so it is shown as on its way rather
        // than as a blank box.
        StringAssert.Contains(html, "bit-isc-ldg");
        StringAssert.Contains(html, "Loading...");
        Assert.IsFalse(html.Contains("bit-isc-emp"));
        Assert.IsFalse(html.Contains("bit-isc-btn"));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldPrerenderTheLoadingStateOfTheManualMode()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var html = await Prerenderer.RenderAsync<BitInfiniteScrolling<int>>(new Dictionary<string, object?>
        {
            [nameof(BitInfiniteScrolling<int>.ItemsProvider)] = CountingProvider(requests),
            [nameof(BitInfiniteScrolling<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitInfiniteScrolling<int>.PageSize)] = 10,
            [nameof(BitInfiniteScrolling<int>.Manual)] = true,
            [nameof(BitInfiniteScrolling<int>.LoadingTemplate)] = (RenderFragment)(b => b.AddContent(0, "Fetching the feed;")),
        });

        // The manual mode fetches its first page by itself too, so the Load more button is not what it shows first.
        Assert.AreEqual(0, requests.Count);
        StringAssert.Contains(html, "Fetching the feed;");
        Assert.IsFalse(html.Contains("bit-isc-btn"));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldPrerenderNothingToLoadForADisabledList()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var html = await Prerenderer.RenderAsync<BitInfiniteScrolling<int>>(new Dictionary<string, object?>
        {
            [nameof(BitInfiniteScrolling<int>.ItemsProvider)] = CountingProvider(requests),
            [nameof(BitInfiniteScrolling<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitInfiniteScrolling<int>.Disabled)] = true,
        });

        // A disabled list loads nothing, interactive or not, so it is not shown as loading either.
        Assert.IsFalse(html.Contains("bit-isc-ldg"));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldPrerenderThePreloadedItems()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var html = await Prerenderer.RenderAsync<BitInfiniteScrolling<int>>(new Dictionary<string, object?>
        {
            [nameof(BitInfiniteScrolling<int>.ItemsProvider)] = CountingProvider(requests),
            [nameof(BitInfiniteScrolling<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitInfiniteScrolling<int>.PageSize)] = 10,
            [nameof(BitInfiniteScrolling<int>.Preload)] = true,
        });

        // Preload is the opt-in for fetching the first page on the server, so its items are in the HTML.
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual(1, Count(html, "Item 0;"));
        Assert.AreEqual(1, Count(html, "Item 9;"));
        Assert.IsFalse(html.Contains("Item 10;"));
        Assert.IsFalse(html.Contains("bit-isc-ldg"));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldPrerenderThePreloadedItemsOfTheManualMode()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var html = await Prerenderer.RenderAsync<BitInfiniteScrolling<int>>(new Dictionary<string, object?>
        {
            [nameof(BitInfiniteScrolling<int>.ItemsProvider)] = CountingProvider(requests),
            [nameof(BitInfiniteScrolling<int>.ItemTemplate)] = itemTemplate,
            [nameof(BitInfiniteScrolling<int>.PageSize)] = 10,
            [nameof(BitInfiniteScrolling<int>.Preload)] = true,
            [nameof(BitInfiniteScrolling<int>.Manual)] = true,
        });

        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual(1, Count(html, "Item 0;"));
        // The first page is in, so the button for the next one is too.
        StringAssert.Contains(html, "bit-isc-btn");
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotShowTheLoadingStateBeforeItsObserverFiresWhenNotPrerendered()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        // An interactive list whose sentinel is not in view yet (a list below the fold): its observer has not asked
        // for anything, so there is nothing to show as loading.
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, CountingProvider(requests));
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.PageSize, 10);
        });

        Assert.AreEqual(0, requests.Count);
        Assert.AreEqual(0, component.FindAll(".bit-isc-ldg").Count);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotLoadThePreloadedPageAgainOnceInteractive()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();
        var pending = new TaskCompletionSource();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, async request =>
            {
                requests.Add(request);
                await pending.Task;
                return Enumerable.Range(request.Skip, request.Count).ToList();
            });
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Manual, true);
        });

        // The preload is still running when the first render's own first page would be fetched, and the observer may
        // fire meanwhile too: neither asks for it a second time.
        _ = component.InvokeAsync(() => component.Instance._LoadMoreItems());
        Assert.AreEqual(1, requests.Count);

        component.InvokeAsync(pending.SetResult);

        component.WaitForAssertion(() => Assert.AreEqual(10, component.Instance.Items.Count));
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual(1, Count(component.Markup, "Item 0;"));
    }
}
