using System.Text.RegularExpressions;
using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The core of BitDateRangePicker in a real browser (/e2e/date-range-picker): a range picked by two clicks and by the
/// keyboard, with the prospective range previewed under the pointer and the arrow keys, a third pick starting over and
/// a reversed pair swapped; the callout opened by a click and the keys, placed against the field and closed by the
/// second pick, Escape, the overlay and the close button; the presets (fixed and provided, disabled, rejected,
/// highlighted, standalone); Apply and Cancel with the range rolled back; MinDate/MaxDate, DisablePast/DisableFuture,
/// MinRange and MaxRange; the day rules and ExcludeDisabledDates; highlighted days; the calendar layout, several months
/// and paged navigation; the month and year pickers; Standalone; ReadOnly; the templates; validation; IsOpen,
/// OpenCallout and CloseCallout; the accessible names; the base parameters; the disabled contract; and the lifecycle.
/// </summary>
[TestClass]
public class BitDateRangePickerE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-range-picker";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtrp-inp");

    private static ILocator Title(ILocator callout) => callout.Locator(".bit-dtrp-dwp .bit-dtrp-pkh").First.Locator(".bit-dtrp-pkt, .bit-dtrp-ptb").First;

    private async Task<string> OpenPickerAsync(string rootId)
    {
        await NoCalloutOpenAsync();

        var pid = await PickerIdAsync(rootId);

        await Input(rootId).ClickAsync();
        await Expect(Input(rootId)).ToHaveAttributeAsync("aria-expanded", "true");
        await SettleAsync(Callout(pid));

        return pid;
    }

    private async Task PressAsync(string key, ILocator focused)
    {
        await Page.Keyboard.PressAsync(key);
        await Expect(focused).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesThePickerBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"drp-basic\"");
        StringAssert.Contains(html, "placeholder=\"Pick a range\"");
        StringAssert.Contains(html, "value=\"Start: 6/3/2024 - End: 6/5/2024\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);

        await Expect(Input("drp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#drp-basic .bit-dtrp-lbl")).ToHaveTextAsync("Stay");
    }

    [TestMethod]
    public async Task Open_ClickingTheField_OpensTheCalloutBelowItWithTheFocusOnToday()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");

        await Expect(Page.Locator("#drp-basic")).ToHaveClassAsync(HasClass("bit-dtrp-opn"));
        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Expect(Day(pid, Today)).ToHaveAttributeAsync("aria-current", "date");
        await Expect(Day(pid, Today)).ToHaveClassAsync(HasClass("bit-dtrp-dtd"));
        await Expect(Title(Callout(pid))).ToHaveTextAsync("June 2024");
        await Expect(Input("drp-basic")).ToHaveAttributeAsync("aria-controls", $"{pid}-callout");
        await Expect(Callout(pid).Locator(".bit-dtrp-cac")).ToHaveAttributeAsync("role", "dialog");
        await Expect(Callout(pid).Locator(".bit-dtrp-cac")).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Callout(pid).Locator(".bit-dtrp-dwp [role=grid]")).ToHaveAttributeAsync("aria-multiselectable", "true");
        await Expect(Page.Locator("#basic-open-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-click-count")).ToHaveTextAsync("1");

        var field = await BoxAsync(Page.Locator("#drp-basic .bit-dtrp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        await Expect(Callout(pid)).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        Assert.AreEqual(field.Y + field.Height + 1, callout.Y, 2, "The callout's top sits on the field's bottom edge.");
        Assert.AreEqual(field.X, callout.X, 2, "The callout starts where the field starts.");
        Assert.IsFalse(await Callout(pid).EvaluateAsync<bool>("el => !!el.closest('#basic')"), "Open, the callout lives under the body.");
    }

    [TestMethod]
    public async Task Pick_TwoClicksPickARangeWithAPreviewInBetween()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-10 00:00 / -");
        await Expect(Page.Locator("#basic-last-select")).ToHaveTextAsync("2024-06-10 00:00 / -");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, 2024, 6, 10)).ToHaveClassAsync(HasClass("bit-dtrp-dss"));

        // The pointer previews the range the next pick would make.
        await Day(pid, 2024, 6, 13).HoverAsync();
        foreach (var day in new[] { 11, 12, 13 })
        {
            await Expect(Day(pid, 2024, 6, day)).ToHaveClassAsync(HasClass("bit-dtrp-dhr"));
        }
        await Expect(Day(pid, 2024, 6, 14)).Not.ToHaveClassAsync(HasClass("bit-dtrp-dhr"));

        await Day(pid, 2024, 6, 14).ClickAsync();

        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 23:59");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-basic")).ToBeFocusedAsync();
        await Expect(Input("drp-basic")).ToHaveValueAsync("Start: 6/10/2024 - End: 6/14/2024");
        await Expect(Page.Locator("#basic-select-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#basic-close-count")).ToHaveTextAsync("1");

        await Input("drp-basic").ClickAsync();
        await Expect(Day(pid, 2024, 6, 10)).ToHaveClassAsync(HasClass("bit-dtrp-dss"));
        await Expect(Day(pid, 2024, 6, 14)).ToHaveClassAsync(HasClass("bit-dtrp-dse"));
        foreach (var day in new[] { 11, 12, 13 })
        {
            await Expect(Day(pid, 2024, 6, day)).ToHaveClassAsync(HasClass("bit-dtrp-dsb"));
        }
        foreach (var day in new[] { 10, 11, 12, 13, 14 })
        {
            await Expect(Day(pid, 2024, 6, day)).ToHaveAttributeAsync("aria-selected", "true");
        }
        await Expect(Day(pid, 2024, 6, 9)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Callout(pid).Locator(".bit-dtrp-grp > .bit-dtrp-sdt")).ToHaveTextAsync("Selected date range Start: 6/10/2024 - End: 6/14/2024");

        // The ends of the range carry the accent, the days between it its lighter body.
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(pid, 2024, 6, 10)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(Day(pid, 2024, 6, 11)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri-light)"));

        // A third pick starts over.
        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-20 00:00 / -");
        await Expect(Day(pid, 2024, 6, 14)).Not.ToHaveClassAsync(HasClass("bit-dtrp-dse"));
    }

    [TestMethod]
    public async Task Pick_AnEndBeforeTheStartIsSwappedAndKeepsTheStartAndEndTimes()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");

        await Day(pid, 2024, 6, 14).ClickAsync();
        await Day(pid, 2024, 6, 10).ClickAsync();

        // The earlier day is the start of the range and the later one its end, whichever was picked first - and
        // the range still runs from the start of its first day to the end of its last, as a forward pick does.
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 23:59");
        await Expect(Input("drp-basic")).ToHaveValueAsync("Start: 6/10/2024 - End: 6/14/2024");
    }

    [TestMethod]
    public async Task Keyboard_TheArrowsPreviewAndEnterPicksBothEnds()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-basic");

        await Page.Locator("#before-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("drp-basic")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await PressAsync("ArrowLeft", Day(pid, 2024, 6, 11));
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 12));
        await PressAsync("ArrowUp", Day(pid, 2024, 6, 5));
        await PressAsync("ArrowDown", Day(pid, 2024, 6, 12));
        await PressAsync("Home", Day(pid, 2024, 6, 9));
        await PressAsync("End", Day(pid, 2024, 6, 15));
        await PressAsync("PageDown", Day(pid, 2024, 7, 15));
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");
        await Expect(Page.Locator("#basic-last-month")).ToHaveTextAsync("2024-07-01");
        await PressAsync("Shift+PageDown", Day(pid, 2025, 7, 15));
        await PressAsync("Shift+PageUp", Day(pid, 2024, 7, 15));
        await PressAsync("PageUp", Day(pid, 2024, 6, 15));
        await PressAsync("Home", Day(pid, 2024, 6, 9));
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 10));

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-10 00:00 / -");

        // The day the arrows move to previews the range as the pointer does.
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 11));
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 12));
        await Expect(Day(pid, 2024, 6, 11)).ToHaveClassAsync(HasClass("bit-dtrp-dhr"));
        await Expect(Day(pid, 2024, 6, 12)).ToHaveClassAsync(HasClass("bit-dtrp-dhr"));

        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 23:59");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_EnterAndSpaceOpenTheFieldAndEscapeClosesIt()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-basic");

        await Input("drp-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-close-count")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync(" ");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Close_ClickingOutsideClosesAndTheFocusIsHeldWhileOpen()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        for (var i = 0; i < 10; i++)
        {
            await Page.Keyboard.PressAsync(i % 3 == 2 ? "Shift+Tab" : "Tab");
            Assert.IsTrue(await FocusIsInsideAsync($"#{pid}-callout"), $"The focus left the callout after press {i + 1}.");
        }

        await Expect(Overlay(pid)).ToBeVisibleAsync();
        await Page.Mouse.ClickAsync(1200, 650);

        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Overlay(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#basic-close-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Navigation_TheMonthButtonsAndTheMonthAndYearPickersMoveTheCalendar()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");
        var callout = Callout(pid);

        await callout.Locator(".bit-dtrp-nbt[title='Go to next month']").ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("July 2024");
        await Expect(Page.Locator("#basic-last-month")).ToHaveTextAsync("2024-07-01");
        await callout.Locator(".bit-dtrp-nbt[title='Go to previous month']").ClickAsync();
        await callout.Locator(".bit-dtrp-nbt[title='Go to previous month']").ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("May 2024");

        await MonthCell(pid, 9).ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("September 2024");
        await Expect(MonthCell(pid, 9)).ToHaveAttributeAsync("aria-selected", "true");

        var yearToggle = callout.Locator(".bit-dtrp-mwp .bit-dtrp-ptb");
        await Expect(yearToggle).ToHaveTextAsync("2024");
        await callout.Locator(".bit-dtrp-nbt[title='Go to next year 2025']").ClickAsync();
        await Expect(yearToggle).ToHaveTextAsync("2025");
        await Expect(Title(callout)).ToHaveTextAsync("September 2025");

        await yearToggle.ClickAsync();
        await Expect(YearCell(pid, 2030)).ToBeVisibleAsync();
        await YearCell(pid, 2030).ClickAsync();
        await Expect(yearToggle).ToHaveTextAsync("2030");
        await Expect(Title(callout)).ToHaveTextAsync("September 2030");

        await yearToggle.ClickAsync();
        await callout.Locator(".bit-dtrp-nbt[title^='Next year range']").ClickAsync();
        await Expect(callout.Locator(".bit-dtrp-mwp .bit-dtrp-ptb")).ToHaveTextAsync("2041 - 2052");

        await callout.Locator(".bit-dtrp-gtb").ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("June 2024");
        await Expect(Page.Locator("#basic-last-month")).ToHaveTextAsync("2024-06-01");
    }

    [TestMethod]
    public async Task Navigation_AfterAPointerTogglesTheYearGrid_TheFocusStaysInTheCallout()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");

        await Callout(pid).Locator(".bit-dtrp-mwp .bit-dtrp-ptb").ClickAsync();
        await Expect(YearCell(pid, 2024)).ToBeVisibleAsync();

        // The toggle that was clicked is replaced by the year grid's own header, so the focus has to be handed over
        // to the grid that took its place - the modal callout must not lose it to the body.
        await Page.WaitForFunctionAsync("sel => document.querySelector(sel)?.contains(document.activeElement)", $"#{pid}-callout");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Navigation_TheMonthAndYearGridsAnswerTheKeyboard()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-basic");

        await MonthCell(pid, 6).FocusAsync();
        await PressAsync("ArrowRight", MonthCell(pid, 7));
        await PressAsync("ArrowDown", MonthCell(pid, 11));
        await PressAsync("Home", MonthCell(pid, 1));
        await PressAsync("End", MonthCell(pid, 12));
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Callout(pid).Locator(".bit-dtrp-mwp .bit-dtrp-ptb")).ToHaveTextAsync("2025");

        await Callout(pid).Locator(".bit-dtrp-mwp .bit-dtrp-ptb").ClickAsync();
        await YearCell(pid, 2025).FocusAsync();
        await PressAsync("ArrowLeft", YearCell(pid, 2024));
        await PressAsync("ArrowDown", YearCell(pid, 2028));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Binding_AValueSetByThePageShowsInTheFieldAndTheCalendar()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-basic");

        await Page.Locator("#btn-basic-set").ClickAsync();
        await Expect(Input("drp-basic")).ToHaveValueAsync("Start: 7/1/2024 - End: 7/4/2024");

        await Input("drp-basic").ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");
        await Expect(Day(pid, 2024, 7, 1)).ToHaveClassAsync(HasClass("bit-dtrp-dss"));
        await Expect(Day(pid, 2024, 7, 4)).ToHaveClassAsync(HasClass("bit-dtrp-dse"));
        await Expect(Day(pid, 2024, 7, 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-basic-null").ClickAsync();
        await Expect(Input("drp-basic")).ToHaveValueAsync("");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Events_FocusInFocusAndFocusOutFollowTheField()
    {
        await OpenAsync(Route);

        await Page.Locator("#before-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#basic-focusin-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-focus-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#drp-basic")).ToHaveClassAsync(HasClass("bit-dtrp-foc"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-focusout-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#drp-basic")).Not.ToHaveClassAsync(HasClass("bit-dtrp-foc"));
    }

    [TestMethod]
    public async Task DefaultValue_AnUncontrolledPickerStartsOnItAndReportsChangesThroughOnChange()
    {
        await OpenAsync(Route);

        await Expect(Input("drp-default")).ToHaveValueAsync("Start: 6/3/2024 - End: 6/5/2024");

        var pid = await OpenPickerAsync("drp-default");
        await Expect(Day(pid, 2024, 6, 3)).ToHaveClassAsync(HasClass("bit-dtrp-dss"));
        await Expect(Day(pid, 2024, 6, 5)).ToHaveClassAsync(HasClass("bit-dtrp-dse"));

        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#default-changed")).ToHaveTextAsync("2024-06-20 00:00 / -");
    }

    [TestMethod]
    public async Task ClearButton_ShownWithARange_ClearsItAndReportsOnClear()
    {
        await OpenAsync(Route);

        var clear = Page.Locator("#drp-clear .bit-dtrp-clr");
        var pid = await PickerIdAsync("drp-clear");

        await Expect(Input("drp-clear")).ToHaveValueAsync("Start: 6/10/2024 - End: 6/14/2024");
        await Expect(clear).ToHaveAttributeAsync("title", "Clear the dates");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Clear the dates");

        await clear.ClickAsync();

        await Expect(Page.Locator("#clear-value")).ToHaveTextAsync("null");
        await Expect(Input("drp-clear")).ToHaveValueAsync("");
        await Expect(Page.Locator("#clear-count")).ToHaveTextAsync("1");
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(Input("drp-clear")).ToBeFocusedAsync();
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task AutoClose_Off_KeepsTheCalloutOpenOnceTheRangeIsComplete()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-noautoclose");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 12).ClickAsync();
        await Expect(Page.Locator("#noautoclose-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 23:59");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, 2024, 6, 12)).ToHaveClassAsync(HasClass("bit-dtrp-dse"));
    }

    [TestMethod]
    public async Task MonthPicker_HighlightsTheCurrentAndTheSelectedMonthAndNamesItsToggle()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-months-hl");

        await Expect(MonthCell(pid, 6)).ToHaveClassAsync(HasClass("bit-dtrp-pcm"));
        await Expect(MonthCell(pid, 6)).ToHaveClassAsync(HasClass("bit-dtrp-psm"));
        await MonthCell(pid, 9).ClickAsync();
        await Expect(MonthCell(pid, 9)).ToHaveClassAsync(HasClass("bit-dtrp-psm"));
        await Expect(MonthCell(pid, 6)).Not.ToHaveClassAsync(HasClass("bit-dtrp-psm"));
        await Expect(MonthCell(pid, 6)).ToHaveClassAsync(HasClass("bit-dtrp-pcm"));
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(MonthCell(pid, 6)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await Page.Keyboard.PressAsync("Escape");

        var titled = await OpenPickerAsync("drp-toggle-title");
        await Expect(Callout(titled).Locator(".bit-dtrp-dwp .bit-dtrp-ptb")).ToHaveAttributeAsync("title", "Pick another month than June 2024");
    }

    [TestMethod]
    public async Task Presets_APresetAppliesItsRangeClosesAndStaysHighlighted()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-presets");
        var group = Callout(pid).Locator(".bit-dtrp-prc");
        var today = group.Locator(".bit-dtrp-prb", new() { HasText = "Today" });
        var week = group.Locator(".bit-dtrp-prb", new() { HasText = "Next 7 days" });

        await Expect(group).ToHaveAttributeAsync("role", "group");
        await Expect(group).ToHaveAttributeAsync("aria-label", "Quick ranges");
        await Expect(group.Locator(".bit-dtrp-prb")).ToHaveCountAsync(4);
        await Expect(today).ToHaveAttributeAsync("title", "Today");
        await Expect(week).ToHaveAttributeAsync("title", "From today on");
        await Expect(week).ToHaveClassAsync(HasClass("e2e-preset-class"));
        await Expect(week).ToHaveCSSAsync("font-style", "italic");
        await Expect(group.Locator(".bit-dtrp-prb", new() { HasText = "Unavailable" })).ToBeDisabledAsync();

        // The presets lead the calendar.
        var presets = await BoxAsync(group);
        var days = await BoxAsync(Callout(pid).Locator(".bit-dtrp-dwp"));
        Assert.IsTrue(presets.X + presets.Width <= days.X + 1, "The presets stand before the day picker.");

        await week.ClickAsync();
        await Expect(Page.Locator("#presets-value")).ToHaveTextAsync("2024-06-12 00:00 / 2024-06-18 23:59");
        await Expect(Page.Locator("#presets-last")).ToHaveTextAsync("Next 7 days");
        await Expect(Page.Locator("#presets-count")).ToHaveTextAsync("1");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-presets")).ToBeFocusedAsync();

        await Input("drp-presets").ClickAsync();
        await Expect(week).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(week).ToHaveClassAsync(HasClass("bit-dtrp-prs"));
        await Expect(today).ToHaveAttributeAsync("aria-pressed", "false");
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(week).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));

        // A preset the bounds rule out is rejected rather than clamped.
        await group.Locator(".bit-dtrp-prb", new() { HasText = "Last year" }).ClickAsync();
        await today.ClickAsync();
        await Expect(Page.Locator("#presets-last")).ToHaveTextAsync("Today");
        await Expect(Page.Locator("#presets-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#presets-value")).ToHaveTextAsync("2024-06-12 00:00 / 2024-06-12 23:59");

        // A pick of its own leaves no preset applied.
        await Input("drp-presets").ClickAsync();
        await Day(pid, 2024, 6, 3).ClickAsync();
        await Expect(today).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    public async Task Presets_AStandalonePickerAppliesThemInPlace()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-presets-sa");

        await Callout(pid).Locator(".bit-dtrp-prb", new() { HasText = "Today" }).ClickAsync();
        await Expect(Page.Locator("#presets-sa-value")).ToHaveTextAsync("2024-06-12 00:00 / 2024-06-12 23:59");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, Today)).ToHaveClassAsync(HasClass("bit-dtrp-dsse"));
    }

    [TestMethod]
    public async Task Apply_TheRangeIsCommittedByApplyAndRolledBackByCancelAndEscape()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-apply");
        var apply = Callout(pid).Locator(".bit-dtrp-apb");
        var cancel = Callout(pid).Locator(".bit-dtrp-cnb");

        await Expect(apply).ToHaveTextAsync("Book");
        await Expect(cancel).ToHaveTextAsync("Never mind");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Expect(apply).ToBeDisabledAsync();
        await Day(pid, 2024, 6, 14).ClickAsync();

        // Picks reach the value while open, and the callout waits for the commit.
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 23:59");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(apply).ToBeEnabledAsync();

        await cancel.ClickAsync();
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#apply-outcome")).ToHaveTextAsync("Cancelled");
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("null");
        await Expect(Input("drp-apply")).ToBeFocusedAsync();

        await Input("drp-apply").ClickAsync();
        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 14).ClickAsync();
        await apply.ClickAsync();
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#apply-outcome")).ToHaveTextAsync("Applied 2024-06-10 00:00 / 2024-06-14 23:59");
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 23:59");

        // Escape rolls back to the range the callout was opened on. The preset is pressed from the keyboard, which
        // keeps the focus on it: a pointer press of a button focuses it in Chromium but leaves the focus on the
        // body in WebKit, where no Escape reaches the callout then.
        await Input("drp-apply").ClickAsync();
        await Expect(Day(pid, 2024, 6, 14)).ToBeFocusedAsync();
        await Callout(pid).Locator(".bit-dtrp-prb", new() { HasText = "Today" }).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("2024-06-12 00:00 / 2024-06-12 23:59");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 23:59");
    }

    [TestMethod]
    public async Task Apply_TheActionsStayAtTheBottomOfTheCallout()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-apply");
        var actions = await BoxAsync(Callout(pid).Locator(".bit-dtrp-act"));
        var group = await BoxAsync(Callout(pid).Locator(".bit-dtrp-grp"));
        var callout = await BoxAsync(Callout(pid));

        Assert.IsTrue(actions.Y >= group.Y + group.Height - 1, "The actions follow the calendar.");
        Assert.AreEqual(callout.Y + callout.Height, actions.Y + actions.Height, 2, "The actions close the callout.");

        var cancel = await BoxAsync(Callout(pid).Locator(".bit-dtrp-cnb"));
        var apply = await BoxAsync(Callout(pid).Locator(".bit-dtrp-apb"));
        Assert.IsTrue(cancel.X < apply.X, "Cancel comes before Apply.");
    }

    [TestMethod]
    public async Task MinMax_TheDaysOutsideAreDisabledAndTheNavigationStops()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-minmax");
        var callout = Callout(pid);

        await Expect(Day(pid, 2024, 6, 6)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 7)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 17)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 18)).ToBeDisabledAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Go to previous month']")).ToBeDisabledAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Go to next month']")).ToBeDisabledAsync();
        await Expect(MonthCell(pid, 5)).ToBeDisabledAsync();
        await Expect(MonthCell(pid, 7)).ToBeDisabledAsync();

        await PressAsync("End", Day(pid, 2024, 6, 15));
        await Page.Keyboard.PressAsync("ArrowDown");
        await PressAsync("ArrowLeft", Day(pid, 2024, 6, 14));
        await Page.Keyboard.PressAsync("Escape");

        var past = await OpenPickerAsync("drp-past");
        await Expect(Day(past, 2024, 6, 11)).ToBeDisabledAsync();
        await Expect(Day(past, 2024, 6, 12)).ToBeEnabledAsync();
        await Page.Keyboard.PressAsync("Escape");

        var future = await OpenPickerAsync("drp-future");
        await Expect(Day(future, 2024, 6, 13)).ToBeDisabledAsync();
        await Expect(Day(future, 2024, 6, 12)).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task Ranges_MinRangeDisablesTheDaysTooCloseToTheStart()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-minrange");

        await Day(pid, 2024, 6, 10).ClickAsync();
        foreach (var day in new[] { 8, 9, 11, 12 })
        {
            await Expect(Day(pid, 2024, 6, day)).ToBeDisabledAsync();
        }
        await Expect(Day(pid, 2024, 6, 13)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 7)).ToBeEnabledAsync();

        await Day(pid, 2024, 6, 13).ClickAsync();
        await Expect(Page.Locator("#minrange-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-13 23:59");
    }

    [TestMethod]
    public async Task Ranges_MaxRangeBoundsTheEndAroundTheStart()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-minmaxrange");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Expect(Day(pid, 2024, 6, 11)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 12)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 17)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 18)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 3)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 2)).ToBeDisabledAsync();

        // Seven whole days from the start of the 10th end at the start of the 17th, so the end of the range is
        // clamped to it rather than taking the end of that day.
        await Day(pid, 2024, 6, 17).ClickAsync();
        await Expect(Page.Locator("#minmaxrange-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-17 00:00");
    }

    [TestMethod]
    public async Task Ranges_AMaxRangeWithHoursClampsTheEndTime()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-maxrange-time");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Expect(Day(pid, 2024, 6, 12)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 13)).ToBeDisabledAsync();
        await Day(pid, 2024, 6, 12).ClickAsync();

        await Expect(Page.Locator("#maxrange-time-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 04:30");
        await Expect(Callout(pid).Locator(".bit-dtrp-tin[aria-label='End hour']")).ToHaveValueAsync("4");

        // The spin buttons that would stretch the range past it are disabled.
        await Expect(Callout(pid).Locator(".bit-dtrp-tbt[title='Increase end hour']")).ToBeDisabledAsync();
        await Expect(Callout(pid).Locator(".bit-dtrp-tbt[title='Decrease end hour']")).ToBeEnabledAsync();
        await Page.Keyboard.PressAsync("Escape");

        var shortId = await OpenPickerAsync("drp-maxrange-short");
        await Expect(Callout(shortId).Locator(".bit-dtrp-tin[aria-label='End hour']")).ToHaveValueAsync("6");
        await Day(shortId, 2024, 6, 10).ClickAsync();
        await Expect(Day(shortId, 2024, 6, 11)).ToBeDisabledAsync();
        await Day(shortId, 2024, 6, 10).ClickAsync();
        await Expect(Page.Locator("#maxrange-short-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-10 06:00");
    }

    [TestMethod]
    public async Task DisabledDays_TheRulesDisableTheirDaysAndTheKeyboardSkipsThem()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-weekend");
        await Expect(Day(pid, 2024, 6, 15)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 16)).ToBeDisabledAsync();
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 13));
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 14));
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 17));

        // A range still spans over a disabled day.
        await Day(pid, 2024, 6, 14).ClickAsync();
        await Day(pid, 2024, 6, 17).ClickAsync();
        await Expect(Page.Locator("#weekend-value")).ToHaveTextAsync("2024-06-14 00:00 / 2024-06-17 23:59");

        var dates = await OpenPickerAsync("drp-dates");
        await Expect(Day(dates, 2024, 6, 14)).ToBeDisabledAsync();
        await Expect(Day(dates, 2024, 6, 18)).ToBeDisabledAsync();
        await Day(dates, 2024, 6, 13).ClickAsync();
        await Expect(Day(dates, 2024, 6, 20)).ToBeEnabledAsync();
        await Day(dates, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#dates-value")).ToHaveTextAsync("2024-06-13 00:00 / 2024-06-20 23:59");

        var rule = await OpenPickerAsync("drp-isdisabled");
        await Expect(Day(rule, 2024, 6, 11)).ToBeDisabledAsync();
        await Expect(Day(rule, 2024, 6, 12)).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task DisabledDays_ExcludeDisabledDatesKeepsTheRangeFromCoveringOne()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-exclude");

        await Day(pid, 2024, 6, 15).ClickAsync();
        await Expect(Day(pid, 2024, 6, 16)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 17)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 19)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 20)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 13)).ToBeDisabledAsync();

        await Day(pid, 2024, 6, 17).ClickAsync();
        await Expect(Page.Locator("#exclude-value")).ToHaveTextAsync("2024-06-15 00:00 / 2024-06-17 23:59");
    }

    [TestMethod]
    public async Task Highlighted_HighlightedDatesGetDayClassAndHighlightToday()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-hl");
        await Expect(Day(pid, 2024, 6, 5)).ToHaveClassAsync(HasClass("bit-dtrp-dhl"));
        await Expect(Day(pid, 2024, 6, 6)).Not.ToHaveClassAsync(HasClass("bit-dtrp-dhl"));
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(pid, 2024, 6, 5)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-ter)"));
        await Page.Keyboard.PressAsync("Escape");

        var fridays = await OpenPickerAsync("drp-dayclass");
        foreach (var day in new[] { 7, 14, 21, 28 })
        {
            await Expect(Day(fridays, 2024, 6, day)).ToHaveClassAsync(HasClass("e2e-friday"));
        }
        await Expect(Day(fridays, 2024, 6, 13)).Not.ToHaveClassAsync(HasClass("e2e-friday"));
        await Page.Keyboard.PressAsync("Escape");

        var noToday = await OpenPickerAsync("drp-notoday");
        await Expect(Day(noToday, Today)).Not.ToHaveClassAsync(HasClass("bit-dtrp-dtd"));
        await Expect(Day(noToday, Today)).ToHaveAttributeAsync("aria-current", "date");
    }

    [TestMethod]
    public async Task Layout_FirstDayWeekNumbersOutsideDaysAndFixedWeeks()
    {
        await OpenAsync(Route);

        var monday = await OpenPickerAsync("drp-monday");
        await Expect(Callout(monday).Locator(".bit-dtrp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Monday");
        await PressAsync("Home", Day(monday, 2024, 6, 10));
        await Page.Keyboard.PressAsync("Escape");

        var weeks = await OpenPickerAsync("drp-weeks");
        var row = Callout(weeks).Locator(".bit-dtrp-dgr", new() { Has = Day(weeks, Today) });
        await Expect(row.Locator(".bit-dtrp-wnm")).ToHaveTextAsync(WeekOfYear(Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString(CultureInfo.InvariantCulture));
        await Expect(row.Locator(".bit-dtrp-wnm")).ToHaveAttributeAsync("title", "Week number 24");
        await Page.Keyboard.PressAsync("Escape");

        var noOut = await OpenPickerAsync("drp-noout");
        await Expect(Day(noOut, 2024, 5, 26)).ToHaveCountAsync(0);
        await Expect(Callout(noOut).Locator(".bit-dtrp-dbe")).ToHaveCountAsync(12);
        await Page.Keyboard.PressAsync("Escape");

        var fixedId = await OpenPickerAsync("drp-fixed");
        await Expect(Title(Callout(fixedId))).ToHaveTextAsync("February 2024");
        await Expect(Callout(fixedId).Locator(".bit-dtrp-dgr")).ToHaveCountAsync(6);
        await Page.Keyboard.PressAsync("Escape");

        var free = await OpenPickerAsync("drp-notfixed");
        await Expect(Callout(free).Locator(".bit-dtrp-dgr")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task Layout_MonthPickerAsAnOverlayHiddenAndTheCloseButton()
    {
        await OpenAsync(Route);

        var overlay = await OpenPickerAsync("drp-overlay");
        var title = Callout(overlay).Locator(".bit-dtrp-dwp .bit-dtrp-ptb");
        await Expect(Callout(overlay).Locator(".bit-dtrp-mwp")).ToHaveCountAsync(0);
        await Expect(title).ToHaveAttributeAsync("title", "June 2024, change month");
        await title.ClickAsync();
        await Expect(Callout(overlay).Locator(".bit-dtrp-mwp")).ToBeVisibleAsync();
        await MonthCell(overlay, 8).ClickAsync();
        await Expect(title).ToHaveTextAsync("August 2024");

        // The month grid went away with the month that was clicked; Escape is pressed once the focus has been handed on.
        await Page.WaitForFunctionAsync("sel => document.querySelector(sel)?.contains(document.activeElement)", $"#{overlay}-callout");
        await Page.Keyboard.PressAsync("Escape");

        var hidden = await OpenPickerAsync("drp-nomonth");
        await Expect(Callout(hidden).Locator(".bit-dtrp-mwp")).ToHaveCountAsync(0);
        await Expect(Callout(hidden).Locator(".bit-dtrp-dwp .bit-dtrp-gtb")).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");

        var close = await OpenPickerAsync("drp-close");
        await Expect(Callout(close).Locator(".bit-dtrp-gtb")).ToHaveCountAsync(0);
        await Callout(close).Locator(".bit-dtrp-nbt[title='Close date range picker']").ClickAsync();
        await Expect(Callout(close)).ToBeHiddenAsync();
        await Expect(Page.Locator("#close-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Months_ARangeFlowsAcrossTheMonthsSideBySide()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-two");
        var panes = Callout(pid).Locator(".bit-dtrp-dwp");

        await Expect(panes).ToHaveCountAsync(2);
        await Expect(panes.Nth(0).Locator(".bit-dtrp-pkt, .bit-dtrp-ptb").First).ToHaveTextAsync("June 2024");
        await Expect(panes.Nth(1).Locator(".bit-dtrp-pkt")).ToHaveTextAsync("July 2024");

        var first = await BoxAsync(panes.Nth(0));
        var second = await BoxAsync(panes.Nth(1));
        Assert.IsTrue(second.X >= first.X + first.Width - 1, "The months are laid out side by side.");

        await Day(pid, 2024, 6, 28).ClickAsync();
        await Day(pid, 2024, 7, 3).HoverAsync();
        await Expect(Day(pid, 2024, 7, 1)).ToHaveClassAsync(HasClass("bit-dtrp-dhr"));
        await Day(pid, 2024, 7, 3).ClickAsync();

        await Expect(Page.Locator("#two-value")).ToHaveTextAsync("2024-06-28 00:00 / 2024-07-03 23:59");

        await Input("drp-two").ClickAsync();
        await Expect(Day(pid, 2024, 6, 30)).ToHaveClassAsync(HasClass("bit-dtrp-dsb"));
        await Expect(Day(pid, 2024, 7, 1)).ToHaveClassAsync(HasClass("bit-dtrp-dsb"));

        // Reopened on the day picked last, the keyboard crosses back into the first month without moving the strip.
        await Expect(Day(pid, 2024, 7, 3)).ToBeFocusedAsync();
        await PressAsync("ArrowLeft", Day(pid, 2024, 7, 2));
        await PressAsync("ArrowLeft", Day(pid, 2024, 7, 1));
        await PressAsync("ArrowLeft", Day(pid, 2024, 6, 30));
        await Expect(panes.Nth(0).Locator(".bit-dtrp-pkt, .bit-dtrp-ptb").First).ToHaveTextAsync("June 2024");
    }

    [TestMethod]
    public async Task Months_ThreeMonthsPagedNavigationAndAStandaloneStrip()
    {
        await OpenAsync(Route);

        var three = await OpenPickerAsync("drp-three");
        await Expect(Callout(three).Locator(".bit-dtrp-dwp")).ToHaveCountAsync(3);
        await Expect(Callout(three).Locator(".bit-dtrp-dwp").Nth(2).Locator(".bit-dtrp-pkt")).ToHaveTextAsync("August 2024");
        await Expect(Callout(three).Locator(".bit-dtrp-dwp").Nth(2).Locator(".bit-dtrp-wnm").First).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");

        var paged = await OpenPickerAsync("drp-paged");
        var panes = Callout(paged).Locator(".bit-dtrp-dwp");
        await panes.Nth(1).Locator(".bit-dtrp-nbt[title='Go to next month']").ClickAsync();
        await Expect(panes.Nth(0).Locator(".bit-dtrp-pkt, .bit-dtrp-ptb").First).ToHaveTextAsync("August 2024");
        await Expect(panes.Nth(1).Locator(".bit-dtrp-pkt")).ToHaveTextAsync("September 2024");
        await Page.Keyboard.PressAsync("Escape");

        var standalone = await PickerIdAsync("drp-two-sa");
        await Expect(Callout(standalone).Locator(".bit-dtrp-dwp")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Standalone_TheCalendarIsInlineAndPicksARangeInPlace()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-sa");
        var calendar = Callout(pid);

        await Expect(calendar).ToBeVisibleAsync();
        await Expect(calendar).ToHaveClassAsync(HasClass("bit-dtrp-sta"));
        await Expect(calendar.Locator(".bit-dtrp-cac")).ToHaveAttributeAsync("role", "group");
        await Expect(Overlay(pid)).ToHaveCountAsync(0);
        await Expect(Page.Locator("#drp-sa .bit-dtrp-wrp")).ToHaveCountAsync(0);

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 12).ClickAsync();
        await Expect(Page.Locator("#sa-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 23:59");
        await Expect(Page.Locator("#drp-sa input.bit-input-hidden")).ToHaveValueAsync("Start: 6/10/2024 - End: 6/12/2024");
        await Expect(calendar).ToBeVisibleAsync();

        var disabled = await PickerIdAsync("drp-sa-dis");
        await Expect(Callout(disabled)).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Day(disabled, 2024, 6, 10)).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task ReadOnly_TheCalendarBrowsesButNothingChangesTheRange()
    {
        await OpenAsync(Route);

        var input = Input("drp-ro");
        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await Expect(Page.Locator("#drp-ro .bit-dtrp-clr")).ToHaveCountAsync(0);

        await input.EvaluateAsync("el => { el.value = 'Start: 1/1/2020 - End: 1/2/2020'; el.dispatchEvent(new Event('change', { bubbles: true })); }");

        var pid = await OpenPickerAsync("drp-ro");
        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dtrp-rol"));
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-readonly", "true");

        await Day(pid, 2024, 6, 20).ClickAsync();
        await Callout(pid).Locator(".bit-dtrp-prb", new() { HasText = "Today" }).ClickAsync();
        await Callout(pid).Locator(".bit-dtrp-nbt[title='Go to next month']").ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");

        await Expect(Page.Locator("#ro-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 23:59");
        await Expect(Callout(pid)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Templates_EveryPartIsRenderedFromItsTemplate()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-tpl");

        await Expect(Page.Locator("#drp-tpl label #tpl-label")).ToHaveTextAsync("Custom label");
        await Expect(Page.Locator("#drp-tpl .bit-dtrp-icn #tpl-icon")).ToBeVisibleAsync();
        await Expect(Page.Locator("#drp-tpl .bit-dtrp-ico")).ToHaveCountAsync(0);

        await Input("drp-tpl").ClickAsync();
        await SettleAsync(Callout(pid));

        var header = await BoxAsync(Callout(pid).Locator("#tpl-header"));
        var footer = await BoxAsync(Callout(pid).Locator("#tpl-footer"));
        var group = await BoxAsync(Callout(pid).Locator(".bit-dtrp-grp"));
        Assert.IsTrue(header.Y + header.Height <= group.Y + 1, "The header sits above the calendar.");
        Assert.IsTrue(footer.Y >= group.Y + group.Height - 1, "The footer sits below the calendar.");

        await Expect(Day(pid, 2024, 6, 20).Locator(".e2e-day-cell[data-day='20']")).ToHaveTextAsync("d20");
        await Expect(MonthCell(pid, 6).Locator(".e2e-month-cell")).ToHaveTextAsync("m6");
        await Callout(pid).Locator(".bit-dtrp-mwp .bit-dtrp-ptb").ClickAsync();
        await Expect(YearCell(pid, 2024).Locator(".e2e-year-cell")).ToHaveTextAsync("y2024");
    }

    [TestMethod]
    public async Task Validation_InAnEditForm_TheRequiredRangeFailsUntilOneIsPicked()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-valid");

        await Page.Locator("#valid-submit").ClickAsync();
        await Expect(Page.Locator("#valid-result")).ToHaveTextAsync("invalid");
        await Expect(Page.Locator("#valid-message")).ToHaveTextAsync("The Range field is required.");
        await Expect(Page.Locator("#drp-valid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("drp-valid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#drp-valid .bit-dtrp-icn")).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Input("drp-valid").ClickAsync();
        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 11).ClickAsync();
        await Expect(Page.Locator("#valid-message")).ToHaveTextAsync("");
        await Expect(Page.Locator("#drp-valid")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Page.Locator("#valid-submit").ClickAsync();
        await Expect(Page.Locator("#valid-result")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-11 23:59");
    }

    [TestMethod]
    public async Task Programmatic_OpenCalloutCloseCalloutAndTheTwoWayBoundIsOpen()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-prog");

        await Page.Locator("#btn-open-method").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#prog-opens")).ToHaveTextAsync("1");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Locator("#btn-close-method").DispatchEventAsync("click");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#prog-closes")).ToHaveTextAsync("1");

        await Page.Locator("#btn-open-param").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Page.Locator("#prog-opens")).ToHaveTextAsync("2");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Programmatic_AOneWayBoundIsOpenLeavesTheClosingToThePage()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-oneway");

        await Page.Locator("#btn-oneway-open").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));

        await Page.Mouse.ClickAsync(1200, 650);
        await Page.Keyboard.PressAsync("Escape");
        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 11).ClickAsync();

        await Expect(Page.Locator("#oneway-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-11 23:59");
        await Expect(Callout(pid)).ToBeVisibleAsync();

        await Page.Locator("#btn-oneway-close").DispatchEventAsync("click");
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Accessibility_TheNamesAndTitlesAreWhatThePageSet()
    {
        await OpenAsync(Route);

        var input = Input("drp-a11y");
        await Expect(input).ToHaveAttributeAsync("role", "combobox");
        await Expect(input).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(input).ToHaveAttributeAsync("aria-label", "Stay dates");
        await Expect(Page.Locator("#drp-a11y .bit-dtrp-clr")).ToHaveAttributeAsync("aria-label", "Clear the stay");

        var pid = await OpenPickerAsync("drp-a11y");
        var callout = Callout(pid);

        await Expect(callout.Locator(".bit-dtrp-cac")).ToHaveAttributeAsync("aria-label", "Stay calendar");
        await Expect(callout.Locator(".bit-dtrp-grp > .bit-dtrp-sdt")).ToHaveTextAsync("Stay selected: Start: 6/10/2024 - End: 6/14/2024");
        await Expect(callout.Locator(".bit-dtrp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Wk");
        await Expect(callout.Locator(".bit-dtrp-wnm").First).ToHaveAttributeAsync("aria-label", new Regex("^Wk \\d+$"));
        await Expect(Day(pid, 2024, 6, 6)).ToHaveAttributeAsync("aria-label", "Thursday, June 6, 2024");
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Earlier month']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Later month']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Done']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtrp-gtb[title='Back to today']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Year 2023 back']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Year 2025 ahead']")).ToBeVisibleAsync();

        var toggle = callout.Locator(".bit-dtrp-mwp .bit-dtrp-ptb");
        await Expect(toggle).ToHaveAttributeAsync("title", "2024, pick a year");
        await toggle.ClickAsync();
        await Expect(callout.Locator(".bit-dtrp-mwp .bit-dtrp-ptb")).ToHaveAttributeAsync("title", "2023 to 2034, pick a month");
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Years 2011 to 2022 back']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtrp-nbt[title='Years 2035 to 2046 ahead']")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Base_IdClassStyleAttributesNamesAndVisibility()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#drp-base");
        var input = Input("drp-base");

        await Expect(root).ToHaveClassAsync(HasClass("e2e-root-class"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-dtrp"));
        await Expect(root).ToHaveCSSAsync("margin-top", "7px");
        await Expect(root).ToHaveAttributeAsync("data-e2e", "root-attr");
        await Expect(root).ToHaveAttributeAsync("title", "Root title");
        await Expect(input).ToHaveAttributeAsync("aria-label", "Base range");
        await Expect(input).ToHaveAttributeAsync("name", "base-range");
        await Expect(input).ToHaveAttributeAsync("tabindex", "0");
        await Expect(input).ToHaveAttributeAsync("data-e2e-input", "input-attr");

        var pid = await OpenPickerAsync("drp-base");
        await Expect(Callout(pid).Locator(".bit-dtrp-cac")).ToHaveAttributeAsync("data-e2e-callout", "callout-attr");
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-hidden").ClickAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        await Page.Locator("#btn-collapsed").ClickAsync();
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-visible").ClickAsync();
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Disabled_TheFieldTheCalloutAndThePagesHandlersAreAllOff()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#drp-disabled");
        var input = Input("drp-disabled");
        var pid = await PickerIdAsync("drp-disabled");

        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(input).ToBeDisabledAsync();
        await Expect(input).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Day(pid, 2024, 6, 10)).ToBeDisabledAsync();

        await root.Locator(".bit-dtrp-wrp").ClickAsync(new() { Force = true });
        await input.DispatchEventAsync("click");
        await root.DispatchEventAsync("click");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(Page.Locator("#drp-toggle-dis")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-onclick")).ToHaveTextAsync("0");

        // Enabled again, the page's own handler on the root hears the field's click.
        await Input("drp-toggle-dis").ClickAsync();
        await Expect(Page.Locator("#toggle-root-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Required_MarksTheLabelAndTheInput()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#drp-required")).ToHaveClassAsync(HasClass("bit-dtrp-req"));
        await Expect(Input("drp-required")).ToHaveAttributeAsync("required", "");
        var asterisk = await Page.Locator("#drp-required .bit-dtrp-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        Assert.AreEqual("\"*\"", asterisk);
    }

    [TestMethod]
    public async Task Lifecycle_ReRenderUnmountWhileOpenAutoFocusAndRenderedOpen()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-life");
        await Expect(Callout(pid).Locator(".bit-dtrp-wnm")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");
        await Page.Locator("#btn-toggle-weeks").ClickAsync();
        await Input("drp-life").ClickAsync();
        await Expect(Callout(pid).Locator(".bit-dtrp-wnm").First).ToBeVisibleAsync();

        await Page.Locator("#btn-unmount").DispatchEventAsync("click");
        await Expect(Page.Locator("#life-mounted")).ToHaveTextAsync("False");
        await Expect(Callout(pid)).ToHaveCountAsync(0);
        await Expect(Overlay(pid)).ToHaveCountAsync(0);

        await Page.Locator("#btn-mount-autofocus").ClickAsync();
        await Expect(Input("drp-autofocus")).ToBeFocusedAsync();

        await Page.Locator("#btn-mount-open").ClickAsync();
        var open = await PickerIdAsync("drp-initopen");
        await Expect(Callout(open)).ToBeVisibleAsync();
        await Expect(Page.Locator("#initopen-opens")).ToHaveTextAsync("1");
        await Expect(Day(open, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#initopen-isopen")).ToHaveTextAsync("False");

        await AssertNoServerErrorsAsync();
        Assert.IsFalse(await IsCircuitDeadAsync());
    }
}
