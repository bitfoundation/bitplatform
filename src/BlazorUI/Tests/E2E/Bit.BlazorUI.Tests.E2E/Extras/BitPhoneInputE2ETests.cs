using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitPhoneInput at /e2e/phone-input (and /e2e/phone-input/autofocus) in a real browser: the three two-way
/// bindings (Value, Number, Country) from both sides, a typed international number switching the country, the
/// national prefix, Mask / MaskSelector / AutoPlaceholder formatting as you type with the caret kept in place,
/// Strict, MaxLength, Immediate with DebounceTime and ThrottleTime, the country list (search by name and dialing
/// code, keyboard, type-ahead, NoWrapNavigation, preferred, custom and excluded countries), the dropdown's display
/// options and NoDropdown, the clear button and its announcement, every event, every public method, validation in
/// and out of an EditForm, Disabled/ReadOnly, the variants, every Color, Size, the public css variables reaching the
/// relocated callout, Style/Class/Styles/Classes, BitParams, localization, autofill attributes, RTL, the responsive
/// panel, Escape inside a dialog, AutoFocus, prerendering and unmounting while open.
/// </summary>
[TestClass]
public class BitPhoneInputE2ETests : E2ETestBase
{
    private const string Route = "/e2e/phone-input";

    [TestMethod]
    public async Task Basic_TypeANumber_ComposesTheE164ValueOnChange()
    {
        await OpenAsync(Route);

        var dropdown = Dropdown("phi-basic");
        await Expect(dropdown).ToHaveAttributeAsync("aria-label", "Country: United States");
        await Expect(dropdown).ToHaveAttributeAsync("title", "United States");
        await Expect(dropdown.Locator(".bit-phi-cod")).ToHaveTextAsync("+1");
        await Expect(dropdown.Locator("img.bit-phi-flg")).ToHaveAttributeAsync("src", "_content/Bit.BlazorUI.Extras/flags/US-flat-16.webp");

        var input = Input("phi-basic");
        await Expect(input).ToHaveAttributeAsync("type", "tel");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Enter your number");
        await input.FillAsync("2025550123");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("null");
        await input.BlurAsync();

        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+12025550123");
        await Expect(Page.Locator("#basic-number")).ToHaveTextAsync("2025550123");
        await Expect(Page.Locator("#basic-country")).ToHaveTextAsync("US");
    }

    [TestMethod]
    public async Task Basic_TypeAnInternationalNumber_SwitchesTheCountry()
    {
        await OpenAsync(Route);

        var input = Input("phi-basic");
        await input.FillAsync("+44 20 7946 0958");
        await input.BlurAsync();

        await Expect(Page.Locator("#basic-country")).ToHaveTextAsync("GB");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+442079460958");
        await Expect(Dropdown("phi-basic")).ToHaveAttributeAsync("aria-label", "Country: United Kingdom");
        await Expect(input).ToHaveValueAsync("2079460958");

        // The 00 international prefix is read the same way.
        await input.FillAsync("0049 170 1234567");
        await input.BlurAsync();
        await Expect(Page.Locator("#basic-country")).ToHaveTextAsync("DE");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+491701234567");
    }

    [TestMethod]
    public async Task Binding_ValueNumberAndCountry_StayInStepFromThePage()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-value").ClickAsync();
        await Expect(Page.Locator("#basic-country")).ToHaveTextAsync("DE");
        await Expect(Page.Locator("#basic-number")).ToHaveTextAsync("17012345678");
        await Expect(Input("phi-basic")).ToHaveValueAsync("17012345678");
        await Expect(Dropdown("phi-basic").Locator(".bit-phi-cod")).ToHaveTextAsync("+49");

        await Page.Locator("#btn-set-country").ClickAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+3317012345678");
        await Expect(Dropdown("phi-basic")).ToHaveAttributeAsync("aria-label", "Country: France");

        await Page.Locator("#btn-set-number").ClickAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+33612345678");
        await Expect(Input("phi-basic")).ToHaveValueAsync("612345678");

