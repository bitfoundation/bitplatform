using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Virtualize;

/// <summary>
/// Covers the BitParams cascade of the Virtualize: what a BitVirtualizeParams fills in, what it leaves alone because
/// the list wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitVirtualizeParamsTests : BunitTestContext
{
    // What belongs to a single list rather than to a group of them: its data, the templates typed over its items, its
    // keys, its sticky predicate, its events and its own layout.
    private static readonly string[] _notCascaded =
    [
        nameof(BitVirtualize<int>.AlignToEnd),
        nameof(BitVirtualize<int>.CascadingParameters),
        nameof(BitVirtualize<int>.ChildContent),
        nameof(BitVirtualize<int>.FooterTemplate),
        nameof(BitVirtualize<int>.HeaderTemplate),
        nameof(BitVirtualize<int>.Horizontal),
        nameof(BitVirtualize<int>.IndexedItemTemplate),
        nameof(BitVirtualize<int>.InitialIndex),
        nameof(BitVirtualize<int>.IsStickyItem),
        nameof(BitVirtualize<int>.ItemKey),
        nameof(BitVirtualize<int>.Items),
        nameof(BitVirtualize<int>.ItemsProvider),
        nameof(BitVirtualize<int>.ItemTemplate),
        nameof(BitVirtualize<int>.Lanes),
        nameof(BitVirtualize<int>.MinLaneSize),
        nameof(BitVirtualize<int>.OnEndReached),
        nameof(BitVirtualize<int>.OnStartReached),
        nameof(BitVirtualize<int>.OnVisibleRangeChanged),
        nameof(BitVirtualize<int>.Reversed),
        nameof(BitVirtualize<int>.ScrollerSelector),
        nameof(BitVirtualize<int>.StickyTemplate),
    ];

    private static RenderFragment<int> ItemTemplate() => item => builder => builder.AddContent(0, $"Item {item}");

    private void SetupViewport(double viewportSize = 300)
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = viewportSize, ScrollOffset = 0 });
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitVirtualizeParams listParams, Action<RenderTreeBuilder>? extraAttributes = null, int count = 100)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { listParams });
            parameters.AddChildContent(builder => BuildList(builder, extraAttributes, count));
        });
    }

    private static void BuildList(RenderTreeBuilder builder, Action<RenderTreeBuilder>? extraAttributes, int count)
    {
        builder.OpenComponent<BitVirtualize<int>>(0);
        builder.AddAttribute(1, nameof(BitVirtualize<int>.Items), Enumerable.Range(0, count).ToArray());
        builder.AddAttribute(2, nameof(BitVirtualize<int>.ItemTemplate), ItemTemplate());
        extraAttributes?.Invoke(builder);
        builder.CloseComponent();
    }

    [TestMethod]
    public void BitVirtualizeParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitVirtualize", BitVirtualizeParams.ParamName);
        Assert.AreEqual(BitVirtualizeParams.ParamName, new BitVirtualizeParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitVirtualizeParams());
    }

    [TestMethod]
    public void BitVirtualizeParamsShouldCarryEveryParameterThatBelongsToAGroupOfLists()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitVirtualize<int>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                   .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                   .Select(p => p.Name)
                                                   .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitVirtualizeParams).GetProperty(name), $"BitVirtualizeParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitVirtualizeShouldTakeTheCascadedValues()
    {
        SetupViewport(300);

        var component = RenderWithParams(new BitVirtualizeParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", Item = "cascaded-item" },
            Styles = new() { Root = "margin:1px" },
            ItemSize = 100,
            OverscanCount = 0,
            Role = "feed",
            ItemRole = "article",
        });

        var root = component.Find(".bit-vir");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("feed", root.GetAttribute("role"));

        // 300px of 100px items with no overscan.
        var items = component.FindAll(".bit-vir-itm");
        Assert.AreEqual(3, items.Count);
        Assert.AreEqual("article", items[0].GetAttribute("role"));
        Assert.IsTrue(items[0].ClassList.Contains("cascaded-item"));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:10000px");
    }

    [TestMethod]
    public void BitVirtualizeShouldTakeTheCascadedDynamicMode()
    {
        SetupViewport(300);

        var component = RenderWithParams(new BitVirtualizeParams
        {
            Dynamic = true,
            EstimatedItemSize = 30,
        });

        Assert.IsFalse(component.Find(".bit-vir-itm").ClassList.Contains("bit-vir-fix"));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:3000px");
        Assert.AreEqual(true, Context.JSInterop.Invocations["BitBlazorUI.Virtualize.setup"][0].Arguments[3]);
    }

    [TestMethod]
    public void BitVirtualizeShouldTakeTheCascadedEmptyTemplate()
    {
        SetupViewport(300);

        var component = RenderWithParams(new BitVirtualizeParams
        {
            EmptyTemplate = builder => builder.AddContent(0, "Nichts da"),
        }, count: 0);

        Assert.AreEqual("Nichts da", component.Find(".bit-vir-emp").TextContent);
    }

    [TestMethod]
    public void BitVirtualizeShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        SetupViewport(300);

        var component = RenderWithParams(new BitVirtualizeParams
        {
            Class = "cascaded",
            ItemSize = 100,
            ItemRole = "article",
            Classes = new() { Item = "cascaded-item" },
        }, builder =>
        {
            builder.AddAttribute(3, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(4, nameof(BitVirtualize<int>.ItemSize), 50f);
            builder.AddAttribute(5, nameof(BitVirtualize<int>.ItemRole), "option");
            builder.AddAttribute(6, nameof(BitVirtualize<int>.Classes), new BitVirtualizeClassStyles { Item = "own-item" });
        });

        var root = component.Find(".bit-vir");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:5000px");

        var item = component.Find(".bit-vir-itm");

        Assert.AreEqual("option", item.GetAttribute("role"));
        Assert.IsTrue(item.ClassList.Contains("own-item"));
        Assert.IsFalse(item.ClassList.Contains("cascaded-item"));
    }

    [TestMethod]
    public void BitVirtualizeShouldDropTheCascadedValuesWithTheCascade()
    {
        SetupViewport(300);

        var component = RenderWithParams(new BitVirtualizeParams
        {
            ItemSize = 100,
            Role = "feed",
        });

        Assert.AreEqual("feed", component.Find(".bit-vir").GetAttribute("role"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
            parameters.AddChildContent(builder => BuildList(builder, null, 100));
        });

        var list = component.FindComponent<BitVirtualize<int>>().Instance;

        Assert.AreEqual(50f, list.ItemSize);
        Assert.AreEqual("list", list.Role);
        Assert.AreEqual("list", component.Find(".bit-vir").GetAttribute("role"));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:5000px");
    }
}
