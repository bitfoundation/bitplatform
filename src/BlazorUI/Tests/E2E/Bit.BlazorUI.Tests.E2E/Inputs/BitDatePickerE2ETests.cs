using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The core of BitDatePicker in a real browser (/e2e/date-picker): the callout opened by a click and by the keyboard,
/// placed against the field and relocated to the body while open, closed by a pick, Escape, the overlay and the close
/// button; the day grid's keyboard (arrows, Home/End, PageUp/PageDown with and without Shift, Enter, the focus trap);
/// the month and year grids beside and over the day picker; MinDate/MaxDate, DisablePast/DisableFuture and the day
/// rules; highlighted days; the calendar layout (first day, week numbers, outside days, fixed weeks, several months,
/// paged navigation); the Month and Year modes; Standalone; the templates; validation and the app's own messages;
/// IsOpen, OpenCallout and CloseCalloutAndFocus; the accessible names; the base parameters; the disabled contract; and
/// the lifecycle - prerender, re-render, mount open, AutoFocus and unmount while open.
/// </summary>
[TestClass]
public class BitDatePickerE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-picker";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtp-inp");

    private async Task<string> OpenPickerAsync(string rootId)
    {
        await NoCalloutOpenAsync();

        var pid = await PickerIdAsync(rootId);

        await Input(rootId).ClickAsync();
        await Expect(Input(rootId)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));

        return pid;
    }

    private async Task PressAsync(string key, ILocator focused)
    {
        await Page.Keyboard.PressAsync(key);
        await Expect(focused).ToBeFocusedAsync();
    }

    private static ILocator Title(ILocator callout) => callout.Locator(".bit-dtp-dwp .bit-dtp-pkh").First.Locator(".bit-dtp-pkt, .bit-dtp-ptb").First;

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesThePickerBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"dp-basic\"");
        StringAssert.Contains(html, "role=\"combobox\"");
        StringAssert.Contains(html, "placeholder=\"Select a date\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);

        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("placeholder", "Select a date");
        await Expect(Page.Locator("#dp-basic .bit-dtp-lbl")).ToHaveTextAsync("Basic");
    }

    [TestMethod]
    public async Task Open_ClickingTheField_OpensTheCalloutOnTodayAndMovesTheFocusIntoIt()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");

        await Expect(Page.Locator("#dp-basic")).ToHaveClassAsync(HasClass("bit-dtp-opn"));
        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Expect(Day(pid, Today)).ToHaveAttributeAsync("aria-current", "date");
        await Expect(Day(pid, Today)).ToHaveClassAsync(HasClass("bit-dtp-dtd"));
        await Expect(Title(Callout(pid))).ToHaveTextAsync("June 2024");
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-controls", $"{pid}-dialog");
        await Expect(Page.Locator($"#{pid}-dialog")).ToHaveAttributeAsync("role", "dialog");
        await Expect(Page.Locator($"#{pid}-dialog")).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Page.Locator("#basic-open-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-click-count")).ToHaveTextAsync("1");

        // The open field is marked by its own color, the focus having moved into the callout.
        var primary = await ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(Page.Locator("#dp-basic .bit-dtp-icn")).ToHaveCSSAsync("border-top-color", primary);
    }

    [TestMethod]
    public async Task Open_TheCalloutIsPlacedRightBelowTheFieldAndRelocatedToTheBody()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");

        var field = await BoxAsync(Page.Locator("#dp-basic .bit-dtp-wrp"));
        var callout = await BoxAsync(Callout(pid));

        await Expect(Callout(pid)).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        Assert.AreEqual(field.Y + field.Height + 1, callout.Y, 2, "The callout's top sits on the field's bottom edge.");
        Assert.AreEqual(field.X, callout.X, 2, "The callout starts where the field starts.");
        Assert.IsTrue(callout.Width > 200 && callout.Height > 200, "The calendar has its size.");
        Assert.AreEqual("fixed", await StyleOfAsync(Callout(pid), "position"));

        // While open the callout lives under the body, out of the section the picker is rendered in.
        Assert.IsFalse(await Callout(pid).EvaluateAsync<bool>("el => !!el.closest('#basic')"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();

        // Closed, it is handed back to where it was rendered.
        Assert.IsTrue(await Callout(pid).EvaluateAsync<bool>("el => !!el.closest('#basic')"));
    }

    [TestMethod]
    public async Task Pick_ClickingADay_SelectsItClosesTheCalloutAndReturnsTheFocus()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");

        await Day(pid, 2024, 6, 20).ClickAsync();

        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Input("dp-basic")).ToHaveValueAsync("6/20/2024");
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-select-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-last-select")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-last-change")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Page.Locator("#basic-close-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dp-basic")).Not.ToHaveClassAsync(HasClass("bit-dtp-opn"));

        // Reopened, the selection is marked, focused and announced.
        await Input("dp-basic").ClickAsync();
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Day(pid, 2024, 6, 20)).ToHaveClassAsync(HasClass("bit-dtp-dbs"));
        await Expect(Day(pid, 2024, 6, 20)).ToBeFocusedAsync();
        await Expect(Day(pid, 2024, 6, 19)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Callout(pid).Locator(".bit-dtp-grp > .bit-dtp-sdt")).ToHaveTextAsync("Selected date 6/20/2024");
        await Expect(Page.Locator("#basic-open-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Close_Escape_ClosesTheCalloutAndReturnsTheFocusToTheField()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Input("dp-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-close-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Close_ClickingOutside_ClosesTheCalloutThroughTheOverlay()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");
        await Expect(Overlay(pid)).ToBeVisibleAsync();

        await Page.Mouse.ClickAsync(1200, 650);

        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Overlay(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#basic-close-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Keyboard_ArrowDownOpensAndTheDayGridKeysMoveTheFocusAndPick()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-basic");

        await Page.Locator("#before-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("dp-basic")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        async Task Press(string key, DateTime expected)
        {
            await Page.Keyboard.PressAsync(key);
            await Expect(Day(pid, expected)).ToBeFocusedAsync();
        }

        await Press("ArrowRight", new(2024, 6, 13));
        await Press("ArrowDown", new(2024, 6, 20));
        await Press("ArrowUp", new(2024, 6, 13));
        await Press("ArrowLeft", new(2024, 6, 12));
        await Press("Home", new(2024, 6, 9));
        await Press("End", new(2024, 6, 15));

        await Press("PageDown", new(2024, 7, 15));
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");
        await Expect(Page.Locator("#basic-last-month")).ToHaveTextAsync("2024-07-01");

        await Press("Shift+PageDown", new(2025, 7, 15));
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2025");
        await Press("Shift+PageUp", new(2024, 7, 15));
        await Press("PageUp", new(2024, 6, 15));
        await Expect(Title(Callout(pid))).ToHaveTextAsync("June 2024");

        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-15 00:00");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-month-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Keyboard_EnterAndSpaceOpenAFieldNothingIsTypedInto()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-basic");

        await Input("dp-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-basic")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        // Space picks the focused day like Enter does.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 13)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("2024-06-13 00:00");
    }

    [TestMethod]
    public async Task Keyboard_TheOpenCalloutHoldsTheFocusThroughTabAndShiftTab()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        for (var i = 0; i < 12; i++)
        {
            await Page.Keyboard.PressAsync(i % 3 == 2 ? "Shift+Tab" : "Tab");
            Assert.IsTrue(await FocusIsInsideAsync($"#{pid}-callout"), $"The focus left the callout after press {i + 1}.");
        }

        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Input("dp-basic")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task Navigation_ThePrevAndNextButtonsMoveTheMonthAndReportIt()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-basic");
        var next = Callout(pid).Locator(".bit-dtp-dwp .bit-dtp-nbt[title='Go to next month']");
        var prev = Callout(pid).Locator(".bit-dtp-dwp .bit-dtp-nbt[title='Go to previous month']");

        await Expect(next).ToHaveAttributeAsync("aria-label", "Go to next month");

        await next.ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");
        await Expect(Page.Locator("#basic-last-month")).ToHaveTextAsync("2024-07-01");
        await Expect(Day(pid, 2024, 7, 31)).ToBeVisibleAsync();

        await prev.ClickAsync();
        await prev.ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("May 2024");
        await Expect(Page.Locator("#basic-last-month")).ToHaveTextAsync("2024-05-01");
        await Expect(Page.Locator("#basic-month-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Binding_AValueSetByThePageShowsInTheFieldAndTheCalendar()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-basic");

        await Page.Locator("#btn-basic-set").ClickAsync();
        await Expect(Input("dp-basic")).ToHaveValueAsync("7/4/2024");

        await Input("dp-basic").ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");
        await Expect(Day(pid, 2024, 7, 4)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Day(pid, 2024, 7, 4)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-basic-null").ClickAsync();
        await Expect(Input("dp-basic")).ToHaveValueAsync("");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Events_FocusInFocusAndFocusOutFollowTheField()
    {
        await OpenAsync(Route);

        await Page.Locator("#before-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("dp-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-focusin-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-focus-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dp-basic")).ToHaveClassAsync(HasClass("bit-dtp-foc"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("#basic-focusout-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dp-basic")).Not.ToHaveClassAsync(HasClass("bit-dtp-foc"));
        await Expect(Page.Locator("#basic-open-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task DefaultValue_AnUncontrolledPickerStartsOnItAndReportsChangesThroughOnChange()
    {
        await OpenAsync(Route);

        await Expect(Input("dp-default")).ToHaveValueAsync("6/3/2024");

        var pid = await OpenPickerAsync("dp-default");
        await Expect(Day(pid, 2024, 6, 3)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Day(pid, 2024, 6, 3)).ToBeFocusedAsync();

        await Day(pid, 2024, 6, 4).ClickAsync();
        await Expect(Page.Locator("#default-changed")).ToHaveTextAsync("2024-06-04 00:00");
        await Expect(Input("dp-default")).ToHaveValueAsync("6/4/2024");
    }

    [TestMethod]
    public async Task MonthPicker_PickingAMonthAYearAndARangeOfYearsMovesTheDayPicker()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-my");
        var callout = Callout(pid);
        var yearToggle = callout.Locator(".bit-dtp-mwp .bit-dtp-pkh .bit-dtp-ptb");

        await Expect(callout.Locator(".bit-dtp-mwp")).ToBeVisibleAsync();
        await Expect(yearToggle).ToHaveTextAsync("2024");
        await Expect(MonthCell(pid, 6)).ToHaveClassAsync(HasClass("bit-dtp-pcm"));
        await Expect(MonthCell(pid, 6)).ToHaveAttributeAsync("aria-selected", "true");

        await MonthCell(pid, 9).ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("September 2024");
        await Expect(Page.Locator("#my-last-month")).ToHaveTextAsync("2024-09-01");
        await Expect(MonthCell(pid, 9)).ToHaveClassAsync(HasClass("bit-dtp-psm"));
        await Expect(MonthCell(pid, 6)).Not.ToHaveClassAsync(HasClass("bit-dtp-psm"));
        await Expect(MonthCell(pid, 6)).ToHaveClassAsync(HasClass("bit-dtp-pcm"));

        // The current month is painted in the accent, the selected one in the selection's background.
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(MonthCell(pid, 6)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(MonthCell(pid, 9)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-sec)"));

        await callout.Locator(".bit-dtp-nbt[title='Go to next year 2025']").ClickAsync();
        await Expect(yearToggle).ToHaveTextAsync("2025");
        await Expect(Title(callout)).ToHaveTextAsync("September 2025");
        await callout.Locator(".bit-dtp-nbt[title='Go to previous year 2024']").ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("September 2024");

        await yearToggle.ClickAsync();
        await Expect(YearCell(pid, 2023)).ToBeVisibleAsync();
        await Expect(YearCell(pid, 2034)).ToBeVisibleAsync();
        await Expect(MonthCell(pid, 1)).ToHaveCountAsync(0);

        await YearCell(pid, 2030).ClickAsync();
        await Expect(yearToggle).ToHaveTextAsync("2030");
        await Expect(Title(callout)).ToHaveTextAsync("September 2030");
        await Expect(Page.Locator("#my-last-month")).ToHaveTextAsync("2030-09-01");

        await yearToggle.ClickAsync();
        await callout.Locator(".bit-dtp-nbt[title='Next year range 2041 - 2052']").ClickAsync();
        await Expect(YearCell(pid, 2041)).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtp-mwp .bit-dtp-ptb")).ToHaveTextAsync("2041 - 2052");
        await callout.Locator(".bit-dtp-nbt[title='Previous year range 2029 - 2040']").ClickAsync();
        await Expect(YearCell(pid, 2029)).ToBeVisibleAsync();

        // Back to today, which is then a button with nothing left to do.
        await callout.Locator(".bit-dtp-gtb").ClickAsync();
        await Expect(Title(callout)).ToHaveTextAsync("June 2024");
        await Expect(Page.Locator("#my-last-month")).ToHaveTextAsync("2024-06-01");
    }

    [TestMethod]
    public async Task MonthPicker_AfterAPointerTogglesTheYearGrid_TheFocusStaysInTheCallout()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-my");

        await Callout(pid).Locator(".bit-dtp-mwp .bit-dtp-ptb").ClickAsync();
        await Expect(YearCell(pid, 2024)).ToBeVisibleAsync();

        // The toggle that was clicked is replaced by the year grid's own header, so the focus has to be handed over
        // to the grid that took its place - the modal callout must not lose it to the body.
        await Page.WaitForFunctionAsync("sel => document.querySelector(sel)?.contains(document.activeElement)", $"#{pid}-callout");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task MonthPicker_TheMonthAndYearGridsAnswerTheKeyboard()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-my");
        var callout = Callout(pid);

        await MonthCell(pid, 6).FocusAsync();

        async Task Press(string key, ILocator expected)
        {
            await Page.Keyboard.PressAsync(key);
            await Expect(expected).ToBeFocusedAsync();
        }

        await Press("ArrowRight", MonthCell(pid, 7));
        await Press("ArrowDown", MonthCell(pid, 11));
        await Press("ArrowUp", MonthCell(pid, 7));
        await Press("ArrowLeft", MonthCell(pid, 6));
        await Press("Home", MonthCell(pid, 1));
        await Press("End", MonthCell(pid, 12));

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(callout.Locator(".bit-dtp-mwp .bit-dtp-ptb")).ToHaveTextAsync("2025");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(callout.Locator(".bit-dtp-mwp .bit-dtp-ptb")).ToHaveTextAsync("2024");

        await MonthCell(pid, 3).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Title(callout)).ToHaveTextAsync("March 2024");

        await callout.Locator(".bit-dtp-mwp .bit-dtp-ptb").ClickAsync();
        await YearCell(pid, 2024).FocusAsync();
        await Press("ArrowRight", YearCell(pid, 2025));
        await Press("ArrowDown", YearCell(pid, 2029));
        await Press("Home", YearCell(pid, 2023));
        await Press("End", YearCell(pid, 2034));
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(YearCell(pid, 2035)).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Input("dp-my")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task MonthPicker_HiddenByIsMonthPickerVisible_LeavesTheDayPickerAlone()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-nomonth");

        await Expect(Callout(pid).Locator(".bit-dtp-mwp")).ToHaveCountAsync(0);
        await Expect(Callout(pid).Locator(".bit-dtp-dvd")).ToHaveCountAsync(0);
        await Expect(Callout(pid).Locator(".bit-dtp-dwp .bit-dtp-gtb")).ToBeVisibleAsync();
        await Expect(Callout(pid).Locator(".bit-dtp-dwp .bit-dtp-ptb")).ToHaveCountAsync(0);

        // The day picker takes the go-to-today button over, which leads back from another month.
        await Callout(pid).Locator(".bit-dtp-nbt[title='Go to next month']").ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");
        await Callout(pid).Locator(".bit-dtp-gtb").ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("June 2024");
        await Expect(Callout(pid).Locator(".bit-dtp-gtb")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task MonthPicker_AsAnOverlay_OpensFromTheTitleOverTheDayPicker()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-overlay");
        var title = Callout(pid).Locator(".bit-dtp-dwp .bit-dtp-ptb");

        await Expect(Callout(pid).Locator(".bit-dtp-mwp")).ToHaveCountAsync(0);
        await Expect(title).ToHaveTextAsync("June 2024");
        await Expect(title).ToHaveAttributeAsync("title", "June 2024, change month");

        await title.ClickAsync();
        await Expect(Callout(pid).Locator(".bit-dtp-mwp")).ToBeVisibleAsync();
        await Expect(Callout(pid).Locator(".bit-dtp-dwp")).ToHaveCountAsync(0);

        await MonthCell(pid, 8).ClickAsync();
        await Expect(Callout(pid).Locator(".bit-dtp-mwp")).ToHaveCountAsync(0);
        await Expect(title).ToHaveTextAsync("August 2024");
    }

    [TestMethod]
    public async Task Disabled_TheFieldDoesNotOpenAndThePagesHandlersAreDropped()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-disabled");
        var input = Input("dp-disabled");

        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(input).ToBeDisabledAsync();
        await Expect(input).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(root.Locator(".bit-dtp-lbl")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-dis)"));

        // A real click, and the click a screen reader dispatches, both reach nothing.
        await root.Locator(".bit-dtp-wrp").ClickAsync(new() { Force = true });
        await input.DispatchEventAsync("click");
        await root.DispatchEventAsync("click");

        // A later event of the same circuit has been handled, so the clicks above have been too.
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(Page.Locator("#dp-toggle-dis")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-onclick")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_TogglingItAtRuntimeRestoresTheFieldAndThePagesHandler()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-toggle-dis");
        var pid = await PickerIdAsync("dp-toggle-dis");

        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await root.Locator(".bit-dtp-wrp").ClickAsync(new() { Force = true });

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("dp-toggle-dis")).ToBeEnabledAsync();
        await Expect(Input("dp-toggle-dis")).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#toggle-root-clicks")).ToHaveTextAsync("0");

        await Input("dp-toggle-dis").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Page.Locator("#toggle-root-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Required_MarksTheLabelAndTheInput()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-required");

        await Expect(root).ToHaveClassAsync(HasClass("bit-dtp-req"));
        await Expect(Input("dp-required")).ToHaveAttributeAsync("required", "");
        var asterisk = await root.Locator(".bit-dtp-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        Assert.AreEqual("\"*\"", asterisk);
    }

    [TestMethod]
    public async Task ReadOnly_TheCalendarBrowsesButNothingChangesTheValue()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-readonly");
        var input = Input("dp-readonly");

        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await Expect(input).ToHaveValueAsync("6/12/2024");
        await Expect(Page.Locator("#dp-readonly .bit-dtp-clr")).ToBeDisabledAsync();
        await Expect(Page.Locator("#dp-readonly .bit-dtp-clr")).ToHaveAttributeAsync("aria-disabled", "true");

        // Typed text is ignored.
        await input.EvaluateAsync("el => { el.value = '1/1/2020'; el.dispatchEvent(new Event('input', { bubbles: true })); }");

        await input.ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));
        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-disabled", "true");

        // aria-disabled makes the day unavailable to Playwright's own actionability checks, so the click is forced.
        await Day(pid, 2024, 6, 20).ClickAsync(new() { Force = true });
        await Callout(pid).Locator(".bit-dtp-nbt[title='Go to next month']").ClickAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("July 2024");

        await Expect(Page.Locator("#readonly-value")).ToHaveTextAsync("2024-06-12 00:00");
        await Expect(Callout(pid)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ClearButton_ShownWithAValue_ClearsItAndReportsOnClear()
    {
        await OpenAsync(Route);

        var clear = Page.Locator("#dp-clear .bit-dtp-clr");
        var pid = await PickerIdAsync("dp-clear");

        await Expect(Input("dp-clear")).ToHaveValueAsync("6/20/2024");
        await Expect(clear).ToHaveAttributeAsync("title", "Clear it");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Clear it");

        await clear.ClickAsync();

        await Expect(Page.Locator("#clear-value")).ToHaveTextAsync("null");
        await Expect(Input("dp-clear")).ToHaveValueAsync("");
        await Expect(Page.Locator("#clear-count")).ToHaveTextAsync("1");
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(Input("dp-clear")).ToBeFocusedAsync();
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task CloseButton_ClosesTheCalloutAndReturnsTheFocus()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-close");
        var close = Callout(pid).Locator(".bit-dtp-nbt[title='Close it']");

        await Expect(close).ToHaveAttributeAsync("aria-label", "Close it");
        await close.ClickAsync();

        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-close")).ToBeFocusedAsync();
        await Expect(Page.Locator("#close-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task AutoClose_Off_KeepsTheCalloutOpenAfterAPick()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-noautoclose");

        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#noautoclose-value")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-selected", "true");

        await Day(pid, 2024, 6, 21).ClickAsync();
        await Expect(Page.Locator("#noautoclose-value")).ToHaveTextAsync("2024-06-21 00:00");
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Callout(pid)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task AllowDeselect_PickingTheSelectedDayAgainClearsTheValue()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-deselect");

        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#deselect-value")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Page.Locator("#deselect-last")).ToHaveTextAsync("2024-06-20 00:00");

        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#deselect-value")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#deselect-last")).ToHaveTextAsync("null");
        await Expect(Day(pid, 2024, 6, 20)).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("June 2024");
    }

    [TestMethod]
    public async Task StartingValue_AnEmptyPickerOpensOnItsMonthAndTime()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-starting");

        await Expect(Title(Callout(pid))).ToHaveTextAsync("December 2020");
        await Expect(Callout(pid).Locator(".bit-dtp-tin[aria-label='Hour']")).ToHaveValueAsync("20");
        await Expect(Callout(pid).Locator(".bit-dtp-tin[aria-label='Minute']")).ToHaveValueAsync("45");
        await Expect(Input("dp-starting")).ToHaveValueAsync("");

        await Day(pid, 2020, 12, 3).ClickAsync();
        await Expect(Input("dp-starting")).ToHaveValueAsync("12/3/2020 20:45");
    }

    [TestMethod]
    public async Task MinMax_TheDaysOutsideAreDisabledAndTheNavigationStopsAtTheBounds()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-minmax");
        var callout = Callout(pid);

        await Expect(Day(pid, 2024, 6, 6)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 6)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Day(pid, 2024, 6, 7)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 17)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 18)).ToBeDisabledAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to previous month']")).ToBeDisabledAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to next month']")).ToBeDisabledAsync();
        await Expect(MonthCell(pid, 5)).ToBeDisabledAsync();
        await Expect(MonthCell(pid, 7)).ToBeDisabledAsync();
        await Expect(MonthCell(pid, 6)).ToBeEnabledAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to next year 2025']")).ToBeDisabledAsync();

        // The arrow keys stop at the bound instead of walking past it.
        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Day(pid, 2024, 6, 15)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 6, 14)).ToBeFocusedAsync();

        await Day(pid, 2024, 6, 18).ClickAsync(new() { Force = true });
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Input("dp-minmax")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task MinMax_DisablePastAndDisableFutureBoundTheCalendarAtToday()
    {
        await OpenAsync(Route);

        var past = await OpenPickerAsync("dp-past");
        await Expect(Day(past, 2024, 6, 11)).ToBeDisabledAsync();
        await Expect(Day(past, 2024, 6, 12)).ToBeEnabledAsync();
        await Expect(Day(past, 2024, 6, 13)).ToBeEnabledAsync();
        await Expect(Callout(past).Locator(".bit-dtp-nbt[title='Go to previous month']")).ToBeDisabledAsync();
        await Page.Keyboard.PressAsync("Escape");

        var future = await OpenPickerAsync("dp-future");
        await Expect(Day(future, 2024, 6, 13)).ToBeDisabledAsync();
        await Expect(Day(future, 2024, 6, 12)).ToBeEnabledAsync();
        await Expect(Day(future, 2024, 6, 11)).ToBeEnabledAsync();
        await Expect(Callout(future).Locator(".bit-dtp-nbt[title='Go to next month']")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task DisabledDays_TheRulesDisableTheirDaysAndTheKeyboardSkipsThem()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-blocked");

        await Expect(Day(pid, 2024, 6, 14)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 15)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 16)).ToBeDisabledAsync();
        await Expect(Day(pid, 2024, 6, 13)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 17)).ToBeEnabledAsync();
        await Expect(Day(pid, 2024, 6, 15)).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-dis)"));

        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 13)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 17)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#blocked-value")).ToHaveTextAsync("2024-06-17 00:00");

        var odd = await OpenPickerAsync("dp-odd");
        await Expect(Day(odd, 2024, 6, 11)).ToBeDisabledAsync();
        await Expect(Day(odd, 2024, 6, 13)).ToBeDisabledAsync();
        await Expect(Day(odd, 2024, 6, 12)).ToBeEnabledAsync();
        await Expect(Day(odd, 2024, 6, 14)).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task Highlighted_TheDaysAreMarkedNamedAndPainted()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-hl");

        await Expect(Day(pid, 2024, 6, 5)).ToHaveClassAsync(HasClass("bit-dtp-dhl"));
        await Expect(Day(pid, 2024, 6, 25)).ToHaveClassAsync(HasClass("bit-dtp-dhl"));
        await Expect(Day(pid, 2024, 6, 6)).Not.ToHaveClassAsync(HasClass("bit-dtp-dhl"));
        await Expect(Day(pid, 2024, 6, 5)).ToHaveAttributeAsync("aria-label", "Wednesday, June 5, 2024, highlighted");
        await Expect(Day(pid, 2024, 6, 6)).ToHaveAttributeAsync("aria-label", "Thursday, June 6, 2024");

        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(pid, 2024, 6, 5)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-ter)"));
        await Expect(Day(pid, 2024, 6, 6)).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Page.Keyboard.PressAsync("Escape");

        var sundays = await OpenPickerAsync("dp-dayclass");
        foreach (var day in new[] { 2, 9, 16, 23, 30 })
        {
            await Expect(Day(sundays, 2024, 6, day)).ToHaveClassAsync(HasClass("e2e-sunday"));
        }
        await Expect(Day(sundays, 2024, 6, 10)).Not.ToHaveClassAsync(HasClass("e2e-sunday"));
        await Page.Keyboard.PressAsync("Escape");

        var noToday = await OpenPickerAsync("dp-notoday");
        await Expect(Day(noToday, Today)).Not.ToHaveClassAsync(HasClass("bit-dtp-dtd"));
        await Expect(Day(noToday, Today)).ToHaveAttributeAsync("aria-current", "date");
    }

    [TestMethod]
    public async Task Layout_FirstDayOfWeekReordersTheColumns()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-monday");
        var headers = Callout(pid).Locator(".bit-dtp-dgh [role=columnheader]");

        await Expect(headers.First).ToHaveAttributeAsync("aria-label", "Monday");
        await Expect(headers.First).ToHaveTextAsync("M");
        await Expect(headers.Last).ToHaveAttributeAsync("aria-label", "Sunday");
        Assert.AreEqual($"{pid}-day-2024-05-27", await Callout(pid).Locator(".bit-dtp-dgr").First.Locator("button").First.GetAttributeAsync("id"));

        // Home and End follow the week the column order lays out.
        await Page.Keyboard.PressAsync("Home");
        await Expect(Day(pid, 2024, 6, 10)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Layout_WeekNumbersFollowTheRuleAndTheFirstDay()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-weeks");
        var numbers = Callout(pid).Locator(".bit-dtp-wnm");

        await Expect(numbers).ToHaveCountAsync(6);
        var first = WeekOfYear(new DateTime(2024, 5, 26), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);
        await Expect(numbers.First).ToHaveTextAsync(first.ToString(CultureInfo.InvariantCulture));
        await Expect(numbers.First).ToHaveAttributeAsync("title", $"Week number {first}");
        await Expect(numbers.First).ToHaveAttributeAsync("role", "rowheader");
        await Expect(Callout(pid).Locator(".bit-dtp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Week");
        await Page.Keyboard.PressAsync("Escape");

        var iso = await OpenPickerAsync("dp-iso");
        var row = Callout(iso).Locator(".bit-dtp-dgr", new() { Has = Day(iso, Today) });
        var expected = WeekOfYear(Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        Assert.AreEqual(24, expected);
        await Expect(row.Locator(".bit-dtp-wnm")).ToHaveTextAsync("24");
    }

    [TestMethod]
    public async Task Layout_ShowOutsideDaysOffLeavesEmptyCells()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-noout");

        await Expect(Day(pid, 2024, 5, 26)).ToHaveCountAsync(0);
        await Expect(Day(pid, 2024, 7, 1)).ToHaveCountAsync(0);
        await Expect(Callout(pid).Locator(".bit-dtp-dbe")).ToHaveCountAsync(12);
        await Expect(Day(pid, 2024, 6, 1)).ToBeVisibleAsync();

        var basic = await PickerIdAsync("dp-basic");
        await Expect(Day(basic, 2024, 5, 26)).ToHaveClassAsync(HasClass("bit-dtp-dbo"));
    }

    [TestMethod]
    public async Task Layout_FixedWeeksAlwaysDrawsSixRows()
    {
        await OpenAsync(Route);

        var fixedId = await OpenPickerAsync("dp-fixed");
        await Expect(Title(Callout(fixedId))).ToHaveTextAsync("February 2024");
        await Expect(Callout(fixedId).Locator(".bit-dtp-dgr")).ToHaveCountAsync(6);
        await Expect(Day(fixedId, 2024, 3, 9)).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");

        var freeId = await OpenPickerAsync("dp-notfixed");
        await Expect(Callout(freeId).Locator(".bit-dtp-dgr")).ToHaveCountAsync(5);
        await Expect(Day(freeId, 2024, 3, 9)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Layout_MonthCountDrawsMonthsSideBySideAndTheKeysFlowAcrossThem()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-two");
        var panes = Callout(pid).Locator(".bit-dtp-dwp");

        await Expect(panes).ToHaveCountAsync(2);
        await Expect(panes.Nth(0).Locator(".bit-dtp-pkt")).ToHaveTextAsync("June 2024");
        await Expect(panes.Nth(1).Locator(".bit-dtp-pkt")).ToHaveTextAsync("July 2024");
        await Expect(panes.Nth(0).Locator(".bit-dtp-nbt[title='Go to previous month']")).ToHaveCountAsync(1);
        await Expect(panes.Nth(0).Locator(".bit-dtp-nbt[title='Go to next month']")).ToHaveCountAsync(0);
        await Expect(panes.Nth(1).Locator(".bit-dtp-nbt[title='Go to next month']")).ToHaveCountAsync(1);

        var first = await BoxAsync(panes.Nth(0));
        var second = await BoxAsync(panes.Nth(1));
        Assert.IsTrue(second.X >= first.X + first.Width - 1, "The second month is laid out beside the first.");
        Assert.AreEqual(first.Y, second.Y, 4);

        await PressAsync("End", Day(pid, 2024, 6, 15));
        await PressAsync("ArrowDown", Day(pid, 2024, 6, 22));
        await PressAsync("ArrowDown", Day(pid, 2024, 6, 29));
        await PressAsync("ArrowRight", Day(pid, 2024, 6, 30));
        await PressAsync("ArrowRight", Day(pid, 2024, 7, 1));
        await Expect(panes.Nth(0).Locator(".bit-dtp-pkt")).ToHaveTextAsync("June 2024");

        await panes.Nth(1).Locator(".bit-dtp-nbt[title='Go to next month']").ClickAsync();
        await Expect(panes.Nth(0).Locator(".bit-dtp-pkt")).ToHaveTextAsync("July 2024");
        await Expect(panes.Nth(1).Locator(".bit-dtp-pkt")).ToHaveTextAsync("August 2024");
    }

    [TestMethod]
    public async Task Layout_PagedNavigationMovesAWholeStripOfMonths()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-three");
        var panes = Callout(pid).Locator(".bit-dtp-dwp");

        await Expect(panes).ToHaveCountAsync(3);
        await Expect(panes.Nth(0).Locator(".bit-dtp-ptb")).ToHaveTextAsync("June 2024");
        await Expect(panes.Nth(0).Locator(".bit-dtp-ptb")).ToHaveAttributeAsync("title", "Choose another month than June 2024");
        await Expect(panes.Nth(2).Locator(".bit-dtp-pkt")).ToHaveTextAsync("August 2024");

        await panes.Nth(2).Locator(".bit-dtp-nbt[title='Go to next month']").ClickAsync();
        await Expect(panes.Nth(0).Locator(".bit-dtp-ptb")).ToHaveTextAsync("September 2024");
        await Expect(panes.Nth(1).Locator(".bit-dtp-pkt")).ToHaveTextAsync("October 2024");
        await Expect(panes.Nth(2).Locator(".bit-dtp-pkt")).ToHaveTextAsync("November 2024");

        await panes.Nth(0).Locator(".bit-dtp-nbt[title='Go to previous month']").ClickAsync();
        await Expect(panes.Nth(0).Locator(".bit-dtp-ptb")).ToHaveTextAsync("June 2024");
    }

    [TestMethod]
    public async Task Mode_MonthPickerSelectsTheFirstDayOfTheMonth()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-monthmode");

        await Expect(Callout(pid).Locator(".bit-dtp-dwp")).ToHaveCountAsync(0);
        await Expect(MonthCell(pid, 6)).ToBeFocusedAsync();

        await MonthCell(pid, 8).ClickAsync();

        await Expect(Page.Locator("#monthmode-value")).ToHaveTextAsync("2024-08-01 00:00");
        await Expect(Input("dp-monthmode")).ToHaveValueAsync("August 2024");
        await Expect(Callout(pid)).ToBeHiddenAsync();

        await Input("dp-monthmode").ClickAsync();
        await Expect(MonthCell(pid, 8)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(MonthCell(pid, 8)).ToHaveClassAsync(HasClass("bit-dtp-psm"));
    }

    [TestMethod]
    public async Task Mode_YearPickerSelectsTheFirstDayOfTheYearWithinItsBounds()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-yearmode");
        var header = Callout(pid).Locator(".bit-dtp-mwp .bit-dtp-pkt");

        await Expect(header).ToHaveTextAsync("2023 - 2034");
        await Expect(YearCell(pid, 2024)).ToBeFocusedAsync();
        await Expect(YearCell(pid, 2025)).ToBeDisabledAsync();
        await Expect(YearCell(pid, 2024)).ToBeEnabledAsync();
        await Expect(MonthCell(pid, 1)).ToHaveCountAsync(0);

        await Callout(pid).Locator(".bit-dtp-nbt[title='Previous year range 2011 - 2022']").ClickAsync();
        await Expect(header).ToHaveTextAsync("2011 - 2022");
        await YearCell(pid, 2020).ClickAsync();

        await Expect(Page.Locator("#yearmode-value")).ToHaveTextAsync("2020-01-01 00:00");
        await Expect(Input("dp-yearmode")).ToHaveValueAsync("2020");
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Mode_AStandaloneMonthPickerSelectsInPlace()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-monthmode-sa");

        await Expect(Callout(pid)).ToBeVisibleAsync();
        await MonthCell(pid, 3).ClickAsync();
        await Expect(Page.Locator("#monthmode-sa-value")).ToHaveTextAsync("2024-03-01 00:00");
        await Expect(MonthCell(pid, 3)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Callout(pid)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Standalone_TheCalendarIsInlineAndPicksWithoutACallout()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-sa");
        var calendar = Callout(pid);

        await Expect(calendar).ToBeVisibleAsync();
        await Expect(calendar).ToHaveClassAsync(HasClass("bit-dtp-sta"));
        await Expect(Page.Locator($"#{pid}-dialog")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.Locator("#dp-sa .bit-dtp-wrp")).ToHaveCountAsync(0);
        await Expect(Overlay(pid)).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dp-sa input.bit-input-hidden")).ToHaveAttributeAsync("tabindex", "-1");
        Assert.AreEqual("relative", await StyleOfAsync(calendar, "position"));

        var label = await BoxAsync(Page.Locator("#dp-sa .bit-dtp-lbl"));
        var box = await BoxAsync(calendar);
        Assert.IsTrue(box.Y >= label.Y + label.Height - 1, "The calendar follows its label in the flow of the page.");

        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#sa-value")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Page.Locator("#dp-sa input.bit-input-hidden")).ToHaveValueAsync("6/20/2024");
        await Expect(calendar).ToBeVisibleAsync();

        var disabled = await PickerIdAsync("dp-sa-dis");
        await Expect(Page.Locator("#dp-sa-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Day(disabled, 2024, 6, 20)).ToBeDisabledAsync();
        await Expect(Callout(disabled).Locator(".bit-dtp-nbt[title='Go to next month']")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Templates_EveryPartIsRenderedFromItsTemplate()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-tpl");

        await Expect(Page.Locator("#dp-tpl label #tpl-label")).ToHaveTextAsync("Custom label");
        await Expect(Page.Locator("#dp-tpl label")).ToHaveAttributeAsync("for", $"{pid}-input");
        await Expect(Page.Locator("#dp-tpl .bit-dtp-icn #tpl-icon")).ToBeVisibleAsync();
        await Expect(Page.Locator("#dp-tpl .bit-dtp-ico")).ToHaveCountAsync(0);

        await Input("dp-tpl").ClickAsync();
        await Expect(Callout(pid).Locator("#tpl-header")).ToBeVisibleAsync();
        await Expect(Callout(pid).Locator("#tpl-footer")).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));

        var header = await BoxAsync(Callout(pid).Locator("#tpl-header"));
        var footer = await BoxAsync(Callout(pid).Locator("#tpl-footer"));
        var grid = await BoxAsync(Callout(pid).Locator(".bit-dtp-grp"));
        Assert.IsTrue(header.Y + header.Height <= grid.Y + 1, "The header sits above the calendar.");
        Assert.IsTrue(footer.Y >= grid.Y + grid.Height - 1, "The footer sits below the calendar.");

        await Expect(Day(pid, 2024, 6, 20).Locator(".e2e-day-cell[data-day='20']")).ToHaveTextAsync("d20");
        await Expect(MonthCell(pid, 6).Locator(".e2e-month-cell")).ToHaveTextAsync("m6");
        await Callout(pid).Locator(".bit-dtp-mwp .bit-dtp-ptb").ClickAsync();
        await Expect(YearCell(pid, 2024).Locator(".e2e-year-cell")).ToHaveTextAsync("y2024");
    }

    [TestMethod]
    public async Task Templates_AButtonOfTheFooterSetsTheValueAndClosesThroughTheReference()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-tpl");

        await Input("dp-tpl").ClickAsync();
        await Callout(pid).Locator("#tpl-tomorrow").ClickAsync();

        await Expect(Page.Locator("#tpl-value")).ToHaveTextAsync("2024-06-13 00:00");
        await Expect(Input("dp-tpl")).ToHaveValueAsync("6/13/2024");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-tpl")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Validation_InAnEditForm_TheRequiredDateFailsUntilADayIsPicked()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-valid");
        var pid = await PickerIdAsync("dp-valid");

        await Page.Locator("#valid-submit").ClickAsync();

        await Expect(Page.Locator("#valid-result")).ToHaveTextAsync("invalid");
        await Expect(Page.Locator("#valid-message")).ToHaveTextAsync("The Date field is required.");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("dp-valid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(root.Locator(".bit-dtp-icn")).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Input("dp-valid").ClickAsync();
        await Day(pid, 2024, 6, 20).ClickAsync();

        await Expect(Page.Locator("#valid-message")).ToHaveTextAsync("");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("dp-valid")).Not.ToHaveAttributeAsync("aria-invalid", "true");

        await Page.Locator("#valid-submit").ClickAsync();
        await Expect(Page.Locator("#valid-result")).ToHaveTextAsync("2024-06-20 00:00");
    }

    [TestMethod]
    public async Task Validation_TheAppsErrorMessageMarksTheFieldAndIsAnnounced()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-errmsg");
        var pid = await PickerIdAsync("dp-errmsg");

        await Expect(root.Locator(".bit-dtp-erm")).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("");

        await Page.Locator("#btn-set-error").ClickAsync();

        await Expect(root.Locator(".bit-dtp-erm")).ToHaveTextAsync("This day is taken.");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("dp-errmsg")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("dp-errmsg")).ToHaveAttributeAsync("aria-describedby", $"{pid}-error");
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("This day is taken.");
        await Expect(root.Locator("[role=status]")).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(root.Locator(".bit-dtp-erm")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Page.Locator("#btn-clear-error").ClickAsync();
        await Expect(root.Locator(".bit-dtp-erm")).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("dp-errmsg")).Not.ToHaveAttributeAsync("aria-describedby", $"{pid}-error");
    }

    [TestMethod]
    public async Task Validation_InvalidDescriptionAndAriaDescriptionDescribeTheInput()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-invalid");
        var pid = await PickerIdAsync("dp-invalid");

        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("dp-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("dp-invalid")).ToHaveAttributeAsync("aria-describedby", $"{pid}-description {pid}-aria-description");
        await Expect(root.Locator(".bit-dtp-des")).ToHaveTextAsync("Marked invalid by the app.");
        await Expect(root.Locator(".bit-dtp-des")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#{pid}-aria-description")).ToHaveTextAsync("Dates are stored in UTC.");
        Assert.AreEqual("0px", await StyleOfAsync(Page.Locator($"#{pid}-aria-description"), "height"));

        var templated = Page.Locator("#dp-msg-tpl");
        await Expect(templated.Locator("#desc-tpl")).ToBeVisibleAsync();
        await Expect(templated.Locator("#error-tpl")).ToBeVisibleAsync();
        await Expect(templated).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(templated.Locator("[role=status]")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Programmatic_OpenCalloutAndCloseCalloutAndFocusDriveTheTwoWayBoundIsOpen()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-prog");

        await Page.Locator("#btn-open-method").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#prog-opens")).ToHaveTextAsync("1");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        // The overlay covers the page while open, so the page's button is pressed the way a script would.
        await Page.Locator("#btn-close-method").DispatchEventAsync("click");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#prog-closes")).ToHaveTextAsync("1");
        await Expect(Input("dp-prog")).ToBeFocusedAsync();

        await Page.Locator("#btn-open-param").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Input("dp-prog")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#prog-opens")).ToHaveTextAsync("2");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#prog-isopen")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#prog-closes")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Programmatic_AOneWayBoundIsOpenLeavesTheClosingToThePage()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("dp-oneway");

        await Page.Locator("#btn-oneway-open").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();

        await Page.Mouse.ClickAsync(1200, 650);
        await Page.Keyboard.PressAsync("Escape");
        await Day(pid, 2024, 6, 20).ClickAsync();

        await Expect(Page.Locator("#oneway-value")).ToHaveTextAsync("2024-06-20 00:00");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Input("dp-oneway")).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Locator("#btn-oneway-close").DispatchEventAsync("click");
        await Expect(Callout(pid)).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Accessibility_TheRolesAndEveryTitleAreWhatThePageSet()
    {
        await OpenAsync(Route);

        var input = Input("dp-a11y");
        await Expect(input).ToHaveAttributeAsync("role", "combobox");
        await Expect(input).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(input).ToHaveAttributeAsync("aria-label", "Stay date");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");

        var pid = await OpenPickerAsync("dp-a11y");
        var callout = Callout(pid);

        await Expect(Page.Locator($"#{pid}-dialog")).ToHaveAttributeAsync("aria-label", "Stay calendar");
        await Expect(callout.Locator(".bit-dtp-dwp [role=grid]")).ToHaveAttributeAsync("aria-label", "June 2024");
        await Expect(callout.Locator(".bit-dtp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Wk");
        await Expect(callout.Locator(".bit-dtp-dgh [role=columnheader]").Nth(1)).ToHaveAttributeAsync("aria-label", "Sunday");
        var week = WeekOfYear(new DateTime(2024, 5, 26), CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);
        await Expect(callout.Locator(".bit-dtp-wnm").First).ToHaveAttributeAsync("aria-label", $"Wk {week}");
        await Expect(Day(pid, 2024, 6, 5)).ToHaveAttributeAsync("aria-label", "Wednesday, June 5, 2024 (booked)");
        await Expect(Day(pid, 2024, 6, 6)).ToHaveAttributeAsync("role", "gridcell");
        await Expect(callout.Locator(".bit-dtp-nbt[title='Earlier month']")).ToHaveAttributeAsync("aria-label", "Earlier month");
        await Expect(callout.Locator(".bit-dtp-nbt[title='Later month']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Done']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtp-gtb[title='Back to today']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Year 2023 back']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Year 2025 ahead']")).ToBeVisibleAsync();

        var yearToggle = callout.Locator(".bit-dtp-mwp .bit-dtp-ptb");
        await Expect(yearToggle).ToHaveAttributeAsync("title", "2024, pick a year");
        await yearToggle.ClickAsync();
        await Expect(callout.Locator(".bit-dtp-mwp .bit-dtp-ptb")).ToHaveAttributeAsync("title", "2023 to 2034, pick a month");
        await Expect(callout.Locator(".bit-dtp-nbt[title='Years 2011 to 2022 back']")).ToBeVisibleAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Years 2035 to 2046 ahead']")).ToBeVisibleAsync();

        await Day(pid, 2024, 6, 20).ClickAsync();
        await input.ClickAsync();
        await Expect(callout.Locator(".bit-dtp-grp > .bit-dtp-sdt")).ToHaveTextAsync("Picked 6/20/2024");
    }

    [TestMethod]
    public async Task Base_IdClassStyleAttributesAndNamesReachTheirElements()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-base");
        var input = Input("dp-base");

        await Expect(root).ToHaveClassAsync(HasClass("e2e-root-class"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-dtp"));
        Assert.AreEqual("7px", await StyleOfAsync(root, "margin-top"));
        await Expect(root).ToHaveAttributeAsync("data-e2e", "root-attr");
        await Expect(root).ToHaveAttributeAsync("title", "Root title");
        await Expect(input).ToHaveAttributeAsync("aria-label", "Base date");
        await Expect(input).ToHaveAttributeAsync("name", "base-date");
        await Expect(input).ToHaveAttributeAsync("tabindex", "0");
        await Expect(input).ToHaveAttributeAsync("data-e2e-input", "input-attr");

        var pid = await OpenPickerAsync("dp-base");
        await Expect(Page.Locator($"#{pid}-dialog")).ToHaveAttributeAsync("data-e2e-callout", "callout-attr");
    }

    [TestMethod]
    public async Task Base_VisibilityHidesAndCollapsesTheRoot()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-base");

        await Page.Locator("#btn-hidden").ClickAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        await Expect(root).ToBeHiddenAsync();

        await Page.Locator("#btn-collapsed").ClickAsync();
        await Expect(root).ToHaveCSSAsync("display", "none");

        await Page.Locator("#btn-visible").ClickAsync();
        await Expect(root).ToBeVisibleAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task Lifecycle_AParameterChangedByThePageReRendersTheCalendar()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-life");
        await Expect(Callout(pid).Locator(".bit-dtp-wnm")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-toggle-weeks").ClickAsync();
        await Input("dp-life").ClickAsync();
        await Expect(Callout(pid).Locator(".bit-dtp-wnm")).ToHaveCountAsync(6);
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAnOpenPickerTakesItsCalloutAndOverlayAway()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-life");
        await Expect(Overlay(pid)).ToBeVisibleAsync();

        await Page.Locator("#btn-unmount").DispatchEventAsync("click");

        await Expect(Page.Locator("#life-mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dp-life")).ToHaveCountAsync(0);
        await Expect(Callout(pid)).ToHaveCountAsync(0);
        await Expect(Overlay(pid)).ToHaveCountAsync(0);

        // Nothing is left over the page: a real click reaches the page again.
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#life-mounted")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#dp-life")).ToBeVisibleAsync();

        await AssertNoServerErrorsAsync();
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Lifecycle_AutoFocusFocusesTheFieldWhenItIsRendered()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-mount-autofocus").ClickAsync();

        await Expect(Input("dp-autofocus")).ToBeFocusedAsync();
        await Expect(Input("dp-autofocus")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Lifecycle_APickerRenderedOpenShowsItsCalloutRightAway()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-mount-open").ClickAsync();

        var pid = await PickerIdAsync("dp-initopen");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Input("dp-initopen")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#initopen-opens")).ToHaveTextAsync("1");
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#initopen-isopen")).ToHaveTextAsync("False");
    }
}
