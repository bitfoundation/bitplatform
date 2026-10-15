namespace Bit.BlazorUI.Tests.E2E.Lists;

/// <summary>
/// BitBasicList in a real browser: Items with a RowTemplate and the default template, EmptyContent (items, provider and
/// virtualized), the header and footer inside the scrolling region, Loading / LoadingLabel / LoadingTemplate and
/// OnLoadingChange around a held ItemsProvider, Virtualize with ItemSize and OverscanCount scrolled with the real mouse
/// wheel, a virtualized ItemsProvider with its placeholders, its requests and ItemsProviderDelay, LoadMore with
/// LoadMoreSize / LoadMoreText / LoadMoreTemplate (and its keyboard activation) / OnLoadMore, LoadMore with Virtualize,
/// AutoLoad while scrolling the real list and AutoLoadThreshold, Horizontal and Styles.Items, the Fit / Full sizes, the
/// public methods (ScrollToStartAsync, ScrollToEndAsync, ScrollToOffsetAsync, ScrollToIndexAsync plain and virtualized,
/// RefreshDataAsync, LoadMoreAsync), the accessibility contract (Role, AriaLabel, aria-busy, the status region, TabIndex
/// and the focus handed to the list after the last page), the BitParams cascade, Style / Class / Styles / Classes /
/// Visibility, Disabled (the LoadMore element and its guard), the --bit-BasicList-* variables, RTL, prerendering and
/// unmounting a list with a live AutoLoad observer.
/// </summary>
[TestClass]
public class BitBasicListE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    [TestMethod]
    public async Task Prerender_TheRowsAreInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/basic-list");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"basic\"");
        StringAssert.Contains(html, "Name: <strong>Person 10</strong>");
        StringAssert.Contains(html, "bit-bsl-itm");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_EveryItemIsRenderedThroughTheRowTemplate_InAScrollingList()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#basic");
        await Expect(list).ToHaveClassAsync(HasClass("bit-bsl"));
        await Expect(list).ToHaveCSSAsync("overflow-y", "auto");
        await Expect(list).ToHaveCSSAsync("height", "500px");
        await Expect(list.Locator("> .bit-bsl-itm")).ToHaveAttributeAsync("role", "list");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
        await Expect(list.GetByRole(AriaRole.Listitem).Nth(9)).ToHaveTextAsync("Name: Person 10");

        // Without a template each item is its own text, in a list item.
        var fruits = Page.Locator("#default-template").GetByRole(AriaRole.Listitem);
        await Expect(fruits).ToHaveTextAsync(new[] { "Apple", "Banana", "Cherry", "Date" });
    }

    [TestMethod]
    public async Task EmptyContent_ReplacesTheRowsInEveryMode()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#empty-content")).ToBeVisibleAsync();
        // An empty list is no list: the role is left off and the name moves to the root, as a group.
        Assert.IsNull(await Page.Locator("#empty > .bit-bsl-itm").GetAttributeAsync("role"));
        await Expect(Page.Locator("#empty")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.Locator("#empty")).ToHaveAttributeAsync("aria-label", "Nobody");

        await Expect(Page.Locator("#empty-provider-content")).ToBeVisibleAsync();
        await Expect(Page.Locator("#empty-virtualize-content")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task HeaderAndFooter_RenderInsideTheScrollingRegion()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#header-footer");
        var children = await list.Locator("> *").EvaluateAllAsync<string[]>("els => els.map(e => e.className)");
        StringAssert.StartsWith(children[0], "bit-bsl-hdr");
        StringAssert.StartsWith(children[^1], "bit-bsl-ftr");
        await Expect(Page.Locator("#list-header")).ToHaveTextAsync("People (10)");

        // Both scroll with the rows: the footer comes into view at the end.
        var root = await BoxAsync("#header-footer");
        Assert.IsTrue((await BoxAsync("#list-footer")).Y > root.Bottom, "the footer starts below the visible rows");
        await ScrollAsync("#header-footer", 2000);
        root = await BoxAsync("#header-footer");
        var footer = await BoxAsync("#list-footer");
        Assert.IsTrue(footer.Bottom <= root.Bottom + Tolerance && footer.Y >= root.Y, "the footer scrolled in at the end");

        // A header made sticky through Styles.Header stays at the top of the scrolled list.
        await ScrollAsync("#header-sticky-part", 2000);
        AssertClose((await BoxAsync("#header-sticky-part")).Y + 1, (await BoxAsync("#sticky-part-header")).Y, "the sticky header part stays in view");
    }

    [TestMethod]
    public async Task StickyHeaderAndStickyFooter_KeepTheTemplatesInViewWhileTheRowsScroll()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#sticky-ends");
        await Expect(list).ToHaveClassAsync(HasClass("bit-bsl-shd"));
        await Expect(list).ToHaveClassAsync(HasClass("bit-bsl-sft"));
        await list.ScrollIntoViewIfNeededAsync();

        // The footer is pinned to the bottom of the list from the start, ahead of the rows it comes after.
        var root = await BoxAsync("#sticky-ends");
        AssertClose(root.Bottom - 1, (await BoxAsync("#sticky-ends-footer")).Bottom, "the sticky footer sits at the bottom of the list");
        AssertClose(root.Y + 1, (await BoxAsync("#sticky-ends-header")).Y, "the header starts at the top");

        // Scrolled to the middle, both stay in view, and over the (positioned) rows passing underneath them.
        await ScrollAsync("#sticky-ends", 120);
        root = await BoxAsync("#sticky-ends");
        var header = await BoxAsync("#sticky-ends-header");
        var footer = await BoxAsync("#sticky-ends-footer");
        AssertClose(root.Y + 1, header.Y, "the sticky header stays in view");
        AssertClose(root.Bottom - 1, footer.Bottom, "the sticky footer stays in view");
        Assert.AreEqual("sticky-ends-header", await Page.EvaluateAsync<string>("([x, y]) => document.elementFromPoint(x, y).id", new[] { header.X + 5, header.Y + 5 }), "the header is painted over the rows");
        Assert.AreEqual("sticky-ends-footer", await Page.EvaluateAsync<string>("([x, y]) => document.elementFromPoint(x, y).id", new[] { footer.X + 5, footer.Bottom - 5 }), "the footer is painted over the rows");

        // A horizontal list pins them to the two ends of its strip instead.
        var strip = Page.Locator("#sticky-ends-horizontal");
        await strip.ScrollIntoViewIfNeededAsync();
        await strip.EvaluateAsync("el => el.scrollLeft = 200");
        await Expect(strip).ToHaveJSPropertyAsync("scrollLeft", 200);
        var box = await BoxAsync("#sticky-ends-horizontal");
        AssertClose(box.X + 1, (await BoxAsync("#sticky-ends-horizontal-header")).X, "the sticky header stays at the inline start");
        AssertClose(box.Right - 1, (await BoxAsync("#sticky-ends-horizontal-footer")).Right, "the sticky footer stays at the inline end");
    }

    [TestMethod]
    public async Task Loading_SwapsTheRowsForTheLoadingContent_AnnouncedAsAStatus()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#loading").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);

        await Page.ClickAsync("#btn-loading");

        var status = Page.Locator("#loading > [role=status]");
        await Expect(status.Locator(".bit-bsl-ldc")).ToBeVisibleAsync();
        await Expect(status.Locator(".bit-bsl-ldt")).ToHaveTextAsync("Fetching people...");
        await Expect(status.Locator(".bit-bsl-spn")).ToBeVisibleAsync();
        await Expect(Page.Locator("#loading > .bit-bsl-itm")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#loading-template [role=status] #custom-loading")).ToHaveTextAsync("Custom loading template");
        await Expect(Page.Locator("#loading-template .bit-bsl-spn")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-loading");
        await Expect(Page.Locator("#loading").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
        await Expect(status.Locator(".bit-bsl-ldc")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ItemsProvider_WithoutVirtualize_FetchesTheWholeSetOnce_AndReportsTheLoadingState()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#all-request")).ToHaveTextAsync($"0:{int.MaxValue}");
        await Expect(Page.Locator("#provider-all .bit-bsl-ldc")).ToBeVisibleAsync();
        await Expect(Page.Locator("#loading-changes")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-release-all");

        await Expect(Page.Locator("#provider-all").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(5);
        await Expect(Page.Locator("#loading-changes")).ToHaveTextAsync("True,False");
        await Expect(Page.Locator("#provider-all .bit-bsl-ldc")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Virtualize_RendersOnlyTheRowsAroundTheVisibleRegion_WhileScrollingTheRealList()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#virtualize");
        await Expect(list.Locator("[data-id='0']")).ToBeVisibleAsync();
        var rendered = await list.GetByRole(AriaRole.Listitem).CountAsync();
        Assert.IsTrue(rendered is > 0 and < 40, $"only a window of 2000 items is rendered ({rendered})");

        // The scroll range is sized from ItemSize.
        var scrollHeight = await list.EvaluateAsync<double>("el => el.scrollHeight");
        Assert.IsTrue(Math.Abs(scrollHeight - 2000 * 40) < 200, $"scroll range {scrollHeight}");

        // A real wheel scroll brings new rows in and drops the first ones.
        await ScrollAsync("#virtualize", 4000);
        await Expect(list.Locator("[data-id='100']")).ToBeVisibleAsync();
        await Expect(list.Locator("[data-id='0']")).ToHaveCountAsync(0);
        Assert.IsTrue(await list.GetByRole(AriaRole.Listitem).CountAsync() < 40, "still only a window");

        // A jump to the middle of the range renders the rows there.
        await list.EvaluateAsync("el => el.scrollTop = 40000");
        await Expect(list.Locator("[data-id='1000']")).ToBeVisibleAsync();
        var row = await BoxAsync("#virtualize [data-id='1000']");
        AssertClose((await BoxAsync("#virtualize")).Y + 1, row.Y, "row 1000 at the top of the list");
    }

    [TestMethod]
    public async Task OverscanCount_PreRendersMoreRowsOutsideTheVisibleRegion()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#virtualize [role=listitem]").First).ToBeVisibleAsync();
        await Expect(Page.Locator("#virtualize-overscan [role=listitem]").First).ToBeVisibleAsync();
        await NextFramesAsync();

        await Page.Locator("#virtualize").EvaluateAsync("el => el.scrollTop = 20000");
        await Page.Locator("#virtualize-overscan").EvaluateAsync("el => el.scrollTop = 20000");
        await Expect(Page.Locator("#virtualize [role=listitem]", new() { HasText = "Person 500" })).ToBeVisibleAsync();
        await Expect(Page.Locator("#virtualize-overscan [role=listitem]", new() { HasText = "Person 500" })).ToBeVisibleAsync();

        var three = await Page.Locator("#virtualize [role=listitem]").CountAsync();
        var ten = await Page.Locator("#virtualize-overscan [role=listitem]").CountAsync();
        Assert.IsTrue(ten >= three + 10, $"OverscanCount 10 renders more rows ({ten}) than 3 ({three})");
    }

    [TestMethod]
    public async Task ItemsProvider_Virtualized_ShowsPlaceholdersThenTheRowsOfTheRequestedRegion()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#provider");
        await Expect(list.Locator("[data-id='0']")).ToHaveTextAsync("Product 0");
        await Expect(Page.Locator("#provider-last")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^0:\\d+$"));
        await Expect(list.Locator(".bsl-placeholder")).ToHaveCountAsync(0);

        // Scrolled far away: the placeholders hold the room until the provider answers for that region.
        var callsBefore = int.Parse(await Page.Locator("#provider-calls").InnerTextAsync());
        await list.EvaluateAsync("el => el.scrollTop = 20000");
        await Expect(list.Locator(".bsl-placeholder").First).ToBeVisibleAsync();
        await Expect(list.Locator("[data-id='500']")).ToHaveTextAsync("Product 500");
        await Expect(list.Locator(".bsl-placeholder")).ToHaveCountAsync(0);
        var start = int.Parse((await Page.Locator("#provider-last").InnerTextAsync()).Split(':')[0]);
        Assert.IsTrue(start is > 400 and <= 500, $"the request starts around the visible index ({start})");
        Assert.IsTrue(int.Parse(await Page.Locator("#provider-calls").InnerTextAsync()) > callsBefore);

        // RefreshDataAsync asks the provider again.
        var callsAfter = int.Parse(await Page.Locator("#provider-calls").InnerTextAsync());
        await Page.ClickAsync("#btn-refresh-provider");
        await Expect(Page.Locator("#provider-calls")).Not.ToHaveTextAsync(callsAfter.ToString());
        await Expect(list.Locator("[data-id='500']")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ItemsProviderDelay_DebouncesTheRequestsOfAFastScroll()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#provider");
        await Expect(list.Locator("[data-id='0']")).ToBeVisibleAsync();
        var before = int.Parse(await Page.Locator("#provider-calls").InnerTextAsync());

        // Twenty regions in a row, faster than the 200ms delay: most of them are never asked for.
        for (var i = 1; i <= 20; i++)
        {
            await list.EvaluateAsync($"el => el.scrollTop = {i * 1000}");
            await NextFramesAsync();
        }

        await Expect(list.Locator("[data-id='500']")).ToBeVisibleAsync();
        var calls = int.Parse(await Page.Locator("#provider-calls").InnerTextAsync()) - before;
        Assert.IsTrue(calls < 20, $"the requests were debounced ({calls} calls for 20 regions)");
    }

    [TestMethod]
    public async Task LoadMore_AppendsAPagePerClick_AndTheButtonGoesAwayAfterTheLast()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#load-more");
        var button = list.Locator("button.bit-bsl-lmb");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
        await Expect(button).ToHaveTextAsync("Show 10 more");
        await Expect(Page.Locator("#load-more-count")).ToHaveTextAsync("10");
        await Expect(Page.Locator("#load-more-calls")).ToHaveTextAsync("1");

        foreach (var expected in new[] { 20, 30, 40 })
        {
            await button.ClickAsync();
            await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(expected);
            await Expect(Page.Locator("#load-more-count")).ToHaveTextAsync(expected.ToString());
        }

        await button.ClickAsync();
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(45);
        await Expect(Page.Locator("#load-more-count")).ToHaveTextAsync("45");
        await Expect(Page.Locator("#load-more-calls")).ToHaveTextAsync("5");
        await Expect(button).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task LoadMoreTemplate_ShowsTheBusyState_AndIsActivatedLikeAButton()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#load-more-template");
        var wrapper = list.Locator(".bit-bsl-lmb");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
        await Expect(wrapper).ToHaveAttributeAsync("role", "button");
        await Expect(wrapper).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Page.Locator("#load-more-template-content")).ToHaveTextAsync("Load more people");

        await wrapper.ClickAsync();
        await Expect(Page.Locator("#load-more-template-content")).ToHaveTextAsync("Loading the next page...");
        await Expect(wrapper).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(list.Locator("> .bit-bsl-itm")).ToHaveAttributeAsync("aria-busy", "true");

        await Page.ClickAsync("#btn-release-page");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(20);
        await Expect(Page.Locator("#template-count")).ToHaveTextAsync("20");
        await Expect(Page.Locator("#load-more-template-content")).ToHaveTextAsync("Load more people");

        // Enter on the focused wrapper loads the next (and last, short) page.
        await wrapper.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#load-more-template-content")).ToHaveTextAsync("Loading the next page...");
        await Page.ClickAsync("#btn-release-page");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(25);
        await Expect(wrapper).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task LoadMore_WithVirtualize_LoadsPagesIntoAVirtualizedList()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#load-more-virtualize");
        await Expect(Page.Locator("#virtualized-load-more-count")).ToHaveTextAsync("20");
        await list.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await list.Locator("button.bit-bsl-lmb").ClickAsync();
        await Expect(Page.Locator("#virtualized-load-more-count")).ToHaveTextAsync("40");
        Assert.IsTrue(await list.GetByRole(AriaRole.Listitem).CountAsync() < 40, "still only a window of the loaded rows");
    }

    [TestMethod]
    public async Task AutoLoad_ScrollingTheRealListToTheEnd_LoadsTheNextPage()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#auto-load");
        await Expect(Page.Locator("#auto-count")).ToHaveTextAsync("10");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);

        await ScrollAsync("#auto-load", 1000);
        await Expect(Page.Locator("#auto-count")).ToHaveTextAsync("20");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(20);

        await ScrollAsync("#auto-load", 1000);
        await Expect(Page.Locator("#auto-count")).ToHaveTextAsync("30");
    }

    [TestMethod]
    public async Task AutoLoadThreshold_RequestsTheNextPageThatManyPixelsEarlier()
    {
        await OpenAsync("/e2e/basic-list");

        // Ten 40px rows end 100px below the bottom of the 300px list: within the 150px threshold, so the next page is
        // requested as soon as the list is in view, without scrolling it, while the list without a threshold stays
        // where it is.
        var list = Page.Locator("#auto-load-threshold");
        await list.ScrollIntoViewIfNeededAsync();
        await Expect(Page.Locator("#threshold-count")).ToHaveTextAsync("20");
        await Expect(Page.Locator("#auto-count")).ToHaveTextAsync("10");

        // The list scrolls itself: scrolled to 100px short of its end, the next page is requested again.
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(20);
        await list.EvaluateAsync("el => el.scrollTop = el.scrollHeight - el.clientHeight - 100");
        await Expect(Page.Locator("#threshold-count")).ToHaveTextAsync("30");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(30);
    }

    [TestMethod]
    public async Task Horizontal_LaysTheRowsOutSideBySide()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#horizontal");
        await Expect(list).ToHaveClassAsync(HasClass("bit-bsl-hrz"));
        await Expect(list).ToHaveCSSAsync("display", "flex");
        var first = await BoxAsync("#horizontal [role=listitem] >> nth=0");
        var second = await BoxAsync("#horizontal [role=listitem] >> nth=1");
        AssertClose(first.Right, second.X, "side by side");
        AssertClose(first.Y, second.Y, "on one line");
        Assert.IsTrue(await list.EvaluateAsync<double>("el => el.scrollWidth") > 900, "it scrolls sideways");

        // Styles.Items lays the rows out as a grid.
        await Expect(Page.Locator("#grid-items > .bit-bsl-itm")).ToHaveCSSAsync("display", "grid");
        var a = await BoxAsync("#grid-items [role=listitem] >> nth=0");
        var b = await BoxAsync("#grid-items [role=listitem] >> nth=1");
        AssertClose(a.Y, b.Y, "two columns");
        AssertClose(a.Right + 10, b.X, "with the gap");
    }

    [TestMethod]
    public async Task Sizing_FullAndFitParameters()
    {
        await OpenAsync("/e2e/basic-list");

        var full = await BoxAsync("#full-size");
        AssertClose(500, full.W, "FullSize width");
        AssertClose(300, full.H, "FullSize height");
        AssertClose(500, (await BoxAsync("#full-width")).W, "FullWidth");
        AssertClose(300, (await BoxAsync("#full-height")).H, "FullHeight");

        var fit = await BoxAsync("#fit-size");
        Assert.IsTrue(fit.W < 200, $"FitSize width follows the content ({fit.W})");
        Assert.IsTrue(fit.H < 200, $"FitSize height follows the content ({fit.H})");
        Assert.IsTrue((await BoxAsync("#fit-width")).W < 200, "FitWidth");
        await Expect(Page.Locator("#fit-width")).ToHaveCSSAsync("height", "500px");
        Assert.IsTrue((await BoxAsync("#fit-height")).H < 200, "FitHeight");
        AssertClose(500, (await BoxAsync("#fit-height")).W, "FitHeight keeps the width");
    }

    [TestMethod]
    public async Task PublicMethods_ScrollToStartEndOffsetAndIndex()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#scroll-list");
        var max = await list.EvaluateAsync<double>("el => el.scrollHeight - el.clientHeight");

        await Page.ClickAsync("#btn-scroll-end");
        await Expect(list).ToHaveJSPropertyAsync("scrollTop", max);

        await Page.ClickAsync("#btn-scroll-start");
        await Expect(list).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.ClickAsync("#btn-scroll-offset");
        await Expect(list).ToHaveJSPropertyAsync("scrollTop", 123);

        // The index is measured off the row itself, so the header's height is no part of the arithmetic.
        await Page.ClickAsync("#btn-scroll-index");
        await WaitForRowAtTopAsync("#sl-51", "#scroll-list");

        await Page.ClickAsync("#btn-scroll-index-smooth");
        await WaitForRowAtTopAsync("#sl-71", "#scroll-list");

        // Virtualized: the row is rendered and brought to the top.
        await Page.ClickAsync("#btn-virtual-index");
        await Expect(Page.Locator("#vsl-500")).ToBeVisibleAsync();
        await WaitForRowAtTopAsync("#vsl-500", "#virtual-scroll-list");
    }

    [TestMethod]
    public async Task PublicMethods_RefreshDataAsyncAndLoadMoreAsync()
    {
        await OpenAsync("/e2e/basic-list");

        // A collection mutated in place is read again by a refresh.
        await Page.ClickAsync("#btn-add");
        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#sl-101")).ToHaveTextAsync("101 - Added 101");

        await Expect(Page.Locator("#api-load-more-count")).ToHaveTextAsync("5");
        await Page.ClickAsync("#btn-load-more-async");
        await Expect(Page.Locator("#api-load-more-count")).ToHaveTextAsync("10");
        await Expect(Page.Locator("#api-load-more").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
    }

    [TestMethod]
    public async Task Accessibility_TheRowsElementIsTheNamedList_WithItsRole()
    {
        await OpenAsync("/e2e/basic-list");

        var named = Page.GetByRole(AriaRole.List, new() { Name = "Team members" });
        await Expect(named).ToHaveCountAsync(1);
        await Expect(named.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
        Assert.IsNull(await Page.Locator("#a11y").GetAttributeAsync("role"));
        Assert.IsNull(await Page.Locator("#a11y").GetAttributeAsync("aria-label"));

        // A null Role leaves the rows unnamed, and the name goes to the root as a group.
        Assert.IsNull(await Page.Locator("#a11y-no-role > .bit-bsl-itm").GetAttributeAsync("role"));
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "No role" })).ToHaveAttributeAsync("id", "a11y-no-role");

        await Expect(Page.Locator("#a11y-custom-role > .bit-bsl-itm")).ToHaveAttributeAsync("role", "listbox");
        await Expect(Page.Locator("#a11y-custom-role").GetByRole(AriaRole.Option)).ToHaveCountAsync(10);
    }

    [TestMethod]
    public async Task TabIndex_MakesTheScrollingRootReachableAndScrollableFromTheKeyboard()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#a11y-tabindex");
        await Expect(list).ToHaveAttributeAsync("tabindex", "0");
        await list.FocusAsync();
        await Expect(list).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(list).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        Assert.IsNull(await Page.Locator("#basic").GetAttributeAsync("tabindex"));
    }

    [TestMethod]
    public async Task Accessibility_ALoadingPageIsAnnounced_AndTheFocusMovesToTheListAfterTheLastPage()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#a11y-focus");
        var button = list.Locator("button.bit-bsl-lmb");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(10);
        await Expect(Page.GetByRole(AriaRole.List, new() { Name = "Loadable" })).ToHaveCountAsync(1);

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Busy, still focusable, and the LoadingLabel announced in the status region.
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToBeFocusedAsync();
        await Expect(button).ToBeEnabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-label", "Load more");
        await Expect(list.Locator("> [role=status] .bit-bsl-stt")).ToHaveTextAsync("Loading more team members...");
        await Expect(list.Locator("> .bit-bsl-itm")).ToHaveAttributeAsync("aria-busy", "true");

        // Released from a script, so the focus stays where it is.
        await Page.EvaluateAsync("() => document.getElementById('btn-release-busy').click()");
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(15);
        await Expect(button).ToHaveCountAsync(0);
        await Expect(list).ToBeFocusedAsync();
        await Expect(list).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(list.Locator("> [role=status] .bit-bsl-stt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CascadingParameters_BitBasicListParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#casc").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(5);
        await Expect(Page.Locator("#casc button.bit-bsl-lmb")).ToHaveTextAsync("More from the cascade");

        await Expect(Page.Locator("#casc-own").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(5);
        await Expect(Page.Locator("#casc-own button.bit-bsl-lmb")).ToHaveTextAsync("Its own text");

        await Expect(Page.Locator("#casc-out").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(45);
        await Expect(Page.Locator("#casc-out .bit-bsl-lmb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task StyleClassStylesClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#styled");
        await Expect(list).ToHaveClassAsync(HasClass("bsl-root-class"));
        await Expect(list).ToHaveClassAsync(HasClass("bsl-extra-root"));
        await Expect(list).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(list).ToHaveCSSAsync("padding-top", "1px");

        var parts = new (string Selector, string Class, string Property, string Value)[]
        {
            ("#styled > .bit-bsl-hdr", "bsl-header-class", "padding-top", "2px"),
            ("#styled > .bit-bsl-itm", "bsl-items-class", "padding-top", "3px"),
            ("#styled > .bit-bsl-ftr", "bsl-footer-class", "padding-top", "4px"),
            ("#styled .bit-bsl-lmb", "bsl-lmb-class", "margin-top", "6px"),
            ("#styled .bit-bsl-lmt", "bsl-lmt-class", "color", "rgb(255, 99, 71)"),
        };

        foreach (var (selector, cls, property, value) in parts)
        {
            await Expect(Page.Locator(selector)).ToHaveClassAsync(HasClass(cls));
            await Expect(Page.Locator(selector)).ToHaveCSSAsync(property, value);
        }

        await Page.ClickAsync("#btn-styled-loading");
        await Expect(Page.Locator("#styled .bit-bsl-ldc")).ToHaveClassAsync(HasClass("bsl-loading-class"));
        await Expect(Page.Locator("#styled .bit-bsl-ldc")).ToHaveCSSAsync("margin-top", "5px");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync("/e2e/basic-list");

        await Page.ClickAsync("#btn-hidden-vis");
        await Expect(Page.Locator("#vis")).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-collapsed-vis");
        await Expect(Page.Locator("#vis")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Disabled_TheLoadMoreElementIsDisabled_AndLoadsNothing()
    {
        await OpenAsync("/e2e/basic-list");

        var list = Page.Locator("#disabled");
        var button = list.Locator("button.bit-bsl-lmb");
        await Expect(list).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
        await Expect(Page.Locator("#disabled-count")).ToHaveTextAsync("5");

        var wrapper = Page.Locator("#disabled-template .bit-bsl-lmb");
        await Expect(wrapper).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(wrapper).ToHaveAttributeAsync("tabindex", "-1");

        // A click delivered anyway (a screen reader, a script) is turned down by the list itself.
        await wrapper.DispatchEventAsync("click");
        await button.DispatchEventAsync("click");
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(list).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#disabled-template").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(5);
        await Expect(Page.Locator("#disabled-count")).ToHaveTextAsync("5");

        await Expect(button).ToBeEnabledAsync();
        await button.ClickAsync();
        await Expect(Page.Locator("#disabled-count")).ToHaveTextAsync("10");
    }

    [TestMethod]
    public async Task CssVariables_ReskinTheHeightTheLoadMoreButtonTheLoadingContentAndTheFocusRing()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#vars")).ToHaveCSSAsync("height", "180px");

        var button = Page.Locator("#vars button.bit-bsl-lmb");
        await Expect(button).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(button).ToHaveCSSAsync("font-size", "19px");
        await Expect(button).ToHaveCSSAsync("font-weight", "300");
        await Expect(Page.Locator("#vars .bit-bsl-lmt")).ToHaveCSSAsync("padding-top", "7px");

        await button.ScrollIntoViewIfNeededAsync();
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
        await Page.Mouse.DownAsync();
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#vars-loading .bit-bsl-ldc")).ToHaveCSSAsync("padding-top", "9px");
        await Expect(Page.Locator("#vars-loading .bit-bsl-ldt")).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(Page.Locator("#vars-loading .bit-bsl-spn")).ToHaveCSSAsync("width", "30px");
        await Expect(Page.Locator("#vars-loading .bit-bsl-spn")).ToHaveCSSAsync("border-top-color", "rgb(19, 20, 21)");

        await Expect(Page.Locator("#vars-disabled button.bit-bsl-lmb")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");

        // The focus ring of the keyboard-focused root.
        await Page.Locator("#btn-hidden-vis").FocusAsync();
        await Page.Locator("#vars").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#vars")).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#vars", "box-shadow"), "rgb(25, 26, 27)");
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_StartTheRowsAtTheRight()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        var list = await BoxAsync("#rtl");
        var first = await BoxAsync("#rtl [role=listitem] >> nth=0");
        AssertClose(list.Right, first.Right, "the first row at the right");
        Assert.IsTrue((await BoxAsync("#rtl [role=listitem] >> nth=1")).Right <= first.X + Tolerance, "the second to its left");

        AssertClose((await BoxAsync("#rtl-inherited")).Right, (await BoxAsync("#rtl-inherited [role=listitem] >> nth=0")).Right, "inherited");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAVirtualizedAutoLoadingList_RaisesNoError()
    {
        await OpenAsync("/e2e/basic-list");

        await Expect(Page.Locator("#mount-list [role=listitem]").First).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-list")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-list [role=listitem]").First).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // Waits until the row sits at the top of the list, just inside its 1px border.
    private Task WaitForRowAtTopAsync(string row, string list)
    {
        return Page.WaitForFunctionAsync(@"([row, list]) => {
            const r = document.querySelector(row), l = document.querySelector(list);
            return !!r && !!l && Math.abs(r.getBoundingClientRect().top - l.getBoundingClientRect().top - 1) <= 1.5;
        }", new[] { row, list });
    }

    private async Task ScrollAsync(string selector, int deltaY)
    {
        var list = Page.Locator(selector);
        await list.ScrollIntoViewIfNeededAsync();
        var rect = await list.BoundingBoxAsync();
        var before = await list.EvaluateAsync<double>("el => el.scrollTop");
        await Page.Mouse.MoveAsync(rect!.X + rect.Width / 2, rect.Y + Math.Min(rect.Height / 2, 100));
        await Page.Mouse.WheelAsync(0, deltaY);
        await Page.EvaluateAsync(@"([selector, before]) => new Promise(resolve => {
            const el = document.querySelector(selector);
            let last = el.scrollTop, stable = 0, frames = 0;
            const tick = () => {
                frames++;
                const now = el.scrollTop;
                if (now === last) stable++; else { stable = 0; last = now; }
                if ((now !== before && stable >= 5) || frames > 120) return resolve(now);
                requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        })", new object[] { selector, before });
    }

    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(@"([property, value]) => {
            const probe = document.createElement('div');
            probe.style.setProperty(property, value);
            document.body.appendChild(probe);
            const resolved = getComputedStyle(probe).getPropertyValue(property);
            probe.remove();
            return resolved;
        }", new[] { property, value });
    }

    private async Task<Box> BoxAsync(string selector)
    {
        var box = await Page.Locator(selector).First.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} has no box");
        return new(box.X, box.Y, box.Width, box.Height);
    }

    private static void AssertClose(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= Tolerance, $"{what}: expected {expected}, got {actual}");
    }

    private sealed record Box(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
    }
}
