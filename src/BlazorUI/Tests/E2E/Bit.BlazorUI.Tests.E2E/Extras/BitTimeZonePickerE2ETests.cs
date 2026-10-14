using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitTimeZonePicker at /e2e/time-zone-picker in a real browser: binding a string id, a TimeZoneInfo and a
/// BitTimeZone from both sides, DefaultValue with Windows and renamed ids, the list ordered by offset, the search by
/// place, country, id, region and offset with its announcement, type-ahead, the keyboard, ReferenceDate, NoOffset,
/// NoCountry, custom and preferred time zones, templates, labels and their accessibility wiring, validation in and out
/// of an EditForm, Disabled/ReadOnly, the variants, every Color, Size, the css variables reaching the relocated callout,
/// Style/Class/Styles/Classes, BitParams, localization, RTL, the responsive panel, Escape inside a dialog, the public
/// methods, prerendering and unmounting while open. DetectTimeZone is driven by <see cref="BitTimeZonePickerDetectE2ETests"/>.
/// </summary>
[TestClass]
public class BitTimeZonePickerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/time-zone-picker";

    [TestMethod]
    public async Task Basic_PickATimeZone_BindsItsIanaId()
    {
        await OpenAsync(Route);

        var field = Field("tzp-basic");
        await Expect(field).ToHaveRoleAsync(AriaRole.Combobox);
        await Expect(field).ToHaveAttributeAsync("aria-label", "Time zone");
        await Expect(field.Locator(".bit-tzp-pls")).ToHaveTextAsync("Select a time zone");

        var callout = await OpenCalloutAsync("tzp-basic");
        await Expect(callout.Locator(".bit-tzp-srch")).ToBeFocusedAsync();
        await Expect(callout.Locator("[role=listbox]")).ToHaveAttributeAsync("aria-label", "Time zones");
        Assert.IsTrue(await callout.Locator("[role=option]").CountAsync() > 300);

        await callout.Locator(".bit-tzp-srch").FillAsync("Tokyo");
        var option = callout.Locator("[role=option]").First;
        await Expect(option).ToHaveAttributeAsync("title", "(UTC+09:00) Tokyo, Japan");
        await Expect(option.Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC+09:00");
        await Expect(option.Locator(".bit-tzp-inm")).ToHaveTextAsync("Tokyo");
        await Expect(option.Locator(".bit-tzp-icn")).ToHaveTextAsync("Japan");
        await option.ClickAsync();

        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("Asia/Tokyo");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(field).ToBeFocusedAsync();
        await Expect(field.Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC+09:00");
        await Expect(field.Locator(".bit-tzp-txt")).ToHaveTextAsync("Tokyo");
        await Expect(field).ToHaveAttributeAsync("title", "(UTC+09:00) Tokyo, Japan");
        await Expect(field).ToHaveAccessibleNameAsync("Time zone");
    }

    [TestMethod]
    public async Task Basic_TheListIsOrderedWestToEast()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-basic");
        var offsets = await callout.Locator("[role=option] .bit-tzp-ofs").EvaluateAllAsync<string[]>("os => os.map(o => o.textContent)");
        var minutes = offsets.Select(o =>
        {
            var sign = o[3] == '-' ? -1 : 1;
            return sign * (int.Parse(o.Substring(4, 2)) * 60 + int.Parse(o.Substring(7, 2)));
        }).ToArray();
        CollectionAssert.AreEqual(minutes.OrderBy(m => m).ToArray(), minutes);
        StringAssert.StartsWith(offsets[0], "UTC-1");
        StringAssert.StartsWith(offsets[^1], "UTC+1");
    }

    [TestMethod]
    public async Task Binding_SetFromThePage_ResolvesAnyIdAndClears()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-tokyo").ClickAsync();
        await Expect(Field("tzp-basic").Locator(".bit-tzp-txt")).ToHaveTextAsync("Tokyo");

        await Page.Locator("#btn-set-windows").ClickAsync();
        await Expect(Field("tzp-basic").Locator(".bit-tzp-txt")).ToHaveTextAsync("Tehran");

        // Reopened, the bound zone is the selected and the active option, scrolled into view.
        var callout = await OpenCalloutAsync("tzp-basic");
        var selected = callout.Locator("[role=option][aria-selected=true]");
        await Expect(selected).ToHaveCountAsync(1);
        await Expect(selected.Locator(".bit-tzp-inm")).ToHaveTextAsync("Tehran");
        await Expect(selected).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Expect(selected).ToBeInViewportAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-clear").ClickAsync();
        await Expect(Field("tzp-basic").Locator(".bit-tzp-pls")).ToHaveTextAsync("Select a time zone");
    }

    [TestMethod]
    public async Task Binding_ATimeZoneInfo_RoundTripsBothWays()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-tzi");
        await callout.Locator(".bit-tzp-srch").FillAsync("Tehran");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#tzi-id")).ToHaveTextAsync(new Regex("^(Asia/Tehran|Iran Standard Time)$"));
        await Expect(Page.Locator("#tzi-offset")).ToHaveTextAsync("UTC+03:30");

        await Page.Locator("#btn-set-tzi").ClickAsync();
        await Expect(Field("tzp-tzi").Locator(".bit-tzp-txt")).ToHaveTextAsync("Kolkata");
        await Expect(Field("tzp-tzi").Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC+05:30");
    }

    [TestMethod]
    public async Task Binding_ABitTimeZone_CarriesItsNameAndCountry()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-btz");
        await callout.Locator(".bit-tzp-srch").FillAsync("sao pa");
        await Expect(callout.Locator("[role=option]").First.Locator(".bit-tzp-inm")).ToHaveTextAsync("Sao Paulo");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#btz-name")).ToHaveTextAsync("Sao Paulo");
        await Expect(Page.Locator("#btz-country")).ToHaveTextAsync("BR");
    }

    [TestMethod]
    public async Task Label_NamesTheFieldAndTheListAndAClickOnItOnlyFocuses()
    {
        await OpenAsync(Route);

        var label = Root("tzp-label").Locator("> label");
        await Expect(Field("tzp-label")).ToHaveAccessibleNameAsync("Time zone");
        await Expect(Field("tzp-label")).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        await label.ClickAsync();
        await Expect(Field("tzp-label")).ToBeFocusedAsync();
        await Expect(Field("tzp-label")).ToHaveAttributeAsync("aria-expanded", "false");

        await Expect(Root("tzp-required")).ToHaveClassAsync(HasClass("bit-tzp-req"));
        await Expect(Field("tzp-required")).ToHaveAttributeAsync("aria-required", "true");
        StringAssert.Contains(await Root("tzp-required").Locator("> label").EvaluateAsync<string>("l => getComputedStyle(l, '::after').content"), "*");

        await Expect(Field("tzp-description")).ToHaveAccessibleDescriptionAsync("Where the office is.");
        await Expect(Root("tzp-label-template").Locator("label .custom-label")).ToHaveTextAsync("Templated label");
        await Expect(Root("tzp-label-template").Locator(".custom-description")).ToHaveTextAsync("Templated description");
        await Expect(Field("tzp-label-template")).ToHaveAccessibleNameAsync("Templated label");

        await Expect(Field("tzp-aria")).ToHaveAttributeAsync("aria-label", "Meeting zone");
        await Expect(Field("tzp-aria")).ToHaveAttributeAsync("title", "Zone field");
    }

    [TestMethod]
    public async Task DefaultValue_AnyIdLandsOnItsCanonicalZoneAndOnChangeFires()
    {
        await OpenAsync(Route);

        await Expect(Field("tzp-default").Locator(".bit-tzp-txt")).ToHaveTextAsync("Tokyo");
        await Expect(Field("tzp-windows").Locator(".bit-tzp-txt")).ToHaveTextAsync("Tehran");
        await Expect(Field("tzp-renamed").Locator(".bit-tzp-txt")).ToHaveTextAsync("Kolkata");

        // An id nothing knows is written as it is rather than taken for an empty field.
        await Expect(Field("tzp-unknown").Locator(".bit-tzp-txt")).ToHaveTextAsync("Mars/Olympus_Mons");
        await Expect(Field("tzp-unknown").Locator(".bit-tzp-ofs")).ToHaveCountAsync(0);

        var callout = await OpenCalloutAsync("tzp-default");
        await Expect(callout.Locator("[aria-selected=true]")).ToBeInViewportAsync();
        await callout.Locator(".bit-tzp-srch").FillAsync("Paris");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#default-changed")).ToHaveTextAsync("Europe/Paris");
    }

    [TestMethod]
    public async Task TimeZones_ReplaceTheListWhichIsShownByOffset()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-offices");
        var names = await callout.Locator("[role=option] .bit-tzp-inm").EvaluateAllAsync<string[]>("os => os.map(o => o.textContent)");
        CollectionAssert.AreEqual(new[] { "New York", "London", "Berlin", "Tehran", "Singapore" }, names);
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("tzp-europe");
        var ids = await callout.Locator("[role=option] .bit-tzp-inm").CountAsync();
        Assert.IsTrue(ids > 30);
        await Expect(callout.Locator("[role=option][title*='Tokyo']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task PreferredTimeZones_ArePinnedFirstAndSetApartByARule()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-preferred");
        var options = callout.Locator("[role=option]");
        await Expect(options.Nth(0).Locator(".bit-tzp-inm")).ToHaveTextAsync("Berlin");
        await Expect(options.Nth(1).Locator(".bit-tzp-inm")).ToHaveTextAsync("Tehran");
        await Expect(options.Nth(2).Locator(".bit-tzp-inm")).ToHaveTextAsync("New York");
        await Expect(options.Nth(2)).ToHaveClassAsync(HasClass("bit-tzp-ilp"));
        await Expect(callout.Locator(".bit-tzp-ilp")).ToHaveCountAsync(1);
    }

    [DataTestMethod]
    [DataRow("sao pa", "Sao Paulo")]
    [DataRow("paris fr", "Paris")]
    [DataRow("Asia/Calcutta", "Kolkata")]
    [DataRow("UTC", "Coordinated Universal Time")]
    [DataRow("+3:30", "Tehran")]
    [DataRow("japan", "Tokyo")]
    public async Task Search_ByPlaceCountryIdAndOffset_FindsTheZone(string term, string name)
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-search");
        await callout.Locator(".bit-tzp-srch").FillAsync(term);
        await Expect(callout.Locator("[role=option] .bit-tzp-inm").First).ToHaveTextAsync(name);
    }

    [TestMethod]
    public async Task Search_ByAnOffsetWrittenAnyWay_FindsEveryZoneOfIt()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-search");
        foreach (var term in new[] { "gmt+0530", "utc+5:30", "+05:30" })
        {
            await callout.Locator(".bit-tzp-srch").FillAsync(term);
            await Expect(callout.Locator("[role=option] .bit-tzp-inm", new() { HasTextString = "Kolkata" })).ToHaveCountAsync(1);
            await Expect(callout.Locator("[role=option] .bit-tzp-ofs").Last).ToHaveTextAsync("UTC+05:30");
            var offsets = await callout.Locator("[role=option] .bit-tzp-ofs").EvaluateAllAsync<string[]>("os => os.map(o => o.textContent)");
            Assert.IsTrue(offsets.All(o => o == "UTC+05:30"), $"{term}: {string.Join(", ", offsets)}");
        }
    }

    [TestMethod]
    public async Task Search_ByRegionAndWholeHour_AnnouncesTheCountAndHandlesNoResults()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-search");
        var search = callout.Locator(".bit-tzp-srch");
        await Expect(search).ToHaveAttributeAsync("placeholder", "City, country or offset");

        await search.FillAsync("pacific");
        await Expect(callout.Locator("[role=option] .bit-tzp-inm", new() { HasTextString = "Los Angeles" })).ToHaveCountAsync(1);

        await search.FillAsync("+5");
        await Expect(callout.Locator("[role=option] .bit-tzp-ofs").First).ToHaveTextAsync(new Regex(@"^UTC\+05:"));
        await Expect(callout.Locator("[role=option] .bit-tzp-ofs").Last).ToHaveTextAsync(new Regex(@"^UTC\+05:"));
        var offsets = await callout.Locator("[role=option] .bit-tzp-ofs").EvaluateAllAsync<string[]>("os => os.map(o => o.textContent)");
        Assert.IsTrue(offsets.Length > 2);
        Assert.IsTrue(offsets.All(o => o.StartsWith("UTC+05:", StringComparison.Ordinal)), string.Join(", ", offsets));
        await Expect(Root("tzp-search").Locator(".bit-tzp-lvr")).ToHaveTextAsync(new Regex($@"^{offsets.Length} time zones found"));

        await search.FillAsync("tehran");
        await Expect(Root("tzp-search").Locator(".bit-tzp-lvr")).ToHaveTextAsync(new Regex("^1 time zone found"));

        await search.FillAsync("zzzz");
        await Expect(callout.Locator("[role=option]")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-tzp-nor")).ToHaveTextAsync("No matching time zone");
        await Expect(Root("tzp-search").Locator(".bit-tzp-lvr")).ToHaveTextAsync(new Regex("^No matching time zone"));
    }

    [TestMethod]
    public async Task Keyboard_TypingOnTheClosedFieldOpensWithTheSearchStarted()
    {
        await OpenAsync(Route);

        await Field("tzp-search").FocusAsync();
        await Page.Keyboard.PressAsync("t");
        var callout = await CalloutAsync("tzp-search");
        await Expect(callout).ToBeVisibleAsync();
        var search = callout.Locator(".bit-tzp-srch");
        await Expect(search).ToHaveValueAsync("t");
        await Expect(search).ToBeFocusedAsync();
        await search.PressSequentiallyAsync("ehr", new() { Delay = 50 });
        await Expect(search).ToHaveValueAsync("tehr");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#search-value")).ToHaveTextAsync("Asia/Tehran");
        await Expect(Field("tzp-search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_ArrowsEscapeAltUpAndTab()
    {
        await OpenAsync(Route);

        await Field("tzp-search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var callout = await CalloutAsync("tzp-search");
        await Expect(callout).ToBeVisibleAsync();
        var options = callout.Locator("[role=option]");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.Nth(1)).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Expect(callout.Locator(".bit-tzp-srch")).ToHaveAttributeAsync("aria-activedescendant", (await options.Nth(1).GetAttributeAsync("id"))!);
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(options.Nth(11)).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(options.Nth(1)).ToHaveClassAsync(HasClass("bit-tzp-act"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("tzp-search")).ToBeFocusedAsync();
        await Expect(Page.Locator("#search-value")).ToHaveTextAsync("null");

        await Page.Keyboard.PressAsync("End");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(options.Last).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Expect(callout.Locator(".bit-tzp-srch")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("tzp-search")).ToHaveAttributeAsync("aria-expanded", "false");

        // Alt+ArrowUp last: it also scrolls the page (see Keyboard_AltArrowUp_LeavesThePageWhereItIs).
        await Field("tzp-search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-tzp-srch")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("tzp-search")).ToBeFocusedAsync();
        await Expect(Page.Locator("#search-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    [Ignore("KNOWN BUG: Alt+ArrowUp closes the open list but its default is not prevented (the prevent-keys script of Extras skips every modified key), so Chromium also scrolls the page by a screen, which in turn dismisses the callout as a page scroll and drops the focus to the body until the field takes it back. Alt+ArrowDown in an open list scrolls the page down, dismisses the list and leaves the focus on the body.")]
    public async Task Keyboard_AltArrowUp_LeavesThePageWhereItIs()
    {
        await OpenAsync(Route);

        await Field("tzp-search").ScrollIntoViewIfNeededAsync();
        await Page.EvaluateAsync("() => window.scrollBy(0, 300)");
        var callout = await OpenCalloutAsync("tzp-search");
        var before = await Page.EvaluateAsync<double>("() => scrollY");
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("tzp-search")).ToBeFocusedAsync();
        Assert.AreEqual(before, await Page.EvaluateAsync<double>("() => scrollY"));
    }

    [TestMethod]
    public async Task NoSearchBox_TheFieldDrivesTheListWithTypeAhead()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-nosearch");
        await Expect(callout.Locator(".bit-tzp-srch")).ToHaveCountAsync(0);
        await Expect(Field("tzp-nosearch")).ToBeFocusedAsync();

        await Field("tzp-nosearch").PressSequentiallyAsync("teh", new() { Delay = 80 });
        var active = callout.Locator(".bit-tzp-act");
        await Expect(active.Locator(".bit-tzp-inm")).ToHaveTextAsync("Tehran");
        await Expect(active).ToBeInViewportAsync();
        await Expect(Field("tzp-nosearch")).ToHaveAttributeAsync("aria-activedescendant", (await active.GetAttributeAsync("id"))!);
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#nosearch-value")).ToHaveTextAsync("Asia/Tehran");
        await Expect(Root("tzp-nosearch").Locator(".bit-tzp-lvr")).ToHaveTextAsync(new Regex("Tehran"));
    }

    [TestMethod]
    public async Task NoWrapNavigation_StopsAtTheEndsAndWrappingIsTheDefault()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-nowrap");
        var options = callout.Locator("[role=option]");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("tzp-wrap");
        options = callout.Locator("[role=option]");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.Last).ToHaveClassAsync(HasClass("bit-tzp-act"));
    }

    [TestMethod]
    public async Task ReferenceDate_ComputesTheOffsetsForThatMoment()
    {
        await OpenAsync(Route);

        await Expect(Field("tzp-january").Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC-05:00");
        await Expect(Field("tzp-july").Locator(".bit-tzp-ofs")).ToHaveTextAsync("UTC-04:00");
    }

    [TestMethod]
    public async Task NoOffsetAndNoCountry_LeaveThemOutOfTheFieldAndTheList()
    {
        await OpenAsync(Route);

        await Expect(Field("tzp-nooffset").Locator(".bit-tzp-ofs")).ToHaveCountAsync(0);
        await Expect(Field("tzp-nooffset")).ToHaveAttributeAsync("title", "Tokyo, Japan");
        var callout = await OpenCalloutAsync("tzp-nooffset");
        await Expect(callout.Locator(".bit-tzp-ofs")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Field("tzp-nocountry")).ToHaveAttributeAsync("title", "(UTC+09:00) Tokyo");
        callout = await OpenCalloutAsync("tzp-nocountry");
        await Expect(callout.Locator(".bit-tzp-icn")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-tzp-ofs").First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Templates_RenderTheValueTheRowsAndTheEmptyResult()
    {
        await OpenAsync(Route);

        await Expect(Field("tzp-templates").Locator(".value-template")).ToHaveTextAsync("Sydney / AU");
        var callout = await OpenCalloutAsync("tzp-templates");
        await Expect(callout.Locator("[aria-selected=true] .item-template")).ToHaveTextAsync("Sydney / Australia/Sydney");
        await callout.Locator(".bit-tzp-srch").FillAsync("qqqq");
        await Expect(callout.Locator(".no-results-template")).ToHaveTextAsync("nothing here");
    }

    [TestMethod]
    public async Task Validation_InvalidAndErrorMessage()
    {
        await OpenAsync(Route);

        await Expect(Root("tzp-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Field("tzp-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.AreEqual(await ResolveColorAsync("--bit-clr-err"), await ComputedStyleAsync("#tzp-invalid .bit-tzp-fld", "border-top-color"));

        await Expect(Root("tzp-error").Locator(".bit-tzp-erm")).ToHaveTextAsync("We have no office there yet.");
        await Expect(Field("tzp-error")).ToHaveAccessibleDescriptionAsync(new Regex("We have no office there yet."));
        await Expect(Field("tzp-error")).ToHaveAccessibleDescriptionAsync(new Regex("Pick an office."));
        await Expect(Root("tzp-error").Locator(".bit-tzp-lvr")).ToHaveTextAsync(new Regex("^We have no office there yet."));
        await Expect(Root("tzp-error-template").Locator(".custom-error")).ToHaveTextAsync("Templated error");
    }

    [TestMethod]
    public async Task Validation_EditForm_RejectsAnEmptyValueThenSubmitsAPickedOne()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Page.Locator("#validation .validation-message").First).ToHaveTextAsync("Select your time zone.");
        await Expect(Field("tzp-form")).ToHaveAttributeAsync("aria-invalid", "true");

        var callout = await OpenCalloutAsync("tzp-form");
        await callout.Locator(".bit-tzp-srch").FillAsync("London");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Field("tzp-form")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Page.Locator("#submitted")).ToHaveTextAsync("Europe/London");
    }

    [TestMethod]
    public async Task Validation_NoValidate_LeavesTheFieldUnmarked()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-submit-2").ClickAsync();
        await Expect(Page.Locator("#novalidate-message .validation-message")).ToHaveTextAsync("Select your time zone.");
        await Expect(Field("tzp-form-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Expect(Root("tzp-form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Disabled_TheFieldIsNativelyDisabledAndRunsNoActivationHandler()
    {
        await OpenAsync(Route);

        await Expect(Root("tzp-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("tzp-disabled")).ToBeDisabledAsync();
        await Expect(Field("tzp-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Field("tzp-disabled").ClickAsync(new() { Force = true });
        await Field("tzp-disabled").DblClickAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(Field("tzp-disabled")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(Field("tzp-disabled")).ToBeEnabledAsync();
        await Expect(Field("tzp-disabled")).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Field("tzp-disabled").DblClickAsync();
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task ReadOnly_StaysFocusableButNeverOpens()
    {
        await OpenAsync(Route);

        await Expect(Root("tzp-readonly")).ToHaveClassAsync(HasClass("bit-tzp-rdo"));
        await Expect(Field("tzp-readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Field("tzp-readonly").ClickAsync();
        await Expect(Field("tzp-readonly")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("a");
        await NextFramesAsync();
        await Expect(Field("tzp-readonly")).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.AreEqual("hidden", await ComputedStyleAsync("#tzp-readonly .bit-tzp-car", "visibility"));
    }

    [TestMethod]
    public async Task Variants_NoBorderAndUnderlined()
    {
        await OpenAsync(Route);

        await Expect(Root("tzp-noborder")).ToHaveClassAsync(HasClass("bit-tzp-nbd"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await ComputedStyleAsync("#tzp-noborder .bit-tzp-fld", "border-top-color"));
        await Expect(Root("tzp-underlined")).ToHaveClassAsync(HasClass("bit-tzp-und"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#tzp-underlined .bit-tzp-fld", "border-top-width"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#tzp-underlined .bit-tzp-fld", "border-bottom-width"));
    }

    [DataTestMethod]
    [DataRow("primary", "bit-tzp-bpr", "--bit-clr-bg-pri")]
    [DataRow("secondary", "bit-tzp-bse", "--bit-clr-bg-sec")]
    [DataRow("tertiary", "bit-tzp-btr", "--bit-clr-bg-ter")]
    [DataRow("transparent", "bit-tzp-btn", null)]
    public async Task Background_EachKind_FillsTheField(string kind, string cssClass, string? token)
    {
        await OpenAsync(Route);

        await Expect(Root($"tzp-bg-{kind}")).ToHaveClassAsync(HasClass(cssClass));
        var expected = token is null ? "rgba(0, 0, 0, 0)" : await ResolveColorAsync(token);
        Assert.AreEqual(expected, await ComputedStyleAsync($"#tzp-bg-{kind} .bit-tzp-fld", "background-color"));
    }

    [DataTestMethod]
    [DataRow("primary", "bit-tzp-brp", "--bit-clr-brd-pri")]
    [DataRow("secondary", "bit-tzp-brs", "--bit-clr-brd-sec")]
    [DataRow("tertiary", "bit-tzp-brt", "--bit-clr-brd-ter")]
    [DataRow("transparent", "bit-tzp-brn", null)]
    public async Task Border_EachKind_ColorsTheFrame(string kind, string cssClass, string? token)
    {
        await OpenAsync(Route);

        await Expect(Root($"tzp-brd-{kind}")).ToHaveClassAsync(HasClass(cssClass));
        var expected = token is null ? "rgba(0, 0, 0, 0)" : await ResolveColorAsync(token);
        Assert.AreEqual(expected, await ComputedStyleAsync($"#tzp-brd-{kind} .bit-tzp-fld", "border-top-color"));
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

        await Expect(Root($"tzp-color-{name}")).ToHaveClassAsync(HasClass($"bit-tzp-{suffix}"));
        Assert.AreEqual(await ResolveColorAsync(token), await ComputedStyleAsync($"#tzp-color-{name} .bit-tzp-fld", "border-top-color"));
    }

    [DataTestMethod]
    [DataRow("small", "sm", "26px")]
    [DataRow("medium", "md", "32px")]
    [DataRow("large", "lg", "40px")]
    public async Task Size_EachValue_SetsTheHeight(string name, string suffix, string height)
    {
        await OpenAsync(Route);

        await Expect(Root($"tzp-size-{name}")).ToHaveClassAsync(HasClass($"bit-tzp-{suffix}"));
        Assert.AreEqual(height, await ComputedStyleAsync($"#tzp-size-{name} .bit-tzp-fld", "min-height"));
        var callout = await OpenCalloutAsync($"tzp-size-{name}");
        await Expect(callout).ToHaveClassAsync(HasClass($"bit-tzp-{suffix}"));
    }

    [TestMethod]
    public async Task CssVariables_OnOnePicker_ReskinItAndItsRelocatedCallout()
    {
        await OpenAsync(Route);

        Assert.AreEqual("12px", await ComputedStyleAsync("#tzp-vars .bit-tzp-fld", "border-top-left-radius"));
        Assert.AreEqual("rgb(255, 0, 0)", await ComputedStyleAsync("#tzp-vars .bit-tzp-fld", "border-top-color"));
        Assert.AreEqual("rgb(255, 255, 0)", await ComputedStyleAsync("#tzp-vars .bit-tzp-fld", "background-color"));
        Assert.AreEqual("rgb(0, 0, 255)", await ComputedStyleAsync("#tzp-vars .bit-tzp-fld .bit-tzp-ofs", "color"));

        var callout = await OpenCalloutAsync("tzp-vars");
        Assert.AreEqual("9px", await callout.EvaluateAsync<string>("c => getComputedStyle(c).borderTopLeftRadius"));
        Assert.AreEqual("rgb(0, 0, 255)", await callout.Locator(".bit-tzp-ofs").First.EvaluateAsync<string>("o => getComputedStyle(o).color"));
        await Page.Keyboard.PressAsync("ArrowDown");
        var selected = callout.Locator("[aria-selected=true]");
        await Expect(selected).Not.ToHaveClassAsync(HasClass("bit-tzp-act"));
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(237, 231, 246)");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheFieldAndTheCallout()
    {
        await OpenAsync(Route);

        Assert.AreEqual("rgb(0, 128, 0)", await ComputedStyleAsync("#tzp-vars-scope .bit-tzp-fld", "color"));
        var callout = await OpenCalloutAsync("tzp-vars-scope");
        Assert.AreEqual("rgb(240, 248, 255)", await callout.EvaluateAsync<string>("c => getComputedStyle(c).backgroundColor"));
        Assert.AreEqual("rgb(0, 0, 128)", await callout.Locator(".bit-tzp-icn").First.EvaluateAsync<string>("c => getComputedStyle(c).color"));
        Assert.AreEqual(50, await callout.Locator("[role=option]").First.EvaluateAsync<double>("o => o.getBoundingClientRect().height"), 1);
    }

    [TestMethod]
    public async Task CssVariables_LoseToAnExplicitColorAndSize()
    {
        await OpenAsync(Route);

        Assert.AreEqual(await ResolveColorAsync("--bit-clr-err"), await ComputedStyleAsync("#tzp-vars-wins .bit-tzp-fld", "border-top-color"));
        Assert.AreEqual("40px", await ComputedStyleAsync("#tzp-vars-wins .bit-tzp-fld", "min-height"));
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("tzp-styled");
        await Expect(root).ToHaveClassAsync(HasClass("custom-class"));
        Assert.AreEqual(300, (await root.BoundingBoxAsync())!.Width, 0.5);
        Assert.AreEqual("rgb(138, 43, 226)", await ComputedStyleAsync("#tzp-styled .bit-tzp-fld", "border-top-color"));
        await Expect(root.Locator(".bit-tzp-tcn")).ToHaveClassAsync(HasClass("custom-text"));
        await Expect(root.Locator(".bit-tzp-fld .bit-tzp-ofs")).ToHaveClassAsync(HasClass("custom-offset"));

        var callout = await OpenCalloutAsync("tzp-styled");
        await Expect(callout).ToHaveClassAsync(HasClass("custom-callout"));
        await Expect(callout.Locator("[role=option]").First).ToHaveClassAsync(HasClass("custom-item"));
        await Expect(callout.Locator(".bit-tzp-icn").First).ToHaveClassAsync(HasClass("custom-country"));
        Assert.AreEqual("rgb(138, 43, 226)", await callout.Locator(".bit-tzp-inm").First.EvaluateAsync<string>("n => getComputedStyle(n).color"));
    }

    [TestMethod]
    public async Task Rtl_MirrorsTheFieldAndTheCallout()
    {
        await OpenAsync(Route);

        await Expect(Root("tzp-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("tzp-rtl")).ToHaveAttributeAsync("dir", "rtl");
        foreach (var id in new[] { "tzp-rtl", "tzp-rtl-inherited" })
        {
            var text = (await Field(id).Locator(".bit-tzp-txt").BoundingBoxAsync())!;
            var caret = (await Field(id).Locator(".bit-tzp-car").BoundingBoxAsync())!;
            Assert.IsTrue(caret.X < text.X, $"{id}: the caret sits at the left end.");
        }

        var callout = await OpenCalloutAsync("tzp-rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-rtl"));
        var option = callout.Locator("[role=option]").First;
        var name = (await option.Locator(".bit-tzp-inm").BoundingBoxAsync())!;
        var country = (await option.Locator(".bit-tzp-icn").BoundingBoxAsync())!;
        Assert.IsTrue(name.X > country.X, "The place reads before its country, from the right.");
    }

    [TestMethod]
    public async Task Responsive_OnASmallScreen_OpensASidePanel()
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-tzp-res"));
        await Expect(callout.Locator(".bit-tzp-rlb")).ToHaveTextAsync("Responsive zone");
        var close = callout.Locator(".bit-tzp-cls");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Close the list");
        await Expect(callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var box = (await callout.BoundingBoxAsync())!;
        Assert.AreEqual(700, box.Height, 2);
        Assert.AreEqual(375, box.X + box.Width, 2);

        await close.ClickAsync();
        await Expect(Field("tzp-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Field("tzp-responsive")).ToBeFocusedAsync();

        // The closed panel is inert, so the Tab key never walks into it.
        await Expect(callout).ToHaveAttributeAsync("inert", "");
    }

    [TestMethod]
    public async Task ExternalIcons_ResponsiveCloseIconAndIconName()
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-responsive-icon");
        await Expect(callout.Locator(".bit-tzp-cls i")).ToHaveClassAsync(HasClass("my-close-icon"));
        await callout.Locator(".bit-tzp-cls").ClickAsync();
        await Expect(Field("tzp-responsive-icon")).ToHaveAttributeAsync("aria-expanded", "false");

        callout = await OpenCalloutAsync("tzp-responsive-iconname");
        await Expect(callout.Locator(".bit-tzp-cls i")).ToHaveClassAsync(new Regex("ChromeClose"));
    }

    [TestMethod]
    public async Task Callout_MaxHeight_CapsTheList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-maxheight");
        var height = await callout.Locator(".bit-tzp-scn").EvaluateAsync<double>("s => s.getBoundingClientRect().height");
        Assert.IsTrue(height <= 200.5, $"The list is {height}px tall.");
    }

    [TestMethod]
    public async Task Events_EveryEventFiresWithItsArguments()
    {
        await OpenAsync(Route);

        await Field("tzp-events").FocusAsync();
        await Expect(Page.Locator("#ev-focus")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Page.Locator("#ev-open")).ToHaveTextAsync("1");
        var callout = await CalloutAsync("tzp-events");
        await callout.Locator(".bit-tzp-srch").PressSequentiallyAsync("lond", new() { Delay = 50 });
        await Expect(Page.Locator("#ev-search")).ToHaveTextAsync("lond");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#ev-change")).ToHaveTextAsync("Europe/London");
        await Expect(Page.Locator("#ev-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-close")).ToHaveTextAsync("1");

        await Page.Locator("#btn-open").FocusAsync();
        await Expect(Page.Locator("#ev-blur")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task IsOpen_TwoWayBinding()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-toggle-open").ClickAsync();
        var callout = await CalloutAsync("tzp-controlled");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("True");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task PublicMethods_OpenCloseSelectFocusAndSelectedTimeZone()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open").ClickAsync();
        var callout = await CalloutAsync("tzp-controlled");
        await Expect(callout).ToBeVisibleAsync();
        await Page.Locator("#btn-close").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-select-id").ClickAsync();
        await Expect(Page.Locator("#controlled-value")).ToHaveTextAsync("Asia/Tokyo");
        await Page.Locator("#btn-read-selected").ClickAsync();
        await Expect(Page.Locator("#selected-name")).ToHaveTextAsync("Tokyo");

        await Page.Locator("#btn-select-tzi").ClickAsync();
        await Expect(Page.Locator("#controlled-value")).ToHaveTextAsync("Etc/UTC");
        await Expect(Field("tzp-controlled").Locator(".bit-tzp-txt")).ToHaveTextAsync("Coordinated Universal Time");

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Field("tzp-controlled")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Name_PostsTheIdThroughAHiddenField()
    {
        await OpenAsync(Route);

        var data = await Page.Locator("#plain-form").EvaluateAsync<string>("f => new FormData(f).get('timeZone')");
        Assert.AreEqual("Europe/Paris", data);
    }

    [TestMethod]
    public async Task Localization_EveryTextIsTheApps()
    {
        await OpenAsync(Route);

        await Expect(Field("tzp-german").Locator(".bit-tzp-pls")).ToHaveTextAsync("Zeitzone wählen");
        var callout = await OpenCalloutAsync("tzp-german");
        var search = callout.Locator(".bit-tzp-srch");
        await Expect(search).ToHaveAttributeAsync("placeholder", "Suchen");
        await Expect(search).ToHaveAttributeAsync("aria-label", "Zeitzone suchen");
        await Expect(callout.Locator("[title*='Wien, Österreich']")).ToHaveCountAsync(1);
        await search.FillAsync("zurich");
        await Expect(callout.Locator("[role=option] .bit-tzp-inm").First).ToHaveTextAsync("Zürich");
        await Expect(Root("tzp-german").Locator(".bit-tzp-lvr")).ToHaveTextAsync(new Regex("^1 Zeitzone gefunden"));
        await search.FillAsync("xyzq");
        await Expect(callout.Locator(".bit-tzp-nor")).ToHaveTextAsync("Keine Zeitzone gefunden");
    }

    [TestMethod]
    public async Task Params_BitTimeZonePickerParams_CascadeAndAnOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(Root("tzp-params")).ToHaveClassAsync(HasClass("bit-tzp-nbd"));
        await Expect(Root("tzp-params")).ToHaveClassAsync(HasClass("bit-tzp-sm"));
        await Expect(Field("tzp-params").Locator(".bit-tzp-pls")).ToHaveTextAsync("Cascaded placeholder");
        var callout = await OpenCalloutAsync("tzp-params");
        await Expect(callout.Locator("[role=option]")).ToHaveCountAsync(5);
        await Expect(callout.Locator(".bit-tzp-icn")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("tzp-params-own")).Not.ToHaveClassAsync(HasClass("bit-tzp-nbd"));
        await Expect(Field("tzp-params-own").Locator(".bit-tzp-pls")).ToHaveTextAsync("Own placeholder");
        await Expect(Root("tzp-params-outside")).Not.ToHaveClassAsync(HasClass("bit-tzp-nbd"));
    }

    [TestMethod]
    public async Task Dialog_EscapeClosesTheListFirstAndTheDialogOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        var callout = await OpenCalloutAsync("tzp-in-dialog");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Field("tzp-in-dialog")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ClickOutside_ClosesTheList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-search");
        await Page.Mouse.ClickAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#search-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Lifecycle_Prerendered_TheServerHtmlCarriesTheField()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"tzp-basic\"");
        StringAssert.Contains(html, "Select a time zone");
        StringAssert.Contains(html, "UTC&#x2B;09:00");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountedWhileOpen_LeavesNoErrorAndNoCallout()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("tzp-unmount");
        var calloutId = await callout.GetAttributeAsync("id");
        await Page.Locator("#btn-unmount").DispatchEventAsync("click");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await OpenCalloutAsync("tzp-unmount");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Field(string id) => Page.Locator($"#{id} > .bit-tzp-fld");

    private async Task<ILocator> OpenCalloutAsync(string id)
    {
        // A list closed just before is closed on the .NET side too (its overlay is hidden from the render) before
        // the next one is opened, so a late close of the one cannot land on the other.
        await Expect(Page.Locator(".bit-tzp-ovl:visible")).ToHaveCountAsync(0);
        await Field(id).ScrollIntoViewIfNeededAsync();
        await Field(id).ClickAsync();
        var callout = await CalloutAsync(id);
        await Expect(callout).ToBeVisibleAsync();
        return callout;
    }

    private async Task<ILocator> CalloutAsync(string id)
    {
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

/// <summary>
/// Drives DetectTimeZone of BitTimeZonePicker at /e2e/time-zone-picker/detect, in a browser context set to run in
/// Asia/Kolkata: an empty picker preselects the visitor's own zone (bound as an id and as a TimeZoneInfo), a picker
/// with a value keeps it, and a disabled one detects nothing.
/// </summary>
[TestClass]
public class BitTimeZonePickerDetectE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new() { TimezoneId = "Asia/Kolkata" };

    [TestMethod]
    public async Task DetectTimeZone_PreselectsTheBrowsersOwnZone()
    {
        await OpenAsync("/e2e/time-zone-picker/detect");

        await Expect(Page.Locator("#detected")).ToHaveTextAsync("Asia/Kolkata");
        await Expect(Page.Locator("#tzp-detect > .bit-tzp-fld .bit-tzp-txt")).ToHaveTextAsync("Kolkata");
        await Expect(Page.Locator("#detected-tzi")).ToHaveTextAsync(new Regex("^(Asia/Kolkata|India Standard Time)$"));
    }

    [TestMethod]
    public async Task DetectTimeZone_KeepsAValueAndSkipsADisabledPicker()
    {
        await OpenAsync("/e2e/time-zone-picker/detect");

        await Expect(Page.Locator("#detected")).ToHaveTextAsync("Asia/Kolkata");
        await Expect(Page.Locator("#tzp-detect-kept > .bit-tzp-fld .bit-tzp-txt")).ToHaveTextAsync("Tokyo");
        await Expect(Page.Locator("#tzp-detect-disabled > .bit-tzp-fld .bit-tzp-pls")).ToHaveTextAsync("Disabled");
        await Expect(Page.Locator("#tzp-no-detect > .bit-tzp-fld .bit-tzp-pls")).ToHaveTextAsync("Not detected");
    }
}
