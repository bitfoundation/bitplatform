using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitCountryPicker at /e2e/country-picker in a real browser: opening and picking from the pointer and the
/// keyboard (arrows, Page keys, Home/End, Enter, Space, Escape, Alt+ArrowUp, Tab, type-ahead), the search box with its
/// ranking, empty result and live announcement, the two-way Value and IsOpen bindings, every event, the public methods,
/// Countries/ExcludeCountries/PreferredCountries, flags, templates, labels and their accessibility wiring, validation
/// in and out of an EditForm, Disabled/ReadOnly, the variants, every Color, Size, the public css variables reaching the
/// relocated callout, Style/Class/Styles/Classes, the BitParams cascade, RTL, the responsive panel, Escape inside a
/// dialog, prerendering and unmounting while open.
/// </summary>
[TestClass]
public class BitCountryPickerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/country-picker";

    [TestMethod]
    public async Task Basic_ClickTheField_OpensASearchableListOfEveryCountry()
    {
        await OpenAsync(Route);

        var field = Field("cnp-basic");
        await Expect(field).ToHaveRoleAsync(AriaRole.Combobox);
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(field).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Expect(field.Locator(".bit-cnp-pls")).ToHaveTextAsync("Select a country");

        var callout = await OpenCalloutAsync("cnp-basic");

        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-cnp-srch")).ToBeFocusedAsync();
        await Expect(callout.Locator("[role=listbox]")).ToBeVisibleAsync();
        Assert.IsTrue(await callout.Locator("[role=option]").CountAsync() > 200);
        await Expect(field).ToHaveAttributeAsync("aria-controls", (await callout.Locator("[role=listbox]").GetAttributeAsync("id"))!);

        // Positioned under the field, as wide as it.
        // Measured once the callout has finished sliding in, not partway through its entry transition.
        await callout.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished))");
        var fieldBox = (await field.BoundingBoxAsync())!;
        var calloutBox = (await callout.BoundingBoxAsync())!;
        Assert.IsTrue(calloutBox.Y >= fieldBox.Y + fieldBox.Height - 2, $"callout at {calloutBox.Y}, field bottom {fieldBox.Y + fieldBox.Height}");
        Assert.AreEqual(fieldBox.Width, calloutBox.Width, 2);
    }

    [TestMethod]
    public async Task Basic_PickACountry_BindsItClosesTheListAndFocusesTheField()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-basic");
        var option = callout.Locator("[role=option][title='Germany']");
        await option.ScrollIntoViewIfNeededAsync();
        await option.ClickAsync();

        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("DE");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("cnp-basic")).ToBeFocusedAsync();
        await Expect(Field("cnp-basic").Locator(".bit-cnp-txt")).ToHaveTextAsync("Germany");
        await Expect(Field("cnp-basic").Locator("img.bit-cnp-flg")).ToHaveAttributeAsync("src", "_content/Bit.BlazorUI.Extras/flags/DE-flat-16.webp");
        await Expect(Field("cnp-basic")).ToHaveAttributeAsync("title", "Germany");

        // Reopened, the selected country is the selected and the active option.
        callout = await OpenCalloutAsync("cnp-basic");
        var selected = callout.Locator("[role=option][aria-selected=true]");
        await Expect(selected).ToHaveCountAsync(1);
        await Expect(selected).ToHaveAttributeAsync("title", "Germany");
        await Expect(selected).ToHaveClassAsync(HasClass("bit-cnp-sel"));
        await Expect(selected).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Expect(selected).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task Binding_SetFromThePage_ShowsTheCountryAndClears()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-japan").ClickAsync();
        await Expect(Field("cnp-basic").Locator(".bit-cnp-txt")).ToHaveTextAsync("Japan");

        await Page.Locator("#btn-clear").ClickAsync();
        await Expect(Field("cnp-basic").Locator(".bit-cnp-pls")).ToHaveTextAsync("Select a country");
        await Expect(Field("cnp-basic").Locator(".bit-cnp-txt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Label_NamesTheFieldAndTheListAndAClickOnItOnlyFocuses()
    {
        await OpenAsync(Route);

        var label = Root("cnp-label").Locator("> label.bit-cnp-lbl");
        await Expect(label).ToHaveTextAsync("Country");
        var labelId = await label.GetAttributeAsync("id");
        await Expect(Field("cnp-label")).ToHaveAttributeAsync("aria-labelledby", labelId!);
        await Expect(Field("cnp-label")).ToHaveAccessibleNameAsync("Country");
        await Expect(Field("cnp-label")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));

        await label.ClickAsync();
        await Expect(Field("cnp-label")).ToBeFocusedAsync();
        await Expect(Field("cnp-label")).ToHaveAttributeAsync("aria-expanded", "false");

        var callout = await OpenCalloutAsync("cnp-label");
        await Expect(callout.Locator("[role=listbox]")).ToHaveAttributeAsync("aria-labelledby", labelId!);
    }

    [TestMethod]
    public async Task Label_RequiredAndDescription_AreWiredForAssistiveTechnologies()
    {
        await OpenAsync(Route);

        await Expect(Root("cnp-required")).ToHaveClassAsync(HasClass("bit-cnp-req"));
        await Expect(Field("cnp-required")).ToHaveAttributeAsync("aria-required", "true");
        var asterisk = await Root("cnp-required").Locator("> label").EvaluateAsync<string>("l => getComputedStyle(l, '::after').content");
        StringAssert.Contains(asterisk, "*");

        var description = Root("cnp-description").Locator(".bit-cnp-des");
        await Expect(description).ToHaveTextAsync("Where you have lived.");
        await Expect(Field("cnp-description")).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);
        await Expect(Field("cnp-description")).ToHaveAccessibleDescriptionAsync("Where you have lived.");

        await Expect(Root("cnp-label-template").Locator("label .custom-label")).ToHaveTextAsync("Templated label");
        await Expect(Root("cnp-label-template").Locator(".bit-cnp-des .custom-description")).ToHaveTextAsync("Templated description");
        await Expect(Field("cnp-label-template")).ToHaveAccessibleNameAsync("Templated label");
    }

    [TestMethod]
    public async Task AriaLabelAndTitle_NameAndTitleTheField()
    {
        await OpenAsync(Route);

        await Expect(Field("cnp-aria")).ToHaveAttributeAsync("aria-label", "Pick a market");
        await Expect(Field("cnp-aria")).ToHaveAttributeAsync("title", "Market field");
        await Expect(Root("cnp-aria")).ToHaveAttributeAsync("title", "Market field");

        // Unnamed, the field and the list fall back to their own names.
        await Expect(Field("cnp-basic")).ToHaveAttributeAsync("aria-label", "Country");
        var callout = await OpenCalloutAsync("cnp-basic");
        await Expect(callout.Locator("[role=listbox]")).ToHaveAttributeAsync("aria-label", "Countries");
    }

    [TestMethod]
    public async Task Defaults_DefaultValueAndDefaultCountry_PreselectAndOnChangeFires()
    {
        await OpenAsync(Route);

        await Expect(Field("cnp-default-value").Locator(".bit-cnp-txt")).ToHaveTextAsync("Canada");
        await Expect(Field("cnp-default-country").Locator(".bit-cnp-txt")).ToHaveTextAsync("Germany");
        await Expect(Page.Locator("#default-changed")).ToHaveTextAsync("null");

        // The very first opening scrolls the preselected country into view.
        var callout = await OpenCalloutAsync("cnp-default-value");
        var selected = callout.Locator("[role=option][aria-selected=true]");
        await Expect(selected).ToHaveAttributeAsync("title", "Canada");
        await Expect(selected).ToBeInViewportAsync();
        await callout.Locator(".bit-cnp-srch").FillAsync("Japan");
        await Expect(callout.Locator("[role=option]").First).ToHaveAttributeAsync("title", "Japan");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#default-changed")).ToHaveTextAsync("JP");
        await Expect(Field("cnp-default-value").Locator(".bit-cnp-txt")).ToHaveTextAsync("Japan");
    }

    [TestMethod]
    public async Task Countries_ReplaceTheListInTheirOwnOrder()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-countries");
        var titles = await callout.Locator("[role=option]").EvaluateAllAsync<string[]>("os => os.map(o => o.title)");
        CollectionAssert.AreEqual(new[] { "Canada", "Germany", "Japan", "Brazil", "Australia" }, titles);
    }

    [TestMethod]
    public async Task ExcludeCountries_LeaveThemOutOfTheList()
    {
        await OpenAsync(Route);

        var all = await (await OpenCalloutAsync("cnp-basic")).Locator("[role=option]").CountAsync();
        await Page.Keyboard.PressAsync("Escape");

        var callout = await OpenCalloutAsync("cnp-exclude");
        await Expect(callout.Locator("[role=option]")).ToHaveCountAsync(all - 3);
        await Expect(callout.Locator("[role=option][title='Canada']")).ToHaveCountAsync(0);
        await Expect(callout.Locator("[role=option][title='Japan']")).ToHaveCountAsync(0);
        await Expect(callout.Locator("[role=option][title='France']")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task PreferredCountries_ArePinnedFirstAndSetApartByARule()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-preferred");
        var options = callout.Locator("[role=option]");
        await Expect(options.Nth(0)).ToHaveAttributeAsync("title", "Japan");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("title", "Brazil");
        await Expect(options.Nth(1)).ToHaveClassAsync(HasClass("bit-cnp-ilp"));
        await Expect(options.Nth(0)).Not.ToHaveClassAsync(HasClass("bit-cnp-ilp"));
        Assert.AreNotEqual("0px", await options.Nth(1).EvaluateAsync<string>("o => getComputedStyle(o).borderBottomWidth"));

        // A search sets no rule apart.
        await callout.Locator(".bit-cnp-srch").FillAsync("a");
        await Expect(callout.Locator(".bit-cnp-ilp")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Search_ByNameAndByIsoCode_RanksTheClosestMatchFirstAndAnnouncesTheCount()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-search");
        var search = callout.Locator(".bit-cnp-srch");
        await Expect(search).ToHaveAttributeAsync("placeholder", "Country or ISO code");
        await Expect(search).ToHaveAttributeAsync("aria-label", "Country or ISO code");

        await search.PressSequentiallyAsync("germ");
        var options = callout.Locator("[role=option]");
        await Expect(options).ToHaveCountAsync(1);
        await Expect(options.First).ToHaveAttributeAsync("title", "Germany");
        await Expect(Root("cnp-search").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex("^1 country found"));
        await Expect(search).ToHaveAttributeAsync("aria-activedescendant", (await options.First.GetAttributeAsync("id"))!);

        await search.FillAsync("de");
        await Expect(options.First).ToHaveAttributeAsync("title", "Germany");
        await Expect(Root("cnp-search").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex(@"^\d+ countries found"));

        await search.FillAsync("Curaçao");
        await Expect(options.First).ToHaveAttributeAsync("title", "Curacao");

        await search.FillAsync("zzzz");
        await Expect(options).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-cnp-nor")).ToHaveTextAsync("No matching country");
        await Expect(Root("cnp-search").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex("^No matching country"));
        await Expect(search).Not.ToHaveAttributeAsync("aria-activedescendant", new Regex(".*"));
    }

    [TestMethod]
    public async Task Search_ArrowsAndEnter_PickTheActiveMatch()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-search");
        var search = callout.Locator(".bit-cnp-srch");
        await search.PressSequentiallyAsync("united");
        var options = callout.Locator("[role=option]");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-cnp-act"));

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.Nth(1)).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Expect(search).ToHaveAttributeAsync("aria-activedescendant", (await options.Nth(1).GetAttributeAsync("id"))!);
        var expected = await options.Nth(1).GetAttributeAsync("title");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("cnp-search").Locator(".bit-cnp-txt")).ToHaveTextAsync(expected!);
        await Expect(Field("cnp-search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Search_EscapeClosesAndTheNextOpeningStartsWithAnEmptySearch()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-search");
        await callout.Locator(".bit-cnp-srch").PressSequentiallyAsync("jap");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("cnp-search")).ToBeFocusedAsync();
        await Expect(Page.Locator("#search-value")).ToHaveTextAsync("null");

        callout = await OpenCalloutAsync("cnp-search");
        await Expect(callout.Locator(".bit-cnp-srch")).ToHaveValueAsync("");
        Assert.IsTrue(await callout.Locator("[role=option]").CountAsync() > 200);
    }

    [TestMethod]
    public async Task Keyboard_ClosedField_ArrowDownOpensAndALetterStartsASearch()
    {
        await OpenAsync(Route);

        await Field("cnp-search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var callout = await CalloutAsync("cnp-search");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-cnp-srch")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Keyboard.PressAsync("n");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-cnp-srch")).ToHaveValueAsync("n");
        await Page.Keyboard.PressAsync("o");
        await Page.Keyboard.PressAsync("r");
        await Expect(callout.Locator(".bit-cnp-srch")).ToHaveValueAsync("nor");
        await Expect(callout.Locator("[role=option]").First).ToHaveAttributeAsync("title", new Regex("^Nor"));
    }

    [TestMethod]
    public async Task Keyboard_TabOutOfTheSearchBox_ClosesTheList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-search");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("cnp-search")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Keyboard_AltArrowUp_ClosesTheList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-search");
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("cnp-search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_AltArrowUpAndDown_LeaveThePageWhereItIs()
    {
        await OpenAsync(Route);

        await Field("cnp-search").ScrollIntoViewIfNeededAsync();
        await Page.EvaluateAsync("() => window.scrollBy(0, 300)");
        var callout = await OpenCalloutAsync("cnp-search");
        var before = await Page.EvaluateAsync<double>("() => scrollY");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await NextFramesAsync();
        Assert.AreEqual(before, await Page.EvaluateAsync<double>("() => scrollY"));
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("cnp-search")).ToBeFocusedAsync();
        Assert.AreEqual(before, await Page.EvaluateAsync<double>("() => scrollY"));
    }

    [TestMethod]
    public async Task NoSearchBox_TheFieldDrivesTheListWithTypeAheadAndSpacePicks()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-nosearch");
        await Expect(callout.Locator(".bit-cnp-srch")).ToHaveCountAsync(0);
        await Expect(Field("cnp-nosearch")).ToBeFocusedAsync();
        var options = callout.Locator("[role=option]");
        await Expect(Field("cnp-nosearch")).ToHaveAttributeAsync("aria-activedescendant", (await options.First.GetAttributeAsync("id"))!);

        await Page.Keyboard.TypeAsync("jap");
        var active = callout.Locator(".bit-cnp-act");
        await Expect(active).ToHaveAttributeAsync("title", "Japan");
        await Expect(active).ToBeInViewportAsync();
        await Expect(Field("cnp-nosearch")).ToHaveAttributeAsync("aria-activedescendant", (await active.GetAttributeAsync("id"))!);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(active).Not.ToHaveAttributeAsync("title", "Japan");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(active).ToHaveAttributeAsync("title", "Japan");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#nosearch-value")).ToHaveTextAsync("JP");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Root("cnp-nosearch").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex("^Japan"));
        await Expect(Field("cnp-nosearch")).ToBeFocusedAsync();

        // With no type-ahead running, Space picks the active country.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(active).ToHaveAttributeAsync("title", "Japan");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(active).ToHaveAttributeAsync("title", "Jersey");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#nosearch-value")).ToHaveTextAsync("JE");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task NoSearchBox_SpaceInsideATypeAhead_IsPartOfTheTerm()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-nosearch");
        await Page.Keyboard.TypeAsync("united k");
        await Expect(callout.Locator(".bit-cnp-act")).ToHaveAttributeAsync("title", "United Kingdom");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#nosearch-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task NoSearchBox_HomeEndAndPageKeys_MoveTheActiveOption()
    {
        await OpenAsync(Route);

        await Field("cnp-nosearch").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        var callout = await CalloutAsync("cnp-nosearch");
        var options = callout.Locator("[role=option]");
        var count = await options.CountAsync();
        await Expect(options.Nth(count - 1)).ToHaveClassAsync(HasClass("bit-cnp-act"));

        await Page.Keyboard.PressAsync("Home");
        await Expect(options.Nth(0)).ToHaveClassAsync(HasClass("bit-cnp-act"));

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(options.Nth(10)).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(options.Nth(0)).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(options.Nth(0)).ToHaveClassAsync(HasClass("bit-cnp-act"));

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#nosearch-value")).Not.ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task NoWrapNavigation_StopsAtTheEndsAndWrappingIsTheDefault()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-nowrap");
        var options = callout.Locator("[role=option]");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("cnp-wrap");
        options = callout.Locator("[role=option]");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.Last).ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-cnp-act"));
    }

    [TestMethod]
    public async Task ClickOutside_ClosesTheListWithoutPicking()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-basic");
        await Page.Mouse.ClickAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("null");
        await Expect(Field("cnp-basic")).ToHaveAttributeAsync("aria-expanded", "false");

        // A second click on the field closes an open list too.
        callout = await OpenCalloutAsync("cnp-basic");
        await Field("cnp-basic").ClickAsync(new() { Force = true });
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Flags_NoFlagsAndFlagUrlSelector()
    {
        await OpenAsync(Route);

        await Expect(Field("cnp-noflags").Locator(".bit-cnp-flg")).ToHaveCountAsync(0);
        await Expect(Field("cnp-noflags").Locator(".bit-cnp-txt")).ToHaveTextAsync("Japan");
        var callout = await OpenCalloutAsync("cnp-noflags");
        await Expect(callout.Locator(".bit-cnp-flg")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Field("cnp-flag-selector").Locator("img.bit-cnp-flg")).ToHaveAttributeAsync("src", "samples/flags/ca.svg");
        callout = await OpenCalloutAsync("cnp-flag-selector");
        await Expect(callout.Locator("[title='Japan'] img")).ToHaveAttributeAsync("src", "samples/flags/jp.svg");
        await Expect(callout.Locator("[title='Brazil'] img")).ToHaveAttributeAsync("src", "_content/Bit.BlazorUI.Extras/flags/BR-flat-16.webp");
        await Expect(callout.Locator("[title='Brazil'] img")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Templates_RenderTheValueTheRowsAndTheEmptyResult()
    {
        await OpenAsync(Route);

        await Expect(Field("cnp-templates").Locator(".value-template")).ToHaveTextAsync("Portugal (PRT)");
        var callout = await OpenCalloutAsync("cnp-templates");
        await Expect(callout.Locator("[title='Spain'] .item-template")).ToHaveTextAsync("Spain / ES");
        await Expect(callout.Locator("[role=option] .bit-cnp-inm")).ToHaveCountAsync(0);
        await callout.Locator(".bit-cnp-srch").FillAsync("qqqq");
        await Expect(callout.Locator(".no-results-template")).ToHaveTextAsync("nothing here");

        await callout.Locator(".bit-cnp-srch").FillAsync("spai");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Field("cnp-templates").Locator(".value-template")).ToHaveTextAsync("Spain (ESP)");
    }

    [TestMethod]
    public async Task Validation_InvalidAndErrorMessage_MarkTheFieldAndDescribeIt()
    {
        await OpenAsync(Route);

        await Expect(Root("cnp-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Field("cnp-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.AreEqual(await ResolveColorAsync("--bit-clr-err"), await ComputedStyleAsync("#cnp-invalid .bit-cnp-fld", "border-top-color"));

        var error = Root("cnp-error").Locator(".bit-cnp-erm");
        await Expect(error).ToHaveTextAsync("We do not ship there yet.");
        var errorId = await error.GetAttributeAsync("id");
        var descriptionId = await Root("cnp-error").Locator(".bit-cnp-des").GetAttributeAsync("id");
        await Expect(Field("cnp-error")).ToHaveAttributeAsync("aria-describedby", $"{errorId} {descriptionId}");
        await Expect(Field("cnp-error")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cnp-error").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex("^We do not ship there yet."));

        // The error is written above the description.
        var errorBox = (await error.BoundingBoxAsync())!;
        var descriptionBox = (await Root("cnp-error").Locator(".bit-cnp-des").BoundingBoxAsync())!;
        Assert.IsTrue(errorBox.Y < descriptionBox.Y);

        await Expect(Root("cnp-error-template").Locator(".bit-cnp-erm .custom-error")).ToHaveTextAsync("Templated error");
        await Expect(Root("cnp-error-template")).ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Validation_EditForm_RejectsAnEmptyValueThenSubmitsAPickedOne()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Page.Locator("#validation .validation-message")).ToHaveTextAsync("Select your nationality.");
        await Expect(Field("cnp-form")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cnp-form")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#submitted")).ToHaveTextAsync("not submitted");

        var callout = await OpenCalloutAsync("cnp-form");
        await callout.Locator(".bit-cnp-srch").FillAsync("France");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Field("cnp-form")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Expect(Root("cnp-form")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Page.Locator("#submitted")).ToHaveTextAsync("Submitted: FR");
    }

    [TestMethod]
    public async Task Validation_NoValidate_LeavesTheFieldUnmarked()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-submit-2").ClickAsync();
        await Expect(Page.Locator("#novalidate-message .validation-message")).ToHaveTextAsync("Other is required.");
        await Expect(Field("cnp-form-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Expect(Root("cnp-form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Disabled_TheFieldIsNativelyDisabledAndRunsNoActivationHandler()
    {
        await OpenAsync(Route);

        var root = Root("cnp-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("cnp-disabled")).ToBeDisabledAsync();
        await Expect(Field("cnp-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.AreEqual(await ResolveColorAsync("--bit-clr-bg-dis"), await ComputedStyleAsync("#cnp-disabled .bit-cnp-fld", "background-color"));

        await Field("cnp-disabled").ClickAsync(new() { Force = true });
        await Field("cnp-disabled").DblClickAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(Field("cnp-disabled")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("0");

        // Out of the tab order.
        await Field("cnp-error-template").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Field("cnp-disabled")).Not.ToBeFocusedAsync();

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("cnp-disabled")).ToBeEnabledAsync();
        await Expect(Field("cnp-disabled")).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Field("cnp-disabled").ClickAsync();
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("Escape");

        // A second real click would land on the overlay of the list the first one opened - at once where the page runs
        // in the browser - so the double click is dispatched on the field.
        await Field("cnp-disabled").DispatchEventAsync("dblclick");
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ReadOnly_StaysFocusableButNeverOpens()
    {
        await OpenAsync(Route);

        await Expect(Root("cnp-readonly")).ToHaveClassAsync(HasClass("bit-cnp-rdo"));
        await Expect(Field("cnp-readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Field("cnp-readonly")).ToBeEnabledAsync();

        await Field("cnp-readonly").ClickAsync();
        await Expect(Field("cnp-readonly")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("a");
        await NextFramesAsync();
        await Expect(Field("cnp-readonly")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Field("cnp-readonly").Locator(".bit-cnp-txt")).ToHaveTextAsync("Spain");
        Assert.AreEqual("hidden", await ComputedStyleAsync("#cnp-readonly .bit-cnp-car", "visibility"));
        Assert.AreEqual("visible", await ComputedStyleAsync("#cnp-basic .bit-cnp-car", "visibility"));
    }

    [TestMethod]
    public async Task Variants_NoBorderAndUnderlined_ShapeTheFrame()
    {
        await OpenAsync(Route);

        await Expect(Root("cnp-noborder")).ToHaveClassAsync(HasClass("bit-cnp-nbd"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await ComputedStyleAsync("#cnp-noborder .bit-cnp-fld", "border-top-color"));

        await Expect(Root("cnp-underlined")).ToHaveClassAsync(HasClass("bit-cnp-und"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#cnp-underlined .bit-cnp-fld", "border-top-width"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#cnp-underlined .bit-cnp-fld", "border-left-width"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#cnp-underlined .bit-cnp-fld", "border-bottom-width"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#cnp-underlined .bit-cnp-fld", "border-top-left-radius"));

        Assert.AreNotEqual("0px", await ComputedStyleAsync("#cnp-plain .bit-cnp-fld", "border-top-width"));
    }

    [DataTestMethod]
    [DataRow("primary", "bit-cnp-bpr", "--bit-clr-bg-pri")]
    [DataRow("secondary", "bit-cnp-bse", "--bit-clr-bg-sec")]
    [DataRow("tertiary", "bit-cnp-btr", "--bit-clr-bg-ter")]
    [DataRow("transparent", "bit-cnp-btn", null)]
    public async Task Background_EachKind_FillsTheField(string kind, string cssClass, string? token)
    {
        await OpenAsync(Route);

        await Expect(Root($"cnp-bg-{kind}")).ToHaveClassAsync(HasClass(cssClass));
        var expected = token is null ? "rgba(0, 0, 0, 0)" : await ResolveColorAsync(token);
        Assert.AreEqual(expected, await ComputedStyleAsync($"#cnp-bg-{kind} .bit-cnp-fld", "background-color"));
    }

    [DataTestMethod]
    [DataRow("primary", "bit-cnp-brp", "--bit-clr-brd-pri")]
    [DataRow("secondary", "bit-cnp-brs", "--bit-clr-brd-sec")]
    [DataRow("tertiary", "bit-cnp-brt", "--bit-clr-brd-ter")]
    [DataRow("transparent", "bit-cnp-brn", null)]
    public async Task Border_EachKind_ColorsTheFrame(string kind, string cssClass, string? token)
    {
        await OpenAsync(Route);

        await Expect(Root($"cnp-brd-{kind}")).ToHaveClassAsync(HasClass(cssClass));
        var expected = token is null ? "rgba(0, 0, 0, 0)" : await ResolveColorAsync(token);
        Assert.AreEqual(expected, await ComputedStyleAsync($"#cnp-brd-{kind} .bit-cnp-fld", "border-top-color"));
    }

    [TestMethod]
    public async Task Callout_MaxHeight_CapsTheScrollingList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-maxheight");
        var scroller = callout.Locator(".bit-cnp-scn");
        var height = await scroller.EvaluateAsync<double>("s => s.getBoundingClientRect().height");
        Assert.IsTrue(height <= 200.5, $"The list is {height}px tall.");
        Assert.IsTrue(await scroller.EvaluateAsync<bool>("s => s.scrollHeight > s.clientHeight"));
    }

    [TestMethod]
    public async Task Events_EveryEventFiresWithItsArguments()
    {
        await OpenAsync(Route);

        await Field("cnp-events").FocusAsync();
        await Expect(Page.Locator("#ev-focus")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Page.Locator("#ev-open")).ToHaveTextAsync("1");
        var callout = await CalloutAsync("cnp-events");
        await callout.Locator(".bit-cnp-srch").PressSequentiallyAsync("ita");
        await Expect(Page.Locator("#ev-search")).ToHaveTextAsync("ita");
        await Expect(Page.Locator("#ev-search-count")).ToHaveTextAsync("3");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#ev-change")).ToHaveTextAsync("IT");
        await Expect(Page.Locator("#ev-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-close")).ToHaveTextAsync("1");

        // Picking the selected country again changes nothing.
        callout = await OpenCalloutAsync("cnp-events");
        await Expect(Page.Locator("#ev-open")).ToHaveTextAsync("2");
        await callout.Locator("[role=option][aria-selected=true]").ClickAsync();
        await Expect(Page.Locator("#ev-close")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#ev-change-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-open").FocusAsync();
        await Expect(Page.Locator("#ev-blur")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task IsOpen_TwoWayBinding_OpensFromThePageAndReportsTheUsersClose()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-toggle-open").ClickAsync();
        var callout = await CalloutAsync("cnp-controlled");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("True");
        await Expect(callout.Locator(".bit-cnp-srch")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("False");

        await Field("cnp-controlled").ClickAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("True");
        await Page.Locator("#btn-toggle-open").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task PublicMethods_OpenCloseSelectAndFocus()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open").ClickAsync();
        var callout = await CalloutAsync("cnp-controlled");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("True");

        await Page.Locator("#btn-close").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("False");

        await Page.Locator("#btn-select-japan").ClickAsync();
        await Expect(Page.Locator("#controlled-value")).ToHaveTextAsync("JP");
        await Expect(Page.Locator("#controlled-changes")).ToHaveTextAsync("1");
        await Expect(Field("cnp-controlled").Locator(".bit-cnp-txt")).ToHaveTextAsync("Japan");

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Field("cnp-controlled")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Name_PostsTheIsoCodeThroughAHiddenField()
    {
        await OpenAsync(Route);

        var hidden = Root("cnp-named").Locator("input[type=hidden]");
        await Expect(hidden).ToHaveAttributeAsync("name", "country");
        await Expect(hidden).ToHaveValueAsync("FR");
        var data = await Page.Locator("#plain-form").EvaluateAsync<string>("f => new FormData(f).get('country')");
        Assert.AreEqual("FR", data);

        var callout = await OpenCalloutAsync("cnp-named");
        await callout.Locator(".bit-cnp-srch").FillAsync("Japan");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(hidden).ToHaveValueAsync("JP");
    }

    [TestMethod]
    public async Task Localization_EveryTextAndTheAnnouncementAreTheApps()
    {
        await OpenAsync(Route);

        await Expect(Field("cnp-german").Locator(".bit-cnp-pls")).ToHaveTextAsync("Land auswählen");
        var callout = await OpenCalloutAsync("cnp-german");
        var search = callout.Locator(".bit-cnp-srch");
        await Expect(search).ToHaveAttributeAsync("placeholder", "Suchen");
        await Expect(search).ToHaveAttributeAsync("aria-label", "Land suchen");
        await Expect(callout.Locator("[role=option]")).ToHaveCountAsync(4);
        await Expect(callout.Locator("[role=option]").First).ToHaveAttributeAsync("title", "Deutschland");

        await search.FillAsync("schw");
        await Expect(Root("cnp-german").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex("^1 Land gefunden"));
        await search.FillAsync("r");
        await Expect(Root("cnp-german").Locator(".bit-cnp-lvr")).ToHaveTextAsync(new Regex(@"^\d Länder gefunden"));
        await search.FillAsync("xyz");
        await Expect(callout.Locator(".bit-cnp-nor")).ToHaveTextAsync("Kein Land gefunden");

        // A diacritic is folded away in the search.
        await search.FillAsync("oster");
        await Expect(callout.Locator("[role=option]").First).ToHaveAttributeAsync("title", "Österreich");
    }

    [TestMethod]
    public async Task Params_BitCountryPickerParams_CascadeAndAnOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(Root("cnp-params")).ToHaveClassAsync(HasClass("bit-cnp-nbd"));
        await Expect(Root("cnp-params")).ToHaveClassAsync(HasClass("bit-cnp-sm"));
        await Expect(Field("cnp-params").Locator(".bit-cnp-txt")).ToHaveTextAsync("Japan");
        var callout = await OpenCalloutAsync("cnp-params");
        await Expect(callout.Locator("[role=option]")).ToHaveCountAsync(5);
        await Expect(callout).ToHaveClassAsync(HasClass("bit-cnp-sm"));
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Field("cnp-params-own").Locator(".bit-cnp-txt")).ToHaveTextAsync("Brazil");
        await Expect(Root("cnp-params-own")).Not.ToHaveClassAsync(HasClass("bit-cnp-nbd"));
        await Expect(Root("cnp-params-own")).ToHaveClassAsync(HasClass("bit-cnp-sm"));

        await Expect(Root("cnp-params-outside")).Not.ToHaveClassAsync(HasClass("bit-cnp-nbd"));
        await Expect(Field("cnp-params-outside").Locator(".bit-cnp-pls")).ToHaveCountAsync(1);
    }

    [DataTestMethod]
    [DataRow("primary", "pri", "--bit-clr-pri")]
    [DataRow("secondary", "sec", "--bit-clr-sec")]
    [DataRow("tertiary", "ter", "--bit-clr-ter")]
    [DataRow("info", "inf", "--bit-clr-inf")]
    [DataRow("success", "suc", "--bit-clr-suc")]
    [DataRow("warning", "wrn", "--bit-clr-wrn")]
    [DataRow("severewarning", "swr", "--bit-clr-swr")]
    [DataRow("error", "err", "--bit-clr-err")]
    [DataRow("primarybackground", "pbg", "--bit-clr-bg-pri")]
    [DataRow("secondarybackground", "sbg", "--bit-clr-bg-sec")]
    [DataRow("tertiarybackground", "tbg", "--bit-clr-bg-ter")]
    [DataRow("primaryforeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("secondaryforeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("tertiaryforeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("primaryborder", "pbr", "--bit-clr-brd-pri")]
    [DataRow("secondaryborder", "sbr", "--bit-clr-brd-sec")]
    [DataRow("tertiaryborder", "tbr", "--bit-clr-brd-ter")]
    public async Task Color_EachValue_PaintsTheFrame(string name, string suffix, string token)
    {
        await OpenAsync(Route);

        await Expect(Root($"cnp-color-{name}")).ToHaveClassAsync(HasClass($"bit-cnp-{suffix}"));
        Assert.AreEqual(await ResolveColorAsync(token), await ComputedStyleAsync($"#cnp-color-{name} .bit-cnp-fld", "border-top-color"));
    }

    [DataTestMethod]
    [DataRow("small", "sm", "26px")]
    [DataRow("medium", "md", "32px")]
    [DataRow("large", "lg", "40px")]
    public async Task Size_EachValue_SetsTheHeightOfTheFieldAndItsList(string name, string suffix, string height)
    {
        await OpenAsync(Route);

        // The Fluent control heights: 26, 32 and 40px.
        await Expect(Root($"cnp-size-{name}")).ToHaveClassAsync(HasClass($"bit-cnp-{suffix}"));
        Assert.AreEqual(height, await ComputedStyleAsync($"#cnp-size-{name} .bit-cnp-fld", "min-height"));

        var callout = await OpenCalloutAsync($"cnp-size-{name}");
        await Expect(callout).ToHaveClassAsync(HasClass($"bit-cnp-{suffix}"));
    }

    [TestMethod]
    public async Task Size_DiffersBetweenValues()
    {
        await OpenAsync(Route);

        var small = (await Field("cnp-size-small").BoundingBoxAsync())!.Height;
        var medium = (await Field("cnp-size-medium").BoundingBoxAsync())!.Height;
        var large = (await Field("cnp-size-large").BoundingBoxAsync())!.Height;
        Assert.IsTrue(small < medium && medium < large, $"{small} < {medium} < {large}");
    }

    [TestMethod]
    public async Task CssVariables_OnOnePicker_ReskinTheFieldAndTheRelocatedCallout()
    {
        await OpenAsync(Route);

        Assert.AreEqual("12px", await ComputedStyleAsync("#cnp-vars .bit-cnp-fld", "border-top-left-radius"));
        Assert.AreEqual("rgb(255, 0, 0)", await ComputedStyleAsync("#cnp-vars .bit-cnp-fld", "border-top-color"));
        Assert.AreEqual("rgb(255, 255, 0)", await ComputedStyleAsync("#cnp-vars .bit-cnp-fld", "background-color"));

        var callout = await OpenCalloutAsync("cnp-vars");
        Assert.AreEqual("9px", await callout.EvaluateAsync<string>("c => getComputedStyle(c).borderTopLeftRadius"));

        // The selected row is the active one on opening; once the keyboard moves on it shows its selected fill.
        await Page.Keyboard.PressAsync("ArrowDown");
        var selected = callout.Locator("[aria-selected=true]");
        await Expect(selected).Not.ToHaveClassAsync(HasClass("bit-cnp-act"));
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(237, 231, 246)");

        // The callout lives in the body while open.
        Assert.IsFalse(await callout.EvaluateAsync<bool>("c => !!c.closest('#style')"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheFieldAndTheCallout()
    {
        await OpenAsync(Route);

        Assert.AreEqual("rgb(0, 128, 0)", await ComputedStyleAsync("#cnp-vars-scope .bit-cnp-fld", "color"));
        var callout = await OpenCalloutAsync("cnp-vars-scope");
        Assert.AreEqual("rgb(240, 248, 255)", await callout.EvaluateAsync<string>("c => getComputedStyle(c).backgroundColor"));
        var rowHeight = await callout.Locator("[role=option]").First.EvaluateAsync<double>("o => o.getBoundingClientRect().height");
        Assert.AreEqual(50, rowHeight, 1);
    }

    [TestMethod]
    public async Task CssVariables_LoseToAnExplicitColorAndSize()
    {
        await OpenAsync(Route);

        Assert.AreEqual(await ResolveColorAsync("--bit-clr-err"), await ComputedStyleAsync("#cnp-vars-wins .bit-cnp-fld", "border-top-color"));
        Assert.AreEqual("40px", await ComputedStyleAsync("#cnp-vars-wins .bit-cnp-fld", "min-height"));
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("cnp-styled");
        await Expect(root).ToHaveClassAsync(HasClass("custom-class"));
        Assert.AreEqual(250, (await root.BoundingBoxAsync())!.Width, 0.5);
        Assert.AreEqual("rgb(138, 43, 226)", await ComputedStyleAsync("#cnp-styled .bit-cnp-fld", "border-top-color"));
        await Expect(root.Locator(".bit-cnp-tcn")).ToHaveClassAsync(HasClass("custom-text"));
        await Expect(root.Locator(".bit-cnp-car")).ToHaveClassAsync(HasClass("custom-caret"));
        await Expect(root.Locator(".bit-cnp-fld img")).ToHaveClassAsync(HasClass("custom-flag"));

        var callout = await OpenCalloutAsync("cnp-styled");
        await Expect(callout).ToHaveClassAsync(HasClass("custom-callout"));
        Assert.AreEqual("rgb(1, 2, 3)", await callout.EvaluateAsync<string>("c => getComputedStyle(c).outlineColor"));
        await Expect(callout.Locator(".bit-cnp-sbx")).ToHaveClassAsync(HasClass("custom-search"));
        await Expect(callout.Locator("[role=option]").First).ToHaveClassAsync(HasClass("custom-item"));
        Assert.AreEqual("rgb(138, 43, 226)", await callout.Locator(".bit-cnp-inm").First.EvaluateAsync<string>("n => getComputedStyle(n).color"));
    }

    [TestMethod]
    public async Task Rtl_MirrorsTheFieldAndTheCallout()
    {
        await OpenAsync(Route);

        await Expect(Root("cnp-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("cnp-rtl")).ToHaveAttributeAsync("dir", "rtl");

        var callout = await OpenCalloutAsync("cnp-rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-rtl"));
        var option = callout.Locator("[role=option]").First;
        var flagBox = (await option.Locator(".bit-cnp-flg").BoundingBoxAsync())!;
        var nameBox = (await option.Locator(".bit-cnp-inm").BoundingBoxAsync())!;
        Assert.IsTrue(flagBox.X > nameBox.X, "The flag sits at the right of the name.");
        await Page.Keyboard.PressAsync("Escape");

        // Inherited from the page, the field reads right to left too.
        var field = Field("cnp-rtl-inherited");
        var fieldFlag = (await field.Locator(".bit-cnp-flg").BoundingBoxAsync())!;
        var fieldText = (await field.Locator(".bit-cnp-txt").BoundingBoxAsync())!;
        Assert.IsTrue(fieldFlag.X > fieldText.X, "The flag of the field sits at the right of its name.");
        var caret = (await field.Locator(".bit-cnp-car").BoundingBoxAsync())!;
        Assert.IsTrue(caret.X < fieldText.X, "The caret sits at the left end.");
    }

    [TestMethod]
    public async Task Responsive_OnASmallScreen_OpensASidePanelWithItsLabelAndClose()
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-cnp-res"));
        await Expect(callout.Locator(".bit-cnp-rlb")).ToHaveTextAsync("Responsive country");

        var close = callout.Locator(".bit-cnp-cls");
        await Expect(close).ToBeVisibleAsync();
        await Expect(close).ToHaveAttributeAsync("aria-label", "Close the list");

        // A full-height panel at the end side of the viewport, once its slide-in has run.
        await Expect(callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var box = (await callout.BoundingBoxAsync())!;
        Assert.AreEqual(700, box.Height, 2);
        Assert.AreEqual(375, box.X + box.Width, 2);

        await close.ClickAsync();
        await Expect(Field("cnp-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Field("cnp-responsive")).ToBeFocusedAsync();
        await Expect(callout).ToHaveCSSAsync("opacity", "0");
    }

    [TestMethod]
    public async Task Responsive_OnAWideScreen_StaysADropDown()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-cnp-res"));
        await Expect(callout.Locator(".bit-cnp-cls")).ToBeHiddenAsync();
        await Expect(callout.Locator(".bit-cnp-rlc")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task ExternalIcons_ResponsiveCloseIconAndIconName()
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-responsive-icon");
        await Expect(callout.Locator(".bit-cnp-cls i")).ToHaveClassAsync(HasClass("my-close-icon"));
        await callout.Locator(".bit-cnp-cls").ClickAsync();
        await Expect(Field("cnp-responsive-icon")).ToHaveAttributeAsync("aria-expanded", "false");

        // The closed panel slides out and fades rather than vanishing at once.
        await Expect(callout).ToHaveCSSAsync("opacity", "0");

        callout = await OpenCalloutAsync("cnp-responsive-iconname");
        await Expect(callout.Locator(".bit-cnp-cls i")).ToHaveClassAsync(new Regex("ChromeClose"));
        await Expect(callout.Locator(".bit-cnp-cls")).ToHaveAttributeAsync("aria-label", "Close");
    }

    [TestMethod]
    public async Task Dialog_EscapeClosesTheListFirstAndTheDialogOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Field("cnp-in-dialog")).ToBeVisibleAsync();

        var callout = await OpenCalloutAsync("cnp-in-dialog");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Field("cnp-in-dialog")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Lifecycle_Prerendered_TheServerHtmlCarriesTheField()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"cnp-basic\"");
        StringAssert.Contains(html, "Select a country");
        StringAssert.Contains(html, "role=\"combobox\"");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountedWhileOpen_LeavesNoErrorAndNoCallout()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("cnp-unmount");
        var calloutId = await callout.GetAttributeAsync("id");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Locator("#btn-unmount").DispatchEventAsync("click");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Root("cnp-unmount")).ToBeVisibleAsync();
        await OpenCalloutAsync("cnp-unmount");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Field(string id) => Page.Locator($"#{id} > .bit-cnp-fld");

    private async Task<ILocator> OpenCalloutAsync(string id)
    {
        // A list closed just before is closed on the .NET side too (its overlay is hidden from the render) before
        // the next one is opened, so a late close of the one cannot land on the other.
        await Expect(Page.Locator(".bit-cnp-ovl:visible")).ToHaveCountAsync(0);
        await Field(id).ScrollIntoViewIfNeededAsync();
        await Field(id).ClickAsync();
        var callout = await CalloutAsync(id);
        await Expect(callout).ToBeVisibleAsync();
        return callout;
    }

    private async Task<ILocator> CalloutAsync(string id)
    {
        // The root owns its callout through aria-owns while it is open.
        await Expect(Root(id)).ToHaveAttributeAsync("aria-owns", new Regex("callout"));
        var owns = await Root(id).GetAttributeAsync("aria-owns");
        return Page.Locator($"[id='{owns!.Split(' ').Last()}']");
    }

    private Task<string> ResolveColorAsync(string token)
    {
        return Page.EvaluateAsync<string>(@"t => {
            const probe = document.createElement('div');
            probe.style.color = `var(${t})`;
            document.body.appendChild(probe);
            const value = getComputedStyle(probe).color;
            probe.remove();
            return value;
        }", token);
    }

}
