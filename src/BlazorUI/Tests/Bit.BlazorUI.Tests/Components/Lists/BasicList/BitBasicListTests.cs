using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Lists.BasicList;

[TestClass]
public class BitBasicListTests : BunitTestContext
{
    [TestMethod,
        DataRow(true, 3_000_000, null),
        DataRow(true, 3_000_000, 5),
        DataRow(true, null, 5),
        DataRow(true, null, null),

        DataRow(false, 3_000_000, null),
        DataRow(false, 3_000_000, 5),
        DataRow(false, null, 5),
        DataRow(false, null, null),
    ]
    public void BitBasicListShouldRenderExpectedChildElements(bool virtualize, int? itemSize, int? overscanCount)
    {
        //https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html#-jsinterop-emulation
        const double viewportHeight = 1_000_000_000;
        var maxItemCount = 100;

        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // To ensure a consistent display structure in the Virtualize component across .NET 8, .NET 9, and .NET 10,
        // we've set the default value of MaxItemCount to 100. This means that even if a higher value is specified,
        // only a maximum of 100 items will be rendered by default.
        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", maxItemCount);

        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Virtualize, virtualize);
            parameters.Add(p => p.Items, GetTestData(500));
            //ItemSize default value is 50.
            parameters.Add(p => p.ItemSize, itemSize ?? 50);
            //OverscanCount default value is 3.
            parameters.Add(p => p.OverscanCount, overscanCount ?? 3);
        });

        var bitList = component.Find(".bit-bsl");

        if (virtualize)
        {
            //When virtualize is true, number of rendered items is greater than number of items show in the list + 2 * overScanCount.
            var overscanItemsCount = 2 * component.Instance.OverscanCount;

#if NET10_0
            maxItemCount += overscanItemsCount;
#endif

            var expectedRenderedItemCount = Math.Ceiling((decimal)(viewportHeight / component.Instance.ItemSize)) + overscanItemsCount;
            expectedRenderedItemCount = Math.Min(expectedRenderedItemCount, maxItemCount);

            var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;

            //When actualRenderedItemCount is smaller than expectedRenderedItemCount, so show all items in viewport then actualRenderedItemCount equals total items count
            if (actualRenderedItemCount < expectedRenderedItemCount)
            {
                Assert.AreEqual(component?.Instance?.Items?.Count, actualRenderedItemCount);
            }
            else
            {
                Assert.AreEqual(expectedRenderedItemCount, actualRenderedItemCount);
            }
        }
        else
        {
            var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;
            Assert.AreEqual(component?.Instance?.Items?.Count, actualRenderedItemCount);
        }

        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", null);
    }

    [TestMethod,
        DataRow(100, "AssignedRole"),
        DataRow(100, null)
    ]
    public void BitBasicListShouldHaveCorrectRole(int itemCount, string role)
    {
        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Items, GetTestData(itemCount));
            if (role.HasValue())
            {
                parameters.Add(p => p.Role, role);
            }
        });

        var list = component.Find(".bit-bsl");

        var listRole = list.GetAttribute("role");

        Assert.AreEqual(role.HasValue() ? role : "list", listRole);
    }

    [TestMethod]
    public void BitBasicListShouldNotRenderRoleWhenItIsNull()
    {
        var component = RenderList(p => p.Add(x => x.Role, (string?)null));

        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("role"));
    }

    [TestMethod, DataRow(100)]
    public void BitBasicListShouldHaveCorrectClass(int itemCount)
    {
        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Items, GetTestData(itemCount));
        });

        var bitList = component.Find("div");

        Assert.IsTrue(bitList.ClassList.Contains("bit-bsl"));
    }

    [TestMethod]
    public void BitBasicListShouldRespectCustomClassAndStyle()
    {
        var component = RenderList(p =>
        {
            p.Add(x => x.Class, "custom-class");
            p.Add(x => x.Style, "color: red;");
        });

        var list = component.Find(".bit-bsl");

        Assert.IsTrue(list.ClassList.Contains("custom-class"));
        StringAssert.Contains(list.GetAttribute("style"), "color: red;");
    }

    [TestMethod]
    public void BitBasicListShouldRespectClassesAndStyles()
    {
        var component = RenderList(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 2); // shorter than the 5 items, so the LoadMore button is rendered
            p.Add(x => x.HeaderTemplate, (RenderFragment)(b => b.AddContent(0, "header")));
            p.Add(x => x.FooterTemplate, (RenderFragment)(b => b.AddContent(0, "footer")));
            p.Add(x => x.Classes, new BitBasicListClassStyles
            {
                Root = "root-class",
                Header = "header-class",
                Footer = "footer-class",
                LoadMoreButton = "lmb-class",
                LoadMoreText = "lmt-class",
            });
            p.Add(x => x.Styles, new BitBasicListClassStyles
            {
                Root = "background: red;",
                Header = "background: green;",
                Footer = "background: blue;",
                LoadMoreButton = "background: yellow;",
                LoadMoreText = "background: pink;",
            });
        });

        Assert.IsTrue(component.Find(".bit-bsl").ClassList.Contains("root-class"));
        Assert.IsTrue(component.Find(".bit-bsl-hdr").ClassList.Contains("header-class"));
        Assert.IsTrue(component.Find(".bit-bsl-ftr").ClassList.Contains("footer-class"));
        Assert.IsTrue(component.Find(".bit-bsl-lmb").ClassList.Contains("lmb-class"));
        Assert.IsTrue(component.Find(".bit-bsl-lmt").ClassList.Contains("lmt-class"));

        StringAssert.Contains(component.Find(".bit-bsl").GetAttribute("style"), "background: red;");
        StringAssert.Contains(component.Find(".bit-bsl-hdr").GetAttribute("style"), "background: green;");
        StringAssert.Contains(component.Find(".bit-bsl-ftr").GetAttribute("style"), "background: blue;");
        StringAssert.Contains(component.Find(".bit-bsl-lmb").GetAttribute("style"), "background: yellow;");
        StringAssert.Contains(component.Find(".bit-bsl-lmt").GetAttribute("style"), "background: pink;");
    }

    [TestMethod]
    public void BitBasicListShouldRenderAriaLabel()
    {
        var component = RenderList(p => p.Add(x => x.AriaLabel, "the people"));

        Assert.AreEqual("the people", component.Find(".bit-bsl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBasicListShouldRenderTabIndex()
    {
        var component = RenderList(p => p.Add(x => x.TabIndex, "0"));

        Assert.AreEqual("0", component.Find(".bit-bsl").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitBasicListShouldRespectDir()
    {
        var component = RenderList(p => p.Add(x => x.Dir, BitDir.Rtl));

        var list = component.Find(".bit-bsl");

        Assert.AreEqual("rtl", list.GetAttribute("dir"));
        Assert.IsTrue(list.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitBasicListShouldRespectVisibility()
    {
        var component = RenderList(p => p.Add(x => x.Visibility, BitVisibility.Collapsed));

        StringAssert.Contains(component.Find(".bit-bsl").GetAttribute("style"), "display:none");
    }

    [TestMethod,
        DataRow("FullWidth", "width:100%"),
        DataRow("FullHeight", "height:100%"),
        DataRow("FitWidth", "width:fit-content"),
        DataRow("FitHeight", "height:fit-content")]
    public void BitBasicListShouldRespectSizeParameters(string parameter, string expectedStyle)
    {
        var component = RenderList(p =>
        {
            switch (parameter)
            {
                case "FullWidth": p.Add(x => x.FullWidth, true); break;
                case "FullHeight": p.Add(x => x.FullHeight, true); break;
                case "FitWidth": p.Add(x => x.FitWidth, true); break;
                case "FitHeight": p.Add(x => x.FitHeight, true); break;
            }
        });

        StringAssert.Contains(component.Find(".bit-bsl").GetAttribute("style"), expectedStyle);
    }

    [TestMethod]
    public void BitBasicListFullSizeAndFitSizeShouldSetBothAxes()
    {
        var full = RenderList(p => p.Add(x => x.FullSize, true));
        var style = full.Find(".bit-bsl").GetAttribute("style");
        StringAssert.Contains(style, "width:100%");
        StringAssert.Contains(style, "height:100%");

        var fit = RenderList(p => p.Add(x => x.FitSize, true));
        style = fit.Find(".bit-bsl").GetAttribute("style");
        StringAssert.Contains(style, "width:fit-content");
        StringAssert.Contains(style, "height:fit-content");
    }

    [TestMethod]
    public void BitBasicListShouldRespectIsEnabled()
    {
        var component = RenderList(p => p.Add(x => x.IsEnabled, false));

        Assert.IsTrue(component.Find(".bit-bsl").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitBasicListShouldRenderItemsAsTextWithoutRowTemplate()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string> { "one", "two", "three" });
        });

        var markup = component.Find(".bit-bsl").TextContent;

        StringAssert.Contains(markup, "one");
        StringAssert.Contains(markup, "two");
        StringAssert.Contains(markup, "three");
    }

    [TestMethod]
    public void BitBasicListShouldRenderEmptyContentWhenThereIsNoItem()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string>());
            p.Add(x => x.RowTemplate, (RenderFragment<string>)(item => b => b.AddContent(0, item)));
            p.Add(x => x.EmptyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<div class='empty'>nothing</div>")));
        });

        Assert.IsNotNull(component.Find(".empty"));
    }

    [TestMethod]
    public void BitBasicListShouldRenderEmptyContentWithoutRowTemplate()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string>());
            p.Add(x => x.EmptyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<div class='empty'>nothing</div>")));
        });

        Assert.IsNotNull(component.Find(".empty"));
    }

    [TestMethod]
    public void BitBasicListShouldNotRenderEmptyContentWhenThereAreItems()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string> { "one" });
            p.Add(x => x.EmptyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<div class='empty'>nothing</div>")));
        });

        Assert.AreEqual(0, component.FindAll(".empty").Count);
    }

    [TestMethod]
    public void BitBasicListShouldRenderHeaderAndFooterAroundTheItems()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string> { "one" });
            p.Add(x => x.HeaderTemplate, (RenderFragment)(b => b.AddContent(0, "the header")));
            p.Add(x => x.FooterTemplate, (RenderFragment)(b => b.AddContent(0, "the footer")));
            p.Add(x => x.RowTemplate, (RenderFragment<string>)(item => b => b.AddMarkupContent(0, $"<div class='row'>{item}</div>")));
        });

        var children = component.Find(".bit-bsl").Children;

        Assert.IsTrue(children[0].ClassList.Contains("bit-bsl-hdr"));
        Assert.AreEqual("the header", children[0].TextContent);

        Assert.IsTrue(children[1].ClassList.Contains("row"));

        Assert.IsTrue(children[^1].ClassList.Contains("bit-bsl-ftr"));
        Assert.AreEqual("the footer", children[^1].TextContent);
    }

    [TestMethod]
    public void BitBasicListShouldRenderLoadingTemplateInsteadOfItems()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Loading, true);
            p.Add(x => x.Items, new List<string> { "one", "two" });
            p.Add(x => x.RowTemplate, (RenderFragment<string>)(item => b => b.AddMarkupContent(0, $"<div class='row'>{item}</div>")));
            p.Add(x => x.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "loading...")));
        });

        Assert.AreEqual(0, component.FindAll(".row").Count);
        Assert.AreEqual("loading...", component.Find(".bit-bsl-ldc").TextContent);

        component.Render(p => p.Add(x => x.Loading, false));

        Assert.AreEqual(2, component.FindAll(".row").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bsl-ldc").Count);
    }

    [TestMethod]
    public void BitBasicListShouldRenderTheDefaultLoadingContentWithoutATemplate()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Loading, true);
            p.Add(x => x.Items, new List<string> { "one" });
        });

        Assert.AreEqual("...", component.Find(".bit-bsl-ldc").TextContent);
    }

    [TestMethod]
    public void BitBasicListShouldApplyTheHorizontalClass()
    {
        var component = RenderList(p => p.Add(x => x.Horizontal, true));

        Assert.IsTrue(component.Find(".bit-bsl").ClassList.Contains("bit-bsl-hrz"));
    }

    [TestMethod]
    public void BitBasicListShouldNotApplyTheHorizontalClassWhileVirtualizing()
    {
        var component = RenderList(p =>
        {
            p.Add(x => x.Horizontal, true);
            p.Add(x => x.Virtualize, true);
        });

        Assert.IsFalse(component.Find(".bit-bsl").ClassList.Contains("bit-bsl-hrz"));
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldRenderTheFirstPageOnly()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        Assert.AreEqual(5, component.FindAll(".row").Count);
        Assert.IsNotNull(component.Find("button.bit-bsl-lmb"));
    }

    [TestMethod]
    public void BitBasicListLoadMoreButtonShouldBeOfTypeButton()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        Assert.AreEqual("button", component.Find("button.bit-bsl-lmb").GetAttribute("type"));
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldAppendAPageOnEachClick()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        component.Find("button.bit-bsl-lmb").Click();
        Assert.AreEqual(10, component.FindAll(".row").Count);

        component.Find("button.bit-bsl-lmb").Click();
        Assert.AreEqual(12, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldHideTheButtonOnceEverythingIsLoaded()
    {
        var component = RenderLoadMoreList(5, GetTestData(10));

        component.Find("button.bit-bsl-lmb").Click();

        Assert.AreEqual(10, component.FindAll(".row").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bsl-lmb").Count);
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldRenderItsText()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));
        Assert.AreEqual("Load more", component.Find(".bit-bsl-lmt").TextContent);

        component.Render(p => p.Add(x => x.LoadMoreText, "give me more"));
        Assert.AreEqual("give me more", component.Find(".bit-bsl-lmt").TextContent);
    }

    [TestMethod]
    public void BitBasicListLoadMoreTemplateShouldNotRenderAButtonElement()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.LoadMoreTemplate, (RenderFragment<bool>)(loading => b => b.AddMarkupContent(0, "<button class='inner'>more</button>")));
        });

        var loadMore = component.Find(".bit-bsl-lmb");

        Assert.AreEqual("DIV", loadMore.TagName);
        Assert.AreEqual("button", loadMore.GetAttribute("role"));

        loadMore.Click();

        Assert.AreEqual(10, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListLoadMoreTemplateShouldLoadOnTheKeyboard()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.LoadMoreTemplate, (RenderFragment<bool>)(loading => b => b.AddContent(0, "more")));
        });

        component.Find(".bit-bsl-lmb").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.AreEqual(10, component.FindAll(".row").Count);

        component.Find(".bit-bsl-lmb").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "a" });
        Assert.AreEqual(10, component.FindAll(".row").Count);

        component.Find(".bit-bsl-lmb").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = " " });
        Assert.AreEqual(12, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldStartOverWhenTheItemsChange()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        component.Find("button.bit-bsl-lmb").Click();
        Assert.AreEqual(10, component.FindAll(".row").Count);

        component.Render(p => p.Add(x => x.Items, GetTestData(30)));

        Assert.AreEqual(5, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldRenderTheSentinelOnlyInAutoLoadMode()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        Assert.AreEqual(0, component.FindAll(".bit-bsl-snt").Count);

        component.Render(p => p.Add(x => x.AutoLoad, true));

        Assert.AreEqual(1, component.FindAll(".bit-bsl-snt").Count);
    }

    [TestMethod]
    public void BitBasicListShouldNotRenderTheSentinelOnceEverythingIsLoaded()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.AutoLoad, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(5));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bsl-snt").Count);
    }

    [TestMethod]
    public void BitBasicListShouldStartTheLoadMorePagingWhenLoadMoreIsTurnedOn()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        Assert.AreEqual(12, component.FindAll(".row").Count);

        component.Render(p => p.Add(x => x.LoadMore, true));

        Assert.AreEqual(5, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListShouldShowEveryItemAgainWhenLoadMoreIsTurnedOff()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        Assert.AreEqual(5, component.FindAll(".row").Count);

        component.Render(p => p.Add(x => x.LoadMore, false));

        Assert.AreEqual(12, component.FindAll(".row").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bsl-lmb").Count);
    }

    [TestMethod]
    public void BitBasicListShouldRepageWhenTheLoadMoreSizeChanges()
    {
        var component = RenderLoadMoreList(5, GetTestData(12));

        Assert.AreEqual(5, component.FindAll(".row").Count);

        component.Render(p => p.Add(x => x.LoadMoreSize, 8));

        Assert.AreEqual(8, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListShouldFallBackToItsItemsWhenTheProviderIsTakenAway()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.Items, GetTestData(3));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.ItemsProvider, CreateProvider(GetTestData(7)));
        });

        Assert.AreEqual(7, component.FindAll(".row").Count);

        component.Render(p => p.Add(x => x.ItemsProvider, (BitBasicListItemsProvider<Person>?)null));

        Assert.AreEqual(3, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListLoadMoreShouldNotLoadWhileDisabled()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.IsEnabled, false);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        // The first page is still what a disabled list shows; only loading further ones is turned down.
        Assert.AreEqual(5, component.FindAll(".row").Count);
        Assert.IsTrue(component.Find("button.bit-bsl-lmb").HasAttribute("disabled"));

        component.Find("button.bit-bsl-lmb").Click();

        Assert.AreEqual(5, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListShouldMarkItselfBusyWhileLoading()
    {
        var component = RenderList(p => p.Add(x => x.Loading, true));

        Assert.AreEqual("true", component.Find(".bit-bsl").GetAttribute("aria-busy"));

        component.Render(p => p.Add(x => x.Loading, false));

        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task BitBasicListLoadMoreShouldPageThroughTheItemsProvider()
    {
        var requests = new List<(int StartIndex, int Count)>();

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 4);
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.ItemsProvider, CreateProvider(GetTestData(10), requests));
        });

        Assert.AreEqual(4, component.FindAll(".row").Count);
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual((0, 4), requests[0]);

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());
        Assert.AreEqual(8, component.FindAll(".row").Count);
        Assert.AreEqual((4, 4), requests[1]);

        // The last page is shorter than the one asked for, so it is recognized as the last one without
        // an extra round trip that would have come back empty.
        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());
        Assert.AreEqual(10, component.FindAll(".row").Count);
        Assert.AreEqual(3, requests.Count);
        Assert.AreEqual(0, component.FindAll(".bit-bsl-lmb").Count);
    }

    [TestMethod]
    public async Task BitBasicListLoadMoreShouldInvokeOnLoadMoreWithTheLoadedCount()
    {
        var counts = new List<int>();

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 4);
            p.Add(x => x.Items, GetTestData(10));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.OnLoadMore, EventCallback.Factory.Create<int>(this, c => counts.Add(c)));
        });

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        CollectionAssert.AreEqual(new[] { 4, 8 }, counts);
    }

    [TestMethod]
    public async Task BitBasicListLoadMoreShouldInvokeOnLoadingChange()
    {
        var states = new List<bool>();

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 4);
            p.Add(x => x.Items, GetTestData(10));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.OnLoadingChange, EventCallback.Factory.Create<bool>(this, s => states.Add(s)));
        });

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        CollectionAssert.AreEqual(new[] { true, false, true, false }, states);
    }

    [TestMethod]
    public void BitBasicListShouldLoadTheWholeSetFromTheProviderWithoutVirtualizeAndLoadMore()
    {
        var requests = new List<(int StartIndex, int Count)>();

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.ItemsProvider, CreateProvider(GetTestData(7), requests));
        });

        Assert.AreEqual(7, component.FindAll(".row").Count);
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual((0, int.MaxValue), requests[0]);
    }

    [TestMethod]
    public void BitBasicListShouldPreferTheItemsProviderOverItems()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.Items, GetTestData(3));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.ItemsProvider, CreateProvider(GetTestData(7)));
        });

        Assert.AreEqual(7, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListRefreshDataShouldPickUpAMutatedItemsCollection()
    {
        var items = GetTestData(3);

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.Items, items);
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        Assert.AreEqual(3, component.FindAll(".row").Count);

        items.Add(new Person { Id = 4, FirstName = "Person 4" });

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual(4, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListRefreshDataShouldStartTheLoadMorePagingOver()
    {
        var component = RenderLoadMoreList(5, GetTestData(20));

        component.Find("button.bit-bsl-lmb").Click();
        Assert.AreEqual(10, component.FindAll(".row").Count);

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual(5, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListRefreshDataShouldRefetchTheWholeProviderSet()
    {
        var requests = new List<(int StartIndex, int Count)>();

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.ItemsProvider, CreateProvider(GetTestData(7), requests));
        });

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual(2, requests.Count);
        Assert.AreEqual(7, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListRefreshDataShouldNotThrowWithoutAProvider()
    {
        var component = RenderList();

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());
    }

    [TestMethod]
    public async Task BitBasicListScrollMethodsShouldNotThrow()
    {
        var component = RenderList();

        await component.InvokeAsync(() => component.Instance.ScrollToStartAsync());
        await component.InvokeAsync(() => component.Instance.ScrollToEndAsync(true));
        await component.InvokeAsync(() => component.Instance.ScrollToOffsetAsync(120));
        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(3));
        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(-1));
    }

    [TestMethod]
    public async Task BitBasicListLoadMoreAsyncShouldBeANoOpOutsideOfLoadMoreMode()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.Items, GetTestData(10));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        await component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        Assert.AreEqual(10, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListAutoLoadShouldLoadThePageTheSentinelAsksFor()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.AutoLoad, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        Assert.AreEqual(5, component.FindAll(".row").Count);

        await component.Instance._OnIntersect();
        Assert.AreEqual(10, component.FindAll(".row").Count);

        await component.Instance._OnIntersect();
        Assert.AreEqual(12, component.FindAll(".row").Count);

        // Everything is loaded, so the sentinel is gone and a further callback is turned down.
        Assert.AreEqual(0, component.FindAll(".bit-bsl-snt").Count);
        await component.Instance._OnIntersect();
        Assert.AreEqual(12, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListAutoLoadShouldNotLoadWithoutTheLoadMoreMode()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.AutoLoad, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        await component.Instance._OnIntersect();

        Assert.AreEqual(12, component.FindAll(".row").Count);
    }

    [TestMethod]
    public async Task BitBasicListDisposeShouldNotThrow()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicList<int>>(p =>
        {
            p.Add(x => x.Items, new List<int> { 1, 2, 3 });
            p.Add(x => x.RowTemplate, (RenderFragment<int>)(item =>
                builder => builder.AddContent(0, $"Item {item}")));
        });

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitBasicListDisposeDuringLoadMoreShouldNotThrow()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var tcs = new TaskCompletionSource<BitBasicListItemsProviderResult<int>>();

        var component = RenderComponent<BitBasicList<int>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.ItemsProvider, _ => new ValueTask<BitBasicListItemsProviderResult<int>>(tcs.Task));
            p.Add(x => x.RowTemplate, (RenderFragment<int>)(item =>
                builder => builder.AddContent(0, $"Item {item}")));
        });

        await component.Instance.DisposeAsync();

        tcs.TrySetCanceled();
    }

    [TestMethod]
    public async Task BitBasicListDisposeInAutoLoadModeShouldNotThrow()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.AutoLoad, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(20));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        await component.Instance.DisposeAsync();
    }



    private static readonly RenderFragment<Person> RowTemplate =
        item => builder => builder.AddMarkupContent(0, $"<div class=\"row\">{item.FirstName}</div>");

    private IRenderedComponent<BitBasicList<Person>> RenderList(
        Action<ComponentParameterCollectionBuilder<BitBasicList<Person>>>? extra = null)
    {
        return RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.Items, GetTestData(5));
            p.Add(x => x.RowTemplate, RowTemplate);
            extra?.Invoke(p);
        });
    }

    private IRenderedComponent<BitBasicList<Person>> RenderLoadMoreList(int loadMoreSize, List<Person> items)
    {
        return RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, loadMoreSize);
            p.Add(x => x.Items, items);
            p.Add(x => x.RowTemplate, RowTemplate);
        });
    }

    private static BitBasicListItemsProvider<Person> CreateProvider(
        List<Person> source, List<(int StartIndex, int Count)>? requests = null)
    {
        return request =>
        {
            requests?.Add((request.StartIndex, request.Count));

            var page = source.Skip(request.StartIndex).Take(request.Count).ToArray();

            return ValueTask.FromResult(BitBasicListItemsProviderResult.From<Person>(page, source.Count));
        };
    }

    private static List<Person> GetTestData(int itemCount)
    {
        List<Person> people = new();
        for (int i = 0; i < itemCount; i++)
        {
            people.Add(new Person
            {
                Id = i + 1,
                FirstName = $"Person {i + 1.ToString()}",
                LastName = $"Person Family {i + 1.ToString()}",
                Job = $"Programmer {i + 1.ToString()}"
            });
        }
        return people;
    }
}
