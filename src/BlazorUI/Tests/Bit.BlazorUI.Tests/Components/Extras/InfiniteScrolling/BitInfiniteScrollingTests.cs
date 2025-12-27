using System.Collections.Generic;
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

    [TestMethod]
    public void BitInfiniteScrollingShouldRenderItemsFromProviderOnPreload()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, (RenderFragment<int>)(item => builder => builder.AddContent(0, $"Item {item}")));
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
            parameters.Add(p => p.ItemTemplate, (RenderFragment<int>)(item => builder => builder.AddContent(0, $"Item {item}")));
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
    public void BitInfiniteScrollingShouldUseCustomLoadingTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");

        var cts = new CancellationTokenSource();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, slowProvider);
            parameters.Add(p => p.ItemTemplate, (RenderFragment<int>)(item => builder => builder.AddContent(0, $"Item {item}")));
            parameters.Add(p => p.LoadingTemplate, builder => builder.AddContent(0, "Custom Loading"));
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Markup.Contains("Custom Loading")));

        cts.Cancel();

        async ValueTask<IEnumerable<int>> slowProvider(BitInfiniteScrollingItemsProviderRequest request)
        {
            await Task.Delay(50, cts.Token);

            return new List<int>();
        }
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldDisposeJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.InfiniteScrolling.dispose");

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, Provider);
            parameters.Add(p => p.ItemTemplate, (RenderFragment<int>)(item => builder => builder.AddContent(0, $"Item {item}")));
        });

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.dispose");
    }
}
