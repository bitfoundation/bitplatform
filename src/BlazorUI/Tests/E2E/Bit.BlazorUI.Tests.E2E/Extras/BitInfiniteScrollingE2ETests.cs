using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitInfiniteScrolling at /e2e/infinite-scrolling in a real browser: the next page fetched when the sentinel
/// scrolls into view of a real scroll container (the list itself, an outer scroller, sideways), the request's Skip
/// and Count, PageSize ending the data on a short page, the loading, empty and end blocks and their templates, the
/// manual mode's Load more button and AutoLoadLimit, a failing provider with its Retry button, OnError and the
/// ErrorTemplate, Reversed with Preload and ItemKey (older pages prepended without moving the reader), Horizontal,
/// ScrollerSelector / RootMargin / LastElement*, OnItemsLoaded / OnEnd and the provider result's HasMore /
/// TotalCount, every public member (state, refresh, append, prepend, set, remove, the scroll methods), MaxItems,
/// ResetKey, the live region and aria-busy, the Feed pattern with its keyboard, the BitParams cascade,
/// Style / Class / Styles / Classes, every --bit-InfiniteScrolling-* variable, RTL, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitInfiniteScrollingE2ETests : E2ETestBase
{
    private const string Route = "/e2e/infinite-scrolling";

    private ILocator List(string id) => Page.Locator($"#{id}");

    private ILocator ItemsOf(string id, string itemClass = "isc-item") => Page.Locator($"#{id} .{itemClass}");

    private ILocator Status(string id) => List(id).Locator("xpath=preceding-sibling::div[1]");

    private ILocator Button(string id) => List(id).Locator("> .bit-isc-btn");

    private ILocator Log => Page.Locator("#isc-log");

    // A list that does not overflow loads only while its end is in the page viewport too, so a list far down the page
    // is brought into view first, the way a reader reaches it.
    private async Task ShowAsync(string id)
    {
        await List(id).ScrollIntoViewIfNeededAsync();
    }

    private Task ScrollToEndAsync(string selector, bool horizontal = false)
    {
        return Page.Locator(selector).EvaluateAsync(horizontal
            ? "e => e.scrollLeft = (getComputedStyle(e).direction === 'rtl' ? -1 : 1) * e.scrollWidth"
            : "e => e.scrollTop = e.scrollHeight");
    }

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_TheFirstPage_LoadsAndTheNextOneWhenTheEndScrollsIntoView()
    {
        await OpenAsync(Route);

        await Expect(ItemsOf("isc-basic")).ToHaveCountAsync(20);
        await Expect(ItemsOf("isc-basic").First).ToHaveTextAsync("Item 0");
        await Expect(Page.Locator("#basic-last")).ToHaveTextAsync("0:0");
        await Expect(List("isc-basic")).ToHaveClassAsync(HasClass("bit-isc"));
        await Expect(List("isc-basic")).ToHaveCSSAsync("overflow-y", "auto");

        // Nothing more is fetched while the end is out of view.
        await NextFramesAsync();
        await Expect(Page.Locator("#basic-calls")).ToHaveTextAsync("1");

        await ScrollToEndAsync("#isc-basic");
        await Expect(ItemsOf("isc-basic")).ToHaveCountAsync(40);
        await Expect(Page.Locator("#basic-last")).ToHaveTextAsync("20:0");
        await Expect(ItemsOf("isc-basic").Nth(39)).ToHaveTextAsync("Item 39");

        // The reader stays where they were: the new items come below.
        Assert.IsTrue(await List("isc-basic").EvaluateAsync<double>("e => e.scrollTop") > 0);

        await ScrollToEndAsync("#isc-basic");
        await Expect(ItemsOf("isc-basic")).ToHaveCountAsync(60);
    }

    [TestMethod]
    public async Task Basic_RealWheelScrolling_LoadsTheNextPage()
    {
        await OpenAsync(Route);

        await Expect(ItemsOf("isc-basic")).ToHaveCountAsync(20);
        await List("isc-basic").HoverAsync();
        for (var i = 0; i < 6 && await ItemsOf("isc-basic").CountAsync() == 20; i++)
        {
            await Page.Mouse.WheelAsync(0, 400);
            await NextFramesAsync();
        }

        await Expect(ItemsOf("isc-basic")).ToHaveCountAsync(40);
    }

    // ---------- page size & states ----------

    [TestMethod]
    public async Task PageSize_IsTheCountOfEachRequestAndAShortPageEndsTheData()
    {
        await OpenAsync(Route);

        await Expect(ItemsOf("isc-paged")).ToHaveCountAsync(15);
        await Expect(Page.Locator("#paged-last")).ToHaveTextAsync("0:15");
        await Expect(List("isc-paged").Locator(".bit-isc-end")).ToHaveCountAsync(0);

        await ScrollToEndAsync("#isc-paged");
        await Expect(ItemsOf("isc-paged")).ToHaveCountAsync(30);
        await ScrollToEndAsync("#isc-paged");
        await Expect(ItemsOf("isc-paged")).ToHaveCountAsync(40);
        await Expect(Page.Locator("#paged-last")).ToHaveTextAsync("30:15");

        // The page of 10 was short of 15: the end without an extra round trip.
        var end = List("isc-paged").Locator(".bit-isc-end");
        await Expect(end).ToHaveTextAsync("You have reached the end of the list.");
        await ScrollToEndAsync("#isc-paged");
        await NextFramesAsync();
        await Expect(Page.Locator("#paged-calls")).ToHaveTextAsync("3");
        await Expect(end).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(end).ToHaveCSSAsync("text-align", "center");

        // Without an EndMessage nothing is rendered at the end.
        await List("isc-no-end").ScrollIntoViewIfNeededAsync();
        foreach (var count in new[] { 15, 30, 40 })
        {
            await Expect(ItemsOf("isc-no-end")).ToHaveCountAsync(count);
            await ScrollToEndAsync("#isc-no-end");
        }

        await Expect(List("isc-no-end").Locator(".bit-isc-ldg")).ToHaveCountAsync(0);
        await Expect(List("isc-no-end").Locator(".bit-isc-end")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Empty_AFirstPageWithNoItem_ShowsTheEmptyMessage()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-empty");
        await ShowAsync("isc-empty-default");

        await Expect(List("isc-empty").Locator(".bit-isc-emp")).ToHaveTextAsync("Nothing to show here.");
        await Expect(List("isc-empty-default").Locator(".bit-isc-emp")).ToHaveTextAsync("There is no item");
        await Expect(Status("isc-empty")).ToHaveTextAsync("Nothing to show here.");
        await Expect(List("isc-empty").Locator(".bit-isc-ldg")).ToHaveCountAsync(0);
    }

    // ---------- templates ----------

    [TestMethod]
    public async Task Templates_ItemLoadingAndEnd_ReplaceTheBuiltInBlocks()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-templates");

        await Expect(List("isc-templates").Locator(".tpl-loading")).ToHaveTextAsync("Fetching...");
        await Expect(List("isc-templates").Locator(".bit-isc-ldg .bit-isc-spn")).ToHaveCountAsync(0);
        await Page.Locator("#btn-release-slow").ClickAsync();
        await Expect(ItemsOf("isc-templates", "tpl-item")).ToHaveCountAsync(15);
        await Expect(ItemsOf("isc-templates", "tpl-item").First).ToHaveTextAsync("Templated 0");
        await Expect(List("isc-templates").Locator(".tpl-loading")).ToHaveCountAsync(0);

        await ScrollToEndAsync("#isc-templates");
        await Expect(List("isc-templates").Locator(".tpl-loading")).ToBeVisibleAsync();
        await Page.Locator("#btn-release-slow").ClickAsync();
        await Expect(ItemsOf("isc-templates", "tpl-item")).ToHaveCountAsync(30);
        await ScrollToEndAsync("#isc-templates");
        await Page.Locator("#btn-release-slow").ClickAsync();
        await Expect(List("isc-templates").Locator(".tpl-end")).ToHaveTextAsync("--- The end ---");

        await Expect(List("isc-empty-template").Locator(".tpl-empty")).ToHaveTextAsync("--- No item ---");
    }

    // ---------- manual ----------

    [TestMethod]
    public async Task Manual_EachPageAfterTheFirst_IsFetchedFromTheButton()
    {
        await OpenAsync(Route);

        await Expect(ItemsOf("isc-manual")).ToHaveCountAsync(10);
        await ScrollToEndAsync("#isc-manual");
        await NextFramesAsync();
        await Expect(Page.Locator("#manual-calls")).ToHaveTextAsync("1");

        var button = Button("isc-manual");
        await Expect(button).ToHaveTextAsync("Load more items");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await button.ClickAsync();
        await Expect(ItemsOf("isc-manual")).ToHaveCountAsync(20);

        // The keyboard presses it too.
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(ItemsOf("isc-manual")).ToHaveCountAsync(30);

        // A full last page still asks once more, and the empty answer ends the list.
        await button.ClickAsync();
        await Expect(List("isc-manual").Locator(".bit-isc-end")).ToHaveTextAsync("No more items to load.");
        await Expect(button).ToHaveCountAsync(0);
        await Expect(Page.Locator("#manual-calls")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task AutoLoadLimit_LoadsTwoPagesOnItsOwnThenSwitchesToTheButton()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-autolimit");

        await Expect(ItemsOf("isc-autolimit")).ToHaveCountAsync(10);
        await ScrollToEndAsync("#isc-autolimit");
        await Expect(ItemsOf("isc-autolimit")).ToHaveCountAsync(20);

        await ScrollToEndAsync("#isc-autolimit");
        var button = Button("isc-autolimit");
        await Expect(button.Locator(".tpl-more")).ToHaveTextAsync("+ Show 10 more");
        await NextFramesAsync();
        await Expect(ItemsOf("isc-autolimit")).ToHaveCountAsync(20);
        await Expect(Page.Locator("#autolimit-calls")).ToHaveTextAsync("2");

        await button.ClickAsync();
        await Expect(ItemsOf("isc-autolimit")).ToHaveCountAsync(30);
    }

    // ---------- errors ----------

    [TestMethod]
    public async Task Error_AFailingPage_ShowsTheMessageAndRetriesFromTheButtonOnly()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-error");

        await Expect(ItemsOf("isc-error")).ToHaveCountAsync(10);
        await ScrollToEndAsync("#isc-error");

        var error = List("isc-error").Locator(".bit-isc-err");
        await Expect(error).ToHaveTextAsync("Something went wrong while loading the items.");
        await Expect(error).ToHaveAttributeAsync("role", "alert");
        await Expect(error).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Log).ToHaveTextAsync("error:Server down");
        var button = Button("isc-error");
        await Expect(button).ToHaveTextAsync("Try again");

        // A failed list stops loading until asked: scrolling again sends nothing.
        await ScrollToEndAsync("#isc-error");
        await NextFramesAsync();
        await Expect(Page.Locator("#faulty-calls")).ToHaveTextAsync("2");

        await button.ClickAsync();
        await Expect(ItemsOf("isc-error")).ToHaveCountAsync(20);
        await Expect(error).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ErrorTemplate_ReplacesTheMessageAndTheButtonAndScreenReadersStillHearIt()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-error-template");

        await Expect(ItemsOf("isc-error-template")).ToHaveCountAsync(10);
        await ScrollToEndAsync("#isc-error-template");

        await Expect(List("isc-error-template").Locator(".tpl-error span")).ToHaveTextAsync("Template failure");
        await Expect(List("isc-error-template").Locator(".bit-isc-err")).ToHaveCountAsync(0);
        await Expect(Button("isc-error-template")).ToHaveCountAsync(0);
        await Expect(Status("isc-error-template")).ToHaveTextAsync("Announced failure");

        await List("isc-error-template").Locator(".tpl-retry").ClickAsync();
        await Expect(ItemsOf("isc-error-template")).ToHaveCountAsync(20);
    }

    // ---------- reversed ----------

    [TestMethod]
    public async Task Reversed_OpensOnTheNewestAndPrependsOlderPagesWithoutMovingTheReader()
    {
        await OpenAsync(Route);

        var list = List("isc-reversed");
        await Expect(list).ToHaveClassAsync(HasClass("bit-isc-rev"));
        await Expect(list).ToHaveCSSAsync("flex-direction", "column");
        await Expect(ItemsOf("isc-reversed", "msg")).ToHaveCountAsync(10);
        await Expect(ItemsOf("isc-reversed", "msg").Last).ToContainTextAsync("Message 39");

        // It starts scrolled to its newest items.
        await Page.WaitForFunctionAsync("e => e.scrollTop + e.clientHeight >= e.scrollHeight - 2", await list.ElementHandleAsync());

        // A note typed into an item stays with it when older items come in above (ItemKey).
        var note = list.Locator(".msg[data-id='35'] .note");
        await note.FillAsync("mine");
        var anchorTop = await list.Locator(".msg[data-id='35']").EvaluateAsync<double>("e => e.getBoundingClientRect().top");

        await list.EvaluateAsync("e => e.scrollTop = 0");
        await Expect(ItemsOf("isc-reversed", "msg")).ToHaveCountAsync(20);
        await Expect(ItemsOf("isc-reversed", "msg").First).ToContainTextAsync("Message 20");
        await Expect(list.Locator(".msg[data-id='35'] .note")).ToHaveValueAsync("mine");

        // The visible items did not jump down by the height of the prepended page.
        var scrollTop = await list.EvaluateAsync<double>("e => e.scrollTop");
        Assert.IsTrue(scrollTop > 100, $"The scroll was moved by the prepended page, was {scrollTop}.");

        await list.EvaluateAsync("e => e.scrollTop = 0");
        await Expect(ItemsOf("isc-reversed", "msg")).ToHaveCountAsync(30);
        await list.EvaluateAsync("e => e.scrollTop = 0");
        await Expect(ItemsOf("isc-reversed", "msg")).ToHaveCountAsync(40);
        await list.EvaluateAsync("e => e.scrollTop = 0");
        var end = list.Locator(".bit-isc-end");
        await Expect(end).ToHaveTextAsync("This is the beginning of the conversation.");
        var endBox = await end.BoundingBoxAsync();
        var firstBox = await ItemsOf("isc-reversed", "msg").First.BoundingBoxAsync();
        Assert.IsTrue(endBox!.Y < firstBox!.Y, "The status blocks sit above the items.");
        _ = anchorTop;
    }

    // ---------- horizontal ----------

    [TestMethod]
    public async Task Horizontal_TheItemsSitInARowAndLoadWhileScrollingSideways()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-horizontal");

        var list = List("isc-horizontal");
        await Expect(list).ToHaveClassAsync(HasClass("bit-isc-hor"));
        await Expect(list).ToHaveCSSAsync("flex-direction", "row");
        await Expect(list).ToHaveCSSAsync("overflow-x", "auto");
        await Expect(ItemsOf("isc-horizontal", "isc-h-item")).ToHaveCountAsync(10);
        var a = await ItemsOf("isc-horizontal", "isc-h-item").Nth(0).BoundingBoxAsync();
        var b = await ItemsOf("isc-horizontal", "isc-h-item").Nth(1).BoundingBoxAsync();
        Assert.AreEqual(a!.Y, b!.Y, 1);
        Assert.IsTrue(b.X > a.X);
        await Expect(list.Locator(".bit-isc-lst")).ToHaveCSSAsync("width", "12px");

        await ScrollToEndAsync("#isc-horizontal", horizontal: true);
        await Expect(ItemsOf("isc-horizontal", "isc-h-item")).ToHaveCountAsync(20);
    }

    // ---------- scroller ----------

    [TestMethod]
    public async Task ScrollerSelector_AnOuterScroller_DrivesTheLoadingAndTheListDoesNotScroll()
    {
        await OpenAsync(Route);

        var list = List("isc-scroller");
        await Expect(list).ToHaveClassAsync(HasClass("bit-isc-ext"));
        await Expect(list).ToHaveCSSAsync("overflow-y", "visible");
        await Expect(ItemsOf("isc-scroller")).ToHaveCountAsync(10);

        var last = list.Locator(".bit-isc-lst");
        await Expect(last).ToHaveClassAsync(HasClass("custom-last"));
        await Expect(last).ToHaveCSSAsync("height", "40px");
        await Expect(last).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(last).ToHaveAttributeAsync("aria-hidden", "true");

        await ScrollToEndAsync("#isc-outer");
        await Expect(ItemsOf("isc-scroller")).ToHaveCountAsync(20);
    }

    [TestMethod]
    public async Task RootMargin_StartsTheNextPageBeforeTheEndIsVisible()
    {
        await OpenAsync(Route);

        await Expect(ItemsOf("isc-scroller")).ToHaveCountAsync(10);
        var outer = Page.Locator("#isc-outer");

        // Stop 120px short of the sentinel: inside the 150px margin, out of sight.
        await outer.EvaluateAsync(
            """
            o => {
                const last = o.querySelector('.bit-isc-lst');
                o.scrollTop += last.getBoundingClientRect().top - o.getBoundingClientRect().bottom - 120;
            }
            """);
        var sentinelBelow = await outer.EvaluateAsync<double>("o => o.querySelector('.bit-isc-lst').getBoundingClientRect().top - o.getBoundingClientRect().bottom");
        Assert.IsTrue(sentinelBelow > 50, $"The sentinel is out of sight ({sentinelBelow}px below).");
        await Expect(ItemsOf("isc-scroller")).ToHaveCountAsync(20);
    }

    // ---------- events ----------

    [TestMethod]
    public async Task Events_OnItemsLoadedEveryPageAndOnEndOnceWithTheProvidersTotal()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-events");

        await Expect(Log).ToHaveTextAsync("loaded:10:0");
        await Page.Locator("#btn-read-progress").ClickAsync();
        await Expect(Page.Locator("#progress")).ToHaveTextAsync("10/25/True");

        await ScrollToEndAsync("#isc-events");
        await Expect(Log).ToHaveTextAsync("loaded:10:0,loaded:10:10");
        await ScrollToEndAsync("#isc-events");

        // The result's HasMore=false ends the list on its own, with no extra request.
        await Expect(Log).ToHaveTextAsync("loaded:10:0,loaded:10:10,loaded:5:20,end");
        await Expect(List("isc-events").Locator(".bit-isc-end")).ToBeVisibleAsync();
        await Page.Locator("#btn-read-progress").ClickAsync();
        await Expect(Page.Locator("#progress")).ToHaveTextAsync("25/25/False");
    }

    // ---------- public members ----------

    [TestMethod]
    public async Task Members_EditTheItemsWithoutTheProviderAndPageOnFromThere()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-members");

        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(10);
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#members-state")).ToHaveTextAsync(new Regex("^10/True/False/True/"));

        await Page.Locator("#btn-m-append").ClickAsync();
        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(12);
        await Expect(ItemsOf("isc-members").Last).ToHaveTextAsync("Item 1001");

        await Page.Locator("#btn-m-prepend").ClickAsync();
        await Expect(ItemsOf("isc-members").First).ToHaveTextAsync("Item -2");

        await Page.Locator("#btn-m-remove").ClickAsync();
        await Expect(Log).ToHaveTextAsync("removed:True");
        await Expect(List("isc-members").Locator("[data-item='0']")).ToHaveCountAsync(0);
        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(13);

        // Three items leave the end of the box in view, so the next page follows on its own - sooner or later after
        // the set, depending on where the page code runs: the set is read off the head of the list.
        await Page.Locator("#btn-m-set").ClickAsync();
        await Expect(ItemsOf("isc-members").First).ToHaveTextAsync("Item 7");
        await Expect(ItemsOf("isc-members").Nth(1)).ToHaveTextAsync("Item 8");
        await Expect(ItemsOf("isc-members").Nth(2)).ToHaveTextAsync("Item 9");
        await Expect(List("isc-members").Locator("[data-item='1001'], [data-item='-2']")).ToHaveCountAsync(0);

        // The next request skips what is loaded, and MaxItems narrows it to what is still missing.
        await Expect(Page.Locator("#members-last")).ToHaveTextAsync("3:10");
        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(13);
        await Expect(ItemsOf("isc-members").Nth(3)).ToHaveTextAsync("Item 3");
        await ScrollToEndAsync("#isc-members");
        await Expect(Page.Locator("#members-last")).ToHaveTextAsync("13:7");
        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(20);
        await Expect(List("isc-members").Locator(".bit-isc-end")).ToBeVisibleAsync();
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#members-state")).ToHaveTextAsync(new Regex("^20/False/False/True/"));

        // Back at the top first: a list refreshed while scrolled to its end fills the room the shorter first page
        // leaves under the reader with the next page, as it should.
        await Page.Locator("#btn-m-top").ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('#isc-members').scrollTop === 0");
        await Page.Locator("#btn-m-refresh").ClickAsync();
        await Expect(Page.Locator("#members-last")).ToHaveTextAsync("0:10");
        await Expect(ItemsOf("isc-members").First).ToHaveTextAsync("Item 0");
        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(10);
    }

    [TestMethod]
    public async Task Members_TheScrollMethods_MoveTheListAndReportItsOffset()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-members");

        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(10);
        var list = List("isc-members");

        await Page.Locator("#btn-m-offset").ClickAsync();
        await Page.WaitForFunctionAsync("e => Math.round(e.scrollTop) === 100", await list.ElementHandleAsync());
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#members-state")).ToHaveTextAsync(new Regex("/100$"));

        await Page.Locator("#btn-m-top").ClickAsync();
        await Page.WaitForFunctionAsync("e => e.scrollTop === 0", await list.ElementHandleAsync());

        await Page.Locator("#btn-m-bottom").ClickAsync();
        await Expect(ItemsOf("isc-members")).ToHaveCountAsync(20);
    }

    // ---------- reset key ----------

    [TestMethod]
    public async Task ResetKey_AChangedFilter_ReloadsFromTheFirstPage()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-reset");

        await Expect(ItemsOf("isc-reset").First).ToHaveTextAsync("Item 0");
        await ScrollToEndAsync("#isc-reset");
        await Expect(ItemsOf("isc-reset")).ToHaveCountAsync(20);

        // Back at the top first: a list reloaded while scrolled to its end fills the room its shorter first page leaves
        // under the reader with the next page straight away, as it should - however soon after the first one lands.
        await List("isc-reset").EvaluateAsync("e => e.scrollTop = 0");
        await Page.WaitForFunctionAsync("() => document.querySelector('#isc-reset').scrollTop === 0");
        await Page.Locator("#btn-filter-odd").ClickAsync();
        await Expect(ItemsOf("isc-reset").First).ToHaveTextAsync("Item 1");
        await Expect(ItemsOf("isc-reset")).ToHaveCountAsync(10);
        await Expect(ItemsOf("isc-reset").Nth(1)).ToHaveTextAsync("Item 3");
    }

    // ---------- loading state & live region ----------

    [TestMethod]
    public async Task Loading_WhileAPageIsInFlight_TheListIsBusyAndSaysSo()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-gated");

        var list = List("isc-gated");
        await Expect(list).ToHaveAttributeAsync("aria-busy", "true");
        var loading = list.Locator(".bit-isc-ldg");
        await Expect(loading).ToContainTextAsync("Loading the page...");
        await Expect(loading.Locator(".bit-isc-spn")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(loading.Locator(".bit-isc-spn")).ToBeVisibleAsync();
        await Expect(Status("isc-gated")).ToHaveTextAsync("Loading the page...");
        await Expect(Status("isc-gated")).ToHaveAttributeAsync("role", "status");
        await Expect(Status("isc-gated")).ToHaveAttributeAsync("aria-busy", "false");
        await Page.Locator("#btn-read-gated").ClickAsync();
        await Expect(Page.Locator("#gated-state")).ToHaveTextAsync("True/0");

        await Page.Locator("#btn-release-gated").ClickAsync();
        await Expect(ItemsOf("isc-gated")).ToHaveCountAsync(5);
        await Expect(list).ToHaveAttributeAsync("aria-busy", "false");
        await Expect(loading).ToHaveCountAsync(0);
        await Expect(Status("isc-gated")).ToHaveTextAsync("5 more items loaded, 5 in total.");

        // The button keeps its place and the focus while its page loads, marked rather than disabled.
        var button = Button("isc-gated");
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToBeFocusedAsync();
        Assert.IsNull(await button.GetAttributeAsync("disabled"), "The button is marked, never disabled.");
        await Expect(button).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-dis)"));

        // A second press while it loads asks nothing more.
        await button.ClickAsync(new() { Force = true });
        await Page.Locator("#btn-release-gated").ClickAsync();
        await Expect(ItemsOf("isc-gated")).ToHaveCountAsync(10);
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Expect(Status("isc-gated")).ToHaveTextAsync(new Regex("5 more items loaded, 10 in total."));
    }

    // ---------- feed ----------

    [TestMethod]
    public async Task Feed_EveryItemIsANamedArticleInASetOfUnknownSize()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-feed");

        var feed = List("isc-feed").Locator("> .bit-isc-fed");
        await Expect(feed).ToHaveAttributeAsync("role", "feed");
        await Expect(feed).ToHaveAttributeAsync("aria-label", "Products");
        await Expect(List("isc-feed")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Feed, new() { Name = "Products" })).ToHaveCountAsync(1);

        var articles = feed.Locator("> article.bit-isc-art");
        await Expect(articles).ToHaveCountAsync(5);
        await Expect(articles.First).ToHaveAttributeAsync("tabindex", "0");
        await Expect(articles.First).ToHaveAttributeAsync("aria-label", "Product 0");
        await Expect(articles.First).ToHaveAttributeAsync("aria-posinset", "1");
        await Expect(articles.First).ToHaveAttributeAsync("aria-setsize", "-1");
        await Expect(Page.GetByRole(AriaRole.Article, new() { Name = "Product 3" })).ToHaveCountAsync(1);

        // The button stays outside the feed box; a page it loads hands the focus to its first article.
        await Expect(feed.Locator(".bit-isc-btn")).ToHaveCountAsync(0);
        await Button("isc-feed").ClickAsync();
        await Expect(articles).ToHaveCountAsync(10);
        await Expect(articles.Nth(5)).ToBeFocusedAsync();
        await Expect(articles.Nth(5)).ToHaveAttributeAsync("aria-posinset", "6");

        await Button("isc-feed").ClickAsync();
        await Expect(articles).ToHaveCountAsync(15);
        await Button("isc-feed").ClickAsync();
        await Expect(List("isc-feed").Locator(".bit-isc-end")).ToHaveTextAsync("All products are loaded.");
        await Expect(articles.First).ToHaveAttributeAsync("aria-setsize", "15");
    }

    [TestMethod]
    public async Task Feed_PageDownPageUpAndCtrlEndHome_MoveTheFocus()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-feed");

        var articles = List("isc-feed").Locator("article");
        await Expect(articles).ToHaveCountAsync(5);
        await articles.First.FocusAsync();

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(articles.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(articles.Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(articles.Nth(1)).ToBeFocusedAsync();

        // The keys belong to a text field inside an article.
        await articles.Nth(1).Locator(".feed-input").FocusAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Expect(articles.Nth(1).Locator(".feed-input")).ToBeFocusedAsync();

        await articles.Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Expect(Button("isc-feed")).ToBeFocusedAsync();

        await articles.Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("Control+Home");
        await Expect(Page.Locator("#before-feed")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Accessibility_ANamedListIsAGroupAndATabIndexMakesItFocusable()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-named");

        var list = List("isc-named");
        await Expect(list).ToHaveAttributeAsync("role", "group");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Named list");
        await Expect(list).ToHaveAttributeAsync("tabindex", "0");
        await Expect(List("isc-basic")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Expect(ItemsOf("isc-named")).ToHaveCountAsync(15);
        await list.FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.WaitForFunctionAsync("e => e.scrollTop > 0", await list.ElementHandleAsync());
    }

    // ---------- cascading ----------

    [TestMethod]
    public async Task Cascading_BitParams_SetTheDefaultsAndTheListsOwnWin()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-cas");
        await ShowAsync("isc-cas-own");

        await Expect(ItemsOf("isc-cas")).ToHaveCountAsync(10);
        await Expect(Button("isc-cas")).ToHaveTextAsync("Cascaded more");
        await Expect(Button("isc-cas-own")).ToHaveTextAsync("Show me more");

        await Button("isc-cas").ClickAsync();
        await Button("isc-cas").ClickAsync();
        await Button("isc-cas").ClickAsync();
        await Expect(List("isc-cas").Locator(".bit-isc-end")).ToHaveTextAsync("Cascaded end");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task StylesAndClasses_EveryPart_TakesItsOwn()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-styled");
        await ShowAsync("isc-styled-empty");
        await ShowAsync("isc-styled-error");

        var list = List("isc-styled");
        await Expect(list).ToHaveClassAsync(HasClass("isc-root-class"));
        await Expect(list).ToHaveClassAsync(HasClass("c-root"));
        await Expect(list).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(list).ToHaveCSSAsync("padding-top", "2px");
        await Expect(list.Locator(".bit-isc-ldg")).ToHaveClassAsync(HasClass("c-loading"));
        await Expect(list.Locator(".bit-isc-ldg")).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(list.Locator(".bit-isc-spn")).ToHaveClassAsync(HasClass("c-spinner"));
        await Expect(list.Locator(".bit-isc-spn")).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(list.Locator(".bit-isc-lst")).ToHaveClassAsync(HasClass("c-last"));

        // The sentinel is out of the layout while a page loads, and takes its style once it can trigger again.
        await Expect(list.Locator(".bit-isc-lst")).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-release-slow2").ClickAsync();
        var keyed = list.Locator(".bit-isc-itm");
        await Expect(keyed).ToHaveCountAsync(15);
        await Expect(list.Locator(".bit-isc-lst")).ToHaveCSSAsync("margin-top", "3px");
        await Expect(keyed.First).ToHaveClassAsync(HasClass("c-item"));
        await Expect(keyed.First).ToHaveCSSAsync("display", "contents");
        await Expect(keyed.First).ToHaveCSSAsync("color", "rgb(10, 20, 30)");

        await ScrollToEndAsync("#isc-styled");
        await Page.Locator("#btn-release-slow2").ClickAsync();
        await Expect(keyed).ToHaveCountAsync(30);
        await ScrollToEndAsync("#isc-styled");
        await Page.Locator("#btn-release-slow2").ClickAsync();
        await Expect(list.Locator(".bit-isc-end")).ToHaveClassAsync(HasClass("c-end"));
        await Expect(list.Locator(".bit-isc-end")).ToHaveCSSAsync("color", "rgb(70, 80, 90)");

        await Expect(List("isc-styled-empty").Locator(".bit-isc-emp")).ToHaveClassAsync(HasClass("c-empty"));
        await Expect(List("isc-styled-empty").Locator(".bit-isc-emp")).ToHaveCSSAsync("color", "rgb(5, 5, 5)");
        await Expect(List("isc-styled-error").Locator(".bit-isc-err")).ToHaveClassAsync(HasClass("c-error"));
        await Expect(List("isc-styled-error").Locator(".bit-isc-err")).ToHaveCSSAsync("padding-top", "9px");
        await Expect(Button("isc-styled-error")).ToHaveClassAsync(HasClass("c-button"));
        await Expect(Button("isc-styled-error")).ToHaveCSSAsync("padding-top", "11px");
        await Expect(Button("isc-styled-error")).ToHaveTextAsync("Retry");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheStatesAndTheButton()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-vars-loading");
        await ShowAsync("isc-vars-manual");
        await ShowAsync("isc-vars-error");
        await ShowAsync("isc-vars-feed");

        var loading = List("isc-vars-loading").Locator(".bit-isc-ldg");
        await Expect(loading).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(loading).ToHaveCSSAsync("font-size", "21px");
        await Expect(loading).ToHaveCSSAsync("padding-top", "13px");
        await Expect(loading).ToHaveCSSAsync("text-align", "right");
        var spinner = loading.Locator(".bit-isc-spn");
        await Expect(spinner).ToHaveCSSAsync("width", "33px");
        await Expect(spinner).ToHaveCSSAsync("margin-right", "17px");
        await Expect(spinner).ToHaveCSSAsync("border-top-color", "rgb(3, 3, 3)");
        await Expect(spinner).ToHaveCSSAsync("border-bottom-color", "rgb(4, 4, 4)");

        await Expect(List("isc-vars-error").Locator(".bit-isc-err")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");

        var button = Button("isc-vars-manual");
        await Expect(button).ToHaveCSSAsync("color", "rgb(5, 5, 5)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(7, 7, 7)");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(button).ToHaveCSSAsync("padding-top", "10px");
        await Expect(button).ToHaveCSSAsync("padding-left", "12px");
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("color", "rgb(6, 6, 6)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(8, 8, 8)");

        var article = List("isc-vars-feed").Locator("article").First;
        await article.FocusAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(List("isc-vars-feed").Locator("article").Nth(1)).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#isc-vars-feed article:focus", "outline-color") + await ComputedStyleAsync("#isc-vars-feed article:focus", "box-shadow"), "rgb(0, 200, 0)");
    }

    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_TheListAndAHorizontalOne_FlowRightToLeft()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-rtl");
        await ShowAsync("isc-rtl-h");

        await Expect(List("isc-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(List("isc-rtl")).ToHaveCSSAsync("direction", "rtl");
        await Expect(Status("isc-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(ItemsOf("isc-rtl")).ToHaveCountAsync(15);
        await ScrollToEndAsync("#isc-rtl");
        await Expect(ItemsOf("isc-rtl")).ToHaveCountAsync(30);
        await ScrollToEndAsync("#isc-rtl");
        await Expect(ItemsOf("isc-rtl")).ToHaveCountAsync(40);
        await Expect(List("isc-rtl").Locator(".bit-isc-end")).ToHaveTextAsync("به انتهای لیست رسیدید.");

        var a = await ItemsOf("isc-rtl-h", "isc-h-item").Nth(0).BoundingBoxAsync();
        var b = await ItemsOf("isc-rtl-h", "isc-h-item").Nth(1).BoundingBoxAsync();
        Assert.IsTrue(b!.X < a!.X, "The second item sits to the left of the first.");
        await ScrollToEndAsync("#isc-rtl-h", horizontal: true);
        await Expect(ItemsOf("isc-rtl-h", "isc-h-item")).ToHaveCountAsync(20);
    }

    // ---------- media ----------

    [TestMethod]
    public async Task Print_TheList_PrintsItsLoadedItemsWithoutTheChrome()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-manual");

        await Expect(ItemsOf("isc-manual")).ToHaveCountAsync(10);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });
        await Expect(List("isc-manual")).ToHaveCSSAsync("overflow-y", "visible");
        await Expect(Button("isc-manual")).ToBeHiddenAsync();
        await Expect(List("isc-gated").Locator(".bit-isc-ldg")).ToBeHiddenAsync();
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_Preload_PutsTheFirstPageInTheHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "Preloaded 14");
        StringAssert.Matches(html, new Regex(@"<div id=""isc-basic"" class=""bit-isc isc-box"""));
        var basic = Regex.Match(html, @"<div id=""isc-basic"".*?<span id=""basic-calls"">", RegexOptions.Singleline).Value;
        Assert.IsFalse(basic.Contains("isc-item"), "Without Preload the provider is not called on the server.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Unmount_WhileAPageIsInFlight_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);
        await ShowAsync("isc-mounted");

        await Expect(List("isc-mounted").Locator(".bit-isc-ldg")).ToBeVisibleAsync();
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(List("isc-mounted")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(List("isc-mounted").Locator(".bit-isc-ldg")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- helpers ----------

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.color = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, cssColor);
    }
}
