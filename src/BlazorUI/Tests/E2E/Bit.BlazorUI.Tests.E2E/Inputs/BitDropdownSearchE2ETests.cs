namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The search of BitDropdown (<c>/e2e/dropdown/search</c>): the search box with its accessible name, placeholder and
/// clear button, a committed search and an Immediate one, OnSearch, AutoFocusSearchBox, the term being discarded on
/// close, typing on a closed field and after arrowing into the list landing in the search box, the four SearchMode
/// values, MinSearchLength with MinSearchLengthText, SearchIgnoreDiacritics, HighlightSearch, a SearchFunction,
/// DebounceTime and ThrottleTime, the result count announced through SearchResultsText, grouping kept while searching,
/// HideSelectedItems in the count, and the EmptyText / EmptyTemplate / NoResultsText / NoResultsTemplate states.
/// </summary>
[TestClass]
public class BitDropdownSearchE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/search";

    private static ILocator SearchInput(ILocator callout) => callout.Locator("input.bit-drp-sin");

    [TestMethod]
    public async Task SearchBox_IsNamedAndPlaceholdered_AndACommittedSearchFiltersOnlyOnceItIsCommitted()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search");
        var input = SearchInput(callout);

        await Expect(input).ToHaveAttributeAsync("role", "searchbox");
        await Expect(input).ToHaveAttributeAsync("aria-label", "Find a fruit");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Search item");
        await Expect(input).ToHaveAttributeAsync("aria-controls", (await ListBox(callout).GetAttributeAsync("id"))!);
        // Without AutoFocusSearchBox the list takes the focus, like any opening.
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await input.FillAsync("ban");

        // Typed but not committed: the list is still the full one, while the clear button already follows the text.
        await Expect(Options(callout)).ToHaveCountAsync(7);
        await Expect(callout.Locator(".bit-drp-sb")).ToHaveClassAsync(HasClass("bit-drp-shv"));
        var clear = callout.GetByRole(AriaRole.Button, new() { Name = "Wipe the search", Exact = true });
        await Expect(clear).ToBeVisibleAsync();
        await Expect(Page.Locator("#search-count")).ToHaveTextAsync("0");

        await input.PressAsync("Enter");

        await Expect(Options(callout)).ToHaveCountAsync(1);
        await Expect(Option(callout, "Banana")).ToBeVisibleAsync();
        await Expect(Page.Locator("#search-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#search-last")).ToHaveTextAsync("ban");
        await Expect(Root("dd-search").Locator(".bit-drp-lvr")).ToHaveTextAsync("1 result available");
    }

    [TestMethod]
    public async Task SearchBox_TheClearButton_EmptiesTheTermAndHandsTheFocusBackToTheInput()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search");
        var input = SearchInput(callout);

        await input.FillAsync("car");
        await input.PressAsync("Enter");
        await Expect(Options(callout)).ToHaveCountAsync(1);

        await callout.Locator("button.bit-drp-sbc").ClickAsync();

        await Expect(input).ToHaveValueAsync("");
        await Expect(input).ToBeFocusedAsync();
        await Expect(Options(callout)).ToHaveCountAsync(7);
        await Expect(callout.Locator("button.bit-drp-sbc")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#search-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#search-last")).ToHaveTextAsync("(null)");
    }

    [TestMethod]
    public async Task SearchBox_TheTerm_IsDiscardedWhenTheCalloutCloses()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search");
        var input = SearchInput(callout);

        await input.FillAsync("gra");
        await input.PressAsync("Enter");
        await Expect(Options(callout)).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-search")).ToBeFocusedAsync();
        await Expect(Page.Locator("#search-last")).ToHaveTextAsync("(null)");

        callout = await OpenDropdownAsync("dd-search");
        await Expect(SearchInput(callout)).ToHaveValueAsync("");
        await Expect(Options(callout)).ToHaveCountAsync(7);
    }

    [TestMethod]
    public async Task Immediate_FiltersAsItIsTyped_HighlightsTheMatch_AndAnnouncesTheCount()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search-auto");
        var input = SearchInput(callout);

        await Expect(input).ToBeFocusedAsync();
        await Expect(callout.Locator(".bit-drp-sb")).ToHaveClassAsync(HasClass("bit-drp-shf"));

        await input.FillAsync("an");

        await Expect(Options(callout)).ToHaveCountAsync(2);
        await Expect(Option(callout, "Banana")).ToBeVisibleAsync();
        await Expect(Option(callout, "Orange")).ToBeVisibleAsync();

        var highlight = Option(callout, "Banana").Locator(".bit-drp-hlt");
        await Expect(highlight).ToHaveTextAsync("an");
        await Expect(Option(callout, "Banana").Locator(".bit-drp-itx")).ToHaveTextAsync("Banana");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await StyleAsync(highlight, "background-color"), "The match is painted in the accent.");
        Assert.IsTrue((await BoxAsync(highlight)).X > (await BoxAsync(Option(callout, "Banana").Locator(".bit-drp-itx"))).X, "The second and third letters are the ones painted.");

        await Expect(Root("dd-search-auto").Locator(".bit-drp-lvr")).ToHaveTextAsync("2 hits");

        // The grouping survives the search: only the group left naming nothing goes, with the divider.
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#search-auto-value")).ToHaveTextAsync("f-ban");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Immediate_TypingAfterArrowingIntoTheList_GoesBackToTheSearchBox()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search-auto");
        var input = SearchInput(callout);

        await input.PressSequentiallyAsync("a");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("p");

        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("ap");
        await Expect(Options(callout)).ToHaveCountAsync(2);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Backspace");

        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("a");
    }

    [TestMethod]
    public async Task MultiSelect_PickingFromASearch_KeepsTheTermAndTheSelectionSurvivesItsClear()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search-multi");
        var input = SearchInput(callout);

        await input.FillAsync("an");
        await Expect(Options(callout)).ToHaveCountAsync(2);

        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#search-multi-values")).ToHaveTextAsync("f-ban");
        await Expect(input).ToHaveValueAsync("an");
        await Expect(Options(callout)).ToHaveCountAsync(2);

        await callout.Locator("button.bit-drp-sbc").ClickAsync();

        await Expect(Options(callout)).ToHaveCountAsync(7);
        await Expect(Option(callout, "Banana")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task TypingOnAClosedField_OpensItAndTypesIntoTheSearchBox()
    {
        await OpenAsync(Url);

        await Trigger("dd-search-type").FocusAsync();
        await Page.Keyboard.PressAsync("b");

        var callout = await CalloutAsync("dd-search-type");
        var input = SearchInput(callout);

        await Expect(callout).ToBeVisibleAsync();
        await Expect(input).ToHaveValueAsync("b");
        await Expect(input).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("r");

        await Expect(input).ToHaveValueAsync("br");
        await Expect(Options(callout)).ToHaveCountAsync(1);
        await Expect(Option(callout, "Broccoli")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("Contains", new[] { "Banana", "Ant", "Pan", "an" })]
    [DataRow("StartsWith", new[] { "Ant", "an" })]
    [DataRow("EndsWith", new[] { "Pan", "an" })]
    [DataRow("ExactMatch", new[] { "an" })]
    public async Task SearchMode_DecidesHowTheTermMatches(string mode, string[] expected)
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync($"dd-mode-{mode}");

        await Expect(SearchInput(callout)).ToBeFocusedAsync();
        await SearchInput(callout).FillAsync("AN");

        await Expect(Options(callout)).ToHaveTextAsync(expected);
    }

    [TestMethod]
    public async Task MinSearchLength_ShowsTheFullListAndAHintUntilTheTermIsLongEnough()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-minlen");
        var input = SearchInput(callout);
        var hint = callout.Locator(".bit-drp-shn");
        var liveRegion = Root("dd-minlen").Locator(".bit-drp-lvr");

        await Expect(hint).ToHaveCountAsync(0);

        await input.PressSequentiallyAsync("a");
        await Expect(hint).ToHaveTextAsync("Please type 2 more");
        await Expect(hint).ToHaveAttributeAsync("role", "presentation");

        await input.PressSequentiallyAsync("p");
        await Expect(hint).ToHaveTextAsync("Please type 1 more");
        await Expect(liveRegion).ToHaveTextAsync("Please type 1 more");
        await Expect(Options(callout)).ToHaveCountAsync(7);
        // OnSearch reports the short term too.
        await Expect(Page.Locator("#minlen-last")).ToHaveTextAsync("ap");

        await input.PressSequentiallyAsync("p");

        await Expect(hint).ToHaveCountAsync(0);
        await Expect(Options(callout)).ToHaveCountAsync(1);
        await Expect(Option(callout, "Apple")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        var other = await OpenDropdownAsync("dd-minlen-default");
        await SearchInput(other).PressSequentiallyAsync("a");
        await Expect(other.Locator(".bit-drp-shn")).ToHaveTextAsync("Type 1 more character to search");
    }

    [TestMethod]
    public async Task SearchIgnoreDiacritics_FindsTheAccentedItem_AndHighlightsItsOwnText()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-diacritics");
        var input = SearchInput(callout);

        await input.FillAsync("muller");
        await Expect(Options(callout)).ToHaveTextAsync(["Müller"]);
        await Expect(Option(callout, "Müller").Locator(".bit-drp-hlt")).ToHaveTextAsync("Müller");

        await input.FillAsync("ngstr");
        await Expect(Options(callout)).ToHaveTextAsync(["Ångström"]);
        await Expect(Option(callout, "Ångström").Locator(".bit-drp-hlt")).ToHaveTextAsync("ngstr");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        var plain = await OpenDropdownAsync("dd-nodiacritics");
        await SearchInput(plain).FillAsync("muller");
        await Expect(Options(plain)).ToHaveCountAsync(0);
        await Expect(plain.Locator(".bit-drp-emp")).ToHaveTextAsync("No results found");
    }

    [TestMethod]
    public async Task SearchFunction_ReplacesTheBuiltInMatching()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-searchfn");

        await SearchInput(callout).FillAsync("v-");

        await Expect(Options(callout)).ToHaveTextAsync(["Broccoli", "Carrot", "Lettuce"]);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Vegetables"]);
        // The highlight is found by the built-in algorithm, which matches nothing of these texts.
        await Expect(callout.Locator(".bit-drp-hlt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task DebounceTime_SearchesOnceTheTypingPauses()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-debounce");

        await SearchInput(callout).PressSequentiallyAsync("ban", new() { Delay = 100 });

        await Expect(Options(callout)).ToHaveCountAsync(1);
        await Expect(Page.Locator("#debounce-last")).ToHaveTextAsync("ban");
        await Expect(Page.Locator("#debounce-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ThrottleTime_LetsTheFirstKeystrokeThroughAndThenOneSearchPerInterval()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-throttle");

        await SearchInput(callout).PressSequentiallyAsync("ban", new() { Delay = 100 });

        await Expect(Page.Locator("#throttle-last")).ToHaveTextAsync("ban");
        await Expect(Page.Locator("#throttle-count")).ToHaveTextAsync("2");
        await Expect(Options(callout)).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task GroupedSearch_KeepsEveryGroupThatStillHasAMatch()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search-group");
        var input = SearchInput(callout);

        await input.PressSequentiallyAsync("o");

        await Expect(Options(callout)).ToHaveTextAsync(["Orange", "Broccoli", "Carrot"]);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits", "Vegetables"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveCountAsync(1);

        await input.FillAsync("let");

        await Expect(Options(callout)).ToHaveTextAsync(["Lettuce"]);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Vegetables"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task HideSelectedItems_TheResultCount_LeavesOutTheHiddenSelection()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-search-hidesel");

        await SearchInput(callout).FillAsync("an");

        await Expect(Options(callout)).ToHaveTextAsync(["Orange"]);
        await Expect(Root("dd-search-hidesel").Locator(".bit-drp-lvr")).ToHaveTextAsync("1 result available");
    }

    [TestMethod]
    public async Task EmptyState_EmptyTextAndEmptyTemplate_FillAnEmptyList()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-empty");
        await Expect(callout.Locator(".bit-drp-emp")).ToHaveTextAsync("No items found");
        await Expect(callout.Locator(".bit-drp-emp")).ToHaveAttributeAsync("role", "presentation");
        await Expect(Options(callout)).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        callout = await OpenDropdownAsync("dd-empty-text");
        await Expect(callout.Locator(".bit-drp-emp")).ToHaveTextAsync("There is nothing here");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        callout = await OpenDropdownAsync("dd-empty-tpl");
        await Expect(callout.Locator(".bit-drp-emp #empty-tpl-content")).ToHaveTextAsync("Nothing to show!");
    }

    [TestMethod]
    [DataRow("dd-noresults", "No results found")]
    [DataRow("dd-noresults-text", "Nothing matched")]
    [DataRow("dd-noresults-tpl", "No match, try again")]
    [DataRow("dd-noresults-fallback", "The empty text")]
    public async Task NoResults_ASearchThatMatchesNothing_SaysSoInItsOwnWords(string id, string expected)
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync(id);

        await SearchInput(callout).FillAsync("zzz");

        await Expect(Options(callout)).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-drp-emp")).ToHaveTextAsync(expected);
        await Expect(Root(id).Locator(".bit-drp-lvr")).ToHaveTextAsync("0 results available");
    }
}
