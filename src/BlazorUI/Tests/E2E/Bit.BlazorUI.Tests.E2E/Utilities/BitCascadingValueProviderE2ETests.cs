namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitCascadingValueProvider in a real page (/e2e/cascading-value-provider), read through consumer components that
/// write what they received: values by type and by name in the prerendered HTML and after the circuit connects,
/// values built from state reaching only the consumers whose value changed, a BitCascadingValueList (typed nulls,
/// Set / Remove / Contains / Find) with Values winning a conflict, nesting, ValueType (an interface, an inner null
/// clearing an outer nullable), IsFixed and Enabled (and toggling them re-creating the content), change
/// notifications from a background thread without a page render (assigning Value, NotifyChanged after an in-place
/// mutation, NotifyChangedAsync awaited), AutoNotify through Observed (INotifyPropertyChanged and
/// INotifyCollectionChanged), Lazy and Computed values, and a provider unmounted while its value keeps changing.
/// </summary>
[TestClass]
public class BitCascadingValueProviderE2ETests : E2ETestBase
{
    private const string Route = "/e2e/cascading-value-provider";

    [TestMethod]
    public async Task Prerender_TheConsumersRenderTheCascadedValuesOnTheServer()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<span id=\"basic-theme\">Light</span>");
        StringAssert.Contains(html, "<span id=\"basic-user\">Yaser Moradi (CEO)</span>");
        StringAssert.Contains(html, "id=\"status\">Loading");
    }

    [TestMethod]
    public async Task Values_ABareValueByItsType_ATupleByItsName_AndNothingOutsideTheProvider()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#basic-theme")).ToHaveTextAsync("Light");
        await Expect(Page.Locator("#basic-auth")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#basic-named-user")).ToHaveTextAsync("Saleh Xafan (CTO)");
        await Expect(Page.Locator("#basic-user")).ToHaveTextAsync("Yaser Moradi (CEO)");
        await Expect(Page.Locator("#basic-outside")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task LiveValues_APageRenderPushesDownOnlyWhatChanged()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#live-theme")).ToHaveTextAsync("Light");
        await Expect(Page.Locator("#live-count")).ToHaveTextAsync("1");
        var themeRenders = int.Parse((await Page.Locator("#live-theme-renders").TextContentAsync())!);
        var countRenders = int.Parse((await Page.Locator("#live-count-renders").TextContentAsync())!);

        await Page.ClickAsync("#btn-live-theme");
        await Expect(Page.Locator("#live-theme")).ToHaveTextAsync("Dark");
        await Expect(Page.Locator("#live-theme-renders")).ToHaveTextAsync($"{themeRenders + 1}");
        await Expect(Page.Locator("#live-count-renders")).ToHaveTextAsync($"{countRenders}");

        await Page.ClickAsync("#btn-live-count");
        await Expect(Page.Locator("#live-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#live-count-renders")).ToHaveTextAsync($"{countRenders + 1}");
        await Expect(Page.Locator("#live-theme-renders")).ToHaveTextAsync($"{themeRenders + 1}");

        // A page render that changes nothing re-renders no consumer, and keeps their state.
        await Page.FillAsync("#live-theme-input", "typed");
        await Page.ClickAsync("#btn-live-rerender");
        await Expect(Page.Locator("#live-renders")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#live-theme-renders")).ToHaveTextAsync($"{themeRenders + 1}");
        await Expect(Page.Locator("#live-theme-input")).ToHaveValueAsync("typed");
    }

    [TestMethod]
    public async Task ValueList_TypedNullsSetRemoveContainsFind_AndValuesWinsAConflict()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#list-theme")).ToHaveTextAsync("Override");
        await Expect(Page.Locator("#list-find-theme")).ToHaveTextAsync("Light");
        await Expect(Page.Locator("#list-count")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#list-auth")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#list-user")).ToHaveTextAsync("Ava Smith (PM)");
        await Expect(Page.Locator("#list-contains-user")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-list-set");
        await Expect(Page.Locator("#list-count")).ToHaveTextAsync("1");
        await Page.ClickAsync("#btn-list-set");
        await Expect(Page.Locator("#list-count")).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-list-toggle-user");
        await Expect(Page.Locator("#list-contains-user")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#list-user")).ToHaveTextAsync("null");

        await Page.ClickAsync("#btn-list-toggle-user");
        await Expect(Page.Locator("#list-contains-user")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#list-user")).ToHaveTextAsync("Ava Smith (PM)");
        await Expect(Page.Locator("#list-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Nesting_AnInnerProviderOverridesOnlyWhatItRedefines()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#outer-theme")).ToHaveTextAsync("Light");
        await Expect(Page.Locator("#outer-count")).ToHaveTextAsync("7");
        await Expect(Page.Locator("#inner-theme")).ToHaveTextAsync("Dark");
        await Expect(Page.Locator("#inner-count")).ToHaveTextAsync("7");
        await Expect(Page.Locator("#inner-user")).ToHaveTextAsync("Ava Smith (PM)");
    }

    [TestMethod]
    public async Task ValueType_DecidesWhichConsumersAValueReaches_AndAnInnerNullClearsAnOuterValue()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#type-greeter")).ToHaveTextAsync("Hello");
        await Expect(Page.Locator("#type-outer-count")).ToHaveTextAsync("5");
        await Expect(Page.Locator("#type-inner-count")).ToHaveTextAsync("null");
        // Cascaded as the interface, it does not reach a consumer of the class that implements it...
        await Expect(Page.Locator("#type-greeter-class")).ToHaveTextAsync("null");
        // ...cascaded as its runtime type, it reaches both, the class being assignable to the interface.
        await Expect(Page.Locator("#type-runtime-greeter-class")).ToHaveTextAsync("By runtime type");
        await Expect(Page.Locator("#type-runtime-greeter")).ToHaveTextAsync("By runtime type");
    }

    [TestMethod]
    public async Task Enabled_ADisabledValueLetsTheOuterOneShowThrough_AndTogglingItReCreatesTheContent()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#flag-theme")).ToHaveTextAsync("Inner");
        await Page.FillAsync("#flag-theme-input", "typed");

        await Page.ClickAsync("#btn-flag-theme");
        await Expect(Page.Locator("#flag-theme-enabled")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#flag-theme")).ToHaveTextAsync("Outer");
        await Expect(Page.Locator("#flag-theme-input")).ToHaveValueAsync("");

        await Page.ClickAsync("#btn-flag-theme");
        await Expect(Page.Locator("#flag-theme")).ToHaveTextAsync("Inner");
        await Expect(Page.Locator("#flag-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task IsFixed_AFixedValueIsNotPushedAgain_AndTogglingIsFixedReCreatesTheContent()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#flag-count")).ToHaveTextAsync("3");
        var renders = await Page.Locator("#flag-count-renders").TextContentAsync();

        await Page.ClickAsync("#btn-flag-fixed-change");
        // A round trip of the page, after which a pushed value would have arrived.
        await Page.ClickAsync("#btn-flag-theme-value");
        await Expect(Page.Locator("#flag-theme")).ToHaveTextAsync("Changed");
        await Expect(Page.Locator("#flag-count")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#flag-count-renders")).ToHaveTextAsync(renders!);

        await Page.FillAsync("#flag-theme-input", "typed");
        await Page.ClickAsync("#btn-flag-isfixed");
        await Expect(Page.Locator("#flag-theme-input")).ToHaveValueAsync("");
        await Expect(Page.Locator("#flag-theme")).ToHaveTextAsync("Changed");
    }

    [TestMethod]
    public async Task ChangeNotifications_AValueAssignedOnABackgroundThread_ReachesTheConsumersWithoutAPageRender()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#notify-theme")).ToHaveTextAsync("Idle");

        await Page.ClickAsync("#btn-notify-assign");

        await Expect(Page.Locator("#notify-theme")).ToHaveTextAsync("Assigned");
        // The page itself was not rendered again: its own copy of the value still shows the old one.
        await Expect(Page.Locator("#notify-page-copy")).ToHaveTextAsync("Idle");
    }

    [TestMethod]
    public async Task ChangeNotifications_NotifyChangedPushesAnObjectMutatedInPlace()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#notify-status")).ToHaveTextAsync("Not started");

        await Page.ClickAsync("#btn-notify-mutate");

        await Expect(Page.Locator("#notify-status")).ToHaveTextAsync("Mutated");
    }

    [TestMethod]
    public async Task ChangeNotifications_NotifyChangedAsyncCompletesOnceTheConsumersHaveTheValue()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#notify-count")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-notify-async");

        await Expect(Page.Locator("#notify-async-done")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#notify-count")).ToHaveTextAsync("42");
    }

    [TestMethod]
    public async Task AutoNotify_AnObservedObjectAndCollection_RefreshTheirConsumersOnTheirOwn()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#observed-status")).ToHaveTextAsync("Waiting");
        await Expect(Page.Locator("#observed-items")).ToHaveTextAsync("first");

        await Page.ClickAsync("#btn-observed-property");
        await Expect(Page.Locator("#observed-status")).ToHaveTextAsync("Changed by itself");

        await Page.ClickAsync("#btn-observed-collection");
        await Expect(Page.Locator("#observed-items")).ToHaveTextAsync("first,item 2");
        await Page.ClickAsync("#btn-observed-collection");
        await Expect(Page.Locator("#observed-items")).ToHaveTextAsync("first,item 2,item 3");
    }

    [TestMethod]
    public async Task Lazy_TheFactoryRunsOnceWhenTheValueIsFirstProvided_NotWhileItIsDisabled()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#lazy-named-user")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#lazy-calls")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#lazy-created")).ToHaveTextAsync("False");

        await Page.ClickAsync("#btn-lazy-enable");
        await Expect(Page.Locator("#lazy-named-user")).ToHaveTextAsync("Lazy Larry (Factory)");

        // Read back by a page render.
        await Page.ClickAsync("#btn-computed-click");
        await Expect(Page.Locator("#lazy-calls")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#lazy-created")).ToHaveTextAsync("True");

        await Page.ClickAsync("#btn-lazy-enable");
        await Page.ClickAsync("#btn-lazy-enable");
        await Page.ClickAsync("#btn-computed-click");
        await Expect(Page.Locator("#lazy-named-user")).ToHaveTextAsync("Lazy Larry (Factory)");
        await Expect(Page.Locator("#lazy-calls")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Computed_TheFactoryRunsOnEveryRender_SoTheValueTracksItsSource()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#computed-flag")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#computed-theme")).ToHaveTextAsync("Even");
        await Expect(Page.Locator("#computed-count")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-computed-click");
        await Expect(Page.Locator("#computed-theme")).ToHaveTextAsync("Odd");
        await Expect(Page.Locator("#computed-count")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-computed-click");
        await Expect(Page.Locator("#computed-theme")).ToHaveTextAsync("Even");
        await Expect(Page.Locator("#computed-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Unmount_ARemovedProviderLetsGoOfItsValue_WhichKeepsChangingWithoutAnError()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#unmount-theme")).ToHaveTextAsync("Mounted");

        await Page.ClickAsync("#btn-unmount-assign");
        await Expect(Page.Locator("#unmount-theme")).ToHaveTextAsync("After 1");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-theme")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-unmount-assign");
        await Page.ClickAsync("#btn-unmount-assign");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-theme")).ToHaveTextAsync("After 3");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
