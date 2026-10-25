namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Virtualization and loading in BitDropdown (<c>/e2e/dropdown/virtualize</c>): only a window of a long Items list is
/// rendered and scrolling the real list (the wheel, the scrollbar, End) renders the rest, an opening lands on a selection
/// thousands of rows down, picks survive scrolling away with a small OverscanCount, a virtualized search covers the whole
/// list, and an ItemsProvider is asked for windows - with the search, positions in the whole set, a debounce collapsing
/// fast scrolling, InitialSelectedItems, VirtualizePlaceholder for rows still on their way and RefreshItemsAsync - and
/// IsLoading shows the spinner, LoadingText or LoadingTemplate until a real fetch hands the items over.
/// </summary>
[TestClass]
public class BitDropdownVirtualizeE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/virtualize";

    [TestMethod]
    public async Task Virtualize_RendersAWindowOfTheList_AndScrollingTheRealListRendersTheRest()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-virt");
        var scroller = ListBox(callout);

        await Expect(Option(callout, "Item 0001")).ToBeVisibleAsync();
        // The rows only exist once the opening has rendered them, and the focus still reaches the first one.
        await Expect(Option(callout, "Item 0001")).ToBeFocusedAsync();
        var rendered = await Options(callout).CountAsync();
        Assert.IsTrue(rendered is > 5 and < 80, $"Only a window of the thousand items is rendered, was {rendered}.");
        await Expect(Option(callout, "Item 1000")).ToHaveCountAsync(0);
        await Expect(Option(callout, "Item 0001")).ToHaveAttributeAsync("aria-setsize", "1000");

        // The list keeps the height of the whole set, so the scrollbar is the real one.
        Assert.IsTrue(await scroller.EvaluateAsync<double>("el => el.scrollHeight") > 30000, "The list is as tall as a thousand rows.");

        // The wheel scrolls the real list (by an amount each engine decides), and the rows it scrolls to get rendered.
        await scroller.HoverAsync();
        await Page.Mouse.WheelAsync(0, 3600);
        await Page.Mouse.WheelAsync(0, 3600);
        await Page.WaitForFunctionAsync("el => el.scrollTop > 600", await scroller.ElementHandleAsync());
        await WaitForStableListAsync(callout);

        var top = await scroller.EvaluateAsync<double>("el => el.scrollTop");
        var row = await PxAsync(Options(callout).First, "height");
        var index = (int)(top / row) + 5;
        var deep = Option(callout, $"Item {index:0000}");

        await Expect(deep).ToBeVisibleAsync();
        await Expect(deep).ToHaveAttributeAsync("aria-posinset", index.ToString());
        await Expect(Option(callout, "Item 0001")).ToHaveCountAsync(0);

        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(Option(callout, "Item 1000")).ToBeVisibleAsync();
        await WaitForStableListAsync(callout);

        await Option(callout, "Item 1000").ClickAsync();

        await Expect(Page.Locator("#virt-value")).ToHaveTextAsync("i1000");
        await Expect(TextContainer("dd-virt")).ToHaveTextAsync("Item 1000");
    }

    [TestMethod]
    public async Task Virtualize_TheOpeningLandsOnASelectionThousandsOfRowsDown()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-virt-selected")).ToHaveTextAsync("Item 2500");

        var callout = await OpenDropdownAsync("dd-virt-selected");
        var selected = Option(callout, "Item 2500");

        await Expect(selected).ToBeFocusedAsync();
        await Expect(selected).ToHaveAttributeAsync("aria-selected", "true");

        var scroller = await BoxAsync(ListBox(callout));
        var item = await BoxAsync(selected);
        Assert.IsTrue(item.Y >= scroller.Y - 1 && item.Y + item.Height <= scroller.Y + scroller.Height + 1, "The selection is in view.");
    }

    [TestMethod]
    public async Task Virtualize_TheFirstKeyboardOpening_PutsTheFocusOnTheFirstItem()
    {
        await OpenAsync(Url);

        await Trigger("dd-virt-search").FocusAsync();
        await Expect(Trigger("dd-virt-search")).ToBeFocusedAsync();

        await Trigger("dd-virt").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        var callout = await CalloutAsync("dd-virt");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Item 0001")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Item 0002")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Virtualize_EndJumpsToTheLastItemOfTheWholeList_AndHomeBack()
    {
        await OpenAsync(Url);

        await Trigger("dd-virt").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-virt");
        await Expect(Option(callout, "Item 0001")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Option(callout, "Item 1000")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(Option(callout, "Item 0001")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task VirtualizeMulti_PicksSurviveScrollingAway()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-virt-multi");
        var scroller = ListBox(callout);

        await Expect(Option(callout, "Item 0001")).ToBeFocusedAsync();
        await WaitForStableListAsync(callout);

        var rendered = await Options(callout).CountAsync();
        Assert.IsTrue(rendered < 40, $"A small OverscanCount keeps the window small, was {rendered}.");

        await Option(callout, "Item 0002").ClickAsync();
        await Expect(Page.Locator("#virt-multi-values")).ToHaveTextAsync("i2");

        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(Option(callout, "Item 1000")).ToBeVisibleAsync();
        await WaitForStableListAsync(callout);
        await Option(callout, "Item 1000").ClickAsync();
        await Expect(Page.Locator("#virt-multi-values")).ToHaveTextAsync("i2,i1000");

        await scroller.EvaluateAsync("el => el.scrollTop = 0");
        await Expect(Option(callout, "Item 0002")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Option(callout, "Item 0001")).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(TextContainer("dd-virt-multi")).ToHaveTextAsync("Item 0002, Item 1000");
    }

    [TestMethod]
    public async Task VirtualizeSearch_FiltersTheWholeListNotJustTheRenderedWindow()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-virt-search");

        await callout.Locator("input.bit-drp-sin").FillAsync("0999");

        await Expect(Options(callout)).ToHaveTextAsync(["Item 0999"]);
    }

    [TestMethod]
    public async Task ItemsProvider_IsAskedForAWindow_AndEveryItemReportsItsPlaceInTheWholeSet()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-provider");

        var first = Option(callout, "Product 0001");
        await Expect(first).ToBeVisibleAsync();
        await Expect(first).ToHaveAttributeAsync("aria-setsize", "1000");
        await Expect(first).ToHaveAttributeAsync("aria-posinset", "1");
        await Expect(Page.Locator("#provider-search")).ToHaveTextAsync("(null)");
        Assert.IsTrue(int.Parse((await Page.Locator("#provider-calls").TextContentAsync())!) >= 1);

        await ListBox(callout).EvaluateAsync("el => el.scrollTop = 18000");

        var deep = Option(callout, "Product 0501");
        await Expect(deep).ToBeVisibleAsync();
        await Expect(deep).ToHaveAttributeAsync("aria-posinset", "501");
        Assert.IsTrue(int.Parse((await Page.Locator("#provider-start").TextContentAsync())!) > 400, "The window asked for starts where the list was scrolled to.");
        Assert.IsTrue(int.Parse((await Page.Locator("#provider-count").TextContentAsync())!) > 0);

        await WaitForStableListAsync(callout);
        await deep.ClickAsync();

        await Expect(Page.Locator("#provider-value")).ToHaveTextAsync("p501");
        await Expect(TextContainer("dd-provider")).ToHaveTextAsync("Product 0501");
    }

    [TestMethod]
    public async Task ItemsProvider_ReceivesTheSearch_AndTheCountComesFromTheProvider()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-provider-search");

        await callout.Locator("input.bit-drp-sin").FillAsync("99");

        await Expect(Page.Locator("#search-search")).ToHaveTextAsync("99");
        await Expect(Option(callout, "Product 0099")).ToBeVisibleAsync();
        await Expect(Option(callout, "Product 0099")).ToHaveAttributeAsync("aria-setsize", "19");
        await Expect(Option(callout, "Product 0001")).ToHaveCountAsync(0);
        await Expect(Root("dd-provider-search").Locator(".bit-drp-lvr")).ToHaveTextAsync("19 results available");
    }

    [TestMethod]
    public async Task ItemsProvider_InitialSelectedItems_NameTheSelectionBeforeItsWindowIsLoaded()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-provider-multi")).ToHaveTextAsync("Product 0700, Product 0900");
        await Expect(TextContainer("dd-provider-initial")).ToHaveTextAsync("Product 0500");

        var callout = await OpenDropdownAsync("dd-provider-multi");
        await Expect(Option(callout, "Product 0001")).ToBeVisibleAsync();
        await WaitForStableListAsync(callout);

        // Select all is not offered over a list only partly loaded, and neither is Ctrl+A.
        await Expect(callout.Locator(".bit-drp-sab")).ToHaveCountAsync(0);

        await Option(callout, "Product 0001").ClickAsync();
        await Expect(Page.Locator("#provider-multi-values")).ToHaveTextAsync("p700,p900,p1");

        await Page.Keyboard.PressAsync("Control+a");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#provider-multi-values")).ToHaveTextAsync("p700,p900,p1");

        var single = await OpenDropdownAsync("dd-provider-initial");
        await Expect(Option(single, "Product 0002")).ToBeVisibleAsync();
        await WaitForStableListAsync(single);
        await Option(single, "Product 0002").ClickAsync();
        await Expect(Page.Locator("#provider-initial-value")).ToHaveTextAsync("p2");
        await Expect(TextContainer("dd-provider-initial")).ToHaveTextAsync("Product 0002");
    }

    [TestMethod]
    public async Task ItemsProviderDebounceTime_CollapsesFastScrollingIntoFewRequests()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-provider-multi");
        await Expect(Option(callout, "Product 0001")).ToBeVisibleAsync();

        var before = int.Parse((await Page.Locator("#multi-calls").TextContentAsync())!);
        var scroller = ListBox(callout);

        foreach (var top in new[] { 3000, 6000, 9000, 12000, 15000 })
        {
            await scroller.EvaluateAsync($"el => el.scrollTop = {top}");
            await NextFramesAsync();
        }

        await Expect(Option(callout, "Product 0417")).ToBeVisibleAsync();

        var after = int.Parse((await Page.Locator("#multi-calls").TextContentAsync())!);
        Assert.IsTrue(after - before <= 2, $"Five scroll positions inside the debounce reach the provider at most twice, reached it {after - before} times.");
    }

    [TestMethod]
    public async Task VirtualizePlaceholder_FillsTheRowsStillBeingFetched()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-provider-slow");
        await Expect(Option(callout, "Slow 0001")).ToBeVisibleAsync();

        await ListBox(callout).EvaluateAsync("el => el.scrollTop = 18000");

        await Expect(callout.Locator(".virt-placeholder").First).ToBeVisibleAsync();
        await Expect(callout.Locator(".virt-placeholder").First).ToContainTextAsync("Loading row");

        await Expect(Option(callout, "Slow 0501")).ToBeVisibleAsync();
        await Expect(callout.Locator(".virt-placeholder")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task RefreshItemsAsync_AsksTheProviderAgain()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-provider");
        await Expect(Option(callout, "Product 0001")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        var calls = int.Parse((await Page.Locator("#provider-calls").TextContentAsync())!);

        await Page.Locator("#btn-rename").ClickAsync();
        await Page.Locator("#btn-refresh").ClickAsync();

        await Expect(Page.Locator("#provider-calls")).Not.ToHaveTextAsync(calls.ToString());

        callout = await OpenDropdownAsync("dd-provider");
        await Expect(Option(callout, "Renamed 0001")).ToBeVisibleAsync();
        await Expect(Option(callout, "Product 0001")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task IsLoading_ShowsASpinnerInTheFieldAndTheLoadingStateInTheList()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#dd-loading .bit-drp-wrp > .bit-drp-icn").Last;
        var spinner = icon.Locator(".bit-drp-spn");

        await Expect(spinner).ToBeVisibleAsync();
        await Expect(spinner).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(icon.Locator("i")).ToHaveCountAsync(0);
        Assert.AreNotEqual("none", await StyleAsync(spinner, "animation-name"), "The spinner turns.");
        await Expect(Root("dd-loading").Locator(".bit-drp-lvr")).ToHaveTextAsync("Loading...");

        var callout = await OpenDropdownAsync("dd-loading");
        var loading = callout.Locator(".bit-drp-ldc");

        await Expect(loading).ToBeVisibleAsync();
        await Expect(loading).ToContainTextAsync("Loading...");
        await Expect(loading.Locator(".bit-drp-spn")).ToBeVisibleAsync();
        await Expect(ListBox(callout)).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Options(callout)).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-drp-sab")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task IsLoading_LoadingTextAndLoadingTemplate_ReplaceTheDefaultMessage()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-loading-text").Locator(".bit-drp-lvr")).ToHaveTextAsync("Fetching the products...");

        var callout = await OpenDropdownAsync("dd-loading-text");
        await Expect(callout.Locator(".bit-drp-ldc")).ToContainTextAsync("Fetching the products...");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        callout = await OpenDropdownAsync("dd-loading-tpl");
        await Expect(callout.Locator(".bit-drp-ldc #loading-tpl-content")).ToHaveTextAsync("Just a moment");
        await Expect(callout.Locator(".bit-drp-ldc .bit-drp-spn")).ToHaveCountAsync(0);
        // The live region announces text, never the template.
        await Expect(Root("dd-loading-tpl").Locator(".bit-drp-lvr")).ToHaveTextAsync("Loading...");
    }

    [TestMethod]
    public async Task IsLoading_ARealFetch_HandsTheItemsToTheOpenList()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#fetch-state")).ToHaveTextAsync("loading");

        var callout = await OpenDropdownAsync("dd-loading-real");
        await Expect(callout.Locator(".bit-drp-ldc")).ToBeVisibleAsync();

        await Expect(Page.Locator("#fetch-state")).ToHaveTextAsync("loaded");
        await Expect(Options(callout)).ToHaveTextAsync(["Item 0001", "Item 0002", "Item 0003"]);
        await Expect(callout.Locator(".bit-drp-ldc")).ToHaveCountAsync(0);
        await Expect(ListBox(callout)).Not.ToHaveAttributeAsync("aria-busy", "true");

        await Option(callout, "Item 0002").ClickAsync();
        await Expect(TextContainer("dd-loading-real")).ToHaveTextAsync("Item 0002");
    }
}
