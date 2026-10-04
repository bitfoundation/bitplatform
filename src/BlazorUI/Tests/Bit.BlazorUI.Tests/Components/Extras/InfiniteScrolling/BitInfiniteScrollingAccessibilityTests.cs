using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.InfiniteScrolling;

/// <summary>
/// Covers what assistive technology is told about the list: the Feed mode (the feed role, its articles and their
/// place in the set, the focus a button-loaded page receives), the role a named list takes, the announcement of a
/// loaded page, and the default loading block, whose spinner is only drawn.
/// </summary>
[TestClass]
public class BitInfiniteScrollingAccessibilityTests : BunitTestContext
{
    private static RenderFragment<int> ItemTemplate() => item => builder => builder.AddContent(0, $"Item {item}");

    private static BitInfiniteScrollingItemsProvider<int> PagedProvider(int total)
    {
        return request =>
        {
            var count = Math.Clamp(total - request.Skip, 0, request.Count);

            return ValueTask.FromResult<IEnumerable<int>>(Enumerable.Range(request.Skip, count).ToList());
        };
    }

    private static BitInfiniteScrollingItemsProvider<int> ResultProvider(int total)
    {
        return request =>
        {
            var count = Math.Clamp(total - request.Skip, 0, request.Count);

            var result = new BitInfiniteScrollingItemsProviderResult<int>(Enumerable.Range(request.Skip, count).ToList(),
                                                                          request.Skip + count < total,
                                                                          total);

            return ValueTask.FromResult<IEnumerable<int>>(result);
        };
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldRenderAFeedOfFocusableArticles()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Feed, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll("article").Count));

        Assert.AreEqual("feed", component.Find(".bit-isc > .bit-isc-fed").GetAttribute("role"));
        Assert.AreEqual(5, component.FindAll(".bit-isc-fed > article").Count);

        var articles = component.FindAll("article");

        for (var i = 0; i < articles.Count; i++)
        {
            Assert.IsTrue(articles[i].ClassList.Contains("bit-isc-art"));
            Assert.AreEqual("0", articles[i].GetAttribute("tabindex"));
            Assert.AreEqual((i + 1).ToString(), articles[i].GetAttribute("aria-posinset"));
            // The size of the set is unknown while more of it can still arrive.
            Assert.AreEqual("-1", articles[i].GetAttribute("aria-setsize"));
            Assert.AreEqual($"Item {i}", articles[i].TextContent.Trim());
        }
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldOwnNothingButItsArticles()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.Feed, true);
            parameters.Add(p => p.AriaLabel, "Products");
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll("article").Count));

        var root = component.Find(".bit-isc");
        var feed = component.Find(".bit-isc-fed");

        // The AriaLabel names the feed, and the root - a generic again - is left without a name of its own.
        Assert.AreEqual("Products", feed.GetAttribute("aria-label"));
        Assert.IsNull(root.GetAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-label"));

        // The button sits beside the feed, never inside it, where ARIA expects nothing but articles.
        Assert.IsTrue(feed.Children.All(c => c.TagName == "ARTICLE"));
        Assert.AreEqual(0, feed.QuerySelectorAll(".bit-isc-btn").Length);
        Assert.AreEqual(1, component.FindAll(".bit-isc > .bit-isc-btn").Count);
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldNameEachArticleWithItemAriaLabel()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Feed, true);
            parameters.Add(p => p.ItemKey, item => item);
            parameters.Add(p => p.ItemAriaLabel, item => item == 1 ? null : $"Post {item}");
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("article").Count));

        var articles = component.FindAll("article");

        Assert.AreEqual("Post 0", articles[0].GetAttribute("aria-label"));
        // An item the function has no name for keeps the name its content gives it.
        Assert.IsFalse(articles[1].HasAttribute("aria-label"));
        Assert.AreEqual("Post 2", articles[2].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedArticlesShouldHaveNoNameOfTheirOwnByDefault()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Feed, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("article").Count));

        Assert.IsTrue(component.FindAll("article").All(a => a.HasAttribute("aria-label") is false));
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldKnowTheSizeOfTheSetOnceItHasEnded()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Feed, true);
        });

        component.WaitForAssertion(() => Assert.IsFalse(component.Instance.HasMore));

        Assert.IsTrue(component.FindAll("article").All(a => a.GetAttribute("aria-setsize") == "3"));
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldTakeTheSizeOfTheSetFromTheProviderResult()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, ResultProvider(50));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Feed, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll("article").Count));

        Assert.IsTrue(component.FindAll("article").All(a => a.GetAttribute("aria-setsize") == "50"));
    }

    [TestMethod]
    public void BitInfiniteScrollingReversedFeedShouldPlaceItsItemsAtTheEndOfAKnownSet()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, ResultProvider(50));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Feed, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll("article").Count));

        // A reversed list holds the newest items, so the five it has are the last five of the fifty.
        CollectionAssert.AreEqual(new[] { "46", "47", "48", "49", "50" },
                                  component.FindAll("article").Select(a => a.GetAttribute("aria-posinset")).ToArray());
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldKeyItsArticles()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Feed, true);
            parameters.Add(p => p.ItemKey, i => i);
            parameters.Add(p => p.Classes, new() { Item = "own-item" });
            parameters.Add(p => p.Styles, new() { Item = "padding:1px" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("article").Count));

        // The article is the element that carries the key, so no second wrapper is rendered for it.
        Assert.AreEqual(0, component.FindAll(".bit-isc-itm").Count);
        Assert.IsTrue(component.FindAll("article").All(a => a.ClassList.Contains("own-item") && a.GetAttribute("style") == "padding:1px"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldStyleTheWrapperOfAKeyedItem()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.ItemKey, i => i);
            parameters.Add(p => p.Classes, new() { Item = "own-item" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-isc-itm.own-item").Count));

        Assert.AreEqual(0, component.FindAll("article").Count);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldPassTheFeedFlagToJs()
    {
        RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Feed, true);
        });

        Assert.AreEqual(true, Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.setup").Arguments[8]);
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedShouldFocusTheFirstArticleOfAPageItsButtonLoaded()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.Feed, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));

        component.Find(".bit-isc-btn").Click();

        component.WaitForAssertion(() =>
        {
            var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.InfiniteScrolling.focusItem");

            Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[0]);
            Assert.AreEqual(5, invocation.Arguments[1]);
        });
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotMoveTheFocusOutsideTheFeedMode()
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

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.InfiniteScrolling.focusItem");
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldBeAGroupOnlyWhenItIsNamed()
    {
        var unnamed = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
        });

        Assert.IsNull(unnamed.Find(".bit-isc").GetAttribute("role"));

        var named = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.AriaLabel, "Products");
        });

        Assert.AreEqual("group", named.Find(".bit-isc").GetAttribute("role"));
        Assert.AreEqual("Products", named.Find(".bit-isc").GetAttribute("aria-label"));

        var labelled = Context.Render(builder =>
        {
            builder.OpenComponent<BitInfiniteScrolling<int>>(0);
            builder.AddAttribute(1, nameof(BitInfiniteScrolling<int>.ItemsProvider), PagedProvider(3));
            builder.AddAttribute(2, "aria-labelledby", "heading");
            builder.CloseComponent();
        });

        Assert.AreEqual("group", labelled.Find(".bit-isc").GetAttribute("role"));
        Assert.AreEqual("heading", labelled.Find(".bit-isc").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldLetAPlainRoleAndNameWin()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitInfiniteScrolling<int>>(0);
            builder.AddAttribute(1, nameof(BitInfiniteScrolling<int>.ItemsProvider), PagedProvider(3));
            builder.AddAttribute(2, "role", "list");
            builder.AddAttribute(3, "aria-label", "Products");
            builder.CloseComponent();
        });

        Assert.AreEqual("list", component.Find(".bit-isc").GetAttribute("role"));
        Assert.AreEqual("Products", component.Find(".bit-isc").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAnnounceEachLoadedPage()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.LoadedMessage, "{0} more items, {1} in all");
        });

        component.WaitForAssertion(() => Assert.AreEqual("5 more items, 5 in all", component.Find(".bit-isc-sts").TextContent));

        component.Find(".bit-isc-btn").Click();

        component.WaitForAssertion(() => Assert.AreEqual("5 more items, 10 in all", component.Find(".bit-isc-sts").TextContent));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAnnounceTheLastPageAlongWithTheEnd()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(3));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.LoadedMessage, "{0} items loaded.");
            parameters.Add(p => p.EndMessage, "The end.");
        });

        component.WaitForAssertion(() => Assert.AreEqual("3 items loaded. The end.", component.Find(".bit-isc-sts").TextContent));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAnnounceAnInvalidLoadedMessageAsWritten()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.LoadedMessage, "{2} loaded {");
        });

        component.WaitForAssertion(() => Assert.AreEqual("{2} loaded {", component.Find(".bit-isc-sts").TextContent));
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAnnounceNothingForALoadedPageWithoutALoadedMessage()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Preload, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.Items.Count));

        Assert.AreEqual(string.Empty, component.Find(".bit-isc-sts").TextContent);
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldNotReannounceALoadedPageAfterAnEdit()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 5);
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.LoadedMessage, "{0} more items, {1} in all");
        });

        component.WaitForAssertion(() => Assert.AreEqual("5 more items, 5 in all", component.Find(".bit-isc-sts").TextContent));

        await component.InvokeAsync(() => component.Instance.AppendItemsAsync([100]));

        // The edit is the last thing that happened to the list, so the page before it is not announced again.
        Assert.AreEqual(6, component.Instance.Items.Count);
        Assert.AreEqual("", component.Find(".bit-isc-sts").TextContent);
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldNotReannounceALoadedPageWhenAnEditReopensTheList()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(20));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.MaxItems, 10);
            parameters.Add(p => p.Manual, true);
            parameters.Add(p => p.LoadedMessage, "{0} more items loaded.");
            parameters.Add(p => p.EndMessage, "No more items.");
        });

        component.WaitForAssertion(() => Assert.AreEqual("10 more items loaded. No more items.", component.Find(".bit-isc-sts").TextContent));

        await component.InvokeAsync(() => component.Instance.RemoveItemAsync(0));

        // The removal reopens the capped list, so the end message goes - and nothing was fetched to announce.
        Assert.IsTrue(component.Instance.HasMore);
        Assert.AreEqual("", component.Find(".bit-isc-sts").TextContent);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAnnounceTheErrorMessageWhenAnErrorTemplateReplacesTheAlert()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => throw new InvalidOperationException("boom"));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.ErrorMessage, "Could not load.");
            parameters.Add(p => p.ErrorTemplate, error => builder => builder.AddMarkupContent(0, $"<p class=\"own-error\">{error.Message}</p>"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".own-error").Count));

        Assert.AreEqual(0, component.FindAll("[role=alert]").Count);
        Assert.AreEqual("Could not load.", component.Find(".bit-isc-sts").TextContent);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotRepeatTheDefaultErrorAlertInTheLiveRegion()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => throw new InvalidOperationException("boom"));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.ErrorMessage, "Could not load.");
        });

        component.WaitForAssertion(() => Assert.AreEqual("Could not load.", component.Find("[role=alert]").TextContent.Trim()));

        Assert.AreEqual(string.Empty, component.Find(".bit-isc-sts").TextContent);
    }

    [TestMethod]
    public void BitInfiniteScrollingLiveRegionShouldFollowTheDirectionOfTheList()
    {
        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, PagedProvider(5));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", component.Find(".bit-isc-sts").GetAttribute("dir"));
    }

    [TestMethod]
    public async Task BitInfiniteScrollingShouldRenderAHiddenSpinnerInTheDefaultLoadingBlock()
    {
        var source = new TaskCompletionSource<IEnumerable<int>>();

        var component = RenderComponent<BitInfiniteScrolling<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => new ValueTask<IEnumerable<int>>(source.Task));
            parameters.Add(p => p.ItemTemplate, ItemTemplate());
            parameters.Add(p => p.Preload, true);
            parameters.Add(p => p.Classes, new() { Spinner = "own-spinner" });
            parameters.Add(p => p.Styles, new() { Spinner = "width:2rem" });
        });

        var spinner = component.Find(".bit-isc-ldg .bit-isc-spn");

        Assert.AreEqual("true", spinner.GetAttribute("aria-hidden"));
        Assert.IsTrue(spinner.ClassList.Contains("own-spinner"));
        Assert.AreEqual("width:2rem", spinner.GetAttribute("style"));
        Assert.AreEqual("Loading...", component.Find(".bit-isc-ldg").TextContent.Trim());

        source.SetResult([]);

        await Task.CompletedTask;
    }
}
