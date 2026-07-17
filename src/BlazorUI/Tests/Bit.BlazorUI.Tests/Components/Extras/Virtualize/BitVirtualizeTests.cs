using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Virtualize;

[TestClass]
public class BitVirtualizeTests : BunitTestContext
{
    private static readonly RenderFragment<int> itemTemplate = item => builder => builder.AddContent(0, $"Item {item}");

    [TestMethod]
    public void BitVirtualizeShouldRenderInitialItemsFromItems()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 1_000).ToArray());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Assert.IsTrue(component.Markup.Contains("bit-vir"));
        Assert.IsTrue(component.Markup.Contains("bit-vir-spc"));
        Assert.IsTrue(component.Markup.Contains("Item 0"));
        Assert.IsTrue(component.Markup.Contains("Item 1"));
        Assert.IsFalse(component.Markup.Contains("Item 999"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderSpacerWithTotalSize()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 56);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("height:5600px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderHorizontalClassAndSpacer()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        var root = component.Find(".bit-vir");
        Assert.IsTrue(root.ClassList.Contains("bit-vir-hor"));

        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("width:5000px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderEmptyTemplateWhenNoItems()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<int>());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.EmptyTemplate, builder => builder.AddContent(0, "No data"));
        });

        Assert.IsTrue(component.Markup.Contains("No data"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderItemsFromProvider()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, provider);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("Item 0"));
            Assert.IsTrue(component.Markup.Contains("Item 1"));
            Assert.IsTrue(component.Markup.Contains("Item 2"));
        });

        static ValueTask<BitVirtualizeItemsProviderResult<int>> provider(BitVirtualizeItemsProviderRequest request)
        {
            var items = Enumerable.Range(request.StartIndex, request.Count).ToList();
            return ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>(items, 100));
        }
    }

    [TestMethod]
    public void BitVirtualizeShouldThrowWhenBothItemsAndItemsProviderAreSet()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            RenderComponent<BitVirtualize<int>>(parameters =>
            {
                parameters.Add(p => p.Items, new List<int> { 1, 2, 3 });
                parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>([], 0)));
                parameters.Add(p => p.ItemTemplate, itemTemplate);
            });
        });
    }

    [TestMethod]
    public void BitVirtualizeShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "height:100px");
        });

        var root = component.Find(".bit-vir");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("height:100px"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldDisposeJsInterop()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Context.JSInterop.SetupVoid("BitBlazorUI.Virtualize.dispose", component.Instance.UniqueId).SetVoidResult();

        await component.Instance.DisposeAsync();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Virtualize.dispose");
        Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[0]);
    }

    [TestMethod]
    public void BitVirtualizeDynamicShouldSizeSpacerFromEstimatedItemSize()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 200).ToArray());
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.EstimatedItemSize, 40);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // In Dynamic mode the scrollable extent starts as EstimatedItemSize * count: 200 * 40 = 8000.
        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("height:8000px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicShouldUpdateTotalSizeWhenItemsMeasured()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.EstimatedItemSize, 40);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // Report a measured size 100px larger than the estimate; the prefix-sum tree should
        // apply the delta to the total: (10 * 40) + 100 = 500.
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [140d]));

        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("height:500px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderStickyHeaderForStickyItems()
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = 300, ScrollOffset = 0 });

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.IsStickyItem, i => i % 10 == 0);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // Index 0 is sticky and sits at the top of the viewport, so it gets pinned as the active header.
        var sticky = component.Find(".bit-vir-stk");
        Assert.IsNotNull(sticky);
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderTranslatedBlockWrapper()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // The rendered window lives inside a translated block so the extent stays reachable past the
        // browser's max-element-size limit; items are direct children of that block.
        var block = component.Find(".bit-vir-spc > .bit-vir-blk");
        Assert.IsNotNull(block);
        Assert.IsTrue(block.GetAttribute("style")!.Contains("translateY"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRecomputeWhenSameInstanceCountChanges()
    {
        var list = Enumerable.Range(0, 10).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Assert.AreEqual("height:500px", component.Find(".bit-vir-spc").GetAttribute("style"));

        // Mutate the same list instance (add 10 items) and re-render with the same reference.
        list.AddRange(Enumerable.Range(10, 10));
        component.Render(parameters => parameters.Add(p => p.Items, list));

        Assert.AreEqual("height:1000px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldMoveRovingFocusOnKeyboardNavigation()
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = 300, ScrollOffset = 0 });

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // Navigate to the last item; it becomes the active (roving tabindex=0) element.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));

        component.WaitForAssertion(() =>
        {
            var active = component.Find("[data-bit-vir-index='99']");
            Assert.AreEqual("0", active.GetAttribute("tabindex"));
        });
    }

    [TestMethod]
    public async Task BitVirtualizeScrollShouldBeNoOpAfterDispose()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Context.JSInterop.SetupVoid("BitBlazorUI.Virtualize.dispose", component.Instance.UniqueId).SetVoidResult();
        await component.Instance.DisposeAsync();

        // A scroll message that arrives after disposal must be ignored without throwing.
        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));
    }

    [TestMethod]
    public void BitVirtualizeReversedShouldScrollToEndOnInitialRender()
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = 300, ScrollOffset = 0 });

        RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // Reversed (chat) mode anchors to the bottom on first render by scrolling to the end.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Virtualize.scrollToOffset");
    }
}
