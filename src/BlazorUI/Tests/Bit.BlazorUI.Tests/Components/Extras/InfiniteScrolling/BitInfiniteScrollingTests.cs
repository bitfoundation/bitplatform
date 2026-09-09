using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.InfiniteScrolling;

[TestClass]
public class BitInfiniteScrollingTests : BunitTestContext
{
    private static ValueTask<IEnumerable<int>> Provider(BitInfiniteScrollingItemsProviderRequest request)
    {
        var items = new List<int>();

        for (var i = request.Skip; i < request.Skip + 3; i++)
        {
            items.Add(i);
        }

        return ValueTask.FromResult<IEnumerable<int>>(items);
    }

    private static RenderFragment<int> ItemTemplate() => item => builder => builder.AddContent(0, $"Item {item}");

    // A provider over a fixed-size source that honors the requested page size, which is what the
    // component needs to detect the end of the data without an extra empty round trip.
    private static BitInfiniteScrollingItemsProvider<int> PagedProvider(int total, Action<BitInfiniteScrollingItemsProviderRequest>? onRequest = null)
    {
        return request =>
        {
            onRequest?.Invoke(request);

            var count = Math.Clamp(total - request.Skip, 0, request.Count);

            return ValueTask.FromResult<IEnumerable<int>>(Enumerable.Range(request.Skip, count).ToList());
        };
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderItemsFromProviderOnPreload()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("Item 0"));
            Assert.IsTrue(component.Markup.Contains("Item 1"));
            Assert.IsTrue(component.Markup.Contains("Item 2"));
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldShowEmptyTemplateWhenNoItems()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, emptyProvider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.EmptyTemplate, builder => builder.AddContent(0, "No data"));
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Markup.Contains("No data")));

        ValueTask<IEnumerable<int>> emptyProvider(BitInfiniteScrollingItemsProviderRequest _)
        {
            return ValueTask.FromResult<IEnumerable<int>>(new List<int>());
        }
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldShowCustomEmptyMessageWhenNoItems()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult<IEnumerable<int>>(new List<int>()));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.EmptyMessage, "Nothing here");
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("Nothing here"));
            Assert.IsFalse(component.Instance.HasMore);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotShowEmptyMessageBeforeTheFirstLoad()
    {
        // Nothing is loaded yet, so the list is not empty - it is unknown. Rendering the empty state here
        // would flash "there is no item" on every first paint.
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
        });

        Assert.IsFalse(component.Markup.Contains("There is no item"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldUseCustomLoadingTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");

        var tcs = new TaskCompletionSource<IEnumerable<int>>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => new ValueTask<IEnumerable<int>>(tcs.Task));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.LoadingTemplate, builder => builder.AddContent(0, "Custom Loading"));
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Markup.Contains("Custom Loading")));

        tcs.TrySetResult(new List<int>());
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderCustomLoadingMessageAndAriaBusy()
    {
        var tcs = new TaskCompletionSource<IEnumerable<int>>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => new ValueTask<IEnumerable<int>>(tcs.Task));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.LoadingMessage, "Fetching...");
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("Fetching..."));
            Assert.AreEqual("true", component.Find(".bit-isc").GetAttribute("aria-busy"));
            Assert.IsTrue(component.Instance.IsLoading);
        });

        tcs.TrySetResult(new List<int> { 1 });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("false", component.Find(".bit-isc").GetAttribute("aria-busy"));
            Assert.IsFalse(component.Instance.IsLoading);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldSendPageSizeAsTheRequestCount()
    {
        var counts = new List<int>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(100, r => counts.Add(r.Count)));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 7);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, counts.Count);
            Assert.AreEqual(7, counts[0]);
            Assert.AreEqual(7, component.Instance.Items.Count);
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldEndWhenPageIsShorterThanPageSize()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(8));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.EndMessage, "That is all");
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(8, component.Instance.Items.Count);
            Assert.IsFalse(component.Instance.HasMore);
            Assert.IsTrue(component.Markup.Contains("That is all"));
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldEndWhenProviderReturnsNoItem()
    {
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ =>
            {
                calls++;
                return ValueTask.FromResult<IEnumerable<int>>(calls == 1 ? new List<int> { 1, 2 } : new List<int>());
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        component.WaitForAssertion(() => Assert.IsFalse(component.Instance.HasMore));

        // Once the end is known, no further request is made even when a load is asked for explicitly.
        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldRespectMaxItems()
    {
        var counts = new List<int>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(100, r => counts.Add(r.Count)));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.MaxItems, 8);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        component.WaitForAssertion(() =>
        {
            // The second page is narrowed down to the three items that are still allowed.
            CollectionAssert.AreEqual(new List<int> { 5, 3 }, counts);
            Assert.AreEqual(8, component.Instance.Items.Count);
            Assert.IsFalse(component.Instance.HasMore);
        });

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        Assert.AreEqual(8, component.Instance.Items.Count);
        Assert.AreEqual(2, counts.Count);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldTrimAPageThatOvershootsMaxItems()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            // A provider that ignores the requested count and always returns ten items.
            parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult<IEnumerable<int>>(Enumerable.Range(0, 10).ToList()));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.MaxItems, 4);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new List<int> { 0, 1, 2, 3 }, component.Instance.Items.ToList());
            Assert.IsFalse(component.Instance.HasMore);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderErrorMessageAndRetryButton()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => throw new InvalidOperationException("boom"));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.ErrorMessage, "It failed");
            parameters.Add(p => p.RetryText, "Try again");
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("It failed"));
            Assert.AreEqual("Try again", component.Find(".bit-isc-btn").TextContent.Trim());
            Assert.IsNotNull(component.Instance.Error);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingRetryShouldReloadTheFailedPage()
    {
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ =>
            {
                calls++;
                if (calls == 1) throw new InvalidOperationException("boom");
                return ValueTask.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 });
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.IsNotNull(component.Instance.Error));

        component.Find(".bit-isc-btn").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsNull(component.Instance.Error);
            Assert.AreEqual(3, component.Instance.Items.Count);
            Assert.IsTrue(component.Markup.Contains("Item 1"));
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderErrorTemplate()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => throw new InvalidOperationException("kaboom"));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<Exception>)(ex => builder => builder.AddContent(0, $"Error: {ex.Message}")));
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("Error: kaboom"));
            Assert.AreEqual(0, component.FindAll(".bit-isc-btn").Count);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldInvokeOnErrorCallback()
    {
        Exception? handled = null;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => throw new InvalidOperationException("nope"));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, ex => handled = ex));
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual("nope", handled?.Message));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldInvokeOnItemsLoadedAndOnEndCallbacks()
    {
        var pages = new List<int>();
        var ended = false;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(8));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.OnItemsLoaded, EventCallback.Factory.Create<IReadOnlyList<int>>(this, items => pages.Add(items.Count)));
            parameters.Add(p => p.OnEnd, EventCallback.Factory.Create(this, () => ended = true));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, pages.Count));

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        component.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new List<int> { 5, 3 }, pages);
            Assert.IsTrue(ended);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingManualModeShouldLoadTheFirstPageAndRenderTheButton()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.LoadMoreText, "More please");
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(5, component.Instance.Items.Count);
            Assert.AreEqual("More please", component.Find(".bit-isc-btn").TextContent.Trim());
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingManualButtonShouldLoadTheNextPage()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Manual, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));

        component.Find(".bit-isc-btn").Click();

        component.WaitForAssertion(() => Assert.AreEqual(10, component.Instance.Items.Count));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotRenderTheButtonInTheAutomaticMode()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));

        Assert.AreEqual(0, component.FindAll(".bit-isc-btn").Count);
    }

    [TestMethod]
    public async Task BitInfiniteScrollingRefreshDataAsyncShouldReloadFromScratch()
    {
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, request =>
            {
                calls++;
                return ValueTask.FromResult<IEnumerable<int>>(new List<int> { request.Skip, request.Skip + 1 });
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, component.Instance.Items.Count);
            Assert.AreEqual(0, component.Instance.Items[0]);
            Assert.AreEqual(3, calls);
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingRefreshDataAsyncShouldDropTheInFlightPage()
    {
        var first = new TaskCompletionSource<IEnumerable<int>>();
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ =>
            {
                calls++;
                return calls == 1
                    ? new ValueTask<IEnumerable<int>>(first.Task)
                    : ValueTask.FromResult<IEnumerable<int>>(new List<int> { 7, 8 });
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.IsLoading));

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        // The first request completes after the refresh already installed the new page: its result is stale
        // and must neither be rendered nor clear the state of the load that replaced it.
        first.TrySetResult(new List<int> { 1, 2, 3 });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, component.Instance.Items.Count);
            Assert.AreEqual(7, component.Instance.Items[0]);
            Assert.IsFalse(component.Instance.IsLoading);
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingAppendItemsAsyncShouldAppendWithoutTheProvider()
    {
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, request =>
            {
                calls++;
                return ValueTask.FromResult<IEnumerable<int>>(new List<int> { 1, 2 });
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.AppendItemsAsync(new[] { 9 }));

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.Instance.Items.Count);
            Assert.AreEqual(9, component.Instance.Items[2]);
            Assert.AreEqual(1, calls);
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingPrependItemsAsyncShouldPrependWithoutTheProvider()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult<IEnumerable<int>>(new List<int> { 5, 6 }));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.PrependItemsAsync(new[] { 4 }));

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.Instance.Items.Count);
            Assert.AreEqual(4, component.Instance.Items[0]);
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingReversedShouldPrependEachPage()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, request =>
                ValueTask.FromResult<IEnumerable<int>>(new List<int> { request.Skip, request.Skip + 1 }));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.Items.Count));

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        component.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new List<int> { 2, 3, 0, 1 }, component.Instance.Items.ToList());
            Assert.IsTrue(component.Find(".bit-isc").ClassList.Contains("bit-isc-rev"));
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldNotLoadWhenItIsNotEnabled()
    {
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ =>
            {
                calls++;
                return ValueTask.FromResult<IEnumerable<int>>(new List<int> { 1 });
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Preload, true);
        });

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        Assert.AreEqual(0, calls);
        Assert.AreEqual(0, component.Instance.Items.Count);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldKeepItsItemsWhenTheProviderDelegateIsRecreated()
    {
        // A parent that writes its provider as a lambda hands over a new delegate instance on every render.
        // Only a change of the underlying method is a new data source; anything else would wipe the list.
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, MakeProvider(1));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.Items.Count));

        var first = MakeProvider(1);
        var second = MakeProvider(1);
        Assert.IsFalse(ReferenceEquals(first, second), "The two delegates have to be distinct instances for this test to mean anything.");

        component.Render(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, second);
        });

        Assert.AreEqual(2, component.Instance.Items.Count);

        // A new delegate instance over the very same method, the way a lambda written in a parent's markup
        // is handed over again on every render of that parent.
        static BitInfiniteScrollingItemsProvider<int> MakeProvider(int seed)
            => _ => ValueTask.FromResult<IEnumerable<int>>(new List<int> { seed, seed + 1 });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldReloadWhenTheProviderChanges()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, FirstProvider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.Items[0]));

        component.Render(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, SecondProvider);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, component.Instance.Items.Count);
            Assert.AreEqual(11, component.Instance.Items[0]);
        });

        ValueTask<IEnumerable<int>> FirstProvider(BitInfiniteScrollingItemsProviderRequest _)
            => ValueTask.FromResult<IEnumerable<int>>(new List<int> { 1, 2 });

        ValueTask<IEnumerable<int>> SecondProvider(BitInfiniteScrollingItemsProviderRequest _)
            => ValueTask.FromResult<IEnumerable<int>>(new List<int> { 11, 12 });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldApplyClassesAndStyles()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.EndMessage, "The end");
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Classes, new BitInfiniteScrollingClassStyles { Root = "custom-root", End = "custom-end", LastElement = "custom-last" });
            parameters.Add(p => p.Styles, new BitInfiniteScrollingClassStyles { Root = "color:red", End = "color:blue" });
        });

        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-isc");
            Assert.IsTrue(root.ClassList.Contains("custom-root"));
            Assert.IsTrue(root.GetAttribute("style")!.Contains("color:red"));

            var end = component.Find(".bit-isc-end");
            Assert.IsTrue(end.ClassList.Contains("custom-end"));
            Assert.IsTrue(end.GetAttribute("style")!.Contains("color:blue"));

            Assert.IsTrue(component.Find(".bit-isc-lst").ClassList.Contains("custom-last"));
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldHideTheSentinelWhenThereIsNoMoreItem()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(component.Instance.HasMore);
            Assert.AreEqual("display:none", component.Find(".bit-isc-lst").GetAttribute("style"));
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldApplyTheLastElementParameters()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(100));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.LastElementClass, "sentinel");
            parameters.Add(p => p.LastElementHeight, "64px");
            parameters.Add(p => p.LastElementStyle, "opacity:0.5");
        });

        component.WaitForAssertion(() =>
        {
            var last = component.Find(".bit-isc-lst");
            Assert.IsTrue(last.ClassList.Contains("sentinel"));

            var style = last.GetAttribute("style")!;
            Assert.IsTrue(style.Contains("height:64px"));
            Assert.IsTrue(style.Contains("opacity:0.5"));
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderRtlDirection()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Preload, true);
        });

        var root = component.Find(".bit-isc");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldNotStartASecondLoadWhileOneIsInFlight()
    {
        var tcs = new TaskCompletionSource<IEnumerable<int>>();
        var calls = 0;

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ =>
            {
                calls++;
                return new ValueTask<IEnumerable<int>>(tcs.Task);
            });
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.IsLoading));

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        Assert.AreEqual(1, calls);

        tcs.TrySetResult(new List<int> { 1 });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldKeepALiveRegionThatFollowsTheState()
    {
        var tcs = new TaskCompletionSource<IEnumerable<int>>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => new ValueTask<IEnumerable<int>>(tcs.Task));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.LoadingMessage, "Loading the next page");
            parameters.Add(p => p.EmptyMessage, "Nothing here");
            parameters.Add(p => p.Preload, true);
        });

        // The region exists from the first render on, which is what makes its later changes announceable.
        var status = component.Find(".bit-isc-sts");
        Assert.AreEqual("status", status.GetAttribute("role"));

        component.WaitForAssertion(() => Assert.AreEqual("Loading the next page", component.Find(".bit-isc-sts").TextContent.Trim()));

        tcs.TrySetResult(new List<int>());

        component.WaitForAssertion(() => Assert.AreEqual("Nothing here", component.Find(".bit-isc-sts").TextContent.Trim()));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderTheTabIndex()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.TabIndex, "0");
        });

        Assert.AreEqual("0", component.Find(".bit-isc").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldPassTheObserverOptionsToJs()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.ScrollerSelector, "window");
            parameters.Add(p => p.RootMargin, "200px");
            parameters.Add(p => p.Threshold, 0.5m);
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.setup");

        Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[0]);
        Assert.AreEqual("window", invocation.Arguments[1]);
        Assert.AreEqual(0.5m, invocation.Arguments[4]);
        Assert.AreEqual("200px", invocation.Arguments[5]);
        Assert.AreEqual(true, invocation.Arguments[6]);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldRebuildTheObserverWhenItsOptionsChange()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.RootMargin, "100px");
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.InfiniteScrolling.setup"].Count);

        component.Render(parameters => parameters.Add(p => p.RootMargin, "300px"));

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.InfiniteScrolling.setup"];
        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual("300px", invocations.Last().Arguments[5]);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotObserveWithoutAProvider()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
        });

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.setup");

        Assert.AreEqual(false, invocation.Arguments[6]);
        Assert.AreEqual("display:none", component.Find(".bit-isc-lst").GetAttribute("style"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldLoadWhenTheProviderArrivesAfterTheFirstRender()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.PageSize, 5);
        });

        Assert.AreEqual(0, component.Instance.Items.Count);

        component.Render(parameters => parameters.Add(p => p.ItemsProvider, PagedProvider(20)));

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldInvokeTheScrollJsInterop()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
        });

        await component.InvokeAsync(() => component.Instance.ScrollToTopAsync());
        await component.InvokeAsync(() => component.Instance.ScrollToBottomAsync(true));

        var invocations = Context.JSInterop.Invocations["BitBlazorUI.InfiniteScrolling.scrollTo"];

        Assert.AreEqual(2, invocations.Count);
        Assert.AreEqual(false, invocations.First().Arguments[1]);
        Assert.AreEqual(true, invocations.Last().Arguments[1]);
        Assert.AreEqual(true, invocations.Last().Arguments[2]);
    }

    [TestMethod]
    public void BitInfiniteScrollingReversedShouldAnchorTheScrollAroundThePrepend()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, request =>
                ValueTask.FromResult<IEnumerable<int>>(new List<int> { request.Skip, request.Skip + 1 }));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Preload, true);
        });

        // The first page lands the viewport on the newest items instead of leaving it at the top, where the
        // sentinel would immediately ask for the next page.
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.scrollTo"));

        component.InvokeAsync(() => component.Instance.LoadMoreAsync()).GetAwaiter().GetResult();

        component.WaitForAssertion(() =>
        {
            Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.prepareScroll");
            Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.restoreScroll");
        });
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldDisposeJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.dispose");

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
        });

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.dispose");
    }

    [TestMethod]
    public async Task BitInfiniteScrollingDisposeShouldNotThrow()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.dispose");

        var component = RenderComponent<BitInfiniteScrolling<int>>(p =>
        {
            p.Add(x => x.ItemsProvider, _ =>
                ValueTask.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 }));
            p.Add(x => x.ItemTemplate, ItemTemplate());
        });

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitInfiniteScrollingDisposeDuringLoadShouldNotThrow()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.dispose");

        var tcs = new TaskCompletionSource<IEnumerable<int>>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(p =>
        {
            p.Add(x => x.ItemsProvider, _ => new ValueTask<IEnumerable<int>>(tcs.Task));
            p.Add(x => x.ItemTemplate, ItemTemplate());
            p.Add(x => x.Preload, true);
        });

        await component.Instance.DisposeAsync();

        tcs.TrySetCanceled();
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldCancelTheInFlightRequestOnDispose()
    {
        var cancelled = false;

        var component = RenderComponent<BitInfiniteScrolling<int>>(p =>
        {
            p.Add(x => x.ItemsProvider, async request =>
            {
                request.CancellationToken.Register(() => cancelled = true);
                await Task.Delay(Timeout.Infinite, request.CancellationToken);
                return new List<int>();
            });
            p.Add(x => x.ItemTemplate, ItemTemplate());
            p.Add(x => x.Preload, true);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.IsLoading));

        await component.Instance.DisposeAsync();

        Assert.IsTrue(cancelled);
    }
}
