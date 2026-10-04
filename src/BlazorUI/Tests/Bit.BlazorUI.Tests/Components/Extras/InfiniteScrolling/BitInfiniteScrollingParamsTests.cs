using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.InfiniteScrolling;

/// <summary>
/// Covers the BitParams cascade of the InfiniteScrolling: what a BitInfiniteScrollingParams fills in, what it leaves
/// alone because the list wrote it for itself, that the first page is already requested with the cascaded values,
/// and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitInfiniteScrollingParamsTests : BunitTestContext
{
    // What belongs to a single list rather than to a group of them: its data, its item template and keys, its
    // events and its own layout.
    private static readonly string[] _notCascaded =
    [
        nameof(BitInfiniteScrolling<int>.CascadingParameters),
        nameof(BitInfiniteScrolling<int>.ChildContent),
        nameof(BitInfiniteScrolling<int>.Horizontal),
        nameof(BitInfiniteScrolling<int>.ItemKey),
        nameof(BitInfiniteScrolling<int>.ItemsProvider),
        nameof(BitInfiniteScrolling<int>.ItemTemplate),
        nameof(BitInfiniteScrolling<int>.OnEnd),
        nameof(BitInfiniteScrolling<int>.OnError),
        nameof(BitInfiniteScrolling<int>.OnItemsLoaded),
        nameof(BitInfiniteScrolling<int>.ResetKey),
        nameof(BitInfiniteScrolling<int>.Reversed),
        nameof(BitInfiniteScrolling<int>.ScrollerSelector),
    ];

    private static RenderFragment<int> ItemTemplate() => item => builder => builder.AddContent(0, $"Item {item}");

    private static BitInfiniteScrollingItemsProvider<int> PagedProvider(int total, List<BitInfiniteScrollingItemsProviderRequest>? requests = null)
    {
        return request =>
        {
            requests?.Add(request);

            var count = Math.Clamp(total - request.Skip, 0, request.Count);

            return ValueTask.FromResult<IEnumerable<int>>(Enumerable.Range(request.Skip, count).ToList());
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitInfiniteScrollingParams listParams,
                                                           Action<RenderTreeBuilder>? extraAttributes = null,
                                                           BitInfiniteScrollingItemsProvider<int>? provider = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { listParams });
            parameters.AddChildContent(builder => BuildList(builder, extraAttributes, provider));
        });
    }

    private static void BuildList(RenderTreeBuilder builder, Action<RenderTreeBuilder>? extraAttributes, BitInfiniteScrollingItemsProvider<int>? provider)
    {
        builder.OpenComponent<BitInfiniteScrolling<int>>(0);
        builder.AddAttribute(1, nameof(BitInfiniteScrolling<int>.ItemsProvider), provider ?? PagedProvider(20));
        builder.AddAttribute(2, nameof(BitInfiniteScrolling<int>.ItemTemplate), ItemTemplate());
        extraAttributes?.Invoke(builder);
        builder.CloseComponent();
    }

    [TestMethod]
    public void BitInfiniteScrollingParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitInfiniteScrolling", BitInfiniteScrollingParams.ParamName);
        Assert.AreEqual(BitInfiniteScrollingParams.ParamName, new BitInfiniteScrollingParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitInfiniteScrollingParams());
    }

    [TestMethod]
    public void BitInfiniteScrollingParamsShouldCarryEveryParameterThatBelongsToAGroupOfLists()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitInfiniteScrolling<int>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                          .Select(p => p.Name)
                                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitInfiniteScrollingParams).GetProperty(name), $"BitInfiniteScrollingParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldTakeTheCascadedValues()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var component = RenderWithParams(new BitInfiniteScrollingParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", Button = "cascaded-button" },
            Styles = new() { Root = "margin:1px" },
            PageSize = 4,
            Manual = true,
            Feed = true,
            LoadMoreText = "Mehr laden",
            LoadedMessage = "{0} geladen",
        }, provider: PagedProvider(20, requests));

        component.WaitForAssertion(() => Assert.AreEqual(4, component.FindAll("article").Count));

        var root = component.Find(".bit-isc");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("feed", root.GetAttribute("role"));
        Assert.AreEqual(4, requests[0].Count);

        var button = component.Find(".bit-isc-btn");

        Assert.IsTrue(button.ClassList.Contains("cascaded-button"));
        Assert.AreEqual("Mehr laden", button.TextContent.Trim());
        Assert.AreEqual("4 geladen", component.Find(".bit-isc-sts").TextContent);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldPreloadWithTheCascadedValues()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var component = RenderWithParams(new BitInfiniteScrollingParams
        {
            Preload = true,
            PageSize = 3,
            MaxItems = 2,
        }, provider: PagedProvider(20, requests));

        component.WaitForAssertion(() => Assert.AreEqual(1, requests.Count));

        // The very first request already carries the cascaded page size, narrowed down by the cascaded cap.
        Assert.AreEqual(2, requests[0].Count);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldTakeTheCascadedTexts()
    {
        var component = RenderWithParams(new BitInfiniteScrollingParams
        {
            Preload = true,
            EmptyMessage = "Nichts da",
        }, provider: PagedProvider(0));

        component.WaitForAssertion(() => Assert.AreEqual("Nichts da", component.Find(".bit-isc-emp").TextContent.Trim()));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var requests = new List<BitInfiniteScrollingItemsProviderRequest>();

        var component = RenderWithParams(new BitInfiniteScrollingParams
        {
            Class = "cascaded",
            PageSize = 4,
            Manual = true,
            LoadMoreText = "Cascaded",
            Classes = new() { Button = "cascaded-button" },
        }, builder =>
        {
            builder.AddAttribute(3, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(4, nameof(BitInfiniteScrolling<int>.PageSize), 6);
            builder.AddAttribute(5, nameof(BitInfiniteScrolling<int>.LoadMoreText), "Own");
            builder.AddAttribute(6, nameof(BitInfiniteScrolling<int>.Classes), new BitInfiniteScrollingClassStyles { Button = "own-button" });
        }, PagedProvider(20, requests));

        component.WaitForAssertion(() => Assert.AreEqual(6, component.FindComponent<BitInfiniteScrolling<int>>().Instance.Items.Count));

        var root = component.Find(".bit-isc");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.AreEqual(6, requests[0].Count);

        var button = component.Find(".bit-isc-btn");

        Assert.AreEqual("Own", button.TextContent.Trim());
        Assert.IsTrue(button.ClassList.Contains("own-button"));
        Assert.IsFalse(button.ClassList.Contains("cascaded-button"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldDropTheCascadedValuesWithTheCascade()
    {
        var component = RenderWithParams(new BitInfiniteScrollingParams
        {
            Manual = true,
            LoadMoreText = "Cascaded",
            PageSize = 4,
        });

        component.WaitForAssertion(() => Assert.AreEqual("Cascaded", component.Find(".bit-isc-btn").TextContent.Trim()));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
            parameters.AddChildContent(builder => BuildList(builder, null, null));
        });

        var list = component.FindComponent<BitInfiniteScrolling<int>>().Instance;

        Assert.AreEqual("Load more", list.LoadMoreText);
        Assert.IsFalse(list.Manual);
        Assert.AreEqual(0, list.PageSize);
    }
}
