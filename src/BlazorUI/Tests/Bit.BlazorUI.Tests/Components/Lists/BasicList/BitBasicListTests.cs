using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

#if NET10_0_OR_GREATER
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

        var list = component.Find(".bit-bsl-itm");

        var listRole = list.GetAttribute("role");

        Assert.AreEqual(role.HasValue() ? role : "list", listRole);
    }

    [TestMethod]
    public void BitBasicListShouldNotRenderRoleWhenItIsNull()
    {
        var component = RenderList(p => p.Add(x => x.Role, (string?)null));

        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("role"));
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
                Items = "items-class",
                LoadMoreButton = "lmb-class",
                LoadMoreText = "lmt-class",
            });
            p.Add(x => x.Styles, new BitBasicListClassStyles
            {
                Root = "background: red;",
                Header = "background: green;",
                Footer = "background: blue;",
                Items = "display: grid;",
                LoadMoreButton = "background: yellow;",
                LoadMoreText = "background: pink;",
            });
        });

        Assert.IsTrue(component.Find(".bit-bsl").ClassList.Contains("root-class"));
        Assert.IsTrue(component.Find(".bit-bsl-hdr").ClassList.Contains("header-class"));
        Assert.IsTrue(component.Find(".bit-bsl-ftr").ClassList.Contains("footer-class"));
        Assert.IsTrue(component.Find(".bit-bsl-itm").ClassList.Contains("items-class"));
        Assert.IsTrue(component.Find(".bit-bsl-lmb").ClassList.Contains("lmb-class"));
        Assert.IsTrue(component.Find(".bit-bsl-lmt").ClassList.Contains("lmt-class"));

        StringAssert.Contains(component.Find(".bit-bsl").GetAttribute("style"), "background: red;");
        StringAssert.Contains(component.Find(".bit-bsl-hdr").GetAttribute("style"), "background: green;");
        StringAssert.Contains(component.Find(".bit-bsl-ftr").GetAttribute("style"), "background: blue;");
        StringAssert.Contains(component.Find(".bit-bsl-itm").GetAttribute("style"), "display: grid;");
        StringAssert.Contains(component.Find(".bit-bsl-lmb").GetAttribute("style"), "background: yellow;");
        StringAssert.Contains(component.Find(".bit-bsl-lmt").GetAttribute("style"), "background: pink;");
    }

    [TestMethod]
    public void BitBasicListShouldRenderAriaLabel()
    {
        var component = RenderList(p => p.Add(x => x.AriaLabel, "the people"));

        // The name belongs to the list, which is the element holding the rows, not the scrolling root.
        Assert.AreEqual("the people", component.Find(".bit-bsl-itm").GetAttribute("aria-label"));
        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("aria-label"));
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

        // The live region sits where the loading content shows, empty (and taking no room) until then.
        Assert.IsTrue(children[1].ClassList.Contains("bit-bsl-sts"));
        Assert.AreEqual(string.Empty, children[1].TextContent.Trim());

        Assert.IsTrue(children[2].ClassList.Contains("bit-bsl-itm"));
        Assert.IsTrue(children[2].Children[0].ClassList.Contains("row"));

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

        var loading = component.Find(".bit-bsl-ldc");

        // The loading content is announced by the live region that is always there and holds it, not by a
        // status of its own, which would be inserted along with its text and skipped by several screen readers.
        Assert.IsFalse(loading.HasAttribute("role"));
        Assert.IsTrue(loading.ParentElement!.ClassList.Contains("bit-bsl-sts"));
        Assert.AreEqual("status", loading.ParentElement.GetAttribute("role"));
        Assert.AreEqual("true", loading.QuerySelector(".bit-bsl-spn")!.GetAttribute("aria-hidden"));
        Assert.AreEqual("Loading...", loading.TextContent.Trim());
        Assert.AreEqual("Loading...", component.Find(".bit-bsl-sts").TextContent.Trim());

        component.Render(p => p.Add(x => x.LoadingLabel, "Fetching"));

        Assert.AreEqual("Fetching", component.Find(".bit-bsl-ldc").TextContent.Trim());
        Assert.AreEqual("Fetching", component.Find(".bit-bsl-sts").TextContent.Trim());
    }

    [TestMethod]
    public void BitBasicListShouldLeaveTheRoleOffWhileEmpty()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string>());
        });

        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("role"));

        component.Render(p => p.Add(x => x.Loading, true));

        // A loading list shows the status of its loading content in place of the list altogether.
        Assert.AreEqual(0, component.FindAll(".bit-bsl-itm").Count);
        Assert.AreEqual(1, component.FindAll(".bit-bsl-ldc").Count);

        component.Render(p =>
        {
            p.Add(x => x.Loading, false);
            p.Add(x => x.Items, new List<string> { "one" });
        });

        Assert.AreEqual("list", component.Find(".bit-bsl-itm").GetAttribute("role"));
    }

    [TestMethod]
    public void BitBasicListShouldKeepEverythingButTheRowsOutOfTheList()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 2);
            p.Add(x => x.Items, new List<string> { "one", "two", "three" });
            p.Add(x => x.HeaderTemplate, (RenderFragment)(b => b.AddContent(0, "header")));
            p.Add(x => x.FooterTemplate, (RenderFragment)(b => b.AddContent(0, "footer")));
        });

        // A list may only own list items, so the header, the footer, the LoadMore button and the live
        // region all sit beside the element carrying the role rather than inside it.
        var list = component.Find("[role=list]");

        Assert.IsTrue(list.ClassList.Contains("bit-bsl-itm"));
        Assert.AreEqual(2, list.Children.Length);
        Assert.IsTrue(list.Children.All(c => c.GetAttribute("role") == "listitem"));

        Assert.IsNull(list.QuerySelector(".bit-bsl-hdr, .bit-bsl-ftr, .bit-bsl-lmb, .bit-bsl-sts"));
    }

    [TestMethod]
    public void BitBasicListDefaultRowsShouldBeListItemsOfTheDefaultRole()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string> { "one", "two" });
        });

        var rows = component.Find(".bit-bsl-itm").Children;

        Assert.AreEqual(2, rows.Length);
        Assert.IsTrue(rows.All(r => r.GetAttribute("role") == "listitem"));

        component.Render(p => p.Add(x => x.Role, "feed"));

        Assert.IsTrue(component.Find(".bit-bsl-itm").Children.All(r => r.HasAttribute("role") is false));
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
    public void BitBasicListLoadMoreTemplateShouldRegisterItsButtonKeysOnce()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.LoadMoreTemplate, (RenderFragment<bool>)(loading => b => b.AddContent(0, "more")));
        });

        // The keys are wired up in the browser, where a key pressed on the wrapper can be told apart from
        // one pressed on a control inside it; the wrapper holds no keyboard handler of its own to fire twice.
        Assert.IsFalse(component.Find(".bit-bsl-lmb").HasAttribute("blazor:onkeydown"));
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Utils.registerButtonKeys"));

        component.Find(".bit-bsl-lmb").Click();
        component.Render();

        Assert.AreEqual(10, component.FindAll(".row").Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Utils.registerButtonKeys"));
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

        // The loading content stands in for the rows and the live region announces it, so neither the root
        // nor a list is marked busy by it.
        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("aria-busy"));
        Assert.AreEqual("Loading...", component.Find(".bit-bsl-sts").TextContent.Trim());

        component.Render(p => p.Add(x => x.Loading, false));

        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("aria-busy"));
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

        await component.InvokeAsync(() => component.Instance._OnIntersect());
        Assert.AreEqual(10, component.FindAll(".row").Count);

        await component.InvokeAsync(() => component.Instance._OnIntersect());
        Assert.AreEqual(12, component.FindAll(".row").Count);

        // Everything is loaded, so the sentinel is gone and a further callback is turned down.
        Assert.AreEqual(0, component.FindAll(".bit-bsl-snt").Count);
        await component.InvokeAsync(() => component.Instance._OnIntersect());
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

        await component.InvokeAsync(() => component.Instance._OnIntersect());

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



    [TestMethod]
    public async Task BitBasicListLoadMoreButtonShouldStayEnabledAndAnnounceWhileLoading()
    {
        var gate = new TaskCompletionSource();
        var source = GetTestData(12);

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.ItemsProvider, async request =>
            {
                if (request.StartIndex > 0) await gate.Task;

                return BitBasicListItemsProviderResult.From<Person>([.. source.Skip(request.StartIndex).Take(request.Count)]);
            });
        });

        Assert.AreEqual(string.Empty, component.Find(".bit-bsl-sts").TextContent.Trim());

        var loading = component.InvokeAsync(() => component.Instance.LoadMoreAsync());

        var button = component.Find("button.bit-bsl-lmb");

        // A disabled button would drop the keyboard focus it holds, so a busy one is only marked busy.
        Assert.IsFalse(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-busy"));
        Assert.AreEqual("Loading...", button.TextContent.Trim());
        // The status says the page is loading, so the button keeps its own name rather than saying it again.
        Assert.AreEqual("Load more", button.GetAttribute("aria-label"));
        Assert.AreEqual("status", component.Find(".bit-bsl-sts").GetAttribute("role"));
        Assert.AreEqual("Loading...", component.Find(".bit-bsl-sts").TextContent.Trim());
        Assert.AreEqual(1, component.FindAll(".bit-bsl-sts .bit-bsl-stt").Count);
        Assert.AreEqual("true", component.Find(".bit-bsl-itm").GetAttribute("aria-busy"));

        // A click while busy is ignored rather than queued.
        button.Click();

        gate.SetResult();
        await loading;

        Assert.AreEqual(10, component.FindAll(".row").Count);
        Assert.AreEqual(string.Empty, component.Find(".bit-bsl-sts").TextContent.Trim());
        Assert.IsFalse(component.Find("button.bit-bsl-lmb").HasAttribute("aria-busy"));
        Assert.IsFalse(component.Find("button.bit-bsl-lmb").HasAttribute("aria-label"));
        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitBasicListShouldKeepTheStatusRegionBeforeTheLoadingStarts()
    {
        var component = RenderList();

        // A live region only announces what is added to it after it was rendered, so it is there, empty,
        // before the list is ever asked to load.
        Assert.AreEqual("status", component.Find(".bit-bsl-sts").GetAttribute("role"));
        Assert.AreEqual(string.Empty, component.Find(".bit-bsl-sts").TextContent.Trim());

        component.Render(p => p.Add(x => x.Loading, true));

        Assert.AreEqual("Loading...", component.Find(".bit-bsl-sts").TextContent.Trim());

        component.Render(p =>
        {
            p.Add(x => x.Loading, true);
            p.Add(x => x.LoadingTemplate, (RenderFragment)(b => b.AddContent(0, "custom")));
        });

        // A template of its own is announced with its own text, which is what is on screen, and not
        // with the label as well.
        Assert.AreEqual("custom", component.Find(".bit-bsl-sts").TextContent.Trim());

        component.Render(p => p.Add(x => x.Loading, false));

        Assert.AreEqual(string.Empty, component.Find(".bit-bsl-sts").TextContent.Trim());
    }

    [TestMethod]
    public void BitBasicListShouldPutTheLoadingContentInAfreshAfterTheFirstRender()
    {
        // Content already in a live region when it is rendered goes unannounced, so a list that starts out
        // loading renders its loading content once more, as a new element, right after its first render.
        var loading = RenderList(p => p.Add(x => x.Loading, true));

        Assert.AreEqual(2, loading.RenderCount);
        Assert.AreEqual(1, loading.FindAll(".bit-bsl-sts .bit-bsl-ldc").Count);

        var loaded = RenderList();

        Assert.AreEqual(1, loaded.RenderCount);
    }

    [TestMethod]
    public void BitBasicListShouldKeepTheAriaLabelWhereverTheRowsHaveNoRoleToName()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.AriaLabel, "the fruits");
            p.Add(x => x.Items, new List<string>());
        });

        // A name is prohibited on an element with no role, so while the rows have none the root carries it,
        // as a group.
        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("aria-label"));
        Assert.AreEqual("group", component.Find(".bit-bsl").GetAttribute("role"));
        Assert.AreEqual("the fruits", component.Find(".bit-bsl").GetAttribute("aria-label"));

        component.Render(p => p.Add(x => x.Items, new List<string> { "apple" }));

        Assert.AreEqual("the fruits", component.Find(".bit-bsl-itm").GetAttribute("aria-label"));
        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("role"));
        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("aria-label"));

        component.Render(p => p.Add(x => x.Loading, true));

        // The loading content stands in for the rows, so the list keeps its name on the root meanwhile.
        Assert.AreEqual("the fruits", component.Find(".bit-bsl").GetAttribute("aria-label"));

        component.Render(p =>
        {
            p.Add(x => x.Loading, false);
            p.Add(x => x.Role, null);
        });

        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("aria-label"));
        Assert.AreEqual("group", component.Find(".bit-bsl").GetAttribute("role"));
        Assert.AreEqual("the fruits", component.Find(".bit-bsl").GetAttribute("aria-label"));

        component.Render(p => p.Add(x => x.Role, "None"));

        Assert.IsFalse(component.Find(".bit-bsl-itm").HasAttribute("aria-label"));
        Assert.AreEqual("the fruits", component.Find(".bit-bsl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBasicListShouldNotGiveTheRootARoleWithoutAnAriaLabel()
    {
        var component = RenderComponent<BitBasicList<string>>(p =>
        {
            p.Add(x => x.Items, new List<string>());
            p.Add(x => x.Role, null);
        });

        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("role"));
        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBasicListShouldHandTheFocusToTheListWhenTheFocusedLoadMoreButtonGoesAway()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.containsActiveElement", _ => true).SetResult(true);

        var component = RenderLoadMoreList(5, GetTestData(10));

        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("tabindex"));

        var button = component.Find("button.bit-bsl-lmb");
        var buttonId = button.Id;

        button.Click();

        Assert.AreEqual(0, component.FindAll(".bit-bsl-lmb").Count);
        Assert.AreEqual("-1", component.Find(".bit-bsl").GetAttribute("tabindex"));
        Context.JSInterop.VerifyFocusAsyncInvoke();

        // The browser is asked about the LoadMore element itself, before it is taken away.
        var asked = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Utils.containsActiveElement");
        Assert.AreEqual(buttonId, asked.Arguments[0]);
    }

    [TestMethod]
    public void BitBasicListShouldNotTakeTheFocusWhenTheLoadMoreButtonWasNotFocused()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.containsActiveElement", _ => true).SetResult(false);

        var component = RenderLoadMoreList(5, GetTestData(10));

        component.Find("button.bit-bsl-lmb").Click();

        Assert.IsFalse(component.Find(".bit-bsl").HasAttribute("tabindex"));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BitBasicListShouldNotAskAboutTheFocusBeforeTheLastPage()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderLoadMoreList(5, GetTestData(20));

        // Focus moving in and out of the LoadMore element is none of the list's business until the element
        // goes away, so no focus event re-renders it and no page but the last asks the browser.
        component.Find("button.bit-bsl-lmb").Click();

        Assert.AreEqual(10, component.FindAll(".row").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Utils.containsActiveElement"));
    }

    [TestMethod]
    public void BitBasicListShouldKeepItsOwnTabIndexWhenHandedTheFocus()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.containsActiveElement", _ => true).SetResult(true);

        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.TabIndex, "0");
            p.Add(x => x.Items, GetTestData(10));
            p.Add(x => x.RowTemplate, RowTemplate);
        });

        component.Find("button.bit-bsl-lmb").Click();

        Assert.AreEqual("0", component.Find(".bit-bsl").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitBasicListLoadMoreTemplateShouldLeaveTheTabOrderWhileDisabled()
    {
        var component = RenderComponent<BitBasicList<Person>>(p =>
        {
            p.Add(x => x.LoadMore, true);
            p.Add(x => x.LoadMoreSize, 5);
            p.Add(x => x.Items, GetTestData(12));
            p.Add(x => x.RowTemplate, RowTemplate);
            p.Add(x => x.LoadMoreTemplate, (RenderFragment<bool>)(loading => b => b.AddContent(0, "more")));
        });

        var loadMore = component.Find(".bit-bsl-lmb");
        Assert.AreEqual("0", loadMore.GetAttribute("tabindex"));
        Assert.IsFalse(loadMore.HasAttribute("aria-disabled"));

        component.Render(p => p.Add(x => x.IsEnabled, false));

        loadMore = component.Find(".bit-bsl-lmb");
        Assert.AreEqual("-1", loadMore.GetAttribute("tabindex"));
        Assert.AreEqual("true", loadMore.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitBasicListShouldUpdateTheRootWhenStylesAndClassesChange()
    {
        var component = RenderList(p =>
        {
            p.Add(x => x.Styles, new BitBasicListClassStyles { Root = "color: red;" });
            p.Add(x => x.Classes, new BitBasicListClassStyles { Root = "first" });
        });

        component.Render(p =>
        {
            p.Add(x => x.Styles, new BitBasicListClassStyles { Root = "color: blue;" });
            p.Add(x => x.Classes, new BitBasicListClassStyles { Root = "second" });
        });

        var root = component.Find(".bit-bsl");

        StringAssert.Contains(root.GetAttribute("style"), "color: blue;");
        Assert.IsTrue(root.ClassList.Contains("second"));
        Assert.IsFalse(root.ClassList.Contains("first"));
    }

    [TestMethod]
    public void BitBasicListParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.BitBasicList", BitBasicListParams.ParamName);

        var @params = new BitBasicListParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitBasicListParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitBasicListShouldApplyCascadingParametersToListsOfAnyItemType()
    {
        var @params = new BitBasicListParams
        {
            Horizontal = true,
            FullWidth = true,
            Role = "feed",
            Class = "cascaded",
        };

        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, [@params]);
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<BitBasicList<Person>>(0);
                builder.AddAttribute(1, nameof(BitBasicList<Person>.Items), GetTestData(3));
                builder.CloseComponent();

                builder.OpenComponent<BitBasicList<string>>(2);
                builder.AddAttribute(3, nameof(BitBasicList<string>.Items), new List<string> { "a", "b" });
                builder.CloseComponent();
            });
        });

        var lists = component.FindAll(".bit-bsl");

        Assert.AreEqual(2, lists.Count);

        foreach (var list in lists)
        {
            Assert.IsTrue(list.ClassList.Contains("bit-bsl-hrz"));
            Assert.IsTrue(list.ClassList.Contains("cascaded"));
            Assert.AreEqual("feed", list.QuerySelector(".bit-bsl-itm")!.GetAttribute("role"));
            StringAssert.Contains(list.GetAttribute("style"), "width:100%");
        }
    }

    [TestMethod]
    public void BitBasicListDirectParametersShouldOverrideCascadingParameters()
    {
        var @params = new BitBasicListParams
        {
            Horizontal = true,
            LoadMoreText = "Cascaded more",
            Role = "feed",
        };

        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, [@params]);
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<BitBasicList<Person>>(0);
                builder.AddAttribute(1, nameof(BitBasicList<Person>.Items), GetTestData(30));
                builder.AddAttribute(2, nameof(BitBasicList<Person>.LoadMore), true);
                builder.AddAttribute(3, nameof(BitBasicList<Person>.Horizontal), false);
                builder.AddAttribute(4, nameof(BitBasicList<Person>.Role), "list");
                builder.CloseComponent();
            });
        });

        var list = component.Find(".bit-bsl");

        Assert.IsFalse(list.ClassList.Contains("bit-bsl-hrz"));
        Assert.AreEqual("list", list.QuerySelector(".bit-bsl-itm")!.GetAttribute("role"));
        Assert.AreEqual("Cascaded more", component.Find(".bit-bsl-lmt").TextContent);
    }

    [TestMethod]
    public void BitBasicListCascadedLoadMoreShouldPageTheItems()
    {
        var @params = new BitBasicListParams
        {
            LoadMore = true,
            LoadMoreSize = 4,
        };

        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, [@params]);
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<BitBasicList<Person>>(0);
                builder.AddAttribute(1, nameof(BitBasicList<Person>.Items), GetTestData(10));
                builder.AddAttribute(2, nameof(BitBasicList<Person>.RowTemplate), RowTemplate);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual(4, component.FindAll(".row").Count);

        component.Find("button.bit-bsl-lmb").Click();

        Assert.AreEqual(8, component.FindAll(".row").Count);
    }

    [TestMethod]
    public void BitBasicListParamsUpdateParametersShouldSetAllProperties()
    {
        var classes = new BitBasicListClassStyles { Root = "custom-root" };
        var styles = new BitBasicListClassStyles { Root = "color: red;" };

        var @params = new BitBasicListParams
        {
            AutoLoad = true,
            AutoLoadThreshold = 120,
            Classes = classes,
            FitHeight = true,
            FitSize = true,
            FitWidth = true,
            FullHeight = true,
            FullSize = true,
            FullWidth = true,
            Horizontal = true,
            ItemSize = 72,
            ItemsProviderDelay = 0,
            LoadingLabel = "Wait",
            LoadMore = true,
            LoadMoreSize = 7,
            LoadMoreText = "More",
            OverscanCount = 9,
            Role = "feed",
            Styles = styles,
            Virtualize = true,
            AriaLabel = "People",
            IsEnabled = false,
            TabIndex = "0",
        };

        var list = new BitBasicList<Person>();

        @params.UpdateParameters(list);

        Assert.IsTrue(list.AutoLoad);
        Assert.AreEqual(120, list.AutoLoadThreshold);
        Assert.AreSame(classes, list.Classes);
        Assert.IsTrue(list.FitHeight);
        Assert.IsTrue(list.FitSize);
        Assert.IsTrue(list.FitWidth);
        Assert.IsTrue(list.FullHeight);
        Assert.IsTrue(list.FullSize);
        Assert.IsTrue(list.FullWidth);
        Assert.IsTrue(list.Horizontal);
        Assert.AreEqual(72f, list.ItemSize);
        Assert.AreEqual(0, list.ItemsProviderDelay);
        Assert.AreEqual("Wait", list.LoadingLabel);
        Assert.IsTrue(list.LoadMore);
        Assert.AreEqual(7, list.LoadMoreSize);
        Assert.AreEqual("More", list.LoadMoreText);
        Assert.AreEqual(9, list.OverscanCount);
        Assert.AreEqual("feed", list.Role);
        Assert.AreSame(styles, list.Styles);
        Assert.IsTrue(list.Virtualize);
        Assert.AreEqual("People", list.AriaLabel);
        Assert.IsFalse(list.IsEnabled);
        Assert.AreEqual("0", list.TabIndex);
    }

    [TestMethod]
    public void BitBasicListParamsShouldCoverEveryGroupParameterOfTheList()
    {
        // Everything that belongs to a single list rather than to a group of them is left out on purpose.
        var excluded = new HashSet<string>
        {
            nameof(BitBasicList<Person>.CascadingParameters),
            nameof(BitBasicList<Person>.EmptyContent),
            nameof(BitBasicList<Person>.FooterTemplate),
            nameof(BitBasicList<Person>.HeaderTemplate),
            nameof(BitBasicList<Person>.Items),
            nameof(BitBasicList<Person>.ItemsProvider),
            nameof(BitBasicList<Person>.Loading),
            nameof(BitBasicList<Person>.LoadingTemplate),
            nameof(BitBasicList<Person>.LoadMoreTemplate),
            nameof(BitBasicList<Person>.OnLoadingChange),
            nameof(BitBasicList<Person>.OnLoadMore),
            nameof(BitBasicList<Person>.RowTemplate),
            nameof(BitBasicList<Person>.VirtualizePlaceholder),
        };

        const BindingFlags declaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var listParameters = typeof(BitBasicList<Person>)
            .GetProperties(declaredPublic)
            .Where(p => p.IsDefined(typeof(ParameterAttribute), true) || p.IsDefined(typeof(CascadingParameterAttribute), true))
            .Select(p => p.Name)
            .Where(n => excluded.Contains(n) is false)
            .ToList();

        var paramsProperties = typeof(BitBasicListParams)
            .GetProperties(declaredPublic)
            .Select(p => p.Name)
            .Where(n => n != nameof(BitBasicListParams.Name))
            .ToList();

        CollectionAssert.AreEquivalent(listParameters, paramsProperties);
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
                FirstName = $"Person {i + 1}",
                LastName = $"Person Family {i + 1}",
                Job = $"Programmer {i + 1}"
            });
        }
        return people;
    }
}
