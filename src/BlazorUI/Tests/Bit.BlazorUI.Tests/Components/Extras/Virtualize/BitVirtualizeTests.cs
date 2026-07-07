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
        Context.JSInterop.SetupVoid("BitBlazorUI.Virtualize.dispose");

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Virtualize.dispose");
    }
}
