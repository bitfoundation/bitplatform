namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The time picker of BitDateRangePicker in a real browser (/e2e/date-range-picker/time): the start and the end time
/// set by the spin buttons (by pointer, by keyboard and held down), by typing and by PageUp/PageDown, and carried into
/// the range and its text; the 12-hour clock with its AM/PM toggles; HourStep and MinuteStep; the overlay with its
/// show and hide buttons; StartingValue; every title and accessible name; the disabled and the read-only time pickers.
/// </summary>
[TestClass]
public class BitDateRangePickerTimeE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-range-picker/time";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtrp-inp");

    private async Task<string> OpenPickerAsync(string rootId)
    {
        await NoCalloutOpenAsync();

        var pid = await PickerIdAsync(rootId);

        await Input(rootId).ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));

        return pid;
    }

    private ILocator Field(string pid, string label) => Callout(pid).Locator($".bit-dtrp-tin[aria-label='{label}']");

    private ILocator Spin(string pid, string title) => Callout(pid).Locator($".bit-dtrp-tbt[title='{title}']");

    [TestMethod]
    public async Task Time_TheStartAndEndTimesGoIntoTheRange()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-time");

        await Expect(Callout(pid).Locator(".bit-dtrp-twp")).ToBeVisibleAsync();
        await Expect(Callout(pid).Locator(".bit-dtrp-mwp")).ToHaveCountAsync(0);
        await Expect(Callout(pid).Locator(".bit-dtrp-dwp .bit-dtrp-ptb")).ToHaveTextAsync("June 2024");
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("0");
        await Expect(Field(pid, "Start minute")).ToHaveValueAsync("0");
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("23");
        await Expect(Field(pid, "End minute")).ToHaveValueAsync("59");

        await Spin(pid, "Increase start hour").ClickAsync();
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("1");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 14).ClickAsync();
        await Expect(Page.Locator("#time-value")).ToHaveTextAsync("2024-06-10 01:00 / 2024-06-14 23:59");

        await Input("drp-time").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Spin(pid, "Decrease end minute").ClickAsync();
        await Expect(Page.Locator("#time-value")).ToHaveTextAsync("2024-06-10 01:00 / 2024-06-14 23:58");

        await Field(pid, "End hour").FillAsync("18");
        await Expect(Page.Locator("#time-value")).ToHaveTextAsync("2024-06-10 01:00 / 2024-06-14 18:58");

        await Field(pid, "Start minute").FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Page.Locator("#time-value")).ToHaveTextAsync("2024-06-10 01:01 / 2024-06-14 18:58");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Page.Locator("#time-value")).ToHaveTextAsync("2024-06-10 01:00 / 2024-06-14 18:58");
        await Expect(Input("drp-time")).ToHaveValueAsync("Start: 6/10/2024 1:00 - End: 6/14/2024 18:58");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-time")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Time_TheSpinButtonsAnswerEnterAndSpace()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-time");

        await Spin(pid, "Increase start hour").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("1");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("2");

        await Spin(pid, "Decrease end minute").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Field(pid, "End minute")).ToHaveValueAsync("58");
    }

    [TestMethod]
    public async Task Time_TwelveHourClockShowsAndSwitchesTheHalvesOfTheDay()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-12h");
        var startAm = Callout(pid).Locator(".bit-dtrp-sic .bit-dtrp-bam");
        var startPm = Callout(pid).Locator(".bit-dtrp-sic .bit-dtrp-bpm");
        var endAm = Callout(pid).Locator(".bit-dtrp-eic .bit-dtrp-bam");
        var endPm = Callout(pid).Locator(".bit-dtrp-eic .bit-dtrp-bpm");

        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("12");
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("11");
        await Expect(startAm).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(startAm).ToBeDisabledAsync();
        await Expect(startPm).ToBeEnabledAsync();
        await Expect(endPm).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(endPm).ToHaveClassAsync(HasClass("bit-dtrp-bns"));

        await startPm.ClickAsync();
        await Expect(startPm).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(startAm).ToBeEnabledAsync();
        await Expect(startPm).ToBeDisabledAsync();

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Expect(Page.Locator("#twelve-value")).ToHaveTextAsync("2024-06-10 12:00 / -");
        await Day(pid, 2024, 6, 14).ClickAsync();
        await Expect(Page.Locator("#twelve-value")).ToHaveTextAsync("2024-06-10 12:00 / 2024-06-14 23:59");

        await Input("drp-12h").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await endAm.ClickAsync();
        await Expect(endAm).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(endPm).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#twelve-value")).ToHaveTextAsync("2024-06-10 12:00 / 2024-06-14 11:59");
        await Expect(Input("drp-12h")).ToHaveValueAsync("Start: 6/10/2024 12:00 PM - End: 6/14/2024 11:59 AM");
    }

    [TestMethod]
    public async Task Time_AnEndTimeSetBeforeTheEndDayIsPickedIsKept()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-time");

        await Spin(pid, "Decrease end hour").ClickAsync();
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("22");

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("22");

        await Spin(pid, "Decrease end hour").ClickAsync();
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("21");

        await Day(pid, 2024, 6, 14).ClickAsync();
        await Expect(Page.Locator("#time-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 21:59");
    }

    [TestMethod]
    public async Task Time_HourStepLaysAGridOverTheHours()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-hourstep");

        await Spin(pid, "Increase start hour").ClickAsync();
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("2");
        await Spin(pid, "Increase start hour").ClickAsync();
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("4");
        await Spin(pid, "Decrease end hour").ClickAsync();
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("22");
    }

    [TestMethod]
    public async Task Time_MinuteStepAndHoldingASpinButtonUntilItIsReleased()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-minstep");

        // The step is taken from the keyboard here: a pointer press held past this picker's short 250ms delay - which
        // a busy machine can stretch a plain click into - would start the press-and-hold spin.
        await Field(pid, "Start minute").FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Field(pid, "Start minute")).ToHaveValueAsync("15");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Field(pid, "Start minute")).ToHaveValueAsync("0");

        var decrease = await BoxAsync(Spin(pid, "Decrease end hour"));
        await Page.Mouse.MoveAsync(decrease.X + (decrease.Width / 2), decrease.Y + (decrease.Height / 2));
        await Page.Mouse.DownAsync();

        var hour = Field(pid, "End hour");
        await Page.WaitForFunctionAsync("el => +el.value <= 19", await hour.ElementHandleAsync());
        await Page.Mouse.UpAsync();

        // A tick already on its way at the release may still land, so the value is read once it has had the time to;
        // then only the absence of a change is waited for - a spin going on after the release would move the hour
        // within this time at its 40ms interval.
        await Page.WaitForTimeoutAsync(300);
        var released = await hour.InputValueAsync();
        await Page.WaitForTimeoutAsync(500);

        Assert.AreEqual(released, await hour.InputValueAsync(), "The spin stopped when the button was released.");
    }

    [TestMethod]
    public async Task Time_AsAnOverlay_TheClockButtonShowsAndHidesTheTimes()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-time-overlay");
        var show = Callout(pid).Locator(".bit-dtrp-nbt[title='Pick the times']");

        await Expect(Callout(pid).Locator(".bit-dtrp-twp")).ToHaveCountAsync(0);
        await Expect(show).ToHaveAttributeAsync("aria-label", "Pick the times");

        await show.ClickAsync();
        await Expect(Callout(pid).Locator(".bit-dtrp-twp")).ToBeVisibleAsync();

        await Callout(pid).Locator(".bit-dtrp-nbt[title='Back to the days']").ClickAsync();
        await Expect(Callout(pid).Locator(".bit-dtrp-twp")).ToHaveCountAsync(0);
        await Expect(Callout(pid).Locator(".bit-dtrp-dwp")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Time_StartingValueOpensOnItsMonthAndTimes()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-starting");

        await Expect(Callout(pid).Locator(".bit-dtrp-dwp .bit-dtrp-ptb")).ToHaveTextAsync("December 2020");
        await Expect(Field(pid, "Start hour")).ToHaveValueAsync("10");
        await Expect(Field(pid, "Start minute")).ToHaveValueAsync("12");
        await Expect(Field(pid, "End hour")).ToHaveValueAsync("16");
        await Expect(Field(pid, "End minute")).ToHaveValueAsync("59");
        await Expect(Input("drp-starting")).ToHaveValueAsync("");

        await Day(pid, 2020, 12, 3).ClickAsync();
        await Day(pid, 2020, 12, 5).ClickAsync();
        await Expect(Page.Locator("#starting-value")).ToHaveTextAsync("2020-12-03 10:12 / 2020-12-05 16:59");
    }

    [TestMethod]
    public async Task Time_EveryTitleAndAccessibleNameIsThePagesOwn()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("drp-time-titles");

        foreach (var label in new[] { "Check-in hour", "Check-in minute", "Check-out hour", "Check-out minute" })
        {
            await Expect(Field(pid, label)).ToHaveCountAsync(1);
        }

        foreach (var title in new[]
                 {
                     "Check-in hour up", "Check-in hour down", "Check-in minute up", "Check-in minute down",
                     "Check-out hour up", "Check-out hour down", "Check-out minute up", "Check-out minute down"
                 })
        {
            await Expect(Spin(pid, title)).ToHaveAttributeAsync("aria-label", title);
        }
    }

    [TestMethod]
    public async Task Time_DisabledAndReadOnlyTimePickersChangeNothing()
    {
        await OpenAsync(Route);

        var disabled = await PickerIdAsync("drp-time-dis");
        await Expect(Field(disabled, "Start hour")).ToBeDisabledAsync();
        await Expect(Field(disabled, "Start hour")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Spin(disabled, "Increase start hour")).ToBeDisabledAsync();

        var readOnly = await PickerIdAsync("drp-time-ro");
        await Expect(Field(readOnly, "Start hour")).ToHaveAttributeAsync("readonly", "");
        await Expect(Field(readOnly, "Start hour")).ToHaveValueAsync("8");

        await Spin(readOnly, "Increase start hour").ClickAsync();
        await Field(readOnly, "End minute").FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Day(readOnly, 2024, 6, 20).ClickAsync(new() { Force = true });

        // The read-only calendar still browses: once the month it moves to shows, the presses above have been handled.
        await Callout(readOnly).Locator(".bit-dtrp-nbt[title='Go to next month']").ClickAsync();
        await Expect(Callout(readOnly).Locator(".bit-dtrp-dwp .bit-dtrp-ptb, .bit-dtrp-dwp .bit-dtrp-pkt").First).ToHaveTextAsync("July 2024");
        await Expect(Field(readOnly, "Start hour")).ToHaveValueAsync("8");
        await Expect(Page.Locator("#ro-value")).ToHaveTextAsync("2024-06-10 08:30 / 2024-06-14 17:45");
    }
}