        await Page.Locator("#btn-clear-value").ClickAsync();
        await Expect(Input("phi-basic")).ToHaveValueAsync("");
        await Expect(Page.Locator("#basic-number")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Binding_PickACountry_RecomposesTheValue()
    {
        await OpenAsync(Route);

        await Input("phi-basic").FillAsync("612345678");
        await Input("phi-basic").BlurAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+1612345678");

        var callout = await OpenCalloutAsync("phi-basic");
        await callout.Locator(".bit-phi-srch").FillAsync("France");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#basic-country")).ToHaveTextAsync("FR");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("+33612345678");
        await Expect(Input("phi-basic")).ToBeFocusedAsync();
        await Expect(Root("phi-basic").Locator(".bit-phi-lvr")).ToHaveTextAsync(new Regex(@"^France, \+33"));
    }

    [TestMethod]
    public async Task NationalPrefix_IsDroppedFromTheValueUnlessKept()
    {
        await OpenAsync(Route);

        await TypeAsync(Input("phi-prefix"), "01701234567");
        await Expect(Page.Locator("#prefix-value")).ToHaveTextAsync("+491701234567");

        await TypeAsync(Input("phi-keepprefix"), "01701234567");
        await Expect(Page.Locator("#keepprefix-value")).ToHaveTextAsync("+4901701234567");
    }

    [TestMethod]
    public async Task Mask_FormatsAsYouTypeWhileTheValueStaysE164()
    {
        await OpenAsync(Route);

        var input = Input("phi-mask");
        await TypeSettledAsync(input, "4155550123");
        await Expect(input).ToHaveValueAsync("(415) 555-0123");
        await Expect(Page.Locator("#mask-value")).ToHaveTextAsync("+14155550123");
        await Expect(Page.Locator("#mask-number")).ToHaveTextAsync("(415) 555-0123");

        // A digit typed in the middle keeps the caret after it, not at the end.
        await input.EvaluateAsync("i => i.setSelectionRange(3, 3)");
        await Page.Keyboard.TypeAsync("9");
        await Expect(input).ToHaveValueAsync("(419) 555-50123");
        Assert.AreEqual(4, await input.EvaluateAsync<int>("i => i.selectionStart"));
        await Expect(Page.Locator("#mask-value")).ToHaveTextAsync("+141955550123");
    }

    [TestMethod]
    public async Task MaskSelector_FormatsPerCountryAndAutoPlaceholderShowsThePattern()
    {
        await OpenAsync(Route);

        // Filled at once: typing digit by digit is the Mask test's subject.
        var input = Input("phi-mask-selector");
        await input.FillAsync("612345678");
        await Expect(input).ToHaveValueAsync("(612) 345-678");

        var callout = await OpenCalloutAsync("phi-mask-selector");
        await callout.Locator(".bit-phi-srch").FillAsync("France");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("6 12 34 56 78");
        await Expect(Page.Locator("#mask-selector-value")).ToHaveTextAsync("+33612345678");

        await Expect(Input("phi-autoplaceholder")).ToHaveAttributeAsync("placeholder", "(###) ###-####");
    }

    [TestMethod]
    public async Task Strict_DropsWhatCannotBePartOfANumber()
    {
        await OpenAsync(Route);

        var input = Input("phi-strict");
        await TypeAsync(input, "6a1b2-3 4");
        await Expect(input).ToHaveValueAsync("61234");
        await Expect(Page.Locator("#strict-value")).ToHaveTextAsync("+3161234");
    }

    [TestMethod]
    public async Task MaxLength_CapsTheText()
    {
        await OpenAsync(Route);

        var input = Input("phi-maxlength");
        await Expect(input).ToHaveAttributeAsync("maxlength", "10");
        await TypeAsync(input, "612345678901234");
        await Expect(input).ToHaveValueAsync("6123456789");
    }

    [TestMethod]
    public async Task Immediate_UpdatesOnEveryKeystrokeWhileTheDefaultWaitsForTheChange()
    {
        await OpenAsync(Route);

        await TypeAsync(Input("phi-immediate"), "202");
        await Expect(Page.Locator("#immediate-value")).ToHaveTextAsync("+1202");

        await TypeAsync(Input("phi-lazy"), "202");
        await NextFramesAsync();
        await Expect(Page.Locator("#lazy-value")).ToHaveTextAsync("null");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#lazy-value")).ToHaveTextAsync("+1202");
    }

    [TestMethod]
    public async Task DebounceTime_UpdatesOnceTheTypingPauses()
    {
        await OpenAsync(Route);

        await TypeAsync(Input("phi-debounce"), "2025550123");
        await Expect(Page.Locator("#debounce-value")).ToHaveTextAsync("+12025550123");
        await Expect(Page.Locator("#debounce-changes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ThrottleTime_UpdatesAtMostOncePerPeriodAndEndsOnTheLastValue()
    {
        await OpenAsync(Route);

        await TypeAsync(Input("phi-throttle"), "2025550123");
        await Expect(Page.Locator("#throttle-value")).ToHaveTextAsync("+12025550123");
        var changes = int.Parse(await Page.Locator("#throttle-changes").InnerTextAsync());
        Assert.IsTrue(changes >= 1 && changes < 10, $"{changes} updates for 10 keystrokes.");
    }

    [TestMethod]
    public async Task Label_NamesTheInputAndTheGroupAndFocusesTheInput()
    {
        await OpenAsync(Route);

        var label = Root("phi-label").Locator("> label.bit-phi-lbl");
        await Expect(label).ToHaveTextAsync("Phone number");
        await Expect(Input("phi-label")).ToHaveAccessibleNameAsync("Phone number");
        var group = Root("phi-label").Locator(".bit-phi-fgp");
        await Expect(group).ToHaveRoleAsync(AriaRole.Group);
        await Expect(group).ToHaveAccessibleNameAsync("Phone number");

        await label.ClickAsync();
        await Expect(Input("phi-label")).ToBeFocusedAsync();
        await Expect(Root("phi-label")).ToHaveClassAsync(HasClass("bit-phi-fcs"));

        // Without a label, the group carries no role.
        await Expect(Root("phi-basic").Locator(".bit-phi-fgp")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task Label_RequiredDescriptionAndTemplates()
    {
        await OpenAsync(Route);

        await Expect(Root("phi-required")).ToHaveClassAsync(HasClass("bit-phi-req"));
        await Expect(Input("phi-required")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Input("phi-required")).ToHaveAttributeAsync("required", "");
        StringAssert.Contains(await Root("phi-required").Locator("> label").EvaluateAsync<string>("l => getComputedStyle(l, '::after').content"), "*");

        await Expect(Input("phi-description")).ToHaveAccessibleDescriptionAsync("Office hours only.");

        await Expect(Root("phi-label-template").Locator("label .custom-label")).ToHaveTextAsync("Templated label");
        await Expect(Root("phi-label-template").Locator(".custom-description")).ToHaveTextAsync("Templated description");
        await Expect(Input("phi-label-template")).ToHaveAccessibleNameAsync("Templated label");

        await Expect(Input("phi-aria")).ToHaveAttributeAsync("aria-label", "Phone field");
        await Expect(Input("phi-aria")).ToHaveAttributeAsync("title", "Phone title");
    }

    [TestMethod]
    public async Task List_OpensUnderTheFieldWithEveryCountryAndItsDialingCode()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-search");
        await Expect(Dropdown("phi-search")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(callout.Locator(".bit-phi-srch")).ToBeFocusedAsync();
        await Expect(callout.Locator(".bit-phi-srch")).ToHaveAttributeAsync("placeholder", "Country or code");
        Assert.IsTrue(await callout.Locator("[role=option]").CountAsync() > 200);

        var selected = callout.Locator("[role=option][aria-selected=true]");
        await Expect(selected).ToHaveAttributeAsync("title", "France");
        await Expect(selected.Locator(".bit-phi-icd")).ToHaveTextAsync("+33");
        await Expect(selected).ToBeInViewportAsync();

        // Measured once the callout has finished sliding in, not partway through its entry transition.
        await callout.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished))");
        var groupBox = (await Root("phi-search").Locator(".bit-phi-fgp").BoundingBoxAsync())!;
        var calloutBox = (await callout.BoundingBoxAsync())!;
        Assert.IsTrue(calloutBox.Y >= groupBox.Y + groupBox.Height - 2);
    }

    [TestMethod]
    public async Task Search_ByNameByDialingCodeAndEmpty()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-search");
        var search = callout.Locator(".bit-phi-srch");
        var options = callout.Locator("[role=option]");

        await search.FillAsync("+33");
        await Expect(options.First).ToHaveAttributeAsync("title", "France");
        await search.FillAsync("0049");
        await Expect(options.First).ToHaveAttributeAsync("title", "Germany");
        await search.FillAsync("+1");
        await Expect(options.First).ToHaveAttributeAsync("title", "United States");
        await search.FillAsync("japa");
        await Expect(options).ToHaveCountAsync(1);
        await Expect(Root("phi-search").Locator(".bit-phi-lvr")).ToHaveTextAsync(new Regex("^1 country found"));

        await search.FillAsync("zzzz");
        await Expect(options).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-phi-nor")).ToHaveTextAsync("No matching country");

        await search.FillAsync("ital");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#search-country")).ToHaveTextAsync("IT");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Input("phi-search")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_ArrowsEscapeAltUpAndTab()
    {
        await OpenAsync(Route);

        await Dropdown("phi-search").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var callout = await CalloutAsync("phi-search");
        await Expect(callout).ToBeVisibleAsync();
        var search = callout.Locator(".bit-phi-srch");
        await Expect(search).ToBeFocusedAsync();

        var active = callout.Locator(".bit-phi-act");
        await Expect(active).ToHaveAttributeAsync("title", "France");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(active).Not.ToHaveAttributeAsync("title", "France");
        await Expect(search).ToHaveAttributeAsync("aria-activedescendant", (await active.GetAttributeAsync("id"))!);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Dropdown("phi-search")).ToBeFocusedAsync();
        await Expect(Page.Locator("#search-country")).ToHaveTextAsync("FR");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(search).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Dropdown("phi-search")).ToHaveAttributeAsync("aria-expanded", "false");

        // Alt+ArrowUp last: it also scrolls the page (see Keyboard_AltArrowUp_LeavesThePageWhereItIs).
        await Dropdown("phi-search").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(search).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Dropdown("phi-search")).ToBeFocusedAsync();
    }

    [TestMethod]
    [Ignore("KNOWN BUG: Alt+ArrowUp closes the open list but its default is not prevented (the prevent-keys script of Extras skips every modified key), so Chromium also scrolls the page by a screen, which in turn dismisses the callout as a page scroll and drops the focus to the body until the field takes it back. Alt+ArrowDown in an open list scrolls the page down, dismisses the list and leaves the focus on the body.")]
    public async Task Keyboard_AltArrowUp_LeavesThePageWhereItIs()
    {
        await OpenAsync(Route);

        await Dropdown("phi-search").ScrollIntoViewIfNeededAsync();
        await Page.EvaluateAsync("() => window.scrollBy(0, 300)");
        var callout = await OpenCalloutAsync("phi-search");
        var before = await Page.EvaluateAsync<double>("() => scrollY");
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Dropdown("phi-search")).ToBeFocusedAsync();
        Assert.AreEqual(before, await Page.EvaluateAsync<double>("() => scrollY"));
    }

    [TestMethod]
    public async Task NoSearchBox_TheDropdownIsAComboboxWithTypeAhead()
    {
        await OpenAsync(Route);

        var dropdown = Dropdown("phi-nosearch");
        await Expect(dropdown).ToHaveRoleAsync(AriaRole.Combobox);
        await Expect(Dropdown("phi-search")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await dropdown.FocusAsync();
        await dropdown.PressSequentiallyAsync("ger", new() { Delay = 80 });
        var callout = await CalloutAsync("phi-nosearch");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-phi-srch")).ToHaveCountAsync(0);
        await Expect(dropdown).ToBeFocusedAsync();
        var active = callout.Locator(".bit-phi-act");
        await Expect(active).ToHaveAttributeAsync("title", "Germany");
        await Expect(dropdown).ToHaveAttributeAsync("aria-activedescendant", (await active.GetAttributeAsync("id"))!);

        await Page.Keyboard.PressAsync("End");
        var options = callout.Locator("[role=option]");
        await Expect(options.Last).ToHaveClassAsync(HasClass("bit-phi-act"));
        await Page.Keyboard.PressAsync("Home");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-phi-act"));
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(options.Nth(10)).ToHaveClassAsync(HasClass("bit-phi-act"));
        var picked = await options.Nth(10).GetAttributeAsync("title");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Dropdown("phi-nosearch")).ToHaveAttributeAsync("aria-label", $"Country: {picked}");
    }

    [TestMethod]
    [Ignore("KNOWN BUG: a type-ahead letter pressed on the closed NoSearchBox dropdown is applied only after the open's interop round trips, so the letters pressed while those run are applied ahead of it: \"ger\" pressed at once lands on Eritrea rather than Germany. Typed at a person's pace on a local link it works (NoSearchBox_TheDropdownIsAComboboxWithTypeAhead).")]
    public async Task NoSearchBox_TypeAheadPressedAtOnceOnTheClosedDropdown_FindsTheCountry()
    {
        await OpenAsync(Route);

        await Dropdown("phi-nosearch").FocusAsync();
        await Page.Keyboard.TypeAsync("ger");
        var callout = await CalloutAsync("phi-nosearch");
        await Expect(callout.Locator(".bit-phi-act")).ToHaveAttributeAsync("title", "Germany");
    }

    [TestMethod]
    public async Task NoWrapNavigation_StopsAtTheEndsAndWrappingIsTheDefault()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-nowrap");
        var options = callout.Locator("[role=option]");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-phi-act"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-phi-act"));
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("phi-wrap");
        options = callout.Locator("[role=option]");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.Last).ToHaveClassAsync(HasClass("bit-phi-act"));
    }

    [TestMethod]
    public async Task Countries_ExcludeCountriesAndPreferredCountries()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-countries");
        var titles = await callout.Locator("[role=option]").EvaluateAllAsync<string[]>("os => os.map(o => o.title)");
        CollectionAssert.AreEqual(new[] { "Canada", "Germany", "Japan", "Brazil", "Australia" }, titles);
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("phi-exclude");
        await Expect(callout.Locator("[role=option][title='Canada']")).ToHaveCountAsync(0);
        await Expect(callout.Locator("[role=option][title='Germany']")).ToHaveCountAsync(0);
        await Expect(callout.Locator("[role=option][title='France']")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");

        callout = await OpenCalloutAsync("phi-preferred");
        var options = callout.Locator("[role=option]");
        await Expect(options.Nth(0)).ToHaveAttributeAsync("title", "Japan");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("title", "Brazil");
        await Expect(options.Nth(1)).ToHaveClassAsync(HasClass("bit-phi-ilp"));
    }

    [TestMethod]
    public async Task Display_NoFlagsNoDialCodePlaceholderAndFlagUrlSelector()
    {
        await OpenAsync(Route);

        await Expect(Dropdown("phi-noflags").Locator(".bit-phi-flg")).ToHaveCountAsync(0);
        await Expect(Dropdown("phi-noflags").Locator(".bit-phi-cod")).ToHaveTextAsync("+81");

        await Expect(Dropdown("phi-nodialcode").Locator(".bit-phi-cod")).ToHaveCountAsync(0);
        await Expect(Dropdown("phi-nodialcode").Locator("img.bit-phi-flg")).ToHaveCountAsync(1);

        await Expect(Dropdown("phi-dropdown-placeholder").Locator(".bit-phi-pls")).ToHaveTextAsync("Country");
        await Expect(Dropdown("phi-dropdown-placeholder")).ToHaveAttributeAsync("aria-label", "Country");

        await Expect(Dropdown("phi-flag-selector").Locator("img")).ToHaveAttributeAsync("src", "samples/flags/ca.svg");
        var callout = await OpenCalloutAsync("phi-flag-selector");
        await Expect(callout.Locator("[title='Germany'] img")).ToHaveAttributeAsync("src", "samples/flags/de.svg");
        await Expect(callout.Locator("[title='Brazil'] img")).ToHaveAttributeAsync("src", "_content/Bit.BlazorUI.Extras/flags/BR-flat-16.webp");
    }

    [TestMethod]
    public async Task NoDropdown_FixesTheCountryAndDescribesIt()
    {
        await OpenAsync(Route);

        var root = Root("phi-nodropdown");
        await Expect(root).ToHaveClassAsync(HasClass("bit-phi-nod"));
        await Expect(root.Locator("button.bit-phi-drp")).ToHaveCountAsync(0);
        var fixedCountry = root.Locator("div.bit-phi-drp");
        await Expect(fixedCountry.Locator(".bit-phi-vhd")).ToHaveTextAsync("Country: Japan");
        await Expect(Input("phi-nodropdown")).ToHaveAttributeAsync("aria-describedby", (await fixedCountry.GetAttributeAsync("id"))!);

        await TypeAsync(Input("phi-nodropdown"), "312345678");
        await Expect(Page.Locator("#nodropdown-value")).ToHaveTextAsync("+81312345678");
        await fixedCountry.ClickAsync();
        await Expect(root).Not.ToHaveAttributeAsync("aria-owns", new Regex(".*"));
    }

    [TestMethod]
    public async Task ClearButton_EmptiesTheNumberKeepsTheCountryAndAnnounces()
    {
        await OpenAsync(Route);

        var clear = Root("phi-clear").Locator(".bit-phi-cbt");
        // Italy keeps the 0 of a landline in international form, so it stays in the number.
        await Expect(Input("phi-clear")).ToHaveValueAsync("0612345678");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Erase");
        await clear.ClickAsync();

        await Expect(Input("phi-clear")).ToHaveValueAsync("");
        await Expect(Input("phi-clear")).ToBeFocusedAsync();
        await Expect(Page.Locator("#clear-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#clear-value")).ToHaveTextAsync("null");
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(Root("phi-clear").Locator(".bit-phi-lvr")).ToHaveTextAsync(new Regex("^Cleared"));
        await Expect(Dropdown("phi-clear")).ToHaveAttributeAsync("aria-label", "Country: Italy");

        await TypeAsync(Input("phi-clear"), "1");
        await Input("phi-clear").BlurAsync();
        await Expect(clear).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task ClearButton_AnnouncementTemplateAndIcons()
    {
        await OpenAsync(Route);

        await Root("phi-clear-silent").Locator(".bit-phi-cbt").ClickAsync();
        await Expect(Input("phi-clear-silent")).ToHaveValueAsync("");
        await NextFramesAsync();
        await Expect(Root("phi-clear-silent").Locator(".bit-phi-lvr")).ToHaveTextAsync("");

        await Root("phi-clear-said").Locator(".bit-phi-cbt").ClickAsync();
        await Expect(Root("phi-clear-said").Locator(".bit-phi-lvr")).ToHaveTextAsync(new Regex("^Number erased"));

        await Expect(Root("phi-clear-template").Locator(".bit-phi-cbt .clear-template")).ToHaveTextAsync("x");
        await Expect(Root("phi-clear-template").Locator(".bit-phi-cbt i")).ToHaveCountAsync(0);
        await Expect(Root("phi-clear-icon").Locator(".bit-phi-cbt i")).ToHaveClassAsync(HasClass("my-clear-icon"));
        await Expect(Root("phi-clear-iconname").Locator(".bit-phi-cbt i")).ToHaveClassAsync(new Regex("Delete"));
        await Expect(Root("phi-clear-iconname").Locator(".bit-phi-cbt")).ToHaveAttributeAsync("aria-label", "Clear number");
    }

    [TestMethod]
    public async Task Validation_InvalidAndErrorMessage()
    {
        await OpenAsync(Route);

        await Expect(Root("phi-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("phi-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.AreEqual(await ResolveColorAsync("--bit-clr-err"), await ComputedStyleAsync("#phi-invalid .bit-phi-fgp", "border-top-color"));

        var error = Root("phi-error").Locator(".bit-phi-erm");
        await Expect(error).ToHaveTextAsync("That number is already registered.");
        await Expect(Input("phi-error")).ToHaveAccessibleDescriptionAsync(new Regex("That number is already registered."));
        await Expect(Input("phi-error")).ToHaveAccessibleDescriptionAsync(new Regex("Nine digits."));
        await Expect(Root("phi-error").Locator(".bit-phi-lvr")).ToHaveTextAsync(new Regex("^That number is already registered."));

        await Expect(Root("phi-error-template").Locator(".custom-error")).ToHaveTextAsync("Templated error");
    }

    [TestMethod]
    public async Task Validation_EditForm_ValidatesTheE164Value()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Page.Locator("#validation .validation-message").First).ToHaveTextAsync("Enter a phone number.");
        await Expect(Input("phi-form")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("phi-form")).ToHaveClassAsync(HasClass("bit-inv"));

        await TypeAsync(Input("phi-form"), "123");
        await Expect(Page.Locator("#validation .validation-message").First).ToHaveTextAsync("Enter a valid international phone number.");

        await Input("phi-form").FillAsync("612345678");
        await Expect(Input("phi-form")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Page.Locator("#submitted")).ToHaveTextAsync("Submitted: +34612345678");
    }

    [TestMethod]
    public async Task Validation_NoValidate_LeavesTheFieldUnmarked()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-submit-2").ClickAsync();
        await Expect(Page.Locator("#novalidate-message .validation-message")).ToHaveTextAsync("Enter a phone number.");
        await Expect(Input("phi-form-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Expect(Root("phi-form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Disabled_EveryControlIsNativelyDisabledAndNoActivationHandlerRuns()
    {
        await OpenAsync(Route);

        var root = Root("phi-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("phi-disabled")).ToBeDisabledAsync();
        await Expect(Input("phi-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Dropdown("phi-disabled")).ToBeDisabledAsync();
        await Expect(Dropdown("phi-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Dropdown("phi-disabled")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(root.Locator(".bit-phi-cbt")).ToBeDisabledAsync();
        await Expect(root.Locator(".bit-phi-cbt")).ToHaveAttributeAsync("aria-disabled", "true");

        await Dropdown("phi-disabled").ClickAsync(new() { Force = true });
        await Input("phi-disabled").DblClickAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(Dropdown("phi-disabled")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("phi-disabled")).ToBeEnabledAsync();
        await Expect(Input("phi-disabled")).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Input("phi-disabled").DblClickAsync();
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task ReadOnly_TheNumberAndTheCountryCannotChange()
    {
        await OpenAsync(Route);

        await Expect(Root("phi-readonly")).ToHaveClassAsync(HasClass("bit-phi-rdo"));
        await Expect(Input("phi-readonly")).ToHaveAttributeAsync("readonly", "");
        await Expect(Dropdown("phi-readonly")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Root("phi-readonly").Locator(".bit-phi-cbt")).ToBeDisabledAsync();
        StringAssert.Contains(await Input("phi-readonly").GetAttributeAsync("aria-describedby"), await Dropdown("phi-readonly").GetAttributeAsync("id"));
        Assert.AreEqual("hidden", await ComputedStyleAsync("#phi-readonly .bit-phi-car", "visibility"));

        await Dropdown("phi-readonly").ClickAsync();
        await NextFramesAsync();
        await Expect(Dropdown("phi-readonly")).ToHaveAttributeAsync("aria-expanded", "false");
        await Input("phi-readonly").FocusAsync();
        await Page.Keyboard.TypeAsync("99");
        await Expect(Input("phi-readonly")).ToHaveValueAsync("1234567");
    }

    [TestMethod]
    public async Task Templates_DropdownItemAndNoResults()
    {
        await OpenAsync(Route);

        await Expect(Dropdown("phi-templates").Locator(".dropdown-template")).ToHaveTextAsync("PT");
        var callout = await OpenCalloutAsync("phi-templates");
        await Expect(callout.Locator("[title='Spain'] .item-template")).ToHaveTextAsync("Spain / ESP");
        await callout.Locator(".bit-phi-srch").FillAsync("qqqq");
        await Expect(callout.Locator(".no-results-template")).ToHaveTextAsync("nothing here");
    }

    [TestMethod]
    public async Task Events_TheInputsEventsFireWithTheirArguments()
    {
        await OpenAsync(Route);

        var input = Input("phi-events");
        await input.ClickAsync();
        await Expect(Page.Locator("#ev-focus")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-focusin")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-click")).ToHaveTextAsync("1");

        await Page.Keyboard.TypeAsync("202");
        await Expect(Page.Locator("#ev-keydown")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#ev-lastkey")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#ev-enter")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#ev-escape")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-keydown")).ToHaveTextAsync("5");

        await Page.Locator("#btn-open").FocusAsync();
        await Expect(Page.Locator("#ev-blur")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-focusout")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#ev-value")).ToHaveTextAsync("+1202");
        await Expect(Page.Locator("#ev-change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Events_TheListsEventsFire()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-events");
        await Expect(Page.Locator("#ev-open")).ToHaveTextAsync("1");
        await callout.Locator(".bit-phi-srch").PressSequentiallyAsync("fra");
        await Expect(Page.Locator("#ev-search")).ToHaveTextAsync("fra");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#ev-close")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-country")).ToHaveTextAsync("FR");
        await Expect(Page.Locator("#ev-country-count")).ToHaveTextAsync("1");

        // A typed-in country fires it as well.
        await Input("phi-events").FillAsync("+49 30 123456");
        await Input("phi-events").BlurAsync();
        await Expect(Page.Locator("#ev-country")).ToHaveTextAsync("DE");
        await Expect(Page.Locator("#ev-country-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#ev-value")).ToHaveTextAsync("+4930123456");
    }

    [TestMethod]
    public async Task IsOpen_TwoWayBinding()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-toggle-open").ClickAsync();
        var callout = await CalloutAsync("phi-controlled");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("False");

        await Dropdown("phi-controlled").ClickAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("True");
        await Page.Locator("#btn-toggle-open").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task PublicMethods_DriveTheField()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open").ClickAsync();
        var callout = await CalloutAsync("phi-controlled");
        await Expect(callout).ToBeVisibleAsync();
        await Page.Locator("#btn-close").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#is-open")).ToHaveTextAsync("False");

        await Page.Locator("#btn-select-japan").ClickAsync();
        await Expect(Dropdown("phi-controlled")).ToHaveAttributeAsync("aria-label", "Country: Japan");
        await Expect(Input("phi-controlled")).ToBeFocusedAsync();

        await Page.Locator("#btn-set-number-async").ClickAsync();
        await Expect(Page.Locator("#controlled-value")).ToHaveTextAsync("+81312345678");
        await Expect(Input("phi-controlled")).ToHaveValueAsync("312345678");

        await Page.Locator("#btn-read-full").ClickAsync();
        await Expect(Page.Locator("#full-number")).ToHaveTextAsync("+81312345678");

        await Page.Locator("#btn-clear-async").ClickAsync();
        await Expect(Input("phi-controlled")).ToHaveValueAsync("");
        await Expect(Page.Locator("#controlled-value")).ToHaveTextAsync("null");

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Input("phi-controlled")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NoFocusOnSelect_LeavesTheFocusOffTheInput()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-nofocus");
        await callout.Locator(".bit-phi-srch").FillAsync("Japan");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dropdown("phi-nofocus")).ToHaveAttributeAsync("aria-label", "Country: Japan");
        await NextFramesAsync();
        await Expect(Input("phi-nofocus")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NameAndCountryName_PostTheValueAndTheCountry()
    {
        await OpenAsync(Route);

        await TypeAsync(Input("phi-named"), "6135550123");
        var data = await Page.Locator("#plain-form").EvaluateAsync<string[]>("f => { const d = new FormData(f); return [d.get('phone'), d.get('phoneCountry')]; }");
        Assert.AreEqual("+16135550123", data[0]);
        Assert.AreEqual("CA", data[1]);
    }

    [TestMethod]
    public async Task Autofill_TheInputCarriesTheKeyboardAndAutofillHints()
    {
        await OpenAsync(Route);

        var input = Input("phi-autofill");
        await Expect(input).ToHaveAttributeAsync("autocomplete", "tel-national");
        await Expect(input).ToHaveAttributeAsync("enterkeyhint", "send");
        await Expect(input).ToHaveAttributeAsync("inputmode", "tel");
        await Expect(input).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(input).ToHaveAttributeAsync("autocorrect", "off");
        await Expect(Input("phi-basic")).ToHaveAttributeAsync("autocomplete", "tel");
    }

    [TestMethod]
    public async Task Localization_EveryTextIsTheApps()
    {
        await OpenAsync(Route);

        await Expect(Dropdown("phi-german")).ToHaveAttributeAsync("aria-label", "Land: Deutschland");
        await Expect(Input("phi-german")).ToHaveAttributeAsync("placeholder", "Nummer eingeben");
        await Expect(Root("phi-german").Locator(".bit-phi-cbt")).ToHaveAttributeAsync("aria-label", "Nummer löschen");

        var callout = await OpenCalloutAsync("phi-german");
        var search = callout.Locator(".bit-phi-srch");
        await Expect(search).ToHaveAttributeAsync("placeholder", "Suchen");
        await Expect(search).ToHaveAttributeAsync("aria-label", "Land suchen");
        await Expect(callout.Locator("[role=listbox]")).ToHaveAttributeAsync("aria-label", "Land");
        await search.FillAsync("schw");
        await Expect(Root("phi-german").Locator(".bit-phi-lvr")).ToHaveTextAsync(new Regex("^1 Land gefunden"));
        await search.FillAsync("xyz");
        await Expect(callout.Locator(".bit-phi-nor")).ToHaveTextAsync("Kein Land gefunden");
    }

    [TestMethod]
    public async Task Params_BitPhoneInputParams_CascadeAndAnOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(Root("phi-params")).ToHaveClassAsync(HasClass("bit-phi-nbd"));
        await Expect(Root("phi-params")).ToHaveClassAsync(HasClass("bit-phi-sm"));
        await Expect(Dropdown("phi-params")).ToHaveAttributeAsync("aria-label", "Country: Japan");
        await Expect(Input("phi-params")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await TypeAsync(Input("phi-params"), "3");
        await Input("phi-params").BlurAsync();
        await Expect(Root("phi-params").Locator(".bit-phi-cbt")).ToHaveCountAsync(1);
        var callout = await OpenCalloutAsync("phi-params");
        await Expect(callout.Locator("[role=option]")).ToHaveCountAsync(5);
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Dropdown("phi-params-own")).ToHaveAttributeAsync("aria-label", "Country: Brazil");
        await Expect(Root("phi-params-own")).Not.ToHaveClassAsync(HasClass("bit-phi-nbd"));

        await Expect(Root("phi-params-outside")).Not.ToHaveClassAsync(HasClass("bit-phi-nbd"));
        await Expect(Dropdown("phi-params-outside")).ToHaveAttributeAsync("aria-label", "Select country");
    }

    [TestMethod]
    public async Task Variants_NoBorderUnderlinedAndFullWidth()
    {
        await OpenAsync(Route);

        await Expect(Root("phi-noborder")).ToHaveClassAsync(HasClass("bit-phi-nbd"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await ComputedStyleAsync("#phi-noborder .bit-phi-fgp", "border-top-color"));

        await Expect(Root("phi-underlined")).ToHaveClassAsync(HasClass("bit-phi-und"));
        Assert.AreEqual("0px", await ComputedStyleAsync("#phi-underlined .bit-phi-fgp", "border-top-width"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#phi-underlined .bit-phi-fgp", "border-bottom-width"));

        await Expect(Root("phi-fullwidth")).ToHaveClassAsync(HasClass("bit-phi-fwd"));
        Assert.AreEqual(600, (await Root("phi-fullwidth").Locator(".bit-phi-fgp").BoundingBoxAsync())!.Width, 1);
        Assert.IsTrue((await Root("phi-notfullwidth").Locator(".bit-phi-fgp").BoundingBoxAsync())!.Width < 590);
    }

    [DataTestMethod]
    [DataRow("primary", "bit-phi-bpr", "--bit-clr-bg-pri")]
    [DataRow("secondary", "bit-phi-bse", "--bit-clr-bg-sec")]
    [DataRow("tertiary", "bit-phi-btr", "--bit-clr-bg-ter")]
    [DataRow("transparent", "bit-phi-btn", null)]
    public async Task Background_EachKind_FillsTheField(string kind, string cssClass, string? token)
    {
        await OpenAsync(Route);

        await Expect(Root($"phi-bg-{kind}")).ToHaveClassAsync(HasClass(cssClass));
        var expected = token is null ? "rgba(0, 0, 0, 0)" : await ResolveColorAsync(token);
        Assert.AreEqual(expected, await ComputedStyleAsync($"#phi-bg-{kind} .bit-phi-fgp", "background-color"));
    }

    [DataTestMethod]
    [DataRow("primary", "bit-phi-brp", "--bit-clr-brd-pri")]
    [DataRow("secondary", "bit-phi-brs", "--bit-clr-brd-sec")]
    [DataRow("tertiary", "bit-phi-brt", "--bit-clr-brd-ter")]
    [DataRow("transparent", "bit-phi-brn", null)]
    public async Task Border_EachKind_ColorsTheFrame(string kind, string cssClass, string? token)
    {
        await OpenAsync(Route);

        await Expect(Root($"phi-brd-{kind}")).ToHaveClassAsync(HasClass(cssClass));
        var expected = token is null ? "rgba(0, 0, 0, 0)" : await ResolveColorAsync(token);
        Assert.AreEqual(expected, await ComputedStyleAsync($"#phi-brd-{kind} .bit-phi-fgp", "border-top-color"));
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

        await Expect(Root($"phi-color-{name}")).ToHaveClassAsync(HasClass($"bit-phi-{suffix}"));
        Assert.AreEqual(await ResolveColorAsync(token), await ComputedStyleAsync($"#phi-color-{name} .bit-phi-fgp", "border-top-color"));
    }

    [DataTestMethod]
    [DataRow("small", "sm", "26px")]
    [DataRow("medium", "md", "32px")]
    [DataRow("large", "lg", "40px")]
    public async Task Size_EachValue_SetsTheHeight(string name, string suffix, string height)
    {
        await OpenAsync(Route);

        await Expect(Root($"phi-size-{name}")).ToHaveClassAsync(HasClass($"bit-phi-{suffix}"));
        Assert.AreEqual(height, await ComputedStyleAsync($"#phi-size-{name} .bit-phi-fgp", "min-height"));
        var callout = await OpenCalloutAsync($"phi-size-{name}");
        await Expect(callout).ToHaveClassAsync(HasClass($"bit-phi-{suffix}"));
    }

    [TestMethod]
    public async Task CssVariables_OnOneField_ReskinItAndItsRelocatedCallout()
    {
        await OpenAsync(Route);

        Assert.AreEqual("12px", await ComputedStyleAsync("#phi-vars .bit-phi-fgp", "border-top-left-radius"));
        Assert.AreEqual("rgb(255, 0, 0)", await ComputedStyleAsync("#phi-vars .bit-phi-fgp", "border-top-color"));
        Assert.AreEqual("rgb(255, 255, 0)", await ComputedStyleAsync("#phi-vars .bit-phi-fgp", "background-color"));
        Assert.AreEqual("rgb(0, 0, 255)", await ComputedStyleAsync("#phi-vars .bit-phi-drp", "border-right-color"));

        var callout = await OpenCalloutAsync("phi-vars");
        Assert.AreEqual("9px", await callout.EvaluateAsync<string>("c => getComputedStyle(c).borderTopLeftRadius"));
        await Page.Keyboard.PressAsync("ArrowDown");
        var selected = callout.Locator("[aria-selected=true]");
        await Expect(selected).Not.ToHaveClassAsync(HasClass("bit-phi-act"));
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(237, 231, 246)");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheFieldAndTheCallout()
    {
        await OpenAsync(Route);

        Assert.AreEqual("rgb(0, 128, 0)", await ComputedStyleAsync("#phi-vars-scope .bit-phi-inp", "color"));
        Assert.AreEqual("rgb(128, 0, 0)", await ComputedStyleAsync("#phi-vars-scope .bit-phi-drp", "color"));
        var callout = await OpenCalloutAsync("phi-vars-scope");
        Assert.AreEqual("rgb(240, 248, 255)", await callout.EvaluateAsync<string>("c => getComputedStyle(c).backgroundColor"));
        Assert.AreEqual("rgb(0, 0, 128)", await callout.Locator(".bit-phi-icd").First.EvaluateAsync<string>("c => getComputedStyle(c).color"));
    }

    [TestMethod]
    public async Task CssVariables_LoseToAnExplicitColorAndSize()
    {
        await OpenAsync(Route);

        Assert.AreEqual(await ResolveColorAsync("--bit-clr-err"), await ComputedStyleAsync("#phi-vars-wins .bit-phi-fgp", "border-top-color"));
        Assert.AreEqual("40px", await ComputedStyleAsync("#phi-vars-wins .bit-phi-fgp", "min-height"));
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("phi-styled");
        await Expect(root).ToHaveClassAsync(HasClass("custom-class"));
        Assert.AreEqual(300, (await root.BoundingBoxAsync())!.Width, 0.5);
        Assert.AreEqual("rgb(138, 43, 226)", await ComputedStyleAsync("#phi-styled .bit-phi-fgp", "border-top-color"));
        Assert.AreEqual("3px", await ComputedStyleAsync("#phi-styled .bit-phi-inp", "letter-spacing"));
        await Expect(root.Locator(".bit-phi-inp")).ToHaveClassAsync(HasClass("custom-input"));
        await Expect(root.Locator(".bit-phi-drp")).ToHaveClassAsync(HasClass("custom-dropdown"));
        await Expect(root.Locator(".bit-phi-fgp")).ToHaveClassAsync(HasClass("custom-fgp"));

        var callout = await OpenCalloutAsync("phi-styled");
        await Expect(callout).ToHaveClassAsync(HasClass("custom-callout"));
        await Expect(callout.Locator("[role=option]").First).ToHaveClassAsync(HasClass("custom-item"));
        Assert.AreEqual("rgb(138, 43, 226)", await callout.Locator(".bit-phi-icd").First.EvaluateAsync<string>("c => getComputedStyle(c).color"));
    }

    [TestMethod]
    public async Task Rtl_MirrorsTheFieldButTheNumberStaysLeftToRight()
    {
        await OpenAsync(Route);

        await Expect(Root("phi-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("phi-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "phi-rtl", "phi-rtl-inherited" })
        {
            var dropdownBox = (await Dropdown(id).BoundingBoxAsync())!;
            var inputBox = (await Input(id).BoundingBoxAsync())!;
            Assert.IsTrue(dropdownBox.X > inputBox.X, $"{id}: the country selector sits at the right.");
            Assert.AreEqual("ltr", await ComputedStyleAsync($"#{id} .bit-phi-inp", "direction"));
            Assert.AreEqual("right", await ComputedStyleAsync($"#{id} .bit-phi-inp", "text-align"));
        }

        var callout = await OpenCalloutAsync("phi-rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-rtl"));
        var option = callout.Locator("[role=option]").First;
        var flag = (await option.Locator(".bit-phi-flg").BoundingBoxAsync())!;
        var name = (await option.Locator(".bit-phi-inm").BoundingBoxAsync())!;
        Assert.IsTrue(flag.X > name.X);
    }

    [TestMethod]
    public async Task Responsive_OnASmallScreen_OpensASidePanel()
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-phi-res"));
        await Expect(callout.Locator(".bit-phi-rlb")).ToHaveTextAsync("Responsive phone");
        var close = callout.Locator(".bit-phi-cls");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Close the list");
        await Expect(callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var box = (await callout.BoundingBoxAsync())!;
        Assert.AreEqual(700, box.Height, 2);
        Assert.AreEqual(375, box.X + box.Width, 2);

        await close.ClickAsync();
        await Expect(Dropdown("phi-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Dropdown("phi-responsive")).ToBeFocusedAsync();
        await Expect(callout).ToHaveCSSAsync("opacity", "0");
    }

    [TestMethod]
    public async Task ExternalIcons_ResponsiveCloseIconAndIconName()
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-responsive-icon");
        await Expect(callout.Locator(".bit-phi-cls i")).ToHaveClassAsync(HasClass("my-close-icon"));
        await callout.Locator(".bit-phi-cls").ClickAsync();
        await Expect(Dropdown("phi-responsive-icon")).ToHaveAttributeAsync("aria-expanded", "false");

        callout = await OpenCalloutAsync("phi-responsive-iconname");
        await Expect(callout.Locator(".bit-phi-cls i")).ToHaveClassAsync(new Regex("ChromeClose"));
    }

    [TestMethod]
    public async Task Callout_MaxHeight_CapsTheList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-maxheight");
        var height = await callout.Locator(".bit-phi-scn").EvaluateAsync<double>("s => s.getBoundingClientRect().height");
        Assert.IsTrue(height <= 200.5, $"The list is {height}px tall.");
    }

    [TestMethod]
    public async Task ClickOutside_ClosesTheList()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-search");
        await Page.Mouse.ClickAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#search-country")).ToHaveTextAsync("FR");
    }

    [TestMethod]
    public async Task Dialog_EscapeClosesTheListFirstAndTheDialogOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        var callout = await OpenCalloutAsync("phi-in-dialog");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Dropdown("phi-in-dialog")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheInputOnTheFirstRender()
    {
        await OpenAsync($"{Route}/autofocus");

        await Expect(Input("phi-autofocus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Lifecycle_Prerendered_TheServerHtmlCarriesTheField()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"phi-basic\"");
        StringAssert.Contains(html, "type=\"tel\"");
        StringAssert.Contains(html, "Enter your number");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountedWhileOpen_LeavesNoErrorAndNoCallout()
    {
        await OpenAsync(Route);

        var callout = await OpenCalloutAsync("phi-unmount");
        var calloutId = await callout.GetAttributeAsync("id");

        await Page.Locator("#btn-unmount").DispatchEventAsync("click");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await OpenCalloutAsync("phi-unmount");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private ILocator Root(string id) => Page.Locator($"#{id}");

    // Typed the way a person types: a keystroke landing before the round trip of the one ahead of it is answered
    // would be overwritten by that answer, as in any Blazor Server input that rewrites its own value.
    private static Task TypeAsync(ILocator input, string text) => input.PressSequentiallyAsync(text, new() { Delay = 80 });

    // Types digits one at a time into the field masked "(###) ###-####" (phi-mask), each once the field shows the masked
    // text of the ones before it: the field rewrites its own text on every keystroke, and the next digit waits for that rewrite
    // rather than racing it (a digit landing before the rewrite of the one ahead is put after it by the rewrite).
    // The keys go to the keyboard rather than through the locator, which would focus the field again before each.
    private async Task TypeSettledAsync(ILocator input, string digits)
    {
        await input.ClickAsync();
        for (var i = 0; i < digits.Length; i++)
        {
            await Page.Keyboard.PressAsync(digits[i].ToString());

            // Settled once the server holds it too (the bound Number the page writes), not only the field: a
            // keystroke the field shows before its round trip has come back can still be rewritten by an older one.
            await Expect(input).ToHaveValueAsync(MaskUs(digits[..(i + 1)]));
            await Expect(Page.Locator("#mask-number")).ToHaveTextAsync(MaskUs(digits[..(i + 1)]));
        }
    }

    private static string MaskUs(string digits)
    {
        const string mask = "(###) ###-####";
        var result = new System.Text.StringBuilder();
        var next = 0;
        foreach (var c in mask)
        {
            if (next >= digits.Length) break;
            result.Append(c == '#' ? digits[next++] : c);
        }
        return result.ToString();
    }

    private ILocator Input(string id) => Page.Locator($"#{id} .bit-phi-inp");

    private ILocator Dropdown(string id) => Page.Locator($"#{id} .bit-phi-drp");

    private async Task<ILocator> OpenCalloutAsync(string id)
    {
        // A list closed just before is closed on the .NET side too (its overlay is hidden from the render) before
        // the next one is opened, so a late close of the one cannot land on the other.
        await Expect(Page.Locator(".bit-phi-ovl:visible")).ToHaveCountAsync(0);
        await Dropdown(id).ScrollIntoViewIfNeededAsync();
        await Dropdown(id).ClickAsync();
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
